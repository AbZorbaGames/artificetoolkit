using System.Collections;
using System.Collections.Generic;
using System.Data.Odbc;
using UnityEditor;
using UnityEngine;

namespace ArtificeToolkit.Editor
{
    public abstract class Artifice_ValidatorModule_SerializedPropertyBatching : Artifice_ValidatorModule
    {
        /// <summary> Override which fills the Logs list with the custom attribute validations for all tracked types </summary>
        public override IEnumerator ValidateCoroutine(List<GameObject> rootGameObjects)
        {
            // Create a set to cache already visited serialized properties
            var visitedProperties = new HashSet<SerializedProperty>();

            // Create an iteration stack to run through all serialized properties (even nested ones)
            var queue = new Queue<SerializedProperty>();
            foreach (var gameObject in rootGameObjects)
            {
                if (gameObject == null)
                    continue;
                
                foreach (var component in gameObject.GetComponentsInChildren<Component>(true))
                {
                    if(component == null)
                        continue;
                    
                    var serializedObject = new SerializedObject(component);
                    queue.Enqueue(serializedObject.GetIterator());
                }
            }
            
            while (queue.Count > 0)
            {
                // Pop next property and skip if already visited 
                var property = queue.Dequeue();

                // If for any reason the target object is destroyed after batch sleep, just skip.
                if (property.serializedObject.targetObject == null)
                    continue;

                // Skip if already visited
                if (visitedProperties.Contains(property))
                    continue;
                visitedProperties.Add(property);

                // Append its children
                foreach (var childProperty in property.GetVisibleChildren())
                    queue.Enqueue(childProperty);

                // Clear reusable list of logs and get current property's logs
                ValidateSerializedProperty(property);
                
                // Declare batch step.
                yield return null;
            }
        }

        protected abstract void ValidateSerializedProperty(SerializedProperty property);
    }
}