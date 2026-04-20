using System.Collections.Generic;
using UnityEngine;

namespace TimelinePlayer
{
    /// <summary>
    /// ScriptableObject stores full sequence auto exported from Timeline
    /// </summary>
    [CreateAssetMenu(menuName = "TimelineTool/Sequence Data", fileName = "NewSequenceData")]
    public class TimelineSequenceData : ScriptableObject
    {
        [Tooltip("Frame rate of the timeline")]
        public float FrameRate = 60f;
        [Tooltip("Total frames of the timeline")]
        public int TotalFrames;
        public List<TrackData> Tracks = new();

        [HideInInspector]
        public string SourceTimelineGuid;

        public float TotalDuration => TotalFrames / FrameRate;
    }
}
