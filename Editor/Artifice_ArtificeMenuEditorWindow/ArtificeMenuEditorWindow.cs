using System.Collections.Generic;
using System.Linq;
using ArtificeToolkit.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace Editor.Artifice_ArtificeMenuEditorWindow
{
    public class VisualElement_ArtificeMenuItem : VisualElement
    {
        #region FIELDS

        public readonly UnityEvent<ArtificeMenuTreeNode> OnClick = new();

        private readonly VisualElement _headerContainer;
        private readonly VisualElement _headerLabel;
        private readonly Image _headerImage;
        private readonly Image _collapseImage;

        private readonly VisualElement _childrenContainer;

        public ArtificeMenuTreeNode Node { get; }
        public VisualElement_ArtificeMenuItem Parent => _parent; // TODO make private if not needed public.

        private VisualElement_ArtificeMenuItem _parent;

        #endregion

        public VisualElement_ArtificeMenuItem(ArtificeMenuTreeNode node)
        {
            Node = node;
            AddToClassList("menu-item");

            // Create header 
            _headerContainer = new VisualElement();
            _headerContainer.AddToClassList("menu-item-header-container");
            _headerContainer.RegisterCallback<MouseDownEvent>(_ => { OnClick?.Invoke(node); });
            hierarchy.Add(_headerContainer);

            if (node.ScriptableObject != null)
                _headerContainer.AddToClassList("menu-item-header-container-allowed-hover");

            // Header collapse icon
            if (node.Get_Children().Count > 0 || node.ScriptableObject == null)
            {
                var arrowImage = EditorGUIUtility.IconContent("d_icon dropdown@2x").image;

                _collapseImage = new Image
                {
                    image = arrowImage
                };

                _collapseImage.AddToClassList("menu-item-collapse-button");
                _collapseImage.RegisterCallback<MouseDownEvent>(evt =>
                {
                    evt.StopImmediatePropagation();
                    ToggleExpanded();
                });
                _headerContainer.Add(_collapseImage);
            }

            // Header Icon
            if (node.Sprite != null)
            {
                _headerImage = new Image();
                _headerImage.AddToClassList("menu-item-header-icon");
                _headerImage.image = node.Sprite.texture;
                _headerContainer.Add(_headerImage);
            }

            // Header label
            _headerLabel = new Label(node.Title);
            _headerLabel.AddToClassList("menu-item-header-label");
            _headerContainer.Add(_headerLabel);

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

            // Since I am selected, expand all parent elements
            if (selected)
            {
                var iterator = Parent;
                while (iterator != null)
                {
                    iterator.ToggleExpanded(true);
                    iterator = iterator.Parent;
                }
            }
        }

        public void Set_Parent(VisualElement_ArtificeMenuItem menuItem)
        {
            _parent = menuItem;

            var depth = Get_Depth();
            _headerContainer.style.paddingLeft = 10 + 10 * depth;
        }

        #region Utilities

        public void ToggleExpanded()
        {
            ToggleExpanded(_childrenContainer.ClassListContains("hide"));
        }

        public void ToggleExpanded(bool isExpanded)
        {
            _childrenContainer.EnableInClassList("hide", !isExpanded);
            _collapseImage.EnableInClassList("menu-item-collapse-button--expanded", isExpanded);
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

    public abstract class ArtificeMenuEditorWindow : EditorWindow, IHasCustomMenu, IArtifice_Persistence
    {
        #region FIELDS

        private VisualElement _mainContainer;

        private VisualElement _menuPanel;
        private VisualElement _content;

        private ArtificeDrawer _artificeDrawer;
        private VisualElement_ArtificeMenuItem _selectedMenuItem = null;
        private readonly Dictionary<ArtificeMenuTreeNode, VisualElement_ArtificeMenuItem> _nodeMap = new();

        #endregion

        /* Mono */
        private void CreateGUI()
        {
            OnRefresh();
        }

        private void OnRefresh()
        {
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

            _menuPanel = new ScrollView(ScrollViewMode.Vertical);
            _menuPanel.AddToClassList("menu-panel-container");
            splitView.Add(_menuPanel);

            _content = new ScrollView(ScrollViewMode.Vertical);
            _content.AddToClassList("content-container");
            splitView.Add(_content);

            // Asks from inherited class the menu tree.
            var nodes = BuildMenuTree();

            // Draw menu item.
            var parentMap = new Dictionary<ArtificeMenuTreeNode, VisualElement_ArtificeMenuItem>();
            var queue = new Queue<ArtificeMenuTreeNode>(nodes);
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();

                // Create menu item 
                var menuItem = new VisualElement_ArtificeMenuItem(current);
                _nodeMap[current] = menuItem;

                if (parentMap.TryGetValue(current, out var parentElement))
                {
                    parentElement.AddChild(menuItem);
                    menuItem.Set_Parent(parentElement);
                }
                else
                    _menuPanel.Add(menuItem);

                // On Click
                menuItem.OnClick.AddListener(node => { SetSelected(menuItem); });

                // Set children of element.
                foreach (var child in current.Get_Children())
                {
                    parentMap[child] = menuItem;
                    queue.Enqueue(child);
                }
            }

            LoadPersistedData();
        }

        protected abstract List<ArtificeMenuTreeNode> BuildMenuTree();

        private void SetSelected(VisualElement_ArtificeMenuItem menuItem)
        {
            // If already selected, skip
            if (menuItem == _selectedMenuItem)
                return;
            
            // If object is null, skip
            if (menuItem.Node.ScriptableObject == null)
            {
                menuItem.ToggleExpanded();
                return;
            }

            _selectedMenuItem?.Set_IsSelected(false);
            _selectedMenuItem = menuItem;
            _selectedMenuItem.Set_IsSelected(true);
            _content.Clear();
            
            var scriptableObject = menuItem.Node.ScriptableObject;
            
            var type = scriptableObject.GetType();
            if (type.IsSubclassOf(typeof(EditorWindow)))
            {
                if (type.IsSubclassOf(typeof(ArtificeEditorWindow)))
                {
                    var serializedObject = new SerializedObject(scriptableObject);
                    _content.Add(_artificeDrawer.CreateInspectorGUI(serializedObject));
                }
                // Plain editor window
                else
                {
                    var editorWindow = scriptableObject as EditorWindow;
                    
                    // Use reflection to call the private/protected 'OnGUI' method
                    var onGuiMethod = type.GetMethod("OnGUI", 
                        System.Reflection.BindingFlags.Instance | 
                        System.Reflection.BindingFlags.NonPublic | 
                        System.Reflection.BindingFlags.Public);
                    
                    var createGuiMethod = type.GetMethod("CreateGUI", 
                        System.Reflection.BindingFlags.Instance | 
                        System.Reflection.BindingFlags.NonPublic | 
                        System.Reflection.BindingFlags.Public);

                    if (onGuiMethod != null)
                    {
                        // We create a container that redirects the drawing to the window instance
                        var container = new IMGUIContainer(() =>
                        {
                            onGuiMethod?.Invoke(editorWindow, null);
                        });
                        _content.Add(container);
                    }
                    else if (createGuiMethod != null)
                    {
                        createGuiMethod?.Invoke(editorWindow, null);
                        _content.Add(editorWindow.rootVisualElement);
                    }
                    else
                    {
                        var label = new Label(
                            $"No OnGUI or CreateGUI methods found in EditorWindow {menuItem.Node.ScriptableObject.name}");
                        label.AddToClassList("content-container-error-prompt");
                        _content.Add(label);
                    }
                }
            }

            SavePersistedData();
        }

        public void AddItemsToMenu(GenericMenu menu)
        {
            menu.AddItem(new GUIContent("Refresh"), false, OnRefresh);
        }

        #region IArtifice_Persistence

        public abstract string ViewPersistenceKey { get; set; }

        public void SavePersistedData()
        {
            Artifice_SCR_PersistedData.instance.SaveData(ViewPersistenceKey, "selectedNode",
                _selectedMenuItem.Node.Title);
        }

        public void LoadPersistedData()
        {
            var selectedNodeTitle = Artifice_SCR_PersistedData.instance.LoadData(ViewPersistenceKey, "selectedNode");

            // If none saved.
            if (string.IsNullOrEmpty(selectedNodeTitle))
            {
                SetSelected(_nodeMap.First().Value);
                return;
            }

            // Set saved as selected
            foreach (var pair in _nodeMap)
            {
                if (pair.Key.Title == selectedNodeTitle)
                    SetSelected(pair.Value);
            }
        }

        #endregion
    }
}