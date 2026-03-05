using ArtificeToolkit.Attributes;
using UnityEditor;
using UnityEngine.UIElements;

namespace ArtificeToolkit.Editor.Artifice_CustomAttributeDrawers.CustomAttributeDrawer_LayoutPercentAttribute
{
    [Artifice_CustomAttributeDrawer(typeof(LayoutPercentAttribute))]
    public class Artifice_CustomAttributeDrawer_LayoutPercentAttribute : Artifice_CustomAttributeDrawer
    {
        public override VisualElement OnWrapGUI(SerializedProperty property, VisualElement root)
        {
            var attribute = (LayoutPercentAttribute)Attribute;
            
            var container = new VisualElement();
            container.name = $"Layout Percent ({property.displayName})";
            
            container.style.flexGrow = 0;
            container.style.flexShrink = 0;

            // Apply style
            if(attribute.WidthPercent.HasValue)
                container.style.width = new StyleLength(Length.Percent(attribute.WidthPercent.Value));
            if(attribute.HeightPercent.HasValue)
                container.style.height = new StyleLength(Length.Percent(attribute.HeightPercent.Value));
            
            container.Add(root);
            
            return container;
        }
    }
}
