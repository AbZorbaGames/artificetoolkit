using System;
using System.Collections.Generic;
using System.Linq;
using ArtificeToolkit.Attributes;
using ArtificeToolkit.Editor.Artifice_CustomAttributeDrawers.CustomAttributeDrawer_Validators;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ArtificeToolkit.Editor
{
    public class Artifice_ValidatorModule_CustomAttributeChecker : Artifice_ValidatorModule_SerializedPropertyBatching
    {
        #region FIELDS

        public override string DisplayName { get; protected set; } = "CustomAttributes Checker";
        
        // Validator Attribute Drawer Map
        private readonly Dictionary<Type, Artifice_CustomAttributeDrawer_Validator_BASE> _validatorDrawerMap;

        #endregion

        public Artifice_ValidatorModule_CustomAttributeChecker()
        {
            _validatorDrawerMap = GenerateValidatorDrawerMap();
        }

        public static Dictionary<Type, Artifice_CustomAttributeDrawer_Validator_BASE> GenerateValidatorDrawerMap()
        {
            // Get all drawers and map validators to their responding type
            var resultingMap = new Dictionary<Type, Artifice_CustomAttributeDrawer_Validator_BASE>();
            var drawerMap = Artifice_Utilities.GetDrawerMap();
            foreach (var pair in drawerMap)
                if (pair.Key.IsSubclassOf(typeof(ValidatorAttribute)))
                    resultingMap[pair.Key] = (Artifice_CustomAttributeDrawer_Validator_BASE)Activator.CreateInstance(pair.Value);
            return resultingMap;
        }
        
        // Override for each batched property
        protected override void ValidateSerializedProperty(SerializedProperty property)
        {
            GenerateValidatorLogs(property, _validatorDrawerMap, Logs);
        }
        
        //TODO move this somewhere else?
        /// <summary> Fills in-parameter list with logs found in property </summary>
        public static void GenerateValidatorLogs(SerializedProperty property,
            Dictionary<Type, Artifice_CustomAttributeDrawer_Validator_BASE> validatorDrawerMap, 
            List<Artifice_Validator.ValidatorLog> logs)
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
                GenerateValidatorLogs(property, arrayCustomAttributes, validatorDrawerMap, logs);
                
                // Generate Children Validations
                foreach (var child in property.GetVisibleChildren())
                    if(child.name != "size")    
                        GenerateValidatorLogs(child, childrenCustomAttributes, validatorDrawerMap, logs);
            }
            else
            {
                // Check property if its valid for stuff
                var customAttributes = property.GetCustomAttributes();
                if (customAttributes != null)
                    GenerateValidatorLogs(property, customAttributes.ToList(), validatorDrawerMap, logs);
            }
        }

        //TODO move this somewhere else?
        /// <summary> Fills in-parameter list with logs found in property for specific parameterized attributes</summary>
        public static void GenerateValidatorLogs(SerializedProperty property, List<CustomAttribute> customAttributes,
            Dictionary<Type, Artifice_CustomAttributeDrawer_Validator_BASE> validatorDrawerMap, 
            List<Artifice_Validator.ValidatorLog> logs)
        {
            var validatorAttributes = customAttributes.Where(attribute => attribute is ValidatorAttribute).ToList();
            foreach (var validatorAttribute in validatorAttributes)
            {
                // Get drawer
                var drawer = validatorDrawerMap[validatorAttribute.GetType()];

                var target = property.serializedObject.targetObject;
                if (target == null)
                    continue;

                // Determine origin location name
                var originLocationName = "";
                var assetPath = AssetDatabase.GetAssetPath(target);
                if (string.IsNullOrEmpty(assetPath) == false)
                    originLocationName = assetPath;
                else if (target is MonoBehaviour monoBehaviour)
                {
                    PrefabStage prefabStage =  PrefabStageUtility.GetCurrentPrefabStage();
                    
                    if(prefabStage != null && prefabStage.IsPartOfPrefabContents(monoBehaviour.gameObject))
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
                        typeof(Artifice_ValidatorModule_CustomAttributeChecker),
                        target,
                        originLocationName
                    );
                    logs.Add(log);
                }
            }
        }
    }
}