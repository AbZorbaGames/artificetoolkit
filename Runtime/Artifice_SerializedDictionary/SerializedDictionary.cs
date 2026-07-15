using System;
using System.Collections;
using System.Collections.Generic;
using ArtificeToolkit.Attributes;
using UnityEngine;

namespace ArtificeToolkit.Runtime.SerializedDictionary
{
    [Serializable]
    public abstract class SerializedDictionaryWrapper
    {
    }

    [Serializable]
    public class SerializedDictionary<TK, TV> : SerializedDictionaryWrapper, IDictionary<TK, TV>,
        ISerializationCallbackReceiver
    {
        [Serializable]
        private class SerializedDictionaryPair
        {
            [HideLabel] public TK Key;
            [HideLabel] public TV Value;

            public SerializedDictionaryPair(TK tk, TV tv)
            {
                Key = tk;
                Value = tv;
            }
        }

        [SerializeField] private List<SerializedDictionaryPair> list = new();
        
        private Dictionary<TK, TV> _dict = new();

        #region ISerializationCallbackReceiver Interface

        public void OnBeforeSerialize()
        {
            // Merge strategy: keep list (editor source of truth) intact,
            // then append any runtime-added Dict entries not already in list.
            var existingKeys = new HashSet<TK>();
            foreach (var pair in list)
            {
                if (pair.Key != null)
                    existingKeys.Add(pair.Key);
            }

            foreach (var kvp in _dict)
            {
                if (!existingKeys.Contains(kvp.Key))
                {
                    list.Add(new SerializedDictionaryPair(kvp.Key, kvp.Value));
                }
            }
        }

        public void OnAfterDeserialize()
        {
            _dict.Clear();
            foreach (var entry in list)
            {
                // First-wins: skip entries with duplicate keys
                if (_dict.ContainsKey(entry.Key))
                    continue;

                _dict.Add(entry.Key, entry.Value);
            }
        }

        #endregion

        #region IDictionary Interface

        public TV this[TK key]
        {
            get => _dict[key];
            set => _dict[key] = value;
        }

        public ICollection<TK> Keys => _dict.Keys;

        public ICollection<TV> Values => _dict.Values;

        public int Count => _dict.Count;

        public bool IsReadOnly => ((IDictionary<TK, TV>)_dict).IsReadOnly;

        public void Add(TK key, TV value) => _dict.Add(key, value);

        public void Add(KeyValuePair<TK, TV> item) => ((IDictionary<TK, TV>)_dict).Add(item);

        public void Clear() => _dict.Clear();

        public bool Contains(KeyValuePair<TK, TV> item) => ((IDictionary<TK, TV>)_dict).Contains(item);

        public bool ContainsKey(TK key) => _dict.ContainsKey(key);

        public void CopyTo(KeyValuePair<TK, TV>[] array, int arrayIndex) =>
            ((IDictionary<TK, TV>)_dict).CopyTo(array, arrayIndex);

        public IEnumerator<KeyValuePair<TK, TV>> GetEnumerator() => _dict.GetEnumerator();

        public bool Remove(TK key) => _dict.Remove(key);

        public bool Remove(KeyValuePair<TK, TV> item) => ((IDictionary<TK, TV>)_dict).Remove(item);

        public bool TryGetValue(TK key, out TV value) => _dict.TryGetValue(key, out value);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        #endregion
    }
}