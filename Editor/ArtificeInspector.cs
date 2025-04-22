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
[CustomEditor(typeof(Object), true), CanEditMultipleObjects]
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
            var artificeIgnore = type.GetCustomAttribute<ArtificeIgnoreAttribute>();

            return artificeIgnore != null
                ? base.CreateInspectorGUI()
                : new ArtificeDrawer().CreateInspectorGUI(serializedObject);
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
    }
}
