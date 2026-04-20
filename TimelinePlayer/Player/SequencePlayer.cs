using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace TimelinePlayer.Player
{
    /// <summary>
    /// Linear Time-based data driven player
    /// Reads TimelineSequenceData and playback ActionClips saved by SequenceExporter
    /// </summary>
    public class SequencePlayer : MonoBehaviour
    {
        [Serializable]
        public class OverrideBinding
        {
            [Tooltip("Must match the TrackName in TrackData.")]
            public string TrackName;
            public ReferenceHub OverrideHub;
        }

        [SerializeField] private TimelineSequenceData _sequenceData;
        [SerializeField] private ReferenceHub _hub;
        [SerializeField] private List<OverrideBinding> _overrideBindings = new();

        private CancellationTokenSource _cts;

        public bool IsPlaying { get; private set; }
        public bool IsPaused { get; private set; }

        public void Play()
        {
            if (IsPlaying) { Stop(); }
            IsPaused = false;
            _cts = new CancellationTokenSource();
            PlayAsync(_cts.Token).Forget();
        }

        public void Stop()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
            IsPaused = false;
            IsPlaying = false;
        }

        public void Pause() => IsPaused = true;
        public void Resume() => IsPaused = false;
        private void OnDisable() => Stop();

        #region Play Flow
        private List<(ClipData clip, ReferenceHub hub)> BuildResolvedClipList()
        {
            var list = new List<(ClipData clip, ReferenceHub hub)>();
            foreach (var track in _sequenceData.Tracks)
            {
                var overridBinding = _overrideBindings.Find(x => x.TrackName.Equals(track.TrackName));
                foreach (var clip in track.Clips)
                {
                    list.Add((clip, overridBinding != null ? overridBinding.OverrideHub : _hub));
                }
            }
            list.Sort((a, b) => a.clip.StartFrame.CompareTo(b.clip.StartFrame));
            return list;
        }
        private async UniTaskVoid PlayAsync(CancellationToken ct)
        {
            if (_sequenceData == null || _sequenceData.Tracks == null)
            {
                Debug.LogWarning($"[SequencePlayer] {name}/PlayAsync: missing SequenceData", this);
                return;
            }

            IsPlaying = true;

            var clipList = BuildResolvedClipList();
            var enteredSet = new HashSet<ClipData>(ReferenceEqualityComparer.Instance);
            var exitedSet = new HashSet<ClipData>(ReferenceEqualityComparer.Instance);
            var frameRate = _sequenceData.FrameRate;

            var elapsed = 0f;
            var totalTime = _sequenceData.TotalDuration;

            //Update Loop
            while (elapsed <= totalTime)
            {
                if (ct.IsCancellationRequested) { break; }

                if (!IsPaused)
                {
                    UpdateFrame(clipList, enteredSet, exitedSet, elapsed, frameRate);
                }

                await UniTask.NextFrame(PlayerLoopTiming.Update, ct);
                if (!IsPaused) { elapsed += Time.deltaTime; }
            }

            //End
            if (ct.IsCancellationRequested) //Cancel
            {
                foreach (var (clip, hub) in clipList)
                {
                    if (enteredSet.Contains(clip) && !exitedSet.Contains(clip))
                    {
                        exitedSet.Add(clip);
                        clip.ActionData?.OnCancel(hub);
                    }
                }

            }
            else //Exit
            {
                foreach (var (clip, hub) in clipList)
                {
                    if (enteredSet.Contains(clip) && !exitedSet.Contains(clip))
                    {
                        exitedSet.Add(clip);
                        clip.ActionData?.OnExit(hub);
                    }
                }
            }

            IsPlaying = false;
        }

        private static void UpdateFrame(
            List<(ClipData clip, ReferenceHub hub)> allClips,
            HashSet<ClipData> entered,
            HashSet<ClipData> exited,
            float elapsed,
            float frameRate)
        {
            foreach (var (clip, hub) in allClips)
            {
                if (clip.ActionData == null) continue;

                var startTime = clip.GetStartTime(frameRate);
                var endTime = clip.GetEndTime(frameRate);
                var inRange = elapsed >= startTime && elapsed < endTime;
                var hasEntered = entered.Contains(clip);
                var hasExited = exited.Contains(clip);

                if (hasExited) continue;

                if (inRange && !hasEntered)
                {
                    entered.Add(clip);
                    clip.ActionData.OnEnter(hub);
                }

                if (inRange && hasEntered)
                    clip.ActionData.OnUpdate(hub, clip.GetNormalizedTime(elapsed, frameRate));

                if (!inRange && hasEntered && elapsed >= endTime)
                {
                    exited.Add(clip);
                    clip.ActionData.OnExit(hub);
                }
            }
        }
        #endregion
        //For lower .NetFrame
        private sealed class ReferenceEqualityComparer : IEqualityComparer<ClipData>
        {
            public static readonly ReferenceEqualityComparer Instance = new();
            public bool Equals(ClipData x, ClipData y) => ReferenceEquals(x, y);
            public int GetHashCode(ClipData obj) =>
                System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
        }
    }
}
