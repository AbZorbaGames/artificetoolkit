using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using RangeAttribute = ArtificeToolkit.Attributes.RangeAttribute;

namespace ArtificeToolkit.Editor.Artifice_CustomAttributeDrawers.CustomAttributeDrawer_RangeAttribute
{
    [Artifice_CustomAttributeDrawer(typeof(RangeAttribute))]
    public class Artifice_CustomAttributeDrawer_RangeAttribute : Artifice_CustomAttributeDrawer
    {
        public override bool IsReplacingPropertyField { get; } = true;

        public override VisualElement OnPropertyGUI(SerializedProperty property)
        {
            // Cast attribute
            var attribute = (RangeAttribute)Attribute;

            var container = new VisualElement();
            container.styleSheets.Add(Artifice_Utilities.GetStyle(GetType()));
            container.name = "RangeField";
            container.AddToClassList("main-container");

            // Create label
            var label = new Label(property.displayName);
            label.AddToClassList("unity-text-element");
            label.AddToClassList("unity-label");
            label.AddToClassList("unity-base-field__label");
            label.AddToClassList("unity-base-text-field__label");
            label.AddToClassList("unity-property-field__label");
            container.Add(label);
            
            // Create slider, bind and return
            var slider = property.propertyType == SerializedPropertyType.Float ? 
                (BindableElement)new Slider(attribute.MinValue, attribute.MaxValue) : new SliderInt((int)attribute.MinValue, (int)attribute.MaxValue);
            slider.BindProperty(property);
            slider.AddToClassList("slider");
            container.Add(slider);
            
            // IntegerField's and FloatField's base class requires generic which cannot be done in this fashion.
            // So use an IF statement for now :(
            // Create input text box for value
            if (property.propertyType == SerializedPropertyType.Float)
            {
                var floatField = new FloatField();
                floatField.BindProperty(property);
                floatField.AddToClassList("input-field");
                container.Add(floatField);
            }
            else
            {
                var integerField = new IntegerField();
                integerField.BindProperty(property);
                integerField.AddToClassList("input-field");
                container.Add(integerField);
            }

            // Track property and clamp it based on range values
            container.TrackPropertyValue(property, OnPropertyValueChange);
            
            return container;
        }

        private void OnPropertyValueChange(SerializedProperty property)
        {
            var attribute = (RangeAttribute)Attribute;
            
            property.serializedObject.Update();
            
            switch (property.propertyType)
            {
                case SerializedPropertyType.Integer:
                    property.intValue = Mathf.Clamp(property.intValue, (int)attribute.MinValue, (int)attribute.MaxValue);
                    break;
                case SerializedPropertyType.Float:
                    property.floatValue = Mathf.Clamp(property.floatValue, attribute.MinValue, attribute.MaxValue);
                    break;
                default:
                    throw new ArgumentException($"MinValueAttribute not supported for type {property.propertyType}");
            }

            property.serializedObject.ApplyModifiedProperties();
        }
    }
}
