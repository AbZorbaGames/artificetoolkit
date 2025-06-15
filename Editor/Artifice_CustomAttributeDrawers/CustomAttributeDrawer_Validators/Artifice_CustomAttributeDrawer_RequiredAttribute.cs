using ArtificeToolkit.Attributes;
using ArtificeToolkit.Editor.Resources;
using UnityEditor;
using UnityEngine;

namespace ArtificeToolkit.Editor.Artifice_CustomAttributeDrawers.CustomAttributeDrawer_Validators
{
    [Artifice_CustomAttributeDrawer(typeof(RequiredAttribute))]
    public class Artifice_CustomAttributeDrawer_RequiredAttribute : Artifice_CustomAttributeDrawer_Validator_BASE
    {
        public override string LogMessage { get; } = "Property is required.";
        public override Sprite LogSprite { get; } = Artifice_SCR_CommonResourcesHolder.instance.ErrorIcon;
        public override LogType LogType { get; } = LogType.Error;

        protected override bool IsApplicableToProperty(SerializedProperty property)
        {
            return property.propertyType is SerializedPropertyType.ObjectReference or SerializedPropertyType.ManagedReference;
        }

        public override bool IsValid(SerializedProperty property)
        {
            if(property.propertyType == SerializedPropertyType.ObjectReference)
                return property.objectReferenceValue != null;
            else if (property.propertyType == SerializedPropertyType.ManagedReference)
                return property.managedReferenceValue != null;
            else
                return false;
        }
    }
}
