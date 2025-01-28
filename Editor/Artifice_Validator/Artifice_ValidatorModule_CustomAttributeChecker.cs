using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Artifice_Editor;
using ArtificeToolkit.Attributes;
using ArtificeToolkit.Editor.Artifice_CustomAttributeDrawers.CustomAttributeDrawer_Validators;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ArtificeToolkit.Editor
{
    using ValidatorLog = Artifice_Validator.ValidatorLog;

    public class Artifice_ValidatorModule_CustomAttributeChecker : Artifice_ValidatorModule
    {
        #region FIELDS

        public override string DisplayName { get; protected set; } = "CustomAttributes Checker";
        
        // Validator Attribute Drawer Map
        private readonly Dictionary<Type, Artifice_CustomAttributeDrawer_Validator_BASE> _validatorDrawerMap;

        #endregion

        public Artifice_ValidatorModule_CustomAttributeChecker()
        {
            // Get all drawers and map validators to their responding type
            _validatorDrawerMap = new Dictionary<Type, Artifice_CustomAttributeDrawer_Validator_BASE>();
            var drawerMap = Artifice_Utilities.GetDrawerMap();
            foreach (var pair in drawerMap)
                if (pair.Key.IsSubclassOf(typeof(ValidatorAttribute)))
                    _validatorDrawerMap[pair.Key] = (Artifice_CustomAttributeDrawer_Validator_BASE)Activator.CreateInstance(pair.Value);
        }

        /// <summary> Override which fills the Logs list with the custom attribute validations for all tracked types </summary>
        public override IEnumerator ValidateCoroutine(int batchSize)
        {
            // Refresh logs
            Logs.Clear();

            // Reusable list for log generation to avoid constant "new List<ValidatorLog>()"
            var reusableLogsList = new List<ValidatorLog>();

            // Create a set to cache already visited serialized properties
            var visitedProperties = new HashSet<SerializedProperty>();

            // Create an iteration stack to run through all serialized properties (even nested ones)
            var queue = new Queue<SerializedProperty>();
            foreach (var serializedProperty in FindPropertiesInTrackedScenes())
                queue.Enqueue(serializedProperty);
            foreach (var serializedProperty in FindPropertiesInPrefabStage())
                queue.Enqueue(serializedProperty);
            foreach (var serializedProperty in FindPropertiesInTrackedAssets())
                queue.Enqueue(serializedProperty);

            var currentBatchCount = 0;
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
                reusableLogsList.Clear();
                GenerateValidatorLogs(property, in reusableLogsList);
                Logs.AddRange(reusableLogsList);

                // Split process to coroutine batches
                if (++currentBatchCount > batchSize)
                {
                    currentBatchCount = 0;
                    yield return null;
                }
            }
        }

        /// <summary> Fills in-parameter list with logs found in property </summary>
        private void GenerateValidatorLogs(SerializedProperty property, in List<ValidatorLog> logs)
        {
            if (property.IsArray())
            {
                // Get array applied custom attributes
                var arrayAppliedCustomAttributes = ArtificeDrawer.ArrayAppliedCustomAttributes;

                // Create new lists
                var childrenCustomAttributes = new List<CustomAttribute>();
            
                // Get property attributes and parse-split them
                var attributes = property.GetCustomAttributes();
                if (attributes != null)
                    foreach (var attribute in attributes)
                        if(arrayAppliedCustomAttributes.Contains(attribute.GetType()) == false)
                            childrenCustomAttributes.Add(attribute);

                foreach (var child in property.GetVisibleChildren())
                    if(child.name != "size")    
                        GenerateValidatorLogs(child, childrenCustomAttributes, logs);
            }
            else
            {
                // Check property if its valid for stuff
                var customAttributes = property.GetCustomAttributes();
                if (customAttributes != null)
                    GenerateValidatorLogs(property, customAttributes.ToList(), logs);
            }
        }

        /// <summary> Fills in-parameter list with logs found in property for specific parameterized attributes</summary>
        private void GenerateValidatorLogs(SerializedProperty property, List<CustomAttribute> customAttributes, in List<ValidatorLog> logs)
        {
            var validatorAttributes = customAttributes.Where(attribute => attribute is ValidatorAttribute).ToList();
            foreach (var validatorAttribute in validatorAttributes)
            {
                // Get drawer
                var drawer = _validatorDrawerMap[validatorAttribute.GetType()];

                var target = (MonoBehaviour)property.serializedObject.targetObject;
                if (target == null)
                    continue;

                // Determine origin location name
                var originLocationName = "";
                var assetPath = AssetDatabase.GetAssetPath(target);
                if (string.IsNullOrEmpty(assetPath) == false)
                    originLocationName = assetPath;
                else if(PrefabStageUtility.GetCurrentPrefabStage() != null && PrefabStageUtility.GetCurrentPrefabStage().IsPartOfPrefabContents(target.gameObject))
                    originLocationName = Artifice_EditorWindow_Validator.PrefabStageKey;
                else
                    originLocationName = target.gameObject.scene.name;

                // Create log
                var log = new ValidatorLog(
                    drawer.LogSprite,
                    drawer.LogMessage,
                    drawer.LogType,
                    typeof(Artifice_ValidatorModule_CustomAttributeChecker),
                    property.serializedObject.targetObject,
                    originLocationName
                );

                // If not valid, add it to list
                if (drawer.IsValid(property) == false)
                    logs.Add(log);
            }
        }
    }
}