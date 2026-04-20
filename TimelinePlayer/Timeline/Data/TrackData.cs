using System;
using System.Collections.Generic;
using UnityEngine;

namespace TimelinePlayer
{
    /// <summary>
    /// Track data, clip holder
    /// </summary>
    [System.Serializable]
    public class TrackData
    {
        public string TrackName;
        public List<ClipData> Clips = new();
    }
    /// <summary>
    /// Single clip data within track, storing timing in frames (fps set in TimelinePlayerSetting)
    /// </summary>
    [Serializable]
    public class ClipData
    {
        [Tooltip("Start time in frame")]
        public int StartFrame;

        [Tooltip("Duration in frame")]
        public int DurationFrames;

        [Tooltip("SerializeReference of ActionData")]
        [SerializeReference]
        public ActionClip ActionData;

        public float GetStartTime(float frameRate) => StartFrame / frameRate;
        public float GetDuration(float frameRate) => DurationFrames / frameRate;
        public float GetEndTime(float frameRate) => (StartFrame + DurationFrames) / frameRate;

        public float GetNormalizedTime(float elapsedTime, float frameRate)
        {
            float duration = GetDuration(frameRate);
            if (duration <= 0f) { return 1f; }
            return Mathf.Clamp01((elapsedTime - GetStartTime(frameRate)) / duration);
        }
    }
}
