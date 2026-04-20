using System;
using System.Collections.Generic;
using UnityEngine;

namespace TimelinePlayer
{
    /// <summary>
    /// ReferenceEntry Holder, Character reference center
    /// </summary>
    public class ReferenceHub : MonoBehaviour
    {
        [SerializeReference]
        private List<ReferenceEntryBase> _refEntries = new();
        private Dictionary<string, ReferenceEntryBase> _lookup = new();
        private void Awake() => BuildLookup();
        private void OnValidate() => BuildLookup();

        /// <summary>
        /// Get ReferenceEntry by Entry-Type & key
        /// </summary>
        public T GetEntry<T>(string key) where T : ReferenceEntryBase
        {
            if (_lookup == null) { BuildLookup(); }
            if (_lookup.TryGetValue(key, out var entryBase) && entryBase is T entry)
            {
                return entry;
            }
            Debug.LogWarning($"[ReferenceHub] {name}/Get: Key/{key} not found", this);
            return default;
        }
        /// <summary>
        /// Try Get ReferenceEntry by Entry-Type & key
        /// </summary>
        public bool TryGetEntry<T>(string key, out T value) where T : ReferenceEntryBase
        {
            if (_lookup == null) BuildLookup();
            if (_lookup.TryGetValue(key, out var entryBase) && entryBase is T entry)
            {
                value = entry;
                return value != null;
            }
            value = default;
            return false;
        }

        private void BuildLookup()
        {
            _lookup = new Dictionary<string, ReferenceEntryBase>();
            foreach (var entry in _refEntries)
            {
                if (entry != null && !string.IsNullOrEmpty(entry.Key))
                {
                    _lookup[entry.Key] = entry;
                }
            }
        }
    }
}
