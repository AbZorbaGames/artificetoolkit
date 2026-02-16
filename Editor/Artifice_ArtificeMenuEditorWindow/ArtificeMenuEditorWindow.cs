using System;
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
    public abstract class ArtificeMenuEditorWindow : EditorWindow, IHasCustomMenu, IArtifice_Persistence
    {
        #region FIELDS
        
        public abstract string ViewPersistenceKey { get; set; }
        
        private VisualElement _menuPanel;
        private VisualElement _content;
        private ArtificeDrawer _artificeDrawer;
        private Artifice_VisualElement_ArtificeMenuItem _selectedMenuItem;
        private readonly Dictionary<ArtificeMenuTreeNode, Artifice_VisualElement_ArtificeMenuItem> _nodeMap = new();

        private readonly List<ScriptableObject> _soInstances = new();
        
        #endregion

        /* Mono */
        protected void CreateGUI() => OnRefresh();

        /* Mono */
        private void OnDestroy()
        {
            ClearInstances();
        }

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
            var parentMap = new Dictionary<ArtificeMenuTreeNode, Artifice_VisualElement_ArtificeMenuItem>();
            var queue = new Queue<ArtificeMenuTreeNode>(nodes);

            while (queue.Count > 0)
            {
                var node = queue.Dequeue();
                var menuItem = new Artifice_VisualElement_ArtificeMenuItem(node);
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

        private void SetSelected(Artifice_VisualElement_ArtificeMenuItem menuItem)
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

        #region Scriptable Object Instantiation

        protected T CreateAndRegister<T>() where T : ScriptableObject
        {
            var instance = CreateInstance(typeof(T)) as T;
            _soInstances.Add(instance);

            return instance;
        }

        private void ClearInstances()
        {
            foreach(var instance in _soInstances)
                DestroyImmediate(instance);
            
            _artificeDrawer.Dispose();
        }
        
        #endregion
        
        #region Persistence
        
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