using System.Linq;
using ArtificeToolkit.Attributes;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace ArtificeToolkit.Editor.Artifice_CustomAttributeDrawers.CustomAttributeDrawer_EnableIfAttribute
{
    /// <summary> Custom VisualAttribute drawer for <see cref="EnableIfAttribute"/> </summary>
    [Artifice_CustomAttributeDrawer(typeof(EnableIfAttribute))]
    public class Artifice_CustomAttributeDrawer_EnableIfAttribute : Artifice_CustomAttributeDrawer
    {
        #region FIELDS

        private EnableIfAttribute _attribute;
        private SerializedProperty _trackedProperty;
        private VisualElement _targetElem;
       
        #endregion
        
        /* Get reference to VisualElement to target */
        public override VisualElement OnWrapGUI(SerializedProperty property, VisualElement root)
        {
            _targetElem = root;
            
            _attribute = (EnableIfAttribute)Attribute;
            
            // Set Data tracked property
            _trackedProperty = property.FindPropertyInSameScope(_attribute.PropertyName);
            if (_trackedProperty == null)
                Debug.LogWarning("Cannot find property with name " + _attribute.PropertyName);

            UpdateRootVisibility(_trackedProperty);
            
            var trackerElement = new VisualElement();
            trackerElement.name = "Tracker Element";
            trackerElement.tooltip = "Used only for TrackPropertyValue method";
            _targetElem.Add(trackerElement);
            trackerElement.TrackPropertyValue(_trackedProperty, UpdateRootVisibility);
            
            return _targetElem;
        }
        
        /* Executes logic on changing visibility */
        private void UpdateRootVisibility(SerializedProperty property)
        {
            var trackedValue = property.GetTarget<object>();
            
            if(_attribute.Values.Any(value => Artifice_Utilities.AreEqual(trackedValue, value)))
                _targetElem.RemoveFromClassList("hide");
            else
                _targetElem.AddToClassList("hide");
        }
    }
}