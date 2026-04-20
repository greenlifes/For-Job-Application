using UnityEngine;
using UnityEngine.Playables;

namespace TimelinePlayer.Timeline
{
    /// <summary>
    /// Actual behaviour on TimelineActionTrack, interval layer which driving ActionClip
    /// </summary>
    public class TimelineActionBehaviour : PlayableBehaviour
    {
        public ActionClip ActionClip;

        private bool _hasEntered;
        private ReferenceHub _hub;

        public override void OnBehaviourPlay(Playable playable, FrameData info)
        {
            _hasEntered = false;
        }

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            if (ActionClip == null) { return; }

            _hub = playerData as ReferenceHub;

            if (!_hasEntered)
            {
                _hasEntered = true;
                ActionClip.OnEnter(_hub);
            }

            var duration = playable.GetDuration();
            var normalizedTime = duration > 0d ? Mathf.Clamp01((float)(playable.GetTime() / duration)) : 1f;

            ActionClip.OnUpdate(_hub, normalizedTime);
        }

        public override void OnBehaviourPause(Playable playable, FrameData info)
        {
            if (!_hasEntered) { return; }
            _hasEntered = false;
            ActionClip?.OnExit(_hub);
            _hub = null;
        }
    }
}
