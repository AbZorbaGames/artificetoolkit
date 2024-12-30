using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
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

            public void AssignRandomKey()
            {
                Key = (TK)CreateRandomObject(typeof(TK), Key);
            }

            private object CreateRandomObject(Type type, object existingInstance = null)
            {
                var random = new System.Random();
                
                if (type == typeof(int))
                    return random.Next(0, 100); // Random integer
                if (type == typeof(float))
                    return (float)random.NextDouble() * 100; // Random float
                if (type == typeof(double))
                    return random.NextDouble() * 100; // Random double
                if (type == typeof(bool))
                    return random.Next(0, 2) == 1; // Random bool
                if (type == typeof(string))
                    return Guid.NewGuid().ToString(); // Random string (unique identifier)
                if (type.IsEnum)
                    return GetRandomEnumValue(type); // Random enum value
                if (type == typeof(DateTime))
                    return DateTime.Now.AddDays(random.Next(-365, 365)); // Random date within a year
                if (type == typeof(char))
                    return (char)random.Next(65, 91); // Random uppercase letter (A-Z)

                // For complex types, use reflection
                var instance = existingInstance ?? Activator.CreateInstance(type);
                foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                {
                    if (field.IsInitOnly) 
                        continue; // Skip readonly fields
                    
                    var randomValue = CreateRandomObject(field.FieldType, field.GetValue(instance));
                    field.SetValue(instance, randomValue);
                }

                foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                {
                    if (!property.CanWrite || property.GetIndexParameters().Length > 0) 
                        continue; // Skip non-writable or indexer properties
                    
                    var randomValue = CreateRandomObject(property.PropertyType, property.GetValue(instance));
                    property.SetValue(instance, randomValue);
                }

                return instance;
            }

            private object GetRandomEnumValue(Type enumType)
            {
                var values = Enum.GetValues(enumType);
                int index = new System.Random().Next(0, values.Length);
                return values.GetValue(index);
            }
        }

        [SerializeField] private List<SerializedDictionaryPair> list = new();

        public Dictionary<TK, TV> Dict = new();

        #region ISerializationCallbackReceiver Interface

        public void OnBeforeSerialize()
        {
            list.Clear();
            foreach (var pair in Dict)
            {
                list.Add(new SerializedDictionaryPair(pair.Key, pair.Value));
            }
        }

        public void OnAfterDeserialize()
        {
            Dict.Clear();
            foreach (var entry in list)
            {
                if (entry.Key == null) // TODO[zack]: warning here. Something is not serialzable.
                    continue;

                var failSafeCounter = 0;
                var failSafeCounterMax = 3;
                while (Dict.ContainsKey(entry.Key))
                {
                    entry.AssignRandomKey();
                    if (failSafeCounter++ > failSafeCounterMax)
                        break;
                }

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