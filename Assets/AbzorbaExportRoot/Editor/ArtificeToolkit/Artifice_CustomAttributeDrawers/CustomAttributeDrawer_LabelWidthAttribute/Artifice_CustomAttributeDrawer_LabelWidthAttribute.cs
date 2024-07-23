using AbzorbaExportRoot.CommonLibrariesAndResources.AbzorbaCustomAttributes;
using UnityEditor;
using UnityEngine.UIElements;

namespace AbzorbaExportRoot.Editor.ArtificeToolkit.Artifice_CustomAttributeDrawers.CustomAttributeDrawer_LabelWidthAttribute
{
    [Artifice_CustomAttributeDrawer(typeof(Abz_LabelWidthAttribute))]
    public class Artifice_CustomAttributeDrawer_LabelWidthAttribute : Artifice_CustomAttributeDrawer
    {
        public override void OnPropertyBoundGUI(SerializedProperty property, VisualElement propertyField)
        {
            // If control uses PropertyField, it will construct after bind which happens before attaching to panel.
            // So wait for the event until the logic is called!
            var attribute = (Abz_LabelWidthAttribute)Attribute;       
            var labelFields = propertyField.Query<Label>().ToList();
            
            foreach (var label in labelFields)
            {
                label.style.maxWidth = attribute.Width;
                label.style.minWidth = attribute.Width;
            }            
        }
    }
}
