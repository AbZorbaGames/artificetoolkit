using System;
using System.Collections.Generic;
using ArtificeToolkit.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace Editor.Artifice_ArtificeMenuEditorWindow
{
    [Serializable]
    public class ArtificeTreeNode
    {
        #region FIELDS

        public readonly string Title;
        public readonly ScriptableObject ScriptableObject;
        private readonly List<ArtificeTreeNode> _children;

        #endregion

        public ArtificeTreeNode(string title, ScriptableObject scriptableObject)
        {
            Title = title;
            ScriptableObject = scriptableObject;
            _children = new List<ArtificeTreeNode>();
        }

        public ICollection<ArtificeTreeNode> Get_Children()
        {
            return _children;
        }

        public void AddChild(ArtificeTreeNode node)
        {
            _children.Add(node);
        }

        public void RemoveChild(ArtificeTreeNode node)
        {
            _children.Remove(node);
        }
    }

    public class VisualElement_ArtificeMenuItem : VisualElement
    {
        #region FIELDS

        public readonly UnityEvent<ArtificeTreeNode> OnClick = new();

        private readonly VisualElement _headerContainer;
        private readonly VisualElement _headerLabel;
        private readonly VisualElement _childrenContainer;

        public ArtificeTreeNode Node { get; private set; }
        public VisualElement_ArtificeMenuItem Parent => _parent;
        
        private VisualElement_ArtificeMenuItem _parent;

        #endregion

        public VisualElement_ArtificeMenuItem(ArtificeTreeNode node)
        {
            Node = node;
            AddToClassList("menu-item");
            
            // Create header 
            _headerContainer = new VisualElement();
            _headerContainer.AddToClassList("menu-item-header-container");
            hierarchy.Add(_headerContainer);
            
            _headerLabel = new Label(node.Title);
            _headerLabel.AddToClassList("menu-item-header-label");
            _headerLabel.RegisterCallback<MouseDownEvent>(_ => { OnClick?.Invoke(node); });
            _headerContainer.Add(_headerLabel);

            if (node.Get_Children().Count > 0)
            {
                var collapseButton = new VisualElement();
                collapseButton.AddToClassList("menu-item-collapse-button");
                collapseButton.RegisterCallback<MouseDownEvent>(_ =>
                {
                    ToggleExpanded();
                });
                _headerContainer.Add(collapseButton);
            }
            
            // Create child container
            _childrenContainer = new VisualElement();
            _childrenContainer.AddToClassList("menu-item-children-container");
            _childrenContainer.AddToClassList("hide");
            hierarchy.Add(_childrenContainer);
        }

        public void AddChild(VisualElement_ArtificeMenuItem menuItem)
        {
            _childrenContainer.Add(menuItem);
            menuItem._parent = this;
        }
        
        public void Set_IsSelected(bool selected)
        {
            _headerContainer.EnableInClassList("menu-item--selected", selected);
        }

        public void Set_Parent(VisualElement_ArtificeMenuItem menuItem)
        {
            _parent = menuItem;

            var depth = Get_Depth();
            _headerLabel.style.marginLeft = 10 + 10 * depth;
        }
        
        #region Utilities
        
        private void ToggleExpanded()
        {
            _childrenContainer.ToggleInClassList("hide");
        }
        
        private int Get_Depth()
        {
            var depth = 0;

            var iterator = _parent;
            while (iterator != null)
            {
                iterator = iterator._parent;
                depth++;
            }

            return depth;
        }
        
        #endregion
    }

    public abstract class ArtificeMenuEditorWindow : EditorWindow
    {
        #region FIELDS

        private VisualElement _mainContainer;

        private VisualElement _menuPanel;
        private VisualElement _content;
        
        private ArtificeDrawer _artificeDrawer;
        private Dictionary<ArtificeTreeNode, VisualElement_ArtificeMenuItem> _nodeMap = new();
        private VisualElement_ArtificeMenuItem _selectedMenuItem = null;
        
        #endregion

        private void Awake()
        {
            Debug.Log("Awake");
        }

        private void OnEnable()
        {
            Debug.Log("On Enable");
        }

        private void OnDisable()
        {
            Debug.Log("On Disable");
        }

        private void CreateGUI()
        {
            Debug.Log("Create GUI");
            
            _artificeDrawer = new();
            _artificeDrawer.SetSerializedPropertyFilter(p => p.name != "m_Script");

            rootVisualElement.Clear();
            rootVisualElement.styleSheets.Add(Artifice_Utilities.GetGlobalStyle());
            rootVisualElement.styleSheets.Add(Artifice_Utilities.GetStyle(typeof(ArtificeMenuEditorWindow)));
            rootVisualElement.AddToClassList("menu-editor-container");
            rootVisualElement.Add(_mainContainer);

            var splitView = new TwoPaneSplitView(0, 200, TwoPaneSplitViewOrientation.Horizontal);
            splitView.AddToClassList("menu-editor-split-pane");
            rootVisualElement.Add(splitView);
            
            _menuPanel = new VisualElement();
            _menuPanel.AddToClassList("menu-panel-container");
            splitView.Add(_menuPanel);
            
            _content = new VisualElement();
            _content.AddToClassList("content-container");
            splitView.Add(_content);
            
            // Asks from inherited class the menu tree.
            var nodes = BuildMenuTree();

            // Draw menu item.
            var parentMap = new Dictionary<ArtificeTreeNode, VisualElement_ArtificeMenuItem>();
            var queue = new Queue<ArtificeTreeNode>(nodes);
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();

                // Create menu item 
                var menuItem = new VisualElement_ArtificeMenuItem(current);
                if (_selectedMenuItem == null)
                {
                    _selectedMenuItem = menuItem;
                    SetContent(menuItem);
                }

                if (parentMap.TryGetValue(current, out var parentElement))
                {
                    parentElement.AddChild(menuItem);            
                    menuItem.Set_Parent(parentElement);
                }
                else
                    _menuPanel.Add(menuItem);

                // On Click
                menuItem.OnClick.AddListener(node =>
                {
                    SetContent(menuItem);
                });

                foreach (var child in current.Get_Children())
                {
                    parentMap[child] = menuItem;
                    queue.Enqueue(child);
                }
            }
            
            // Preselect the first one or cached one!?
            
        }

        protected abstract List<ArtificeTreeNode> BuildMenuTree();

        private void SetContent(VisualElement_ArtificeMenuItem menuItem)
        {
            _selectedMenuItem?.Set_IsSelected(false);
            _selectedMenuItem = menuItem;
            _selectedMenuItem.Set_IsSelected(true);
            
            _content.Clear();
            var serializedObject = new SerializedObject(menuItem.Node.ScriptableObject);
            _content.Add(_artificeDrawer.CreateInspectorGUI(serializedObject));
        }
    }
}