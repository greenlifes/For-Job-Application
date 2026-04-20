using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace TimelinePlayer.Timeline
{
    /// <summary>
    /// PlayableAsset clip on TimelineActionTrack, Standard custom clip
    /// </summary>
    [Serializable]
    public class TimelineActionClipHolder : PlayableAsset, ITimelineClipAsset
    {
        [Tooltip("Inline action instance serialized directly into this clip asset.")]
        [SerializeReference]
        public ActionClip ActionClip;

        public ClipCaps clipCaps => ClipCaps.None;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<TimelineActionBehaviour>.Create(graph);
            playable.GetBehaviour().ActionClip = ActionClip;
            return playable;
        }
    }
}
