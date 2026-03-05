using ArtificeToolkit.Attributess;
using ArtificeToolkit.Editor.Resources;
using ArtificeToolkit.Editor.VisualElements;
using UnityEditor;
using UnityEngine.UIElements;

namespace ArtificeToolkit.Editor.Artifice_CustomAttributeDrawers.CustomAttributeDrawer_ListElementNameAttribute
{
    [Artifice_CustomAttributeDrawer(typeof(ListElementNameAttribute))]
    public class Artifice_CustomAttributeDrawer_ListElementNameAttribute : Artifice_CustomAttributeDrawer
    {   
        /*
         * No implementation here. Drawer added just for consistency and to follow pattern.
         *
         * This element will be applied and checked internally through the Artifice_VisualElement_AbstractListView
         */

        public override VisualElement OnPrePropertyGUI(SerializedProperty property)
        {
            if (property.isArray == false)
                return new Artifice_VisualElement_InfoBox("ListElementName can only be applied to lists or arrays.", Artifice_SCR_CommonResourcesHolder.instance.ErrorIcon);
            
            return null;
        }
    }
}
