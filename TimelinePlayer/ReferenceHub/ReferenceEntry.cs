using System;
using UnityEngine;

namespace TimelinePlayer
{
    public enum ReferenceType
    {
        GameObject,
        MonoBehaviour,
        Int,
        Float,
        Bool,
        String,
        Vector2,
        Vector3,
        Color,
    }
    [Serializable]
    public abstract class ReferenceEntryBase
    {
        public string Key;
        public ReferenceType Type;
    }
    [Serializable]
    public abstract class ReferenceEntry<T> : ReferenceEntryBase
    {
        public abstract T Value { get; }
    }

    [Serializable]
    public class GameObjectEntry : ReferenceEntry<GameObject>
    {
        [SerializeField] protected GameObject _value;
        public override GameObject Value => _value;
    }
    [Serializable]
    public class MonoBehaviourEntry : ReferenceEntry<MonoBehaviour>
    {
        [SerializeField] private MonoBehaviour _value;
        public override MonoBehaviour Value => _value;

        /// <summary>Convenience cast to a specific MonoBehaviour subtype.</summary>
        public T Get<T>() where T : MonoBehaviour => _value as T;
    }

    [Serializable]
    public class IntEntry : ReferenceEntry<int>
    {
        [SerializeField] private int _value;
        public override int Value => _value;
    }

    [Serializable]
    public class FloatEntry : ReferenceEntry<float>
    {
        [SerializeField] private float _value;
        public override float Value => _value;
    }

    [Serializable]
    public class BoolEntry : ReferenceEntry<bool>
    {
        [SerializeField] private bool _value;
        public override bool Value => _value;
    }

    [Serializable]
    public class StringEntry : ReferenceEntry<string>
    {
        [SerializeField] private string _value;
        public override string Value => _value ?? string.Empty;
    }

    [Serializable]
    public class Vector2Entry : ReferenceEntry<Vector2>
    {
        [SerializeField] private Vector2 _value;
        public override Vector2 Value => _value;
    }

    [Serializable]
    public class Vector3Entry : ReferenceEntry<Vector3>
    {
        [SerializeField] private Vector3 _value;
        public override Vector3 Value => _value;
    }

    [Serializable]
    public class ColorEntry : ReferenceEntry<Color>
    {
        [SerializeField] private Color _value;
        public override Color Value => _value;
    }
}
