using System;

namespace TimelinePlayer
{
    /// <summary>
    /// Base class for all ActionClip
    /// </summary>
    [Serializable]
    public abstract class ActionClip
    {
        public abstract void OnEnter(ReferenceHub hub);
        public abstract void OnUpdate(ReferenceHub hub, float normalizedTime);
        public abstract void OnExit(ReferenceHub hub);
        public abstract void OnCancel(ReferenceHub hub);
    }
}
