using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ArtificeToolkit.Editor.VisualElements;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace ArtificeToolkit.Editor.Artifice_InspectorHeader
{
    /// <summary> Handles a single Dock. This is the component that is inserted at the top of the Inspector view.
    /// Multiple Docks can be present if more than one Inspector windows are open (1 Dock per inspector). </summary>
    public class Artifice_InspectorHeader_Dock
    {
        #region FIELDS

        public readonly EditorWindow InspectorWindow;

        private const string InspectorListClassName = "unity-inspector-editors-list";
        private const string InspectorNoMultiEditClassName = "unity-inspector-no-multi-edit-warning";
        private const string ComponentViewerName = "ComponentViewer";
        private const string AllButtonIconId = "ViewToolOrbit On";
        private const string FilterButtonIconId = "d_align_horizontally_right_active";

        private Object _inspectingObject;
        private readonly PropertyInfo _inspectorLockedPropertyInfo;
        private bool _inspectorWasLocked;
        private bool _isProjectPrefab;
        private bool _isProjectModel;

        private List<int> _filteredComponentIDs;
        private readonly Dictionary<int, Component> _indexToComponentDictionary = new();
        private readonly HashSet<string> _noMultiEditVisualElementsHashset = new();

        private VisualElement _rootVisualElement;
        private VisualElement _inspectorHeader;
        private VisualElement _filterComponentsContainer;
        private VisualElement _filterComponentsButton;
        private ToolbarSearchField _searchComponentsToolbar;
        private readonly List<VisualElement> _filterComponentButtons = new();
        private string _searchedComponentPrompt = string.Empty;

        private readonly Texture _allButtonIconTexture;
        private readonly Texture _filterButtonIconTexture;

        #endregion

        #region PUBLIC METHODS

        public void SetSearchedComponentPrompt(string searchedComponentPrompt)
        {
            if (IsInspectorLocked())
                return;
            _searchedComponentPrompt = searchedComponentPrompt;
            Update();
            _searchComponentsToolbar.SetValueWithoutNotify(_searchedComponentPrompt);
        }

        public Artifice_InspectorHeader_Dock(EditorWindow window, Object obj)
        {
            InspectorWindow = window;
            _inspectorLockedPropertyInfo =
                window.GetType().GetProperty("isLocked", BindingFlags.Public | BindingFlags.Instance);
            _inspectorWasLocked = IsInspectorLocked();
            _allButtonIconTexture = EditorGUIUtility.IconContent(AllButtonIconId).image;
            _filterButtonIconTexture = EditorGUIUtility.IconContent(FilterButtonIconId).image;
            SetDockSelectionToObject(obj);
        }

        public void SetDockSelectionToObject(Object obj)
        {
            // Reset inspector header if needed
            if (
                _inspectingObject == null ||
                _inspectingObject != obj && IsInspectorLocked() == false
            )
            {
                _inspectorHeader?.Clear();
                _inspectorHeader = null;
                _searchedComponentPrompt = string.Empty;
                _searchComponentsToolbar?.SetValueWithoutNotify(string.Empty);
            }

            _inspectingObject = obj;
            if (_inspectingObject is not GameObject)
                return;

            if (!_inspectingObject)
            {
                _isProjectPrefab = false;
                _isProjectModel = false;
                return;
            }

            RefreshNoMultiInspectVisualsSet();

            _filteredComponentIDs = new List<int>();
            var isAsset = AssetDatabase.Contains(_inspectingObject);
            var prefabType = PrefabUtility.GetPrefabAssetType(_inspectingObject);
            _isProjectPrefab = isAsset && prefabType is PrefabAssetType.Regular or PrefabAssetType.Variant;
            _isProjectModel = isAsset && prefabType is PrefabAssetType.Model;
        }

        public void Update()
        {
            if (!InspectingObjectIsValid())
                return;

            // This contains all visual elements that represent the objects components as children.
            _rootVisualElement ??= InspectorWindow.rootVisualElement.Q(null, InspectorListClassName);

            if (_rootVisualElement == null)
                return;

            if (InspectorJustUnlocked() && Selection.activeObject != _inspectingObject)
                SetDockSelectionToObject(Selection.activeObject);

            // Build and cache inspector header
            _inspectorHeader ??= BuildUI();

            // If inspector header has no parent, insert to root visual element.
            if (_inspectorHeader.parent == null)
            {
                if (!ShouldShowComponentViewerGui() && _rootVisualElement.childCount > GetComponentViewerIndex())
                    _rootVisualElement.Insert(GetComponentViewerIndex(), _inspectorHeader);
            }

            UpdateComponentVisibility();
        }

        public void RemoveGUI()
        {
            if (!InspectingObjectIsValid())
                return;

            if (ShouldShowComponentViewerGui())
                _rootVisualElement?.RemoveAt(GetComponentViewerIndex());
            _inspectorHeader?.Clear();
            _searchedComponentPrompt = string.Empty;
            InspectorWindow.Repaint();
        }

        public bool IsInspectorLocked()
        {
            return (bool)_inspectorLockedPropertyInfo.GetValue(InspectorWindow);
        }

        #endregion

        #region Build UI

        private VisualElement BuildUI()
        {
            _indexToComponentDictionary.Clear();
            var components = GetAllVisibleComponents();

            for (var i = 0; i < components.Count; i++)
                _indexToComponentDictionary.Add(i, components[i]);
         
            // Create main container.
            var mainContainer = new VisualElement();
            mainContainer.styleSheets.Add(Artifice_Utilities.GetGlobalStyle());
            mainContainer.styleSheets.Add(Artifice_Utilities.GetStyle(GetType()));
            
            // Add parts of the main container.
            mainContainer.Add(BuildUI_QuickToolsLine());
            mainContainer.Add(BuildUI_FilterComponentsList());

            return mainContainer;
        }

        private VisualElement BuildUI_QuickToolsLine()
        {
            var container = new VisualElement();
            container.AddToClassList("main-line");

            container.Add(BuildUI_InspectorHeaderButtons());
            container.Add(BuildUI_SearchBar());

            return container;
        }
        
        private VisualElement BuildUI_InspectorHeaderButtons()
        {
            var container = new VisualElement();
            container.AddToClassList("enhancer-options-container");

            var components = _indexToComponentDictionary.Values.ToList();

            var collapseAllButton =
                new Artifice_VisualElement_LabeledButton("Collapse All", () => { SetExpandState(components, false); });
            container.Add(collapseAllButton);

            var expandAllButton =
                new Artifice_VisualElement_LabeledButton("Expand All", () => { SetExpandState(components, true); });
            container.Add(expandAllButton);

            _filterComponentsButton = new Artifice_VisualElement_LabeledButton("Filter",
                () => { _filterComponentsContainer.ToggleInClassList("visibility-toggle"); });
            _filterComponentsButton.Insert(0, new Image()
            {
                image = _filterButtonIconTexture
            });
            container.Add(_filterComponentsButton);

            return container;
        }

        private VisualElement BuildUI_SearchBar()
        {
            // Create Search Field
            _searchComponentsToolbar = new ToolbarSearchField();
            _searchComponentsToolbar.AddToClassList("search-bar-container");
            _searchComponentsToolbar.RegisterValueChangedCallback(evt =>
            {
                _searchedComponentPrompt = evt.newValue;
                UpdateComponentVisibility();
            });

            return _searchComponentsToolbar;
        }

        private VisualElement BuildUI_FilterComponentsList()
        {
            var components = GetAllVisibleComponents();

            _filterComponentsContainer = new VisualElement();
            _filterComponentsContainer.AddToClassList("visibility-toggle");
            _filterComponentsContainer.AddToClassList("filter-components-container");

            // Add searchbar
            var searchbarField = new ToolbarSearchField();
            searchbarField.RegisterValueChangedCallback(evt =>
            {
                var searchText = evt.newValue;

                foreach (var filterComponentButton in _filterComponentButtons)
                {
                    var label = filterComponentButton.Q<Label>();
                    var type = label.text;

                    // Search check
                    if (string.IsNullOrEmpty(searchText) ||
                        type.Contains(searchText, StringComparison.InvariantCultureIgnoreCase))
                        filterComponentButton.RemoveFromClassList("hide");
                    else
                        filterComponentButton.AddToClassList("hide");
                }
            });
            _filterComponentsContainer.Add(searchbarField);

            // Add All button
            var filterAllComponentButton = BuildUI_FilterComponentButton("All", null);
            filterAllComponentButton.AddToClassList("filter-component-button-selected");
            filterAllComponentButton.Insert(0, new Image()
            {
                image = _allButtonIconTexture
            });
            _filterComponentsContainer.Add(filterAllComponentButton);

            // Create scroll view with snap scroll handling. 
            var scrollView = new ScrollView { mouseWheelScrollSize = 9 };
            scrollView.AddToClassList("filter-components-scrollView");
            scrollView.RegisterCallback<WheelEvent>(evt => { evt.StopPropagation(); });
            _filterComponentsContainer.Add(scrollView);

            // Register callback to all button
            filterAllComponentButton.RegisterCallback<MouseDownEvent>(_ =>
            {
                _filterComponentsButton.RemoveFromClassList("filter-component-button-selected");
                filterAllComponentButton.AddToClassList("filter-component-button-selected");
                foreach (var selectedComponent in _filterComponentButtons)
                    selectedComponent.RemoveFromClassList("filter-component-button-selected");

                _filteredComponentIDs.Clear();
                UpdateComponentVisibility();
            });

            // Add components as buttons
            foreach (var component in components)
            {
                var filterComponentButton = BuildUI_FilterComponentButton(component.GetType().Name, component);
                _filterComponentButtons.Add(filterComponentButton);
                scrollView.Add(filterComponentButton);

                // Add click callback to button
                filterComponentButton.RegisterCallback<MouseDownEvent>(evt =>
                {
                    if (evt.button == 0) // Add or remove selection among others.
                    {
                        if (_filteredComponentIDs.Contains(component.GetInstanceID()))
                        {
                            _filteredComponentIDs.Remove(component.GetInstanceID());
                            filterComponentButton.RemoveFromClassList("filter-component-button-selected");
                        }
                        else
                        {
                            _filteredComponentIDs.Add(component.GetInstanceID());
                            filterComponentButton.AddToClassList("filter-component-button-selected");
                        }
                    }
                    else if (evt.button == 1) // Deselect previous, and select right-clicked element.
                    {
                        _filteredComponentIDs.Clear();
                        foreach (var selectedComponent in _filterComponentButtons)
                            selectedComponent.RemoveFromClassList("filter-component-button-selected");

                        _filteredComponentIDs.Add(component.GetInstanceID());
                        filterComponentButton.AddToClassList("filter-component-button-selected");
                    }

                    // Update style class of all button
                    if (_filteredComponentIDs.Count == 0)
                    {
                        filterAllComponentButton.AddToClassList("filter-component-button-selected");
                        _filterComponentsButton.RemoveFromClassList("filter-component-button-selected");
                    }
                    else
                    {
                        filterAllComponentButton.RemoveFromClassList("filter-component-button-selected");
                        _filterComponentsButton.AddToClassList("filter-component-button-selected");
                    }

                    UpdateComponentVisibility();
                });
            }

            return _filterComponentsContainer;
        }

        private VisualElement BuildUI_FilterComponentButton(string title, Component component)
        {
            var filterComponentButton = new VisualElement();
            filterComponentButton.AddToClassList("filter-component-button");

            if (component != null)
            {
                var content = EditorGUIUtility.ObjectContent(component, component.GetType());
                var image = new Image
                {
                    image = content?.image
                };
                filterComponentButton.Add(image);
            }

            var label = new Label(title);
            filterComponentButton.Add(label);

            return filterComponentButton;
        }

        #endregion

        #region COMPONENT VISIBILITY CONTROL

        private void UpdateComponentVisibility()
        {
            var startIndex = GetComponentViewerIndex() + 1;
            var skippedComponentsCount = 0;

            var isSearchUsed = string.IsNullOrWhiteSpace(_searchedComponentPrompt) == false;
            var isFilterUsed = _filteredComponentIDs.Count > 0;

            for (var i = startIndex; i < _rootVisualElement.childCount; i++)
            {
                if (_noMultiEditVisualElementsHashset.Contains(_rootVisualElement[i].name))
                {
                    skippedComponentsCount++;
                    continue;
                }

                var compIndex = i - startIndex - skippedComponentsCount;
                if (_indexToComponentDictionary.TryGetValue(compIndex, out var component))
                {
                    var shouldShow = !(isFilterUsed &&
                                       _filteredComponentIDs.Contains(component.GetInstanceID()) == false);

                    if (shouldShow && isSearchUsed && !component.GetType().Name.Contains(_searchedComponentPrompt,
                            StringComparison.InvariantCultureIgnoreCase))
                        shouldShow = false;

                    // Final resolution of should show.
                    _rootVisualElement[i].style.display = shouldShow ? DisplayStyle.Flex : DisplayStyle.None;
                }
            }
        }

        private void RefreshNoMultiInspectVisualsSet()
        {
            _noMultiEditVisualElementsHashset.Clear();
            if (Selection.gameObjects.Length <= 1 || _rootVisualElement == null)
                return;

            var noMultiEditIndex = _rootVisualElement.childCount;
            for (var i = 0; i < _rootVisualElement.childCount; i++)
            {
                if (_rootVisualElement[i].ClassListContains(InspectorNoMultiEditClassName))
                {
                    noMultiEditIndex = i;
                    break;
                }
            }

            for (var i = noMultiEditIndex + 1; i < _rootVisualElement.childCount; i++)
                _noMultiEditVisualElementsHashset.Add(_rootVisualElement[i].name);
        }

        #endregion

        #region UTILITIES

        private bool ShouldShowComponentViewerGui()
        {
            var insertIndex = GetComponentViewerIndex();

            if (insertIndex >= _rootVisualElement.childCount)
                return false;

            var potentialComponentViewer = _rootVisualElement.hierarchy.ElementAt(insertIndex);
            return potentialComponentViewer is { name: ComponentViewerName };
        }

        private int GetComponentViewerIndex()
        {
            // Prefabs in project have an additional visual element on top.
            return _isProjectPrefab ? 2 : 1;
        }

        private bool InspectorJustUnlocked()
        {
            var currentlyLocked = IsInspectorLocked();
            var res = _inspectorWasLocked && !currentlyLocked;
            _inspectorWasLocked = currentlyLocked;
            return res;
        }

        private List<Component> GetAllVisibleComponents()
        {
            if (!InspectingObjectIsValid())
                return null;

            var selectedGameObject = _inspectingObject as GameObject;
            if (Selection.gameObjects.Length == 1)
                return GetAllVisibleComponents(selectedGameObject);

            // Get all visible components that each selected object shares
            var components = GetAllVisibleComponents(selectedGameObject);
            if (IsInspectorLocked())
                return components;

            foreach (var otherGameObject in Selection.gameObjects)
            {
                if (otherGameObject == selectedGameObject) continue;

                var otherComps = GetAllVisibleComponents(otherGameObject);
                // Going backwards to prevent the "change list size at iteration" thingy
                for (var i = components.Count - 1; i >= 0; i--)
                    if (!ComponentListContainsType(otherComps, components[i].GetType()))
                        components.RemoveAt(i);
            }

            return components;
        }

        private List<Component> GetAllVisibleComponents(GameObject gameObject)
        {
            var comps = gameObject.GetComponents<Component>();
            var res = new List<Component>(comps.Length);

            foreach (var comp in comps)
                if (comp && !comp.hideFlags.HasFlag(HideFlags.HideInInspector))
                    res.Add(comp);

            return res;
        }

        private bool ComponentListContainsType(List<Component> list, Type componentType)
        {
            foreach (var component in list)
                if (component.GetType() == componentType)
                    return true;

            return false;
        }

        private bool InspectingObjectIsValid() =>
            _inspectingObject && _inspectingObject is GameObject && !_isProjectModel;

        #endregion

        #region BUTTON CALLBACKS

        private void SetExpandState(List<Component> comps, bool expandAll)
        {
            foreach (var component in comps)
                InternalEditorUtility.SetIsInspectorExpanded(component, expandAll);

            ActiveEditorTracker.sharedTracker.ForceRebuild();
        }

        #endregion
    }
}