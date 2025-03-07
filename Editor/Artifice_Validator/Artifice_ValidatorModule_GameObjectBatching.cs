using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ArtificeToolkit.Editor
{
    public abstract class Artifice_ValidatorModule_GameObjectBatching : Artifice_ValidatorModule
    {
        /// <summary> This override handles all batching and parsing logic for validating rootGameObjects. Inheritors should use the ValidateGameObject coroutine. </summary>
        public override IEnumerator ValidateCoroutine(List<GameObject> rootGameObjects)
        {
            var queue = new Queue<GameObject>(rootGameObjects);
            var alreadyVisited = new HashSet<GameObject>(); // This is probably not needed but since hierarchy is a DAG, but be safe, dont block at any case the user.
            while (queue.Count > 0)
            {
                var gameObject = queue.Dequeue();
                if (gameObject == null)
                    continue;
                
                if(alreadyVisited.Contains(gameObject))
                    continue;
                alreadyVisited.Add(gameObject);
                
                ValidateGameObject(gameObject);
                
                // Add all children of gameObject to queue.
                for (var i = 0; i < gameObject.transform.childCount; i++)
                    queue.Enqueue(gameObject.transform.GetChild(i).gameObject);
                
                // Batch step
                yield return null;
            }
            
            HasFinishedValidateCoroutine = true;
        }

        protected abstract void ValidateGameObject(GameObject gameObject);
    }
}
