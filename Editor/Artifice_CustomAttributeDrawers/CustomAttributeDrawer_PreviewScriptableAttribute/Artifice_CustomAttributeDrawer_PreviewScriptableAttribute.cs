using ArtificeToolkit.Attributes;
using ArtificeToolkit.Editor.Resources;
using ArtificeToolkit.Editor.VisualElements;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace ArtificeToolkit.Editor.Artifice_CustomAttributeDrawers.CustomAttributeDrawer_PreviewScriptableAttribute
{
    [Artifice_CustomAttributeDrawer(typeof(PreviewScriptableAttribute))]
    public class Artifice_CustomAttributeDrawer_PreviewScriptableAttribute : Artifice_CustomAttributeDrawer, IArtifice_Persistence
    {
        private VisualElement _wrapper;
        private VisualElement _header;
        private VisualElement _expandedContainer;
        private Artifice_VisualElement_ToggleButton _toggle;

        private bool _state; // Open or closed
        private SerializedProperty _property;

        public override VisualElement OnWrapGUI(SerializedProperty property, VisualElement root)
        {
            _property = property;
            _state = false;
            
            _wrapper = new VisualElement();
            _wrapper.name = "PreviewScriptable Wrapper";
            _wrapper.styleSheets.Add(Artifice_Utilities.GetStyle(GetType()));
            _wrapper.AddToClassList("previewScriptable-container");
            _wrapper.AddToClassList("previewScriptable-close");

            _header = new VisualElement();
            _header.AddToClassList("align-horizontal");
            _wrapper.Add(_header);
            
            // Add default UI
            root.style.width = Length.Percent(100);
            _header.Add(root);

            // Add expanded icon
            _toggle = new Artifice_VisualElement_ToggleButton("Expand", Artifice_SCR_CommonResourcesHolder.instance.MagnifyingGlassIcon, _state);
            _toggle.AddToClassList("expand-toggle");
            _toggle.OnButtonPressed += UpdateExpandedView;
            _header.Add(_toggle);

            // Add expanded container
            _expandedContainer = new VisualElement();
            _expandedContainer.AddToClassList("expanded-view-container");
            _wrapper.Add(_expandedContainer);

            _wrapper.TrackPropertyValue(property, OnPropertyValueChanged);
            
            LoadPersistedData();
            
            return _wrapper;
        }
        
        /// <summary> Uses Artifice Drawer to draw an expanded view of the <see cref="SerializedProperty"/></summary>
        private void DrawExpandedView(SerializedProperty property)
        {
            _expandedContainer.Clear();
            var drawer = new ArtificeDrawer();
            
            var target = property.GetTarget<object>();

            _expandedContainer.style.marginLeft = 10 * (property.depth + 1); 
            
            // If null return
            if (target == null)
                return;
        
            // If wrong type return with error
            if (target.GetType().IsSubclassOf(typeof(ScriptableObject)) == false)
            {
                var infoBox = new Artifice_VisualElement_InfoBox("Attribute only applies for scriptable objects.", Artifice_SCR_CommonResourcesHolder.instance.ErrorIcon);
                _expandedContainer.Add(infoBox);
                return;
            }
            
            // Else, parse as serialized object
            var serializedObject = new SerializedObject((ScriptableObject)target);
            foreach (var childProperty in serializedObject.GetIterator().GetVisibleChildren())
            {
                if (childProperty.name == "m_Script")
                    continue;
                _expandedContainer.Add(drawer.CreatePropertyGUI(childProperty));
            }
        }

        /// <summary> Updates styles based on expanded state </summary>
        private void UpdateExpandedView(bool value)
        {
            _state = value;
            switch (value)
            {
                case true:
                    _toggle.Text = "Shrink";
                    
                    _header.RemoveFromClassList("previewScriptable-header-disabled");
                    _header.AddToClassList("previewScriptable-header-enabled");
                    
                    _wrapper.RemoveFromClassList("previewScriptable-close");
                    _wrapper.AddToClassList("previewScriptable-open");
                    
                    DrawExpandedView(_property);
                    
                    break;
                case false:
                    _toggle.Text = "Expand";
                    
                    _wrapper.RemoveFromClassList("previewScriptable-open");
                    
                    _header.RemoveFromClassList("previewScriptable-header-open");
                    _header.AddToClassList("previewScriptable-header-disabled");
                    
                    _wrapper.RemoveFromClassList("previewScriptable-open");
                    _wrapper.AddToClassList("previewScriptable-close");
                    
                    _expandedContainer.Clear();
                    
                    break;
            }
            
            SavePersistedData();
        }
        
        /// <summary> Callback for property change invocation</summary>
        private void OnPropertyValueChanged(SerializedProperty property)
        {
            DrawExpandedView(property);
            UpdateExpandedView(_state);
        }

        #region Artifice View Persistency Pattern
        
        public string ViewPersistenceKey
        {
            get
            {
                if (_property.objectReferenceValue != null)
                    return _property.objectReferenceValue.name;
                
                return "-1";
            }
            set { }
        }

        public void SavePersistedData()
        {
            var stateString = _state ? "True" : "False";
            Artifice_SCR_PersistedData.instance.SaveData(ViewPersistenceKey, "isOpen", stateString);
        }

        public void LoadPersistedData()
        {
            var value = Artifice_SCR_PersistedData.instance.LoadData(ViewPersistenceKey, "isOpen");
            _state = value == "True";
            _toggle.SetState(_state);
            UpdateExpandedView(_state);
        }
        
        #endregion
    }
}
