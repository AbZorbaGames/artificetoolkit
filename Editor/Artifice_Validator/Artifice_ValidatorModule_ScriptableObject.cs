using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ArtificeToolkit.Editor
{
    public class Artifice_ValidatorModule_ScriptableObject : Artifice_ValidatorModule_SerializedPropertyBatching
    {
        #region FIELDS

        public override string DisplayName { get; protected set; } = "ScriptableObject CustomAttributes Checker";
        
        #endregion

        public override IEnumerator ValidateCoroutine(List<GameObject> _)
        {
            // Create an iteration stack to run through all serialized properties (even nested ones)
            Queue<SerializedProperty> queue = new();
            foreach (ScriptableObject scriptableObject in FindScriptableObjects())
            {
                SerializedObject serializedObject = new(scriptableObject);
                queue.Enqueue(serializedObject.GetIterator());
            }
            
            // Create a set to cache already visited serialized properties
            HashSet<SerializedProperty> visitedProperties = new();
            
            while (queue.Count > 0)
            {
                // Pop next property and skip if already visited 
                SerializedProperty property = queue.Dequeue();

                // If for any reason the target object is destroyed after batch sleep, just skip.
                if (property.serializedObject.targetObject == null)
                    continue;

                // Skip if already visited
                if (!visitedProperties.Add(property))
                    continue;

                // Append its children
                foreach (SerializedProperty childProperty in property.GetVisibleChildren())
                    queue.Enqueue(childProperty);

                // Clear reusable list of logs and get current property's logs
                ValidateSerializedProperty(property);
                
                // Declare a batch step.
                yield return null;
            }
        }

        private static List<ScriptableObject> FindScriptableObjects()
        {
            string searchString = $"t:{typeof(ScriptableObject).FullName}";
            
            string[] guidAssets = AssetDatabase.FindAssets(searchString);
            List<ScriptableObject> result = new();

            foreach (string guid in guidAssets)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);

                if (assetPath.Contains("Disabled")) continue;

                ScriptableObject asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);
                if (asset != null)
                    result.Add(asset);
            }

            return result;
        }
        
        protected override void ValidateSerializedProperty(SerializedProperty property)
        {
            Artifice_ValidatorExtensions.GenerateValidatorLogs(property, Logs, GetType());
        }
    }
}
