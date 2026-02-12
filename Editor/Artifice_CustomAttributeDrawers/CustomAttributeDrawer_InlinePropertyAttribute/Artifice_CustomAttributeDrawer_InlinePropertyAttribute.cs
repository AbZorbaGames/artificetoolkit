using ArtificeToolkit.Editor.Resources;
using ArtificeToolkit.Editor.VisualElements;
using CustomAttributes;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace ArtificeToolkit.Editor.Artifice_CustomAttributeDrawers.CustomAttributeDrawer_InlinePropertyAttribute
{
    [Artifice_CustomAttributeDrawer(typeof(InlinePropertyAttribute))]
    public class Artifice_CustomAttributeDrawer_InlinePropertyAttribute : Artifice_CustomAttributeDrawer
    {
        public override bool IsReplacingPropertyField { get; } = true;

        public override VisualElement OnPropertyGUI(SerializedProperty property)
        {
            if (property.hasVisibleChildren == false)
            {
                var container = new VisualElement();
                var errorMessage = $"InlineProperty can only be applied to properties with visible children. {property.displayName} is not valid.";

                Debug.LogWarning(errorMessage);
                container.Add(new Artifice_VisualElement_InfoBox(errorMessage, Artifice_SCR_CommonResourcesHolder.instance.ErrorIcon));
                container.Add(new PropertyField(property));
                
                return container;
            }
            
            var attribute = (InlinePropertyAttribute)Attribute;
            var type = attribute.Type;
            
            var mainContainer = new VisualElement();
            mainContainer.styleSheets.Add(Artifice_Utilities.GetStyle(typeof(Artifice_CustomAttributeDrawer_InlinePropertyAttribute)));
            mainContainer.AddToClassList("inline-property-main-container");
            mainContainer.name = $"{property.name} Inline Property Container";
            
            if(type is InlinePropertyStyle.WithoutTitleBorderless)
                mainContainer.AddToClassList("inline-property-borderless");
            
            if(type is InlinePropertyStyle.WithTitle)
                mainContainer.Add(BuildUI_Header(property));
            mainContainer.Add(BuildUI_Children(property));
            
            return mainContainer;
        }

        private VisualElement BuildUI_Header(SerializedProperty property)
        {
            var container = new VisualElement();
            container.AddToClassList("inline-property-header-container");

            var label = new Label(property.displayName);
            label.AddToClassList("inline-property-header");
            container.Add(label);
           
            return container;   
        }

        private VisualElement BuildUI_Children(SerializedProperty property)
        {
            var container = new VisualElement();
            container.AddToClassList("inline-property-children-container");
            
            var artificeDrawer = new ArtificeDrawer();

            // Create content
            foreach (var childProperty in property.GetVisibleChildren())
                container.Add(artificeDrawer.CreatePropertyGUI(childProperty));
            
            return container;
        }
    }
}