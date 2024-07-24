using System;
using Editor.Artifice_SerializedDictionary;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    [Serializable]
    [FilePath("Artifice/PersistantData.save", FilePathAttribute.Location.PreferencesFolder)]
    public class Artifice_SCR_ArtificePersistedData : ScriptableSingleton<Artifice_SCR_ArtificePersistedData>
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

        public void ClearData(string viewPersistenceKey)
        {
            persistedData[viewPersistenceKey].Clear();
        }
    }
}