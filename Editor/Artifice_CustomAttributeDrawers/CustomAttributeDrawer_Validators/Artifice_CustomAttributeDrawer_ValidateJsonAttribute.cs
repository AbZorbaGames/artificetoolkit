using System;
using ArtificeToolkit.Editor.Artifice_CustomAttributeDrawers;
using ArtificeToolkit.Editor.Artifice_CustomAttributeDrawers.CustomAttributeDrawer_Validators;
using ArtificeToolkit.Editor.Resources;
using CustomAttributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace Editor.Artifice_CustomAttributeDrawers.CustomAttributeDrawer_Validators
{
    [Artifice_CustomAttributeDrawer(typeof(ValidateJsonAttribute))]
    public class Artifice_CustomAttributeDrawer_ValidateJsonAttribute : Artifice_CustomAttributeDrawer_Validator_BASE
    {
        public override string LogMessage { get; } = "Value is not in JSON format.";
        public override Sprite LogSprite { get; } = Artifice_SCR_CommonResourcesHolder.instance.ErrorIcon;
        public override LogType LogType { get; } = LogType.Error;
        
        protected override bool IsApplicableToProperty(SerializedProperty property)
        {
            return property.propertyType == SerializedPropertyType.String;
        }

        public override bool IsValid(SerializedProperty property)
        {
            if (string.IsNullOrWhiteSpace(property.stringValue))
                return false;

            try
            {
                JToken.Parse(property.stringValue); // Parses any valid JSON (object, array, etc.)
                return true;
            }
            catch (JsonReaderException)
            {
                return false;
            }
        }
    }
}
