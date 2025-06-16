using ArtificeToolkit.Attributes;
using UnityEditor;
using UnityEngine.UIElements;

namespace ArtificeToolkit.Editor.Artifice_CustomAttributeDrawers.CustomAttributeDrawer_SpaceAttribute
{
    [Artifice_CustomAttributeDrawer(typeof(SpaceAttribute))]
    public class Artifice_CustomAttributeDrawer_SpaceAttribute : Artifice_CustomAttributeDrawer
    {
        public override VisualElement OnWrapGUI(SerializedProperty property, VisualElement root)
        {
            var attribute = (SpaceAttribute)Attribute;
            var wrapper = new VisualElement();
            wrapper.name = "Space Wrapper";
            wrapper.Add(root);
            wrapper.style.marginTop = attribute.ValueTop;
            wrapper.style.marginBottom = attribute.ValueBottom;
            wrapper.style.marginLeft = attribute.ValueLeft;
            wrapper.style.marginRight = attribute.ValueRight;
            
            return wrapper;
        }
    }
}
