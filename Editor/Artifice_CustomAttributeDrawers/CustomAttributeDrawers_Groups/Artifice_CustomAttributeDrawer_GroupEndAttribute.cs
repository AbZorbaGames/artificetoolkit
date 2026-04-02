using UnityEditor;
using UnityEngine.UIElements;

namespace ArtificeToolkit.Editor.Artifice_CustomAttributeDrawers.CustomAttributeDrawers_Groups
{
    [Artifice_CustomAttributeDrawer(typeof(GroupEndAttribute))]
    public class Artifice_CustomAttributeDrawer_GroupEndAttribute : Artifice_CustomAttributeDrawer
    {
        public override VisualElement OnPrePropertyGUI(SerializedProperty property)
        {
            Artifice_CustomAttributeUtility_GroupsHolder.Instance.PopOpenGroup();
            return base.OnPrePropertyGUI(property);
        }
    }
}