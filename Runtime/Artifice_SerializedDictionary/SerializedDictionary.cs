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

        public Dictionary<TK, TV> Dict = new();

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

            foreach (var kvp in Dict)
            {
                if (!existingKeys.Contains(kvp.Key))
                {
                    list.Add(new SerializedDictionaryPair(kvp.Key, kvp.Value));
                }
            }
        }

        public void OnAfterDeserialize()
        {
            Dict.Clear();
            foreach (var entry in list)
            {
                // First-wins: skip entries with duplicate keys
                if (Dict.ContainsKey(entry.Key))
                    continue;

                Dict.Add(entry.Key, entry.Value);
            }
        }

        #endregion

        #region IDictionary Interface

        public TV this[TK key]
        {
            get => Dict[key];
            set => Dict[key] = value;
        }

        public ICollection<TK> Keys => Dict.Keys;

        public ICollection<TV> Values => Dict.Values;

        public int Count => Dict.Count;

        public bool IsReadOnly => ((IDictionary<TK, TV>)Dict).IsReadOnly;

        public void Add(TK key, TV value) => Dict.Add(key, value);

        public void Add(KeyValuePair<TK, TV> item) => ((IDictionary<TK, TV>)Dict).Add(item);

        public void Clear() => Dict.Clear();

        public bool Contains(KeyValuePair<TK, TV> item) => ((IDictionary<TK, TV>)Dict).Contains(item);

        public bool ContainsKey(TK key) => Dict.ContainsKey(key);

        public void CopyTo(KeyValuePair<TK, TV>[] array, int arrayIndex) =>
            ((IDictionary<TK, TV>)Dict).CopyTo(array, arrayIndex);

        public IEnumerator<KeyValuePair<TK, TV>> GetEnumerator() => Dict.GetEnumerator();

        public bool Remove(TK key) => Dict.Remove(key);

        public bool Remove(KeyValuePair<TK, TV> item) => ((IDictionary<TK, TV>)Dict).Remove(item);

        public bool TryGetValue(TK key, out TV value) => Dict.TryGetValue(key, out value);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        #endregion
    }
}