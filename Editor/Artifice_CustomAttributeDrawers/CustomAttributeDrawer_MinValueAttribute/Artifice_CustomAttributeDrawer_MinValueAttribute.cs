using System.Linq;
using ArtificeToolkit.Attributes;
using ArtificeToolkit.Editor.Artifice_CustomAttributeDrawers.CustomAttributeDrawer_Validators;
using ArtificeToolkit.Editor.Resources;
using UnityEditor;
using UnityEngine;

namespace ArtificeToolkit.Editor.Artifice_CustomAttributeDrawers.CustomAttributeDrawer_MinValueAttribute
{
    [Artifice_CustomAttributeDrawer(typeof(MinValueAttribute))]
    public class Artifice_CustomAttributeDrawer_MinValueAttribute : Artifice_CustomAttributeDrawer_Validator_BASE
    {
        public override string LogMessage { get; } = "Property value is below minimum accepted value.";
        public override Sprite LogSprite { get; } = Artifice_SCR_CommonResourcesHolder.instance.ErrorIcon;
        public override LogType LogType { get; } = LogType.Error;
        
        protected override bool IsApplicableToProperty(SerializedProperty property)
        {
            return property.propertyType is SerializedPropertyType.Integer or SerializedPropertyType.Float;
        }

        public override bool IsValid(SerializedProperty property)
        {
            var attribute = (MinValueAttribute)property.GetCustomAttributes().FirstOrDefault(attribute => attribute is MinValueAttribute);
            Debug.Assert(attribute != null, "Attribute cannot be null here.");
            
            switch (property.propertyType)
            {
                case SerializedPropertyType.Integer:
                    return property.intValue >= attribute.Value;
                case SerializedPropertyType.Float:
                    return property.floatValue >= attribute.Value;
                default:
                    return false;
            }
        }
    }
}
