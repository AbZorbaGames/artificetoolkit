using System.Xml;
using ArtificeToolkit.Editor.Resources;
using CustomAttributes;
using UnityEditor;
using UnityEngine;

namespace ArtificeToolkit.Editor.Artifice_CustomAttributeDrawers.CustomAttributeDrawer_Validators
{
    [Artifice_CustomAttributeDrawer(typeof(ValidateUxmlAttribute))]
    public class Artifice_CustomAttributeDrawer_ValidateUxmlAttribute : Artifice_CustomAttributeDrawer_Validator_BASE
    {
        public override string LogMessage { get; } = "Value is not in UXML format.";
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
                var xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(property.stringValue);
                return true;
            }
            catch (XmlException)
            {
                return false;
            }
        }
    }
}
