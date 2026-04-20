using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace TimelinePlayer.Timeline
{
    /// <summary>
    /// Standard Custom Timeline track holds TimelineActionClips, Binding with ReferenceHub
    /// </summary>
    [TrackColor(0.18f, 0.78f, 0.42f)]
    [TrackClipType(typeof(TimelineActionClipHolder))]
    [TrackBindingType(typeof(ReferenceHub))]
    public class TimelineActionTrack : TrackAsset
    {
        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        {
            return ScriptPlayable<TimelineActionMixerBehaviour>.Create(graph, inputCount);
        }
    }
    /// <summary>
    /// Mixer behaviour for TimelineActionTrack, Do no Mix
    /// </summary>
    public class TimelineActionMixerBehaviour : PlayableBehaviour { }
}
