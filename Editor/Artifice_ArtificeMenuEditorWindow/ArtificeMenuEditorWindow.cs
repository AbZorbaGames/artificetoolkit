using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ArtificeToolkit.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace Editor.Artifice_ArtificeMenuEditorWindow
{
    public class VisualElement_ArtificeMenuItem : VisualElement
    {
        public readonly UnityEvent<ArtificeMenuTreeNode> OnClick = new();
        public ArtificeMenuTreeNode Node { get; }
        private VisualElement_ArtificeMenuItem Parent { get; set; }

        private readonly VisualElement _headerContainer;
        private readonly VisualElement _childrenContainer;
        private Image _collapseImage;

        public VisualElement_ArtificeMenuItem(ArtificeMenuTreeNode node)
        {
            Node = node;
            AddToClassList("menu-item");

            _headerContainer = CreateHeader(node);
            _childrenContainer = CreateChildrenContainer();

            hierarchy.Add(_headerContainer);
            hierarchy.Add(_childrenContainer);
        }

        private VisualElement CreateHeader(ArtificeMenuTreeNode node)
        {
            var container = new VisualElement();
            container.AddToClassList("menu-item-header-container");
            container.RegisterCallback<MouseDownEvent>(_ => OnClick?.Invoke(node));

            if (node.ScriptableObject != null)
                container.AddToClassList("menu-item-header-container-allowed-hover");

            if (node.Get_Children().Count > 0 || node.ScriptableObject == null)
            {
                _collapseImage = new Image { image = EditorGUIUtility.IconContent("d_icon dropdown@2x").image };
                _collapseImage.AddToClassList("menu-item-collapse-button");
                _collapseImage.RegisterCallback<MouseDownEvent>(evt =>
                {
                    evt.StopImmediatePropagation();
                    ToggleExpanded();
                });
                container.Add(_collapseImage);
            }

            if (node.Sprite != null)
            {
                var icon = new Image { image = node.Sprite.texture };
                icon.AddToClassList("menu-item-header-icon");
                container.Add(icon);
            }

            var label = new Label(node.Title);
            label.AddToClassList("menu-item-header-label");
            container.Add(label);
            return container;
        }

        private VisualElement CreateChildrenContainer()
        {
            var container = new VisualElement();
            container.AddToClassList("menu-item-children-container");
            container.AddToClassList("hide");
            return container;
        }

        public void AddChild(VisualElement_ArtificeMenuItem menuItem)
        {
            _childrenContainer.Add(menuItem);
            menuItem.SetParent(this);
        }

        public void SetSelected(bool selected)
        {
            _headerContainer.EnableInClassList("menu-item--selected", selected);
            if (selected) ExpandPath();
        }

        private void ExpandPath()
        {
            var iterator = Parent;
            while (iterator != null)
            {
                iterator.ToggleExpanded(true);
                iterator = iterator.Parent;
            }
        }

        public void SetParent(VisualElement_ArtificeMenuItem parent)
        {
            Parent = parent;
            _headerContainer.style.paddingLeft = 10 + (10 * GetDepth());
        }

        public void ToggleExpanded(bool? forceState = null)
        {
            bool expand = forceState ?? _childrenContainer.ClassListContains("hide");
            _childrenContainer.EnableInClassList("hide", !expand);
            _collapseImage?.EnableInClassList("menu-item-collapse-button--expanded", expand);
        }

        private int GetDepth() => Parent == null ? 0 : 1 + Parent.GetDepth();
    }


    public abstract class ArtificeMenuEditorWindow : EditorWindow, IHasCustomMenu, IArtifice_Persistence
    {
        private VisualElement _menuPanel;
        private VisualElement _content;
        private ArtificeDrawer _artificeDrawer;
        private VisualElement_ArtificeMenuItem _selectedMenuItem;
        private readonly Dictionary<ArtificeMenuTreeNode, VisualElement_ArtificeMenuItem> _nodeMap = new();

        protected void CreateGUI() => OnRefresh();

        private void OnRefresh()
        {
            Initialize();
            SetupLayout();
            BuildAndPopulateTree();
            LoadPersistedData();
        }

        private void Initialize()
        {
            _artificeDrawer = new ArtificeDrawer();
            _artificeDrawer.SetSerializedPropertyFilter(p => p.name != "m_Script");
            _nodeMap.Clear();
        }

        private void SetupLayout()
        {
            rootVisualElement.Clear();
            rootVisualElement.styleSheets.Add(Artifice_Utilities.GetGlobalStyle());
            rootVisualElement.styleSheets.Add(Artifice_Utilities.GetStyle(typeof(ArtificeMenuEditorWindow)));
            rootVisualElement.AddToClassList("menu-editor-container");

            var splitView = new TwoPaneSplitView(0, 200, TwoPaneSplitViewOrientation.Horizontal);
            rootVisualElement.Add(splitView);

            _menuPanel = new ScrollView(ScrollViewMode.Vertical);
            _menuPanel.AddToClassList("menu-panel-container");
            
            _content = new ScrollView(ScrollViewMode.Vertical);
            _content.AddToClassList("content-container");
            

            splitView.Add(_menuPanel);
            splitView.Add(_content);
        }

        private void BuildAndPopulateTree()
        {
            var nodes = BuildMenuTree();
            var parentMap = new Dictionary<ArtificeMenuTreeNode, VisualElement_ArtificeMenuItem>();
            var queue = new Queue<ArtificeMenuTreeNode>(nodes);

            while (queue.Count > 0)
            {
                var node = queue.Dequeue();
                var menuItem = new VisualElement_ArtificeMenuItem(node);
                _nodeMap[node] = menuItem;

                if (parentMap.TryGetValue(node, out var parentUI))
                    parentUI.AddChild(menuItem);
                else
                    _menuPanel.Add(menuItem);

                menuItem.OnClick.AddListener(_ => SetSelected(menuItem));

                foreach (var child in node.Get_Children())
                {
                    parentMap[child] = menuItem;
                    queue.Enqueue(child);
                }
            }
        }

        private void SetSelected(VisualElement_ArtificeMenuItem menuItem)
        {
            if (menuItem == _selectedMenuItem) return;

            if (menuItem.Node.ScriptableObject == null)
            {
                menuItem.ToggleExpanded();
                return;
            }

            _selectedMenuItem?.SetSelected(false);
            _selectedMenuItem = menuItem;
            _selectedMenuItem.SetSelected(true);

            RenderContent(menuItem.Node.ScriptableObject);
            SavePersistedData();
        }

        private void RenderContent(ScriptableObject target)
        {
            _content.Clear();

            if (target is EditorWindow window)
            {
                RenderEditorWindowContent(window);
            }
            else
            {
                _content.Add(_artificeDrawer.CreateInspectorGUI(new SerializedObject(target)));
            }
        }

        private void RenderEditorWindowContent(EditorWindow window)
        {
            var type = window.GetType();
            var flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

            var onGui = type.GetMethod("OnGUI", flags);
            if (onGui != null)
            {
                _content.Add(new IMGUIContainer(() => onGui.Invoke(window, null)));
                return;
            }

            var createGui = type.GetMethod("CreateGUI", flags) ?? type.DeclaringType?.GetMethod("CreateGUI", flags);
            if (createGui != null)
            {
                if (window.rootVisualElement.childCount == 0)
                    createGui.Invoke(window, null);

                _content.Add(window.rootVisualElement);
            }
            else
            {
                _content.Add(new Label($"No UI found for {window.name}"));
            }
        }

        public void AddItemsToMenu(GenericMenu menu) => menu.AddItem(new GUIContent("Refresh"), false, OnRefresh);

        #region Persistence

        public abstract string ViewPersistenceKey { get; set; }

        public void SavePersistedData()
        {
            if (_selectedMenuItem != null)
                Artifice_SCR_PersistedData.instance.SaveData(ViewPersistenceKey, "selectedNode",
                    _selectedMenuItem.Node.Title);
        }

        public void LoadPersistedData()
        {
            var savedTitle = Artifice_SCR_PersistedData.instance.LoadData(ViewPersistenceKey, "selectedNode");
            var target = _nodeMap.Values.FirstOrDefault(m => m.Node.Title == savedTitle) ??
                         _nodeMap.Values.FirstOrDefault();

            if (target != null) SetSelected(target);
        }

        #endregion

        protected abstract List<ArtificeMenuTreeNode> BuildMenuTree();
    }
}