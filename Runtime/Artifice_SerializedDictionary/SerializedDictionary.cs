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
            // The inspector edits list directly. Dictionary mutations keep it in sync.
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
            set
            {
                _dict[key] = value;
                SetSerializedPair(key, value);
            }
        }

        public ICollection<TK> Keys => _dict.Keys;

        public ICollection<TV> Values => _dict.Values;

        public int Count => _dict.Count;

        public bool IsReadOnly => ((IDictionary<TK, TV>)_dict).IsReadOnly;

        public void Add(TK key, TV value)
        {
            _dict.Add(key, value);
            SetSerializedPair(key, value);
        }

        public void Add(KeyValuePair<TK, TV> item) => Add(item.Key, item.Value);

        public void Clear()
        {
            _dict.Clear();
            list.Clear();
        }

        public bool Contains(KeyValuePair<TK, TV> item) => ((IDictionary<TK, TV>)_dict).Contains(item);

        public bool ContainsKey(TK key) => _dict.ContainsKey(key);

        public void CopyTo(KeyValuePair<TK, TV>[] array, int arrayIndex) =>
            ((IDictionary<TK, TV>)_dict).CopyTo(array, arrayIndex);

        public IEnumerator<KeyValuePair<TK, TV>> GetEnumerator() => _dict.GetEnumerator();

        public bool Remove(TK key)
        {
            if (!_dict.Remove(key))
                return false;

            list.RemoveAll(pair => EqualityComparer<TK>.Default.Equals(pair.Key, key));
            return true;
        }

        public bool Remove(KeyValuePair<TK, TV> item)
        {
            if (!((IDictionary<TK, TV>)_dict).Remove(item))
                return false;

            list.RemoveAll(pair => EqualityComparer<TK>.Default.Equals(pair.Key, item.Key));
            return true;
        }

        public bool TryGetValue(TK key, out TV value) => _dict.TryGetValue(key, out value);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private void SetSerializedPair(TK key, TV value)
        {
            foreach (var pair in list)
            {
                if (!EqualityComparer<TK>.Default.Equals(pair.Key, key))
                    continue;

                pair.Value = value;
                return;
            }

            list.Add(new SerializedDictionaryPair(key, value));
        }

        #endregion
    }
}
