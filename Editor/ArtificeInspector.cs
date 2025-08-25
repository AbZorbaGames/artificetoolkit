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
            var targetObject = serializedObject.targetObject;
            
            // The target (inspected) Object can be null if it is a missing script
            if (targetObject == null)
            {
                return base.CreateInspectorGUI();
            }
            
            // Check if targetObject has ArtificeIgnore
            var type = targetObject.GetType();
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
            if(shouldIgnore)
                Artifice_Utilities.AddIgnoredTypeName(type.Name);
            else
                Artifice_Utilities.RemoveIgnoredTypeName(type.Name);
        }

        private static bool HasArtificeIgnore(Type type)
        {
            return Artifice_Utilities.ShouldIgnoreTypeName(type.Name);
        }
        
        #endregion
    }
}
