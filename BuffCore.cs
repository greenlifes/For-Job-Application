using System.Collections.Generic;
using UnityEngine;
using Green.Status;
using System;
using Sirenix.OdinInspector;
using UnityAtoms;
using System.Reflection;
using System.Linq;

namespace Green.Buffs
{
    public delegate void BuffAction(ref StatusValue value, IList<int> keys);

    [Serializable]
    public abstract class BuffCore
    {
        [TitleGroup("Buff Core Setting")]
        //Buff Basic Setting
        public float Duration = -1f;
        public int Stack = 1;
        public int MaxStack = 1;
        public bool CasterIndependent;  //Separate with different caster
        public bool IsElemental; //TODO elemental determining should not belong here
        //
        public bool Conditional;
        [ShowIf("Conditional"), LabelText("$Title")]
        public bool UseAnd;
        [ShowIf("Conditional")]
        public StatusKeyConstant[] ConditionKeys;
        [ShowIf("Conditional")]
        public bool ReverseConditional;
        //Optional Function Register
        [TitleGroup("Optional Function Register"), ValueDropdown("DropDownCharacterStatusVoid")]
        public string OnApplySetting = "";
        [Tooltip("Be sure to return ClaimRestartCalculate"), ValueDropdown("DropDownCharacterStatusBool")]
        public string OnUpdateSetting = "";
        [OnValueChanged("MethodStringTyping")]
        public List<MethodString> TagPairSetting = new();
        //
        [NonSerialized] public Action<CharacterStatus> OnApply;
        [NonSerialized] public Func<CharacterStatus, bool> OnUpdate; //Return ClaimRestartCalculate

        [NonSerialized] public Dictionary<int, BuffAction> TagPair = new();
        //Buff Status
        [NonSerialized] private int _casterHash;
        [NonSerialized] private float _startTime;
        [NonSerialized, ShowInInspector, Sirenix.OdinInspector.ReadOnly] private int _stackCount = 1;
        //
        public int StackCount
        {
            get
            {
                return _stackCount;
            }
        }
        public int OriginalID { get; internal set; }
        public bool CheckOriginalID(int id) => OriginalID == id;

        //Basic Buff Flow Function
        public virtual void OnStart(CharacterStatus characterStatus, out bool claimRestartCalculate) { claimRestartCalculate = false; }
        public virtual void OnStack(CharacterStatus characterStatus, int lastStack, out bool claimRestartCalculate) { claimRestartCalculate = false; }
        public virtual void OnClear(CharacterStatus characterStatus, out bool claimRestartCalculate) { claimRestartCalculate = false; }
        public virtual void OnEnd(CharacterStatus characterStatus, out bool claimRestartCalculate) { claimRestartCalculate = false; }
        public virtual void SetCaster(GameObject caster) => _casterHash = caster.GetHashCode();
        public bool CheckCaster(GameObject caster) => _casterHash == caster.GetHashCode();
        public int AddStackCount(int count) // return last stack
        {
            var lastStack = _stackCount;
            _stackCount = Mathf.Clamp(_stackCount + count, 1, MaxStack);
            return lastStack;
        }
        public float RemainDuration => Duration > 0 ? _startTime + Duration - Time.time : Mathf.Infinity;
        public void ResetTimer() => _startTime = Time.time;
        public bool Agree(ref StatusValue statusValue, IList<int> keys)
        {
            if (!Conditional || ConditionKeys.Length == 0) { return !ReverseConditional; }
            if (keys == null) { return ReverseConditional; } // No Status
            if (UseAnd) // And
            {
                foreach (var condition in ConditionKeys)
                {
                    var unlock = keys.Contains(condition.Value.Hash);
                    if (!unlock) { return ReverseConditional; }
                }
                return !ReverseConditional;
            }
            else // Or
            {
                foreach (var condition in ConditionKeys)
                {
                    var unlock = keys.Contains(condition.Value.Hash);
                    if (unlock) { return !ReverseConditional; }
                }
                return ReverseConditional;
            }
        }
        public virtual BuffCore CreateBuff()
        {
            var clone = this.MemberwiseClone() as BuffCore;
            if (clone != null) clone.TagPair = new Dictionary<int, BuffAction>();
            return clone;
        }
        private void MethodStringTyping() { foreach (var ms in TagPairSetting) { ms.Type = this.GetType(); } }
        private IEnumerable<string> DropDownCharacterStatusVoid()
        {
            var testTypes = new[] { typeof(CharacterStatus) };
            var returnType = typeof(void);
            var find = Array.FindAll(this.GetType().GetMethods(), info =>
            {
                if (info.ReturnType != returnType) { return false; }
                var types = Array.ConvertAll(info.GetParameters(), i => i.ParameterType);
                return types.SequenceEqual(testTypes);
            });
            return Array.ConvertAll(find, x => x.Name);
        }
        private IEnumerable<string> DropDownCharacterStatusBool()
        {
            var testTypes = new[] { typeof(CharacterStatus) };
            var returnType = typeof(bool);
            var find = Array.FindAll(this.GetType().GetMethods(), info =>
            {
                if (info.ReturnType != returnType) { return false; }
                var types = Array.ConvertAll(info.GetParameters(), i => i.ParameterType);
                return types.SequenceEqual(testTypes);
            });
            return Array.ConvertAll(find, x => x.Name);
        }
        [Serializable]
        public class MethodString
        {
            public StatusTagConstant Tag;
            public Type Type;
            [ValueDropdown("DropDownList")]
            public string Method;

            private IEnumerable<string> DropDownList()
            {
                if (Type == null) { return new[] { "No Type" }; }
                var testTypes = new[] { typeof(StatusValue).MakeByRefType(), typeof(IList<int>) };
                var returnType = typeof(void);
                var find = Array.FindAll(Type.GetMethods(), info =>
                {
                    if (info.ReturnType != returnType) { return false; }
                    var types = Array.ConvertAll(info.GetParameters(), i => i.ParameterType);
                    return types.SequenceEqual(testTypes);
                });
                return Array.ConvertAll(find, x => x.Name);
            }
        }
    }
    public interface IBuffSettingCore
    {
        public int GetOriginalID();
        public bool IsCasterIndependent();
    }
    public class BuffSetting : ScriptableObject, IBuffSettingCore
    {
        public virtual BuffCore CreateBuff() => null;
        public int GetOriginalID() => this.GetHashCode();
        public virtual bool IsCasterIndependent() => false;
    }
    public abstract class BuffSettingCore<T> : BuffSetting where T : BuffCore, new()
    {
        [SerializeField, HideLabel, TitleGroup("$Title")/*, OnStateUpdate("DataUpdate")*/]
        private T _data = new T();

        public T GetBuffCore() => _data;

        public override BuffCore CreateBuff()
        {
            if (!_gotMethod) { OnEnable(); }
            var creation = _data.CreateBuff();
            creation.OriginalID = this.GetOriginalID();
            creation.OnApply = (Action<CharacterStatus>)_apply?.CreateDelegate(typeof(Action<CharacterStatus>), creation);
            creation.OnUpdate = (Func<CharacterStatus, bool>)_update?.CreateDelegate(typeof(Func<CharacterStatus, bool>), creation);
            foreach (var pairSetting in _tagPair)
            {
                creation.TagPair[pairSetting.Key.Value.Hash] = (BuffAction)pairSetting.Value?.CreateDelegate(typeof(BuffAction), creation);
            }
            return creation;
        }
        public override bool IsCasterIndependent() => _data.CasterIndependent;
        [NonSerialized] bool _gotMethod;
        private void OnEnable()
        {
            _gotMethod = true;
            if (_data.OnApplySetting != "") { _apply = _data.GetType().GetMethod(_data.OnApplySetting); }
            if (_data.OnUpdateSetting != "") { _update = _data.GetType().GetMethod(_data.OnUpdateSetting); }
            foreach (var pairSetting in _data.TagPairSetting)
            {
                if (pairSetting.Tag != null && pairSetting.Method != "")
                {
                    _tagPair ??= new Dictionary<StatusTagConstant, MethodInfo>();
                    _tagPair[pairSetting.Tag] = _data.GetType().GetMethod(pairSetting.Method);
                }
            }
        }
        private MethodInfo _apply;
        private MethodInfo _update;
        private Dictionary<StatusTagConstant, MethodInfo> _tagPair = new();
    }
    public interface IBuffDamage
    {
        public void AddDamageRecord(int damage);
    }
}
