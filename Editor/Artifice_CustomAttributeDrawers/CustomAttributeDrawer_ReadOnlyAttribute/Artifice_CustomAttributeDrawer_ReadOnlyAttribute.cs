using ArtificeToolkit.Attributes;
using UnityEditor;
using UnityEngine.UIElements;

namespace ArtificeToolkit.Editor.Artifice_CustomAttributeDrawers.CustomAttributeDrawer_ReadOnlyAttribute
{
    [Artifice_CustomAttributeDrawer(typeof(ReadOnlyAttribute))]
    public class Artifice_CustomAttributeDrawer_ReadOnlyAttribute : Artifice_CustomAttributeDrawer
    {
        // Set the property field enabled to false.
        public override void OnPropertyBoundGUI(SerializedProperty property, VisualElement propertyField)
        {
            if (propertyField is Artifice_VisualElement_AbstractListView listView)
            {
                listView.Set_Enabled(false);
            }
            else
            {
                propertyField.SetEnabled(false);
            }
        }
    }
}
