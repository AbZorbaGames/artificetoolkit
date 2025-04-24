using ArtificeToolkit.Attributes;
using UnityEngine;

namespace ArtificeToolkit.Editor
{
    public class Artifice_EditorWindow_IgnoreList : ArtificeEditorWindow
    {
        #region FIELDS

        [InfoBox("A list of all the locally defined components to ignore with Artifice inspector.")]
        [SerializeField, HideLabel, ReadOnly]
        private string ignoreList = "";

        public const string ViewPersistenceKey = "ArtificeIgnoreList";

        #endregion

        public static void ShowWindow()
        {
            var wnd = GetWindow(typeof(Artifice_EditorWindow_IgnoreList));
            wnd.titleContent.text = "Artifice Ignore List";
        }

        protected override void CreateGUI()
        {
            // Base
            base.CreateGUI();
            Refresh();
        }

        [Button]
        private void Refresh()
        {
            ignoreList = "~~~~Ignore List~~~~\n";

            var dictionary = Artifice_SCR_PersistedData.instance.LoadAll("ArtificeIgnoreList");
            foreach (var pair in dictionary)
                if (bool.TryParse(pair.Value, out var value) && value)
                    ignoreList += $"{pair.Key}\n";
        }
    }
}   