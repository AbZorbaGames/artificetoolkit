using ArtificeToolkit.Editor.Resources;
using ArtificeToolkit.Editor.VisualElements;
using CustomAttributes;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace ArtificeToolkit.Editor.Artifice_CustomAttributeDrawers.CustomAttributeDrawer_InlinePreviewAttribute
{
    [Artifice_CustomAttributeDrawer(typeof(InlinePreviewAttribute))]
    public class Artifice_CustomAttributeDrawer_InlinePreviewAttribute : Artifice_CustomAttributeDrawer
    {
        private VisualElement _wrapper;
        private VisualElement _header;
        private VisualElement _expandedContainer;
        private Artifice_VisualElement_ToggleButton _toggle;

        private bool _state; // Open or closed
        private SerializedProperty _property;
        private ArtificeDrawer _artificeDrawer;

        private UnityEditor.Editor _cachedNativeEditor = null;

        public override VisualElement OnWrapGUI(SerializedProperty property, VisualElement root)
        {
            // Allow Object Reference property types only. Non-breaking error and return otherwise.
            if (property.propertyType != SerializedPropertyType.ObjectReference)
            {
                var container = new VisualElement();

                container.Add(new Artifice_VisualElement_InfoBox(
                    "",
                    Artifice_SCR_CommonResourcesHolder.instance.ErrorIcon
                ));
                container.Add(root);

                return container;
            }
            
            _artificeDrawer = new ArtificeDrawer();
            _artificeDrawer.SetSerializedPropertyFilter(p => p.name != "m_Script");

            _property = property;
            _state = false;

            _wrapper = new();
            _wrapper.name = "Inline Preview Wrapper";
            _wrapper.styleSheets.Add(Artifice_Utilities.GetStyle(GetType()));
            _wrapper.AddToClassList("inline-preview-container");
            _wrapper.AddToClassList("inline-preview-close");

            _header = new VisualElement();
            _header.AddToClassList("align-horizontal");
            _wrapper.Add(_header);

            // Add default UI to header.
            root.style.width = Length.Percent(100);
            _header.Add(root);

            // Add expanded icon
            _toggle = new Artifice_VisualElement_ToggleButton(
                "Expand",
                Artifice_SCR_CommonResourcesHolder.instance.MagnifyingGlassIcon,
                _state
            );
            _toggle.AddToClassList("expand-toggle");
            _toggle.OnButtonPressed += UpdateExpandedView;
            _header.Add(_toggle);

            // Add expanded container
            _expandedContainer = new VisualElement();
            _expandedContainer.AddToClassList("expanded-view-container");
            _wrapper.Add(_expandedContainer);

            // Add tracking to check changes of object references to update.
            _wrapper.TrackPropertyValue(property, OnPropertyValueChanged);

            LoadPersistedData();
            SubscribeForCleanUp();

            return _wrapper;
        }

        /// <summary> Uses Artifice Drawer to draw an expanded view of the <see cref="SerializedProperty"/></summary>
        private void DrawExpandedView(SerializedProperty property)
        {
            _expandedContainer.Clear();
            _expandedContainer.style.marginLeft = 10 * (property.depth + 1);

            var target = property.objectReferenceValue;

            // If null return
            if (target == null)
            {
                var nullLabel = new Label("Value is not set.");
                nullLabel.AddToClassList("null-label");
                _expandedContainer.Add(nullLabel);
                return;
            }

            // Check whether we require a native unity editor.
            var requiresNativeEditor = target is Material or Texture ||
                                       target is AudioClip ||
                                       target is Shader ||
                                       target is ComputeShader;

            if (!requiresNativeEditor)
            {
                // Use artifice inspector GUI
                var serializedObject = new SerializedObject(target);
                _expandedContainer.Add(_artificeDrawer.CreateInspectorGUI(serializedObject));
                _artificeDrawer.SetArtificeIndicatorVisibility(false);
            }
            else
            {
                //  Use Native Unity Editor (For Complex Targeted Assets)
                UnityEditor.Editor.CreateCachedEditor(target, null, ref _cachedNativeEditor);

                var editorContainer = new IMGUIContainer(() =>
                {
                    // Important:
                    //  Guard against invalid width.
                    // If width is ~0, MaterialEditor skips Layout phase but crashes on Repaint.
                    // We must ensure we have valid space before letting it run.
                    var width = EditorGUIUtility.currentViewWidth;
                    if (width is <= 10 or float.NaN) 
                        return;
        
                    // Additional safety: Ensure we aren't in a weird event state
                    if (Event.current.type == EventType.Used)
                        return;

                    if (_cachedNativeEditor != null && _cachedNativeEditor.target != null)
                    {
                        // 3. Wrap in Vertical to allow MaterialEditor to reserve its own space safely
                        GUILayout.BeginVertical();
            
                        // DrawHeader is risky inside nested UIs, but safe if width > 10
                        _cachedNativeEditor.DrawHeader(); 
            
                        _cachedNativeEditor.OnInspectorGUI();
            
                        if (_cachedNativeEditor.HasPreviewGUI())
                        {
                            GUILayout.Space(10);
                
                            var previewRect = GUILayoutUtility.GetRect(width, 128);
                
                            // Draw the actual background (dark grey box)
                            if (Event.current.type == EventType.Repaint)
                                GUI.Box(previewRect, "", EditorStyles.helpBox);

                            // Ask the editor to draw the interactive preview inside that rect
                            _cachedNativeEditor.OnInteractivePreviewGUI(previewRect, GUIStyle.none);
                        }

                        GUILayout.EndVertical();
                    }
                });

                // Optional: Allow the container to expand to fit the IMGUI content
                editorContainer.style.flexGrow = 1; 

                _expandedContainer.Add(editorContainer);
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

                    _header.RemoveFromClassList("inline-preview-header-disabled");
                    _header.AddToClassList("inline-preview-header-enabled");

                    _wrapper.RemoveFromClassList("inline-preview-close");
                    _wrapper.AddToClassList("inline-preview-open");

                    DrawExpandedView(_property);

                    break;
                case false:
                    _toggle.Text = "Expand";

                    _wrapper.RemoveFromClassList("inline-preview-open");

                    _header.RemoveFromClassList("inline-preview-header-open");
                    _header.AddToClassList("inline-preview-header-disabled");

                    _wrapper.RemoveFromClassList("inline-preview-open");
                    _wrapper.AddToClassList("inline-preview-close");

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

        private void SubscribeForCleanUp()
        {
            _expandedContainer.RegisterCallback<DetachFromPanelEvent>(evt =>
            {
                if (_cachedNativeEditor != null)
                {
                    Object.DestroyImmediate(_cachedNativeEditor);
                    _cachedNativeEditor = null;
                }
            });
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