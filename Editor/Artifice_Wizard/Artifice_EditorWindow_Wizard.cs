using System;
using Artifice.Editor;
using ArtificeToolkit.Editor.Artifice_InspectorHeader;
using UnityEngine;
using UnityEngine.UIElements;

namespace ArtificeToolkit.Editor
{
    public class Artifice_EditorWindow_Wizard : ArtificeEditorWindow
    {
        public static void ShowWindow()
        {
            var wnd = GetWindow<Artifice_EditorWindow_Wizard>();
            wnd.titleContent = new GUIContent("Artifice Wizard"); // optional icon
            wnd.minSize = new Vector2(420, 500);
        }

        protected override void CreateGUI()
        {
            rootVisualElement.Clear();

            // Load styles
            var styleSheet = Artifice_Utilities.GetStyle(GetType());
            if (styleSheet != null) rootVisualElement.styleSheets.Add(styleSheet);
            rootVisualElement.styleSheets.Add(Artifice_Utilities.GetGlobalStyle());

            var scrollView = new ScrollView(ScrollViewMode.Vertical);
            scrollView.AddToClassList("main-container");
            rootVisualElement.Add(scrollView);

            // Title
            var titleLabel = new Label("Artifice Toolkit");
            titleLabel.AddToClassList("main-title");
            scrollView.Add(titleLabel);

            var subtitle = new Label("Configure core features of the Artifice Toolkit");
            subtitle.AddToClassList("sub-title");
            scrollView.Add(subtitle);

            // === Main Artifice Enable ===
            scrollView.Add(CreateToggleSection(
                "Enable Artifice Drawer",
                Artifice_Utilities.ArtificeDrawerEnabled,
                "Turns the entire inspector drawing system of Artifice Toolkit, allowing the usage of custom attributes.\n\n" +
                "When disabled:\n" +
                "• All the CustomAttributes (Title, BoxGroup, Required, etc.) stop working\n" +
                "• Performance is slightly improved in large projects inspectors\n" +
                "• You can safely keep Artifice in the project without affecting inspectors, if you want the package for its other features.",
                Artifice_Utilities.ToggleArtificeDrawer
            ));

            // === Inspector Header ===
            scrollView.Add(CreateToggleSection(
                "Inspector Header Enhancement",
                Artifice_InspectorHeader_Main.IsEnabled(),
                "Adds beautiful header banner in the inspector with mandatory features like \'Search\' and script filter.\n\n" +
                "Features:\n" +
                "• Search to filter by script name\n" +
                "• Isolate by filtering specific scripts at a time\n" +
                "• Collapse/Expand all Components for better clarity.",
                Artifice_InspectorHeader_Main.SetEnabled
            ));

            // Optional: Add more features here in the future
            // Example:
            // scrollView.Add(CreateToggleSection("Validator Toolbar", ..., ...));
            scrollView.Add(CreateToggleSection(
                "Validator Toolbar",
                Artifice_Toolbar_Validator.IsEnabled,
                "Add on your top left toolbar, a quick and easy way to open/close your validator and see an overview of your validations.",
                Artifice_Toolbar_Validator.Set_IsEnabled
            ));
            
            scrollView.Add(CreateAdditionalFeaturesList());
        }

        private VisualElement CreateToggleSection(string title, bool currentValue, string tooltip, Action<bool> onValueChanged)
        {
            var container = new VisualElement();
            container.AddToClassList("toggle-section");
            container.style.marginBottom = 22;
            container.tooltip = tooltip;

            var titleLabel = new Label(title);
            titleLabel.AddToClassList("toggle-section-title");
            container.Add(titleLabel);

            var toggle = new Toggle();
            toggle.value = currentValue;
            toggle.AddToClassList("toggle-style");

            toggle.RegisterValueChangedCallback(evt =>
            {
                onValueChanged(evt.newValue);
            });

            container.Add(toggle);

            // Optional description/help box
            var helpLabel = new Label(tooltip.Split('\n')[0]); // First line as summary
            helpLabel.AddToClassList("toggle-help-summary");
            container.Add(helpLabel);

            return container;
        }

        private VisualElement CreateAdditionalFeaturesList()
        {
            var container = new ScrollView(ScrollViewMode.Vertical);
            container.AddToClassList("additional-features-container");

            var header = new Label("Additional Options");
            header.AddToClassList("additional-features-header");
            container.Add(header);

            // mScript Visibility
            var mScriptVisibilityToggle = new Toggle("m_Script Visibility");
            mScriptVisibilityToggle.AddToClassList("additional-features-toggle");
            container.Add(mScriptVisibilityToggle);

            mScriptVisibilityToggle.value = Artifice_Utilities.MScriptVisibility;
            mScriptVisibilityToggle.RegisterValueChangedCallback(value =>
            {
                Artifice_Utilities.MScriptVisibility = value.newValue;
                Artifice_Utilities.TriggerNextFrameReselection();
            });
            
            return container;
        }
    }
}