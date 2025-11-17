using Artifice.Editor;
using ArtificeToolkit.Attributes;
using ArtificeToolkit.Editor.Artifice_InspectorHeader;
using UnityEngine;
using UnityEngine.UIElements;
using SpaceAttribute = ArtificeToolkit.Attributes.SpaceAttribute;

namespace ArtificeToolkit.Editor
{
    public class Artifice_EditorWindow_Wizard : ArtificeEditorWindow
    {
        #region FIELDS

        private enum ToggleOption
        {
            On, Off    
        }

        [SerializeField, Space(20), Title("Enable Artifice"), EnumToggle, HideLabel, OnValueChanged(nameof(OnArtificeEnabledChange))]
        private ToggleOption isArtificeEnabled;
        
        [SerializeField, Space(20), Title("Validator Toolbar"), EnumToggle, HideLabel, OnValueChanged(nameof(OnValidatorToolbarEnabledChanged))]
        private ToggleOption shouldUseValidatorToolbar;
        
        [SerializeField, Space(20), Title("Inspector Header"), EnumToggle, HideLabel, OnValueChanged(nameof(OnInspectorHeaderEnabledChanged))]
        private ToggleOption shouldUseInspectorHeader;
        
        #endregion
        
        public static void ShowWindow()
        {
            var wnd = GetWindow(typeof(Artifice_EditorWindow_Wizard));
            wnd.titleContent.text = "Artifice Wizard";
        }

        protected override void CreateGUI()
        {
            var root = rootVisualElement;
            root.styleSheets.Add(Artifice_Utilities.GetStyle(GetType()));
            root.styleSheets.Add(Artifice_Utilities.GetGlobalStyle());

            var title = new Label("Artifice Toolkit");
            title.AddToClassList("window-title");

            // Add explanation toggles.
            
            // Make sure it expands horizontally
            title.style.flexGrow = 1;
            
            root.Add(title);
            
            // Initialize values
            isArtificeEnabled = ToToggleOption(Artifice_Utilities.ArtificeDrawerEnabled);
            shouldUseInspectorHeader = ToToggleOption(Artifice_InspectorHeader_Main.IsEnabled());
            
            // Render the FIELDS with artifice editor window.
            base.CreateGUI();
        }

        private void OnArtificeEnabledChange()
        {
            Artifice_Utilities.ToggleArtificeDrawer(ToBool(isArtificeEnabled));
        }

        private void OnValidatorToolbarEnabledChanged()
        {
            Artifice_Toolbar_Validator.Set_IsEnabled(ToBool(shouldUseValidatorToolbar));
        }

        private void OnInspectorHeaderEnabledChanged()
        {
            Artifice_InspectorHeader_Main.SetEnabled(ToBool(shouldUseInspectorHeader));
        }
        
        #region Utilities

        private bool ToBool(ToggleOption option)
        {
            return option == ToggleOption.On;
        }

        private ToggleOption ToToggleOption(bool option)
        {
            return option ? ToggleOption.On : ToggleOption.Off;
        }
        
        #endregion
    }
}
