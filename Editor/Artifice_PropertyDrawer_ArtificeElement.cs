using UnityEditor;
using UnityEngine.UIElements;

namespace ArtificeToolkit.Editor
{
    [CustomPropertyDrawer(typeof(ArtificeElement))]
    public class Artifice_PropertyDrawer_ArtificeElement : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            // An empty visual element
            return new VisualElement();
        }
    }
}
