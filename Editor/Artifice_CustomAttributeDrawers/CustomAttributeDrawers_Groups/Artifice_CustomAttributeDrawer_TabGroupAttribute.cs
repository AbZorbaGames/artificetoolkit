using System;
using ArtificeToolkit.Attributes;
using UnityEditor;

namespace ArtificeToolkit.Editor.Artifice_CustomAttributeDrawers.CustomAttributeDrawers_Groups
{
    [Artifice_CustomAttributeDrawer(typeof(TabGroupAttribute))]
    public class Artifice_CustomAttributeDrawer_TabGroupAttribute : Artifice_CustomAttributeDrawer_GroupAttribute
    {
        protected override Type VisualElementType { get; } = typeof(Artifice_VisualElement_TabGroup);

        protected override (Artifice_VisualElement_Group firstElem, Artifice_VisualElement_Group lastElem) CreateOrGetContainer(SerializedProperty property)
        {
            var attribute = (TabGroupAttribute)Attribute;

            var groupTuple = Artifice_CustomAttributeUtility_GroupsHolder.Instance.Get(property, attribute.GroupName, VisualElementType);
            
            var tabGroup = (Artifice_VisualElement_TabGroup)groupTuple.lastElem;
            tabGroup.SetGroupColor(attribute.GroupColor);
            if(tabGroup.ContainsTab(attribute.TabSection) == false)
                tabGroup.CreateTab(attribute.TabSection);
            tabGroup.SelectTab(attribute.TabSection);            
            
            // Reset to first tab always
            tabGroup.LoadPersistedData();
            
            return groupTuple;
        }
    }
}