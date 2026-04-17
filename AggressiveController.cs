using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using System.Collections.ObjectModel;
using System.Linq;

namespace Yoto
{
    public class AggressiveToken
    {
        public readonly IAggressivePlayer Target;
        public int Cost;
        public AggressiveToken(IAggressivePlayer target, int cost)
        {
            Target = target;
            Cost = cost;
        }
    }
    public class IntentionToken
    {
        public readonly IAggressivePlayer Target;
        public float Intention;
        public IntentionState State;
        public readonly GlobalCancellationTokenSource TokenSource;

        public IntentionToken(IAggressivePlayer target)
        {
            Target = target;
            Intention = 0;
            State = IntentionState.Near;
            TokenSource = new();
        }
        public enum IntentionState
        {
            Near,
            Far
        }
    }
    public enum IntentionAction
    {
        Attack,
        Dash,
        Counter,
        Near,
        Far
    }
    [CreateAssetMenu(fileName = "AggressiveController", menuName = "7Quark/System/AggressiveController")]
    public class AggressiveController : ScriptableObjectSystem
    {
        [Serializable]
        public class PriorityWeightDic : UnitySerializedDictionary<Priority, float> { }
        [Serializable]
        public enum Priority
        {
            Aggresive,
            Distance,
            HP,
        }

        [SerializeField] private float _recalculatePeriod = 1.0f;
        [SerializeField] private float _remainAggressiveTime = 3.0f; //Prevent token flicker
        [SerializeField, LabelText("Priority Weight")] private PriorityWeightDic _priorityWeightDic = new();

        private readonly List<IAggressivePlayer> _players = new();
        private readonly List<IAggressiveNpc> _npcs = new();
        private readonly Dictionary<IAggressivePlayer, List<AggressiveToken>> _tokenDic = new();
        private GlobalCancellationTokenSource _tokenSource;

        public IReadOnlyCollection<IAggressiveNpc> GetNPCList => _npcs.AsReadOnly();

        #region SOS Cycle
        public override UniTask SystemInitialize(CancellationToken token)
        {
            _tokenSource = new GlobalCancellationTokenSource();
            return UniTask.CompletedTask;
        }
        public override void SystemEnable()
        {
            _tokenSource.New();
            ResetWeightContainer();
            RecalculateSequence(TimeSpan.FromSeconds(_recalculatePeriod), _tokenSource.Token).Forget();
        }
        public override void SystemDisable()
        {
            _tokenSource.End();
            _players.Clear();
            _npcs.Clear();
            _priorityWeightDic.Clear();
            _tokenDic.Clear();
            foreach (var pair in _intentionDic) { pair.Value.TokenSource.End(); }
            _intentionDic.Clear();
            _calculatedTarget = null;
        }
        #endregion

        #region NPC Targeting
        public IAggressivePlayer GetCloestPlayer(Vector3 position)
        {
            IAggressivePlayer target = null;
            var minSqrLen = float.PositiveInfinity;
            foreach (var player in _players)
            {
                var sqr = Vector3.SqrMagnitude((player as MonoBehaviour)!.transform.position - position);
                if (sqr < minSqrLen)
                {
                    target = player;
                    minSqrLen = sqr;
                }
            }
            return target;
        }
        public IAggressivePlayer GetRandomPlayer()
        {
            if (_players.Count == 0) { return null; }
            else { return _players[UnityEngine.Random.Range(0, _players.Count)]; }
        }
        public ReadOnlyCollection<IAggressivePlayer> GetAllPlayer()
        {
            return _players.AsReadOnly();
        }
        #endregion

        #region Aggressive System
        private (int npc, float weight, int target)[] _calculatedTarget;
        private int[] _playerCost;    // x = remain cost, y = phantom cost
        private readonly List<IAggressiveNpc> _downgradeList = new(); //Aggressive -> Aggro
        private readonly List<IAggressiveNpc> _remainList = new(); //Aggressive -> Aggressive
        private readonly List<IAggressiveNpc> _eliminateBufferList = new(); //Countdown to downgrade
        private readonly Dictionary<IAggressiveNpc, float> _eliminateTimeDic = new(); //Countdown start record
        private void ResetWeightContainer(bool playerUpdate = false)
        {
            _calculatedTarget = new (int, float, int)[_npcs.Count];
            if (playerUpdate) { _playerCost = new int[_players.Count]; }
        }
        private void WeightCalculate()
        {
            for (var i = 0; i < _npcs.Count; i++)
            {
                _calculatedTarget[i] = (i, 0, 0);
                for (var player = 0; player < _players.Count; player++)
                {
                    var score = 0f;
                    foreach (var key in _priorityWeightDic.Keys)
                    {
                        score += Scoring(key, _players[player], _npcs[i]);
                    }
                    if (score > _calculatedTarget[i].weight)
                    {
                        _calculatedTarget[i].weight = score;
                        _calculatedTarget[i].target = player;
                    }
                }
            }
        }
        private float Scoring(Priority priority, IAggressivePlayer player, IAggressiveNpc npc)
        {
            if ((player as MonoBehaviour) == null || (npc as MonoBehaviour) == null) { return 0; }
            switch (priority)
            {
                case Priority.Aggresive:
                    return npc.AggressiveInfo.AggressiveData.Aggressive * _priorityWeightDic[Priority.Aggresive];
                case Priority.Distance:
                    var distance = Vector3.Distance(((MonoBehaviour)player).transform.position, ((MonoBehaviour)npc).transform.position);
                    return -distance * _priorityWeightDic[Priority.Distance];
                case Priority.HP:
                    var status = npc as NPCStatus;
                    if (status != null)
                    {
                        return (1f - (float)status.HP / status.MaxHP) * 100f * _priorityWeightDic[Priority.HP];
                    }
                    break;
            }
            return 0;
        }
        private async UniTaskVoid RecalculateSequence(TimeSpan recalculatePeriod, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                Recalculate();
                await UniTask.Delay(recalculatePeriod, cancellationToken: token);
            }
        }

        private void Recalculate()
        {
            if (_npcs.Count <= 0)//No Npc
            {
                return;
            }
            else if (_players.Count <= 0)//No Player
            {
                RetrieveEveryAggro();
                return;
            }

            WeightCalculate();

            _downgradeList.Clear();
            _remainList.Clear();
            _eliminateBufferList.Clear();
            foreach (var npc in _npcs)
            {
                if (npc.AggressiveInfo.AggressiveToken == null) { continue; }
                else if (_eliminateTimeDic.TryGetValue(npc, out var value))
                {
                    if (Time.time - value >= _remainAggressiveTime) { _downgradeList.Add(npc); }
                    else { _remainList.Add(npc); }
                }
                else { _eliminateBufferList.Add(npc); }
            }
            for (var i = 0; i < _players.Count; i++)
            {
                _playerCost[i] = _players[i].TokenSpace;
                if (!_tokenDic.TryAdd(_players[i], new List<AggressiveToken>()))
                {
                    _tokenDic[_players[i]].Clear();
                }
            }
            //Process Remain List & Eliminate Buffer
            foreach (var npc in _remainList.Concat(_eliminateBufferList))
            {
                var playerIndex = _players.IndexOf(npc.AggressiveInfo.AggressiveToken.Target);
                _playerCost[playerIndex] -= npc.AggressiveInfo.AggressiveData.Cost;
                _tokenDic[_players[playerIndex]].Add(npc.AggressiveInfo.AggressiveToken);
                TokenRedistribute(npc, npc.AggressiveInfo.AggressiveToken);
            }
            //Distribute remain token
            var sortedNpc = _calculatedTarget.OrderByDescending(x => x.weight);
            foreach (var (npc, weight, target) in sortedNpc)
            {
                if (_playerCost[target] <= 0) { continue; }

                var cost = _npcs[npc].AggressiveInfo.AggressiveData.Cost;
                if (cost <= _playerCost[target] && !_remainList.Contains(_npcs[npc]) && !_eliminateBufferList.Contains(_npcs[npc]))
                {
                    _playerCost[target] -= cost;
                    var token = new AggressiveToken(_players[target], cost);
                    _tokenDic[_players[target]].Add(token);
                    TokenRedistribute(_npcs[npc], token);
                    _downgradeList.Remove(_npcs[npc]);
                }
                else if (_npcs[npc].AggressiveInfo.AggressiveToken != null && !_eliminateTimeDic.ContainsKey(_npcs[npc]))
                {
                    _eliminateTimeDic.Add(_npcs[npc], Time.time);
                }
            }
            foreach (var npc in _downgradeList)
            {
                TokenRedistribute(npc, null);
                _eliminateTimeDic.Remove(npc);
            }
        }

        private void TokenRedistribute(IAggressiveNpc npc, AggressiveToken newToken)
        {
            var oldToken = npc.AggressiveInfo.AggressiveToken;
            if (newToken == null && oldToken != null)//Remove token
            {
                npc.AggressiveInfo.AggressiveToken = null;
                npc.RetrieveToken(oldToken);
            }
            else if (oldToken == null && newToken != null)//Get new token
            {
                npc.AggressiveInfo.AggressiveToken = newToken;
                npc.ReceiveToken(newToken);
            }
            else if (oldToken != null && newToken != null)//Update token
            {
                npc.AggressiveInfo.AggressiveToken = newToken;
                if (oldToken.Target != newToken.Target) { npc.UpdateToken(newToken); }
            }
        }
        private void RetrieveEveryAggro()
        {
            while (_npcs.Count > 0) { Lost(_npcs[0], false); }
        }
        //Player
        public void Debut(IAggressivePlayer player)
        {
            if (!_players.Contains(player))
            {
                _players.Add(player);
                ResetWeightContainer(true);
                Recalculate();
            }
        }
        public void BowOut(IAggressivePlayer player)
        {
            if (_players.Contains(player))
            {
                _players.Remove(player);
                ResetWeightContainer(true);
                Recalculate();
            }
        }
        //NPC
        public void Aggro(IAggressiveNpc npc)
        {
            if (!_npcs.Contains(npc))
            {
                if (_npcs.Count == 0 && npc is NPCStatus status) { status.ClearCooldown(); }
                _npcs.Add(npc);
                ResetWeightContainer();
                npc.AggressiveInfo.SetAggro = true;
                Recalculate();
            }
        }
        public void Lost(IAggressiveNpc npc, bool recalculate = true)
        {
            if (_npcs.Contains(npc))
            {
                _npcs.Remove(npc);
                ResetWeightContainer();
                if (_eliminateTimeDic.ContainsKey(npc)) { _eliminateTimeDic.Remove(npc); }
                npc.AggressiveInfo.SetAggro = false;
                npc.AggressiveInfo.AggressiveToken = null;
                if (recalculate) { Recalculate(); }
            }
        }
        #endregion

        #region Intention System
        [Serializable] public class IntentionWeightDictionary : UnitySerializedDictionary<IntentionAction, float> { }
        [SerializeField, BoxGroup("Intention/Limits")] private float _nearLimit = 100f;
        [SerializeField, BoxGroup("Intention/Limits")] private float _nearThreshold = 50f;
        [SerializeField, BoxGroup("Intention/Limits")] private float _farThreshold = -50f;
        [SerializeField, BoxGroup("Intention/Limits")] private float _farLimit = -100f;
        [SerializeField, BoxGroup("Intention")] private float _distanceCycle = 0.1f;
        [SerializeField, BoxGroup("Intention")] private IntentionWeightDictionary _intentionWeight = new();

        [ShowInInspector, BoxGroup("Intention"), ReadOnly] private Dictionary<IAggressiveNpc, IntentionToken> _intentionDic = new();
        public IntentionToken ObserveIntention(IAggressiveNpc observer, IAggressivePlayer target, float observeDistance)
        {
            if (observer == null || target == null)
            {
                Log.Error("ObserveIntention : Null Observe");
                return null;
            }
            if (_intentionDic.TryGetValue(observer, out var intention)) { return intention; }

            intention = new IntentionToken(target);
            _intentionDic[observer] = intention;
            intention.TokenSource.New();
            Observing(observer, intention, observeDistance, intention.TokenSource.Token).Forget();
            return intention;
        }
        public void ObserveEnd(IAggressiveNpc observer)
        {
            if (!_intentionDic.TryGetValue(observer, out var intention)) { return; }
            intention.TokenSource.End();
            _intentionDic.Remove(observer);
        }
        private async UniTaskVoid Observing(IAggressiveNpc observer, IntentionToken intention, float observeDistance, CancellationToken token)
        {
            if (!observer.NpcAvailable) { return; }
            //Action
            Action<IntentionAction> onAct = act => IntentionAct(act, intention);
            intention.Target.IntentionOnAction += onAct;

            Action<Vector3> onDash = dashDir => IntentionDash(observer.IntentionGetPosition, dashDir, intention);
            intention.Target.IntentionOnDash += onDash;
            //Distance
            var near = _intentionWeight[IntentionAction.Near];
            var far = _intentionWeight[IntentionAction.Far];
            var canceled = false;
            while (!canceled && observer.NpcAvailable)
            {
                var distance = Vector3.Distance(observer.IntentionGetPosition, intention.Target.IntentionGetPosition);
                intention.Intention += distance > observeDistance ? far : near;
                CheckIntentionState(intention);
                canceled = await UniTask.Delay(TimeSpan.FromSeconds(_distanceCycle), cancellationToken: token).SuppressCancellationThrow();
            }
            intention.Target.IntentionOnAction -= onAct;
            intention.Target.IntentionOnDash -= onDash;
        }
        private void IntentionAct(IntentionAction act, IntentionToken intention)
        {
            intention.Intention += _intentionWeight[act];
            CheckIntentionState(intention);
        }
        private void IntentionDash(Vector3 self, Vector3 dir, IntentionToken intention)
        {
            var connectVec = self - intention.Target.IntentionGetPosition;
            var dot = Vector3.Dot(dir, connectVec.normalized);
            intention.Intention += dot * _intentionWeight[IntentionAction.Dash];
            CheckIntentionState(intention);
        }
        private void CheckIntentionState(IntentionToken token)
        {
            var intention = token.Intention;
            if (intention >= _nearThreshold) { token.State = IntentionToken.IntentionState.Near; }
            else if (intention <= _farThreshold) { token.State = IntentionToken.IntentionState.Far; }
            token.Intention = Mathf.Clamp(intention, _farLimit, _nearLimit);
        }
#if UNITY_EDITOR
        public void EvaluateIntention(IntentionToken token, out float value, out float nearValue, out float farValue)
        {
            value = token.Intention / Mathf.Abs(token.Intention >= 0 ? _nearLimit : _farLimit);
            nearValue = _nearThreshold / _nearLimit;
            farValue = _farThreshold / _farLimit;
        }
#endif
        #endregion
    }
}
