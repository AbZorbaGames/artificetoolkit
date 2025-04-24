using System;
using System.Reflection;
using ArtificeToolkit.Attributes;
using ArtificeToolkit.Editor.Artifice_CustomAttributeDrawers.CustomAttributeDrawers_Groups;
using UnityEditor;
using UnityEngine.UIElements;

// ReSharper disable InvertIf
// ReSharper disable MemberCanBeMadeStatic.Local

namespace ArtificeToolkit.Editor
{
    /// <summary> Propagates rendering to the <see cref="ArtificeDrawer"/></summary>
//[CustomEditor(typeof(Object), true), CanEditMultipleObjects]
    public class ArtificeInspector : UnityEditor.Editor
    {
        #region FIELDS

        private ArtificeDrawer _drawer;

        #endregion

        /* Mono */
        public override VisualElement CreateInspectorGUI()
        {
            // Check if targetObject has ArtificeIgnore
            var type = serializedObject.targetObject.GetType();
            var hasArtificeIgnoreAttribute = type.GetCustomAttribute<ArtificeIgnoreAttribute>() != null;
            var hasMarkedAsArtificeIgnore = HasArtificeIgnore(type);

            // Render with Default inspector or Artifice Inspector based on ignore values
            var inspector = hasArtificeIgnoreAttribute || hasMarkedAsArtificeIgnore
                ? base.CreateInspectorGUI()
                : new ArtificeDrawer().CreateInspectorGUI(serializedObject);

            return inspector;
        }

        /* Mono */
        private void OnDisable()
        {
            if (_drawer != null) // Folder inspectors would errors otherwise
            {
                _drawer.Dispose();
                // Clear Box Group Holder data
                Artifice_CustomAttributeUtility_GroupsHolder.Instance.ClearSerializedObject(serializedObject);
            }
        }

        #region Artifice Ignore List

        [MenuItem("CONTEXT/Object/Artifice Ignore List/Add", false, 105)]
        private static void AddToIgnore(MenuCommand command)
        {
            var type = command.context.GetType();
            SetArtificeIgnore(type, true);
            Artifice_Utilities.TriggerNextFrameReselection();
        }

        [MenuItem("CONTEXT/Object/Artifice Ignore List/Add", true)]
        private static bool ValidateAdd(MenuCommand command)
        {
            var type = command.context.GetType();
            return !HasArtificeIgnore(type);
        }

        [MenuItem("CONTEXT/Object/Artifice Ignore List/Remove", false, 105)]
        private static void RemoveFromIgnore(MenuCommand command)
        {
            var type = command.context.GetType();
            SetArtificeIgnore(type, false);
            Artifice_Utilities.TriggerNextFrameReselection();
        }

        [MenuItem("CONTEXT/Object/Artifice Ignore List/Remove", true)]
        private static bool ValidateRemove(MenuCommand command)
        {
            var type = command.context.GetType();
            return HasArtificeIgnore(type);
        }

        #endregion

        #region Utility

        private static void SetArtificeIgnore(Type type, bool shouldIgnore)
        {
            Artifice_SCR_PersistedData.instance.SaveData(Artifice_EditorWindow_IgnoreList.ViewPersistenceKey, $"{type.Name}", shouldIgnore.ToString());
        }

        private static bool HasArtificeIgnore(Type type)
        {
            var stringValue = Artifice_SCR_PersistedData.instance.LoadData(Artifice_EditorWindow_IgnoreList.ViewPersistenceKey, $"{type.Name}");
            return bool.TryParse(stringValue, out var value) && value;
        }
        
        #endregion
    }
}
