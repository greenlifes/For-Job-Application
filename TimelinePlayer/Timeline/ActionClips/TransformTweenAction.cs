using System;
using Midou.Utility;
using UnityEngine;

namespace TimelinePlayer.Actions
{
    /// <summary>
    /// ActionClip : Tweening transform local position
    /// </summary>
    [Serializable]
    public class GameObjectTweenAction : ActionClip
    {
        public enum TweenType
        {
            Linear,
            EaseOutCubic,
            EaseOutExpo
        }

        [Tooltip("GameObjectEntry Key")]
        public string GameObjectKey;
        public TweenType Type;
        public float MoveDistance;
        public Vector3 MoveDirection;
        [Tooltip("Return to enter position on clip end or cancel")]
        public bool ReturnOriginPosOnEnd;

        [NonSerialized] private GameObjectEntry _target;
        [NonSerialized] private Vector3 _originalLocalPos;

        public override void OnEnter(ReferenceHub hub)
        {
            _target = hub.GetEntry<GameObjectEntry>(GameObjectKey);
            _originalLocalPos = _target.Value.transform.localPosition;
            MoveDirection = MoveDirection.normalized;
        }
        public override void OnUpdate(ReferenceHub hub, float normalizedTime)
        {
            if (_target != null)
            {
                var displace = GetTweenValue(Type, normalizedTime) * MoveDistance;
                _target.Value.transform.localPosition = _originalLocalPos + (displace * MoveDirection);
            }
        }
        public override void OnExit(ReferenceHub hub)
        {
            if (ReturnOriginPosOnEnd) { _target.Value.transform.localPosition = _originalLocalPos; }
        }

        public override void OnCancel(ReferenceHub hub)
        {
            if (ReturnOriginPosOnEnd) { _target.Value.transform.localPosition = _originalLocalPos; }
        }
        private float GetTweenValue(TweenType type, float t)
        {
            return type switch
            {
                TweenType.Linear => t,
                TweenType.EaseOutCubic => TweenValue.EaseOutCubic(t),
                TweenType.EaseOutExpo => TweenValue.EaseOutExpo(t),
                _ => t
            };
        }
    }
}
