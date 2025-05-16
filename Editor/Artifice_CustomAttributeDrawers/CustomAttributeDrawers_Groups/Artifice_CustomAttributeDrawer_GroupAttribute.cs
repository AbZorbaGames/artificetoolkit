using System;
using ArtificeToolkit.Attributes;
using UnityEditor;
using UnityEngine.UIElements;

namespace ArtificeToolkit.Editor.Artifice_CustomAttributeDrawers.CustomAttributeDrawers_Groups
{
    [Artifice_CustomAttributeDrawer(typeof(GroupAttribute))]
    public abstract class Artifice_CustomAttributeDrawer_GroupAttribute : Artifice_CustomAttributeDrawer
    {
        protected virtual Type VisualElementType { get; } = null;

        public Type Get_VisualElementType() => VisualElementType;
        
        /* Use OnPrePropertyGUI to initialize nested container order and types */
        public override VisualElement OnPrePropertyGUI(SerializedProperty property)
        {
            CreateOrGetContainer(property);
            return null;
        }
        
        /* All inherited members should use this method for drawing unless they know their stuff */
        public override VisualElement OnWrapGUI(SerializedProperty property, VisualElement root)
        {
            var groupContainer = CreateOrGetContainer(property);
            if (root == groupContainer) // If this is removed, it may cause a GroupContainer to add its self, causing stack overflow.
                return root;

            // Axiom: VisualElement root will use the name of propertyPath. On any build, this ensures to have unique copies only! 
            if (groupContainer.Query<VisualElement>(name: root.name).First() == null)
            {
                groupContainer.name = ((GroupAttribute)Attribute).GroupName;
                root.AddToClassList("group-child");
                groupContainer.Add(root);
            }
            
            return groupContainer;
        }

        /* Inherited methods should override this to return the instance of their own reflected VisualElement type. */
        protected virtual Artifice_VisualElement_Group CreateOrGetContainer(SerializedProperty property)
        {
            var attribute = (GroupAttribute)Attribute;
            var groupTuple = Artifice_CustomAttributeUtility_GroupsHolder.Instance.Get(property, attribute.GroupName, VisualElementType);
            groupTuple.lastElem.LoadPersistedData();
            groupTuple.lastElem.SetGroupColor(attribute.GroupColor);
            return groupTuple.firstElem;
        }
    }
}