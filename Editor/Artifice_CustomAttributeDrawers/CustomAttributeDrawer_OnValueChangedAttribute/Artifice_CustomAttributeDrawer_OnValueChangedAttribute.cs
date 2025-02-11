using System.Reflection;
using ArtificeToolkit.Attributes;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace ArtificeToolkit.Editor.Artifice_CustomAttributeDrawers.CustomAttributeDrawer_OnValueChangedAttribute
{
    [Artifice_CustomAttributeDrawer(typeof(OnValueChanged))]
    public class Artifice_CustomAttributeDrawer_OnValueChangedAttribute : Artifice_CustomAttributeDrawer
    {
        public override void OnPropertyBoundGUI(SerializedProperty property, VisualElement propertyField)
        {
            var attribute = (OnValueChanged)Attribute;

            var tracker = new VisualElement();
            propertyField.Add(tracker);

            // Find method
            // Get reference to target object.
            // If property does not have a SerializedProperty parent, its parent is the serializedObject
            var propertyParent = property.FindParentProperty();
            var parentTarget = propertyParent != null
                ? propertyParent.GetTarget<object>()
                : property.serializedObject.targetObject;
            
            var methodInfo = parentTarget.GetType().GetMethod(
                attribute.MethodName,
                BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic
            );

            // Subscribe to track
            tracker.TrackPropertyValue(property, changed => { methodInfo.Invoke(parentTarget, null); });
        }
    }
}