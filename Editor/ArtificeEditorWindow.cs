using UnityEditor;

// ReSharper disable InconsistentNaming

namespace ArtificeToolkit.Editor
{
    public class ArtificeEditorWindow : EditorWindow
    {
        #region FIELDS

        protected ArtificeDrawer ArtificeDrawer;
        
        #endregion
        
        /* Mono */
        protected virtual void CreateGUI()
        {
            ArtificeDrawer = new ArtificeDrawer();
            ArtificeDrawer.SetSerializedPropertyFilter(property => property.name != "m_Script");

            // Get reference to serializedObject and simply call 
            var serializedObject = new SerializedObject(this);
            
            rootVisualElement.styleSheets.Add(Artifice_Utilities.GetStyle(typeof(ArtificeEditorWindow)));
            rootVisualElement.AddToClassList("root-visual-element");
            rootVisualElement.Add(ArtificeDrawer.CreateInspectorGUI(serializedObject));
        }

        /* Mono */
        private void OnDestroy()
        {
            ArtificeDrawer?.Dispose();
        }
    }
}