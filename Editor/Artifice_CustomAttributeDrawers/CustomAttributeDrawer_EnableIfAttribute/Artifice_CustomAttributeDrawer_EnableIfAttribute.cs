using System;
using System.Linq;
using ArtificeToolkit.Attributes;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace ArtificeToolkit.Editor.Artifice_CustomAttributeDrawers.CustomAttributeDrawer_EnableIfAttribute
{
    [Artifice_CustomAttributeDrawer(typeof(EnableIfAttribute))]
    public class Artifice_CustomAttributeDrawer_EnableIfAttribute : Artifice_CustomAttributeDrawer, IArtifice_ShouldIncludeInValidation
    {
        private EnableIfAttribute _attribute;
        private SerializedProperty _trackedProperty;
        private VisualElement _targetElem;
        private bool _isReflected;

        public override VisualElement OnWrapGUI(SerializedProperty property, VisualElement root)
        {
            _targetElem = root;
            _attribute = (EnableIfAttribute)Attribute;

            // Try find property (serialized or reflected)
            _trackedProperty = property.FindPropertyInSameScope(_attribute.PropertyName);
            if (_trackedProperty == null)
                _isReflected = true;
        
            // Setup tracking depending on type
            // 1) Serialized Property
            // 2) Reflection
            if (_isReflected == false)
            {
                // Use Unity’s built-in tracker for serialized fields
                UpdateRootVisibility(_trackedProperty);
                var trackerElement = new VisualElement { name = "Tracker Element" };
                _targetElem.Add(trackerElement);
                trackerElement.TrackPropertyValue(_trackedProperty, UpdateRootVisibility);
            }
            else
            {
                // Execute once and every 100 ms
                EvaluateReflectedCondition(property);
                root.schedule.Execute(() => EvaluateReflectedCondition(property))
                    .Every(400);
            }

            return _targetElem;
        }

        private void EvaluateReflectedCondition(SerializedProperty property)
        {
            var target = property.serializedObject.targetObject;
            if (target == null)
                return;

            property.FindReflectedPropertyInSameScope(_attribute.PropertyName, out var returnValue);
        
            var shouldShow = _attribute.Values.Any(value => Artifice_Utilities.AreEqual(returnValue, value));
            SetVisibility(shouldShow);
        }

        private void UpdateRootVisibility(SerializedProperty trackedProperty)
        {
            var trackedValue = trackedProperty.GetTarget<object>();
            var shouldShow = _attribute.Values.Any(v => Artifice_Utilities.AreEqual(trackedValue, v));
            SetVisibility(shouldShow);
        }

        private void SetVisibility(bool visible)
        {
            if (visible)
                _targetElem.RemoveFromClassList("hide");
            else
                _targetElem.AddToClassList("hide");
        }
    
        public bool ShouldIncludeInValidation(SerializedProperty property, CustomAttribute customAttribute)
        {
            var attribute = customAttribute as EnableIfAttribute;
            if (attribute == null)
                throw new ArgumentException();
        
            var trackedProperty = property.FindPropertyInSameScope(attribute.PropertyName);
            if (trackedProperty != null)
            {
                var targetValue = trackedProperty.GetTarget<object>();
                return attribute.Values.Any(value => Artifice_Utilities.AreEqual(targetValue, value));
            }
            else
            {
                property.FindReflectedPropertyInSameScope(attribute.PropertyName, out var returnValue);
                return attribute.Values.Any(v => Artifice_Utilities.AreEqual(returnValue, v));
            }    
        }
    }
}