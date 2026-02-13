using System.Collections.Generic;
using ArtificeToolkit.Editor;
using ArtificeToolkit.Editor.Artifice_ArtificeMenuEditorWindow;
using UnityEditor;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace Editor.Artifice_ArtificeMenuEditorWindow
{
    public class ArtificeMenuNode
    {
        #region FIELDS

        public readonly string Title;
        public readonly object Obj;
        private readonly List<ArtificeMenuNode> _children;

        #endregion

        public ArtificeMenuNode(string title, object obj)
        {
            Title = title;
            Obj = obj;
            _children = new List<ArtificeMenuNode>();
        }

        public ICollection<ArtificeMenuNode> Get_Children()
        {
            return _children;
        }

        public void AddChild(ArtificeMenuNode node)
        {
            _children.Add(node);
        }

        public void RemoveChild(ArtificeMenuNode node)
        {
            _children.Remove(node);
        }
    }

    public class VisualElement_ArtificeMenuItem : VisualElement
    {
        #region FIELDS

        public readonly UnityEvent<ArtificeMenuNode> OnClick = new();

        private readonly Label _label;
        private readonly VisualElement _childrenMenuItem;
        
        public override VisualElement contentContainer => _childrenMenuItem;
        
        #endregion

        public VisualElement_ArtificeMenuItem(ArtificeMenuNode node)
        {
            _label = new Label(node.Title);
            _label.RegisterCallback<MouseDownEvent>(evt => { OnClick?.Invoke(node); });
            hierarchy.Add(_label);
            
            _childrenMenuItem = new VisualElement();
            _childrenMenuItem.AddToClassList("hide");
            hierarchy.Add(_childrenMenuItem);
        }
        
        public void Set_IsExpanded(bool option)
        {
            _childrenMenuItem.EnableInClassList("hide", option);
        }
    }

    public abstract class ArtificeMenuEditorWindow : EditorWindow
    {
        #region FIELDS

        private VisualElement _mainContainer;

        private VisualElement _menuPanel;
        private VisualElement _content;

        private ArtificeDrawer _artificeDrawer;

        #endregion

        private void CreateGUI()
        {
            _artificeDrawer = new();
            _artificeDrawer.SetSerializedPropertyFilter(p => p.name != "m_Script");

            rootVisualElement.Clear();
            rootVisualElement.styleSheets.Add(Artifice_Utilities.GetGlobalStyle());
            rootVisualElement.styleSheets.Add(Artifice_Utilities.GetStyle(typeof(ArtificeMenuEditorWindow)));
            rootVisualElement.AddToClassList("menu-editor-container");
            rootVisualElement.Add(_mainContainer);

            var splitView = new TwoPaneSplitView(0, 100, TwoPaneSplitViewOrientation.Horizontal);
            splitView.AddToClassList("menu-editor-split-pane");
            rootVisualElement.Add(splitView);
            
            _menuPanel = new VisualElement();
            _menuPanel.AddToClassList("menu-panel-container");
            splitView.Add(_menuPanel);
            
            _content = new VisualElement();
            _content.AddToClassList("content-container");
            splitView.Add(_content);
            
            var nodes = BuildMenuTree();

            var parentMap = new Dictionary<ArtificeMenuNode, VisualElement_ArtificeMenuItem>();
            var queue = new Queue<ArtificeMenuNode>(nodes);
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();

                // Add menu item for 
                var menuItem = new VisualElement_ArtificeMenuItem(current);
                
                if(parentMap.TryGetValue(current, out var parentElement))
                    parentElement.Add(menuItem);            
                else
                    _menuPanel.Add(menuItem);

                // On Click
                menuItem.OnClick.AddListener(node =>
                {
                    _content.Clear();
                    _content.Add(ArtificeBinder.CreateField(current.Obj));
                });

                foreach (var child in current.Get_Children())
                {
                    parentMap[child] = menuItem;
                    queue.Enqueue(child);
                }
            }
        }

        protected abstract List<ArtificeMenuNode> BuildMenuTree();
    }
}