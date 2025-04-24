using System;
using ArtificeToolkit.Runtime.SerializedDictionary;
using UnityEditor;
using UnityEngine;

namespace ArtificeToolkit.Editor
{
    [Serializable]
    [FilePath("Artifice/PersistantData.save", FilePathAttribute.Location.PreferencesFolder)]
    public class Artifice_SCR_PersistedData : ScriptableSingleton<Artifice_SCR_PersistedData>
    {
        [SerializeField]
        private SerializedDictionary<string, SerializedDictionary<string, string>> persistedData = new();
        
        public void SaveData(string viewPersistenceKey, string key, string value)
        {
            if (viewPersistenceKey == null)
                return;
            
            if (persistedData.ContainsKey(viewPersistenceKey) == false)
                persistedData[viewPersistenceKey] = new SerializedDictionary<string, string>();
            
            persistedData[viewPersistenceKey][key] = value;

            Save(true);
        }

        public string LoadData(string viewPersistenceKey, string key)
        {
            if (persistedData.ContainsKey(viewPersistenceKey) == false || persistedData[viewPersistenceKey].ContainsKey(key) == false)
                return null;
            
            return persistedData[viewPersistenceKey][key];
        }

        public SerializedDictionary<string, string> LoadAll(string viewPersistenceKey)
        {
            return persistedData.ContainsKey(viewPersistenceKey) == false ? 
                null : persistedData[viewPersistenceKey];
        }
        
        public void ClearData(string viewPersistenceKey)
        {
            persistedData[viewPersistenceKey].Clear();
        }
    }
}