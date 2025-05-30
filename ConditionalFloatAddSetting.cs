using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using Green.Status;

namespace Green.Buffs
{
    [CreateAssetMenu(fileName = "ConditionalFloatAdd", menuName = "7Quark/Buffs/ConditionalFloatAdd")]
    public class ConditionalFloatAddSetting : BuffSettingCore<ConditionalFloatAdd> { }
    [Serializable]
    public class ConditionalFloatAdd : BuffCore
    {
        [SuffixLabel("/Stack", Overlay = true)]
        public float AddValue;
        public void Amp(ref StatusValue statusValue, IList<int> keys)
        {
            if (!statusValue.Is(StatusValueKind.Float) || !Agree(ref statusValue, keys)) { return; }
            statusValue.FloatValue += AddValue * StackCount;
        }
    }
}
