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
                "Enable Artifice Toolkit",
                Artifice_Utilities.ArtificeDrawerEnabled,
                "Turns the entire Artifice drawing system on or off globally.\n\n" +
                "When disabled:\n" +
                "• All custom property drawers (NiceButton, Title, ReadOnly, etc.) stop working\n" +
                "• Performance is slightly improved in large projects\n" +
                "• You can safely keep Artifice in the project without affecting inspectors",
                Artifice_Utilities.ToggleArtificeDrawer
            ));

            // === Inspector Header ===
            scrollView.Add(CreateToggleSection(
                "Inspector Header Decorations",
                Artifice_InspectorHeader_Main.IsEnabled(),
                "Adds beautiful header banners with script icon, name, and optional description above MonoBehaviours and ScriptableObjects.\n\n" +
                "Features:\n" +
                "• Automatic script icon and class name\n" +
                "• Custom title/description via [InspectorHeader] attribute\n" +
                "• Subtle gradient background and clean typography",
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
        }

        private VisualElement CreateToggleSection(string title, bool currentValue, string tooltip, System.Action<bool> onValueChanged)
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
    }
}