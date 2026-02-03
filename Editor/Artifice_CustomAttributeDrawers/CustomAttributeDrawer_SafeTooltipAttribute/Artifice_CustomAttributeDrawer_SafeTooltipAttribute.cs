using ArtificeToolkit.Editor.Artifice_CustomAttributeDrawers;
using CustomAttributes;
using UnityEditor;
using UnityEngine.UIElements;

namespace Editor.Artifice_CustomAttributeDrawers.CustomAttributeDrawer_SafeTooltipAttribute
{
    [Artifice_CustomAttributeDrawer(typeof(SafeTooltipAttribute))]
    public class Artifice_CustomAttributeDrawer_SafeTooltipAttribute : Artifice_CustomAttributeDrawer
    {
        public override void OnPropertyBoundGUI(SerializedProperty property, VisualElement propertyField)
        {
            var attribute = (SafeTooltipAttribute)Attribute;
            propertyField.tooltip = attribute.Tooltip;
        }
    }
}
