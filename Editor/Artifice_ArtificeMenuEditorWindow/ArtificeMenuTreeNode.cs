using System;
using System.Collections.Generic;
using UnityEngine;

namespace Editor.Artifice_ArtificeMenuEditorWindow
{
    [Serializable]
    public class ArtificeMenuTreeNode
    {
        #region FIELDS

        public readonly string Title;
        public readonly ScriptableObject ScriptableObject;
        public readonly Sprite Sprite;
        private readonly List<ArtificeMenuTreeNode> _children;

        #endregion

        public ArtificeMenuTreeNode(string title, ScriptableObject scriptableObject, Sprite sprite = null)
        {
            Title = title;
            ScriptableObject = scriptableObject;
            Sprite = sprite;
            _children = new List<ArtificeMenuTreeNode>();
        }

        public ICollection<ArtificeMenuTreeNode> Get_Children()
        {
            return _children;
        }

        public void AddChild(ArtificeMenuTreeNode node)
        {
            _children.Add(node);
        }

        public void RemoveChild(ArtificeMenuTreeNode node)
        {
            _children.Remove(node);
        }
    }
}