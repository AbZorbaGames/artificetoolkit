using System.Collections;
using System.Collections.Generic;
using System.IO;
using Artifice_Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ArtificeToolkit.Editor
{
    using ValidatorLog = Artifice_Validator.ValidatorLog;
    
    public abstract class Artifice_ValidatorModule
    {
        #region FIELDS

        /// <summary>Display name of module</summary>
        public virtual string DisplayName { get; protected set; } = "Undefined";

        /// <summary>If true, its displayed on the filters list. Otherwise its hidden, but always runs.</summary>
        public virtual bool DisplayOnFilters { get; protected set; } = true;

        /// <summary>When set to true, module will only run with dedicated button call</summary>
        public virtual bool OnDemandOnlyModule { get; protected set; } = false;
        
        /// <summary>Each module will empty and fill this list with its validations when <see cref="ValidateCoroutine"/> is called</summary>
        public readonly List<ValidatorLog> Logs = new List<ValidatorLog>();

        private int _currentBatchCount = 0; 
        
        #endregion

        /* Main Abstract Method for Validation */
        public abstract IEnumerator ValidateCoroutine(int batchSize);
        
        #region Utility
        
        protected bool CheckBatchCount(int batchSize)
        {
            if (++_currentBatchCount > batchSize)
            {
                _currentBatchCount = 0;
                return true;
            }
            return false;
        }
        
        protected Artifice_SCR_ValidatorConfig GetConfig()
        {
            // Do this on every call, since its possible for the selected config to be changed
            const string configKeyPath = Artifice_Validator.ConfigPathKey;
            return AssetDatabase.LoadAssetAtPath<Artifice_SCR_ValidatorConfig>(EditorPrefs.GetString(configKeyPath));
        } 
        
        /// <summary>Returns a list of all serialized properties found in open and config-selected scenes.</summary>
        protected List<SerializedProperty> FindPropertiesInTrackedScenes()
        {
            var list = new List<SerializedProperty>();

            // For open, tracked scenes.
            foreach (var pair in GetConfig().scenesMap)
            {
                var sceneName = pair.Key;
                var shouldSearchScene = pair.Value;

                var scene = SceneManager.GetSceneByName(sceneName);
                if(shouldSearchScene && scene.isLoaded)
                    foreach (var monoBehaviour in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                        list.Add(new SerializedObject(monoBehaviour).GetIterator());
            }
            
            return list;
        }

        /// <summary>Returns a list of all serialized properties found in prefab stage.</summary>
        protected List<SerializedProperty> FindPropertiesInPrefabStage()
        {
            var list = new List<SerializedProperty>();

            // For prefab stage
            var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage != null)
            {
                foreach (var rootGameObject in prefabStage.prefabContentsRoot.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    var serializedObject = new SerializedObject(rootGameObject);
                    var iterator = serializedObject.GetIterator();
                    list.Add(iterator);
                }
            }
            
            return list;
        }
        
        /// <summary>Returns a list of all serialized properties found in tracked assets.</summary>
        protected List<SerializedProperty> FindPropertiesInTrackedAssets()
        {
            var list = new List<SerializedProperty>();
            
            // Only search at selected asset paths
            foreach (var (assetFolderPath, includedInSearch) in GetConfig().assetPathsMap)
            {
                if (includedInSearch == false)
                    continue;
                
                var filePaths = Directory.GetFiles(assetFolderPath, "*.prefab", SearchOption.AllDirectories);
                foreach (string filePath in filePaths)
                {
                    var relativePath = Artifice_Utilities.ConvertGlobalToRelativePath(filePath);
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(relativePath);
                    if (prefab == null)
                    {
                        Debug.LogWarning($"Artifice Validator: Unable to load prefab '{relativePath}'");
                        continue;
                    }
        
                    var monoBehaviours = prefab.GetComponents<MonoBehaviour>();
                    foreach (var monoBehaviour in monoBehaviours)
                    {
                        if (monoBehaviour == null)
                            continue;
                        var serializedObject = new SerializedObject(monoBehaviour);
                        var iterator = serializedObject.GetIterator();
                        if(iterator != null)
                            list.Add(iterator);
                    }
                }
            }
            
            return list;
        }
        
        #endregion
    }
}