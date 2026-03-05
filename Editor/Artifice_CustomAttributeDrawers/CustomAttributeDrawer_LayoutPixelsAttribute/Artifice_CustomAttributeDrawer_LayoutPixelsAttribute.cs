using ArtificeToolkit.Attributes;
using UnityEditor;
using UnityEngine.UIElements;

namespace ArtificeToolkit.Editor.Artifice_CustomAttributeDrawers.CustomAttributeDrawer_LayoutPixelsAttribute
{
    [Artifice_CustomAttributeDrawer(typeof(LayoutPixelsAttribute))]
    public class Artifice_CustomAttributeDrawer_LayoutPixelsAttribute : Artifice_CustomAttributeDrawer
    {
        public override VisualElement OnWrapGUI(SerializedProperty property, VisualElement root)
        {
            var attribute = (LayoutPixelsAttribute)Attribute;
            
            var container = new VisualElement();
            container.name = $"Layout Percent ({property.displayName})";
            
            container.style.flexGrow = 0;
            container.style.flexShrink = 0;

            // Apply style
            if(attribute.WidthPixels.HasValue)
                container.style.width = new StyleLength(Length.Pixels(attribute.WidthPixels.Value));
            if(attribute.HeightPixels.HasValue)
                container.style.height = new StyleLength(Length.Pixels(attribute.HeightPixels.Value));
            
            container.Add(root);
            
            return container;
        }
    }
}
