using UnityEditor;
using UnityEngine.UIElements;

namespace ArtificeToolkit.Editor.Artifice_CustomAttributeDrawers.CustomAttributeDrawers_Groups
{
    [Artifice_CustomAttributeDrawer(typeof(GroupEndAttribute))]
    public class Artifice_CustomAttributeDrawer_GroupEndAttribute : Artifice_CustomAttributeDrawer_GroupAttribute
    {
        public override VisualElement OnPrePropertyGUI(SerializedProperty property)
        {
            GroupsHolder.PopOpenGroup();
            return null;
        }

        public override VisualElement OnWrapGUI(SerializedProperty property, VisualElement root)
        {
            return root;
        }
    }
}
