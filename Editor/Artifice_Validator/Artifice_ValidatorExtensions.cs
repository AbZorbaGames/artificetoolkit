using System;
using System.Collections.Generic;
using System.Linq;
using ArtificeToolkit.Attributes;
using ArtificeToolkit.Editor.Artifice_CustomAttributeDrawers.CustomAttributeDrawer_EnableIfAttribute;
using ArtificeToolkit.Editor.Artifice_CustomAttributeDrawers.CustomAttributeDrawer_Validators;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ArtificeToolkit.Editor
{
    public static class Artifice_ValidatorExtensions
    {
        /// <summary> Fills in-parameter list with logs found in property </summary>
        /// <remarks>Passing the List as a parameter as a minor optimization to avoid instantiating the list on each call of GenerateValidatorLogs.</remarks>
        public static void GenerateValidatorLogs(SerializedProperty property, List<Artifice_Validator.ValidatorLog> logs, Type validatorType)
        {
            if (property.IsArray())
            {
                // Create new lists
                var arrayCustomAttributes = new List<CustomAttribute>();
                var childrenCustomAttributes = new List<CustomAttribute>();

                // Get property attributes and parse-split them
                var attributes = property.GetCustomAttributes();
                if (attributes != null)
                    foreach (var attribute in attributes)
                        if (attribute is IArtifice_ArrayAppliedAttribute)
                            arrayCustomAttributes.Add(attribute);
                        else
                            childrenCustomAttributes.Add(attribute);

                // Generate Array Validations
                GenerateValidatorLogs(property, arrayCustomAttributes, logs, validatorType);

                // Generate Children Validations
                foreach (var child in property.GetVisibleChildren())
                    if (child.name != "size")
                        GenerateValidatorLogs(child, childrenCustomAttributes, logs, validatorType);
            }
            else
            {
                // Check property if its valid for stuff
                var customAttributes = property.GetCustomAttributes();
                if (customAttributes != null)
                    GenerateValidatorLogs(property, customAttributes.ToList(), logs, validatorType);
            }
        }

        /// <summary> Fills in-parameter list with logs found in property for specific parameterized attributes</summary>
        private static void GenerateValidatorLogs(SerializedProperty property, List<CustomAttribute> customAttributes, List<Artifice_Validator.ValidatorLog> logs, Type validatorType)
        {
            var validatorAttributes = customAttributes.Where(attribute => attribute is ValidatorAttribute).ToList();
            foreach (var validatorAttribute in validatorAttributes)
            {
                // Get drawer and cast to validator drawer.
                var drawer =
                    Artifice_Utilities.GetDrawerInstancesMap()[validatorAttribute.GetType()] as
                        Artifice_CustomAttributeDrawer_Validator_BASE;
                if (drawer == null)
                {
                    Debug.LogWarning($"Could not find drawer for validator type {validatorAttribute.GetType().Name}");
                    continue;
                }

                var target = property.serializedObject.targetObject;
                if (target == null)
                    continue;

                // Inject validator attribute to drawer
                drawer.Attribute = validatorAttribute;
                
                // Determine origin location name.
                var originLocationName = "";
                var assetPath = AssetDatabase.GetAssetPath(target);
                if (string.IsNullOrEmpty(assetPath) == false)
                    originLocationName = assetPath;
                else if (target is MonoBehaviour monoBehaviour)
                {
                    var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();

                    if (prefabStage != null && prefabStage.IsPartOfPrefabContents(monoBehaviour.gameObject))
                        originLocationName = Artifice_EditorWindow_Validator.PrefabStageKey;
                    else
                        originLocationName = monoBehaviour.gameObject.scene.name;
                }

                // If not valid, add it to list
                if (drawer.IsValid(property) == false)
                {
                    // Create log
                    var log = new Artifice_Validator.ValidatorLog(
                        drawer.LogSprite,
                        drawer.LogMessage,
                        drawer.LogType,
                        validatorType,
                        target,
                        originLocationName
                    );
                    logs.Add(log);
                }
            }
        }

        /// <summary> Returns true if property should be taken into consideration in the validation or not. </summary>
        public static bool ShouldValidateProperty(SerializedProperty property)
        {
            // Check if property is under enable if => false. In that case skip that property and its children.
            var customAttributes = property.GetCustomAttributes().ToList();
            var attribute = customAttributes.Find(attribute => attribute is EnableIfAttribute);
            if (attribute is EnableIfAttribute enableIfAttribute)
                return Artifice_CustomAttributeDrawer_EnableIfAttribute.ShouldIncludeInValidation(property, enableIfAttribute);

            return true;
        }
        
        public static List<ScriptableObject> FindScriptableObjects()
        {
            var searchString = $"t:{typeof(ScriptableObject).FullName}";
            
            var guidAssets = AssetDatabase.FindAssets(searchString);
            List<ScriptableObject> result = new();

            foreach (var guid in guidAssets)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);

                if (assetPath.Contains("Disabled")) continue;

                var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);
                if (asset != null)
                    result.Add(asset);
            }

            return result;
        }
    }
}