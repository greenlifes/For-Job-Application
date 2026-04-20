using System;
using UnityEngine;

namespace TimelinePlayer.Actions
{
    /// <summary>
    /// ActionClip : Set GameObject active state OnEnter/OnEnd
    /// </summary>
    [Serializable]
    public class GameObjectActiveAction : ActionClip
    {
        public enum ActiveOperate
        {
            True,
            False,
            OriginalState,
            OriginalInvert
        }

        [Tooltip("GameObjectEntry Key")]
        public string GameObjectKey;
        [Tooltip("SetActive value on clip start")]
        public ActiveOperate ActiveStateOnEnter = ActiveOperate.True;
        [Tooltip("SetActive value on clip end or cancel")]
        public ActiveOperate ActiveStateOnEnd = ActiveOperate.False;

        [NonSerialized] private bool _originalState;
        [NonSerialized] private GameObjectEntry _targetEntry;
        private void SetActiveState(GameObjectEntry entry, ActiveOperate operate)
        {
            if (entry == null || entry.Value == null) { return; }
            entry.Value.SetActive(operate switch
            {
                ActiveOperate.True => true,
                ActiveOperate.False => false,
                ActiveOperate.OriginalState => _originalState,
                ActiveOperate.OriginalInvert => !_originalState,
                _ => _originalState
            });
        }
        public override void OnEnter(ReferenceHub hub)
        {
            _targetEntry = (hub != null) ? hub.GetEntry<GameObjectEntry>(GameObjectKey) : null;
            _originalState = _targetEntry != null && _targetEntry.Value != null && _targetEntry.Value.activeSelf;
            SetActiveState(_targetEntry, ActiveStateOnEnter);
        }
        public override void OnUpdate(ReferenceHub hub, float normalizedTime) { }

        public override void OnExit(ReferenceHub hub)
            => SetActiveState(_targetEntry, ActiveStateOnEnd);

        public override void OnCancel(ReferenceHub hub)
            => SetActiveState(_targetEntry, ActiveStateOnEnd);
    }
}
