using System.Linq;
using ArtificeToolkit.Attributes;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace ArtificeToolkit.Editor.Artifice_CustomAttributeDrawers.CustomAttributeDrawer_InfoBoxAttribute
{
    [Artifice_CustomAttributeDrawer(typeof(ConditionalInfoBoxAttribute))]
    public class Artifice_CustomAttributeDrawer_ConditionalInfoBoxAttribute : Artifice_CustomAttributeDrawer_InfoBoxAttribute
    {
        #region FIELDS

        private ConditionalInfoBoxAttribute _attribute;
        private SerializedProperty _trackedProperty;
        private VisualElement _infoBoxElem;
        
        #endregion

        public override VisualElement OnWrapGUI(SerializedProperty property, VisualElement root)
        {
            var wrapper = new VisualElement();
            wrapper.name = "ConditionalInfoBox Wrapper";
            
            _attribute = (ConditionalInfoBoxAttribute)Attribute;
            
            // Set Data tracked property
            _trackedProperty = property.FindPropertyInSameScope(_attribute.PropertyName);
            if (_trackedProperty == null)
                Debug.LogWarning("Cannot find property with name " + _attribute.PropertyName); 
            
            // Add info box
            _infoBoxElem = base.OnWrapGUI(property, null);
            _infoBoxElem.TrackPropertyValue(_trackedProperty, UpdateRootVisibility);
            UpdateRootVisibility(_trackedProperty);
            wrapper.Add(_infoBoxElem);
    
            // Add root
            wrapper.Add(root);
            
            return wrapper;
        }
        
        /* Executes logic on changing visibility */
        private void UpdateRootVisibility(SerializedProperty property)
        {
            var trackedValue = property.GetTarget<object>();
            
            if(_attribute.Values.Any(value => Artifice_Utilities.AreEqual(trackedValue, value)))
                _infoBoxElem.RemoveFromClassList("hide");
            else
                _infoBoxElem.AddToClassList("hide");
        }
        
    }
}
