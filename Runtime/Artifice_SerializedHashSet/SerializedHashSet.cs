using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ArtificeToolkit.Runtime.SerializedHashSet
{
    [Serializable]
    public abstract class SerializedHashSetWrapper
    {
    }

    [Serializable]
    public class SerializedHashSet<T> : SerializedHashSetWrapper, ISet<T>, ISerializationCallbackReceiver
    {
        #region FIELDS

        [SerializeField] 
        private List<T> list = new();
        
        private HashSet<T> _hashSet = new();

        #endregion

        #region ISerializationCallbackReceiver

        public void OnBeforeSerialize()
        {
            // Noop. List will always remain as the user wants it, even with invalid elements.
            // But, invalid elements will be shown with a warning that they will not be included in the hashset while their are invalid.
        }

        public void OnAfterDeserialize()
        {
            _hashSet.Clear();
            for (var i = 0; i < list.Count; i++)
                _hashSet.Add(list[i]);
        }

        #endregion

        #region IEnumerable

        public IEnumerator<T> GetEnumerator() => _hashSet.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        #endregion

        #region ISet<T>

        public bool Add(T item)
        {
            if (_hashSet.Add(item))
            {
                list.Add(item);
                return true;
            }

            return false;
        }

        void ICollection<T>.Add(T item) => Add(item);

        public void ExceptWith(IEnumerable<T> other)
        {
            _hashSet.ExceptWith(other);
            SyncSerialized();
        }

        public void IntersectWith(IEnumerable<T> other)
        {
            _hashSet.IntersectWith(other);
            SyncSerialized();
        }

        public bool IsProperSubsetOf(IEnumerable<T> other) =>
            _hashSet.IsProperSubsetOf(other);

        public bool IsProperSupersetOf(IEnumerable<T> other) =>
            _hashSet.IsProperSupersetOf(other);

        public bool IsSubsetOf(IEnumerable<T> other) =>
            _hashSet.IsSubsetOf(other);

        public bool IsSupersetOf(IEnumerable<T> other) =>
            _hashSet.IsSupersetOf(other);

        public bool Overlaps(IEnumerable<T> other) =>
            _hashSet.Overlaps(other);

        public bool SetEquals(IEnumerable<T> other) =>
            _hashSet.SetEquals(other);

        public void SymmetricExceptWith(IEnumerable<T> other)
        {
            _hashSet.SymmetricExceptWith(other);
            SyncSerialized();
        }

        public void UnionWith(IEnumerable<T> other)
        {
            _hashSet.UnionWith(other);
            SyncSerialized();
        }

        #endregion

        #region ICollection<T>

        public void Clear()
        {
            Debug.Log("Clear");
            _hashSet.Clear();
            list.Clear();
        }

        public bool Contains(T item) => _hashSet.Contains(item);

        public void CopyTo(T[] array, int arrayIndex) =>
            _hashSet.CopyTo(array, arrayIndex);

        public bool Remove(T item)
        {
            if (_hashSet.Remove(item))
            {
                list.Remove(item);
                return true;
            }

            return false;
        }

        public int Count => _hashSet.Count;

        public bool IsReadOnly => false;

        #endregion

        #region Helpers

        private void SyncSerialized()
        {
            list.Clear();
            list.AddRange(_hashSet);
        }

        #endregion
    }
}