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
            // Get all drawers and map validators to their responding type
            _validatorDrawerMap = new Dictionary<Type, Artifice_CustomAttributeDrawer_Validator_BASE>();
            var drawerMap = Artifice_Utilities.GetDrawerMap();
            foreach (var pair in drawerMap)
                if (pair.Key.IsSubclassOf(typeof(ValidatorAttribute)))
                    _validatorDrawerMap[pair.Key] = (Artifice_CustomAttributeDrawer_Validator_BASE)Activator.CreateInstance(pair.Value);
        }
        
        // Override for each batched property
        protected override void ValidateSerializedProperty(SerializedProperty property)
        {
            GenerateValidatorLogs(property);
        }
        
        /// <summary> Fills in-parameter list with logs found in property </summary>
        private void GenerateValidatorLogs(SerializedProperty property)
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
                GenerateValidatorLogs(property, arrayCustomAttributes);
                
                // Generate Children Validations
                foreach (var child in property.GetVisibleChildren())
                    if(child.name != "size")    
                        GenerateValidatorLogs(child, childrenCustomAttributes);
            }
            else
            {
                // Check property if its valid for stuff
                var customAttributes = property.GetCustomAttributes();
                if (customAttributes != null)
                    GenerateValidatorLogs(property, customAttributes.ToList());
            }
        }

        /// <summary> Fills in-parameter list with logs found in property for specific parameterized attributes</summary>
        private void GenerateValidatorLogs(SerializedProperty property, List<CustomAttribute> customAttributes)
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

                // If not valid, add it to list
                if (drawer.IsValid(property) == false)
                {
                    // Create log
                    var log = new Artifice_Validator.ValidatorLog(
                        drawer.LogSprite,
                        drawer.LogMessage,
                        drawer.LogType,
                        typeof(Artifice_ValidatorModule_CustomAttributeChecker),
                        (Component)property.serializedObject.targetObject,
                        originLocationName
                    );
                    Logs.Add(log);
                }
            }
        }
    }
}