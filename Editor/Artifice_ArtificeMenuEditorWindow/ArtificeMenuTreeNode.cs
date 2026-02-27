using System;
using System.Collections.Generic;
using UnityEngine;

namespace ArtificeToolkit.Editor.Artifice_ArtificeMenuEditorWindow
{
    [Serializable]
    public class ArtificeMenuTreeNode
    {
        #region FIELDS

        public readonly string Title;
        public readonly ScriptableObject ScriptableObject;
        public readonly Texture Texture;
        public readonly List<ArtificeMenuTreeNode> Children;

        #endregion

        public ArtificeMenuTreeNode(string title, ScriptableObject scriptableObject, Texture texture = null, List<ArtificeMenuTreeNode> children = null)
        {
            Title = title;
            ScriptableObject = scriptableObject;
            Texture = texture;
            Children = children ?? new List<ArtificeMenuTreeNode>();
        }

        public void AddChild(ArtificeMenuTreeNode node)
        {
            Children.Add(node);
        }

        public void RemoveChild(ArtificeMenuTreeNode node)
        {
            Children.Remove(node);
        }
    }
}