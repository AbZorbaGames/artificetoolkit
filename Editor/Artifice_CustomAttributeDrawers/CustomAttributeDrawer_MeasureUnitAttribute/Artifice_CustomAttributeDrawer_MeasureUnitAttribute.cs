using ArtificeToolkit.Attributes;
using UnityEditor;
using UnityEngine.UIElements;

namespace ArtificeToolkit.Editor.Artifice_CustomAttributeDrawers.CustomAttributeDrawer_MeasureUnitAttribute
{
    [Artifice_CustomAttributeDrawer(typeof(MeasureUnitAttribute))]
    public class Artifice_CustomAttributeDrawer_MeasureUnitAttribute : Artifice_CustomAttributeDrawer 
    {
        public override VisualElement OnWrapGUI(SerializedProperty property, VisualElement root)
        {
            var wrapper = new VisualElement();
            wrapper.name = "MeasureUnit Wrapper";
            wrapper.styleSheets.Add(Artifice_Utilities.GetStyle(GetType()));
            wrapper.AddToClassList("measure-unit-holder");
            wrapper.Add(root);

            root.AddToClassList("measure-unit-root");
            
            var attribute = (MeasureUnitAttribute)Attribute;
            var label = new Label($"{attribute.UnitName}");
            label.AddToClassList("measure-unit-label");
            wrapper.Add(label);

            return wrapper;
        }
    }
}