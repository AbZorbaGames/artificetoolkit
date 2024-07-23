using AbzorbaExportRoot.CommonLibrariesAndResources.AbzorbaCustomAttributes;
using AbzorbaExportRoot.Editor.ArtificeToolkit.Artifice_CommonResources;
using UnityEditor;
using UnityEngine;

namespace AbzorbaExportRoot.Editor.ArtificeToolkit.Artifice_CustomAttributeDrawers.CustomAttributeDrawer_Validators
{
    [Artifice_CustomAttributeDrawer(typeof(Abz_AssetsOnlyAttribute))]
    public class Artifice_CustomAttributeDrawer_AssetsOnlyAttribute : Artifice_CustomAttributeDrawer_Validator_BASE
    {
        public override string LogMessage { get; } = "Property must be an Asset";
        public override Sprite LogSprite { get; } = Artifice_SCR_CommonResourcesHolder.instance.ErrorIcon;
        public override LogType LogType { get; } = LogType.Error; 

        protected override bool IsApplicableToProperty(SerializedProperty property)
        {
            return property.propertyType == SerializedPropertyType.ObjectReference;
        }

        public override bool IsValid(SerializedProperty property)
        {
            return property.objectReferenceValue == null || AssetDatabase.Contains(property.objectReferenceValue);
        }
    }
}