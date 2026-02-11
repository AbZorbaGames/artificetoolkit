using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ArtificeToolkit.Editor.Resources;
using ArtificeToolkit.Editor.VisualElements;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace ArtificeToolkit.Editor.Artifice_InspectorHeader
{
    public class Artifice_InspectorHeader_Dock
    {
        #region Fields & Constants

        public readonly EditorWindow InspectorWindow;

        private const string InspectorListClassName = "unity-inspector-editors-list";
        private const string InspectorNoMultiEditClassName = "unity-inspector-no-multi-edit-warning";
        private const string ComponentViewerName = "ComponentViewer";

        private Object _inspectingObject;
        private readonly PropertyInfo _inspectorLockedPropertyInfo;
        private bool _inspectorWasLocked;
        private bool _isProjectPrefab;
        private bool _isProjectModel;

        // Logic State
        private readonly HashSet<int> _filteredComponentIDs = new();
        private readonly Dictionary<int, Component> _indexToComponentDictionary = new();
        private readonly HashSet<string> _noMultiEditVisualElementsHashset = new();
        private string _searchedComponentPrompt = string.Empty;

        // UI References
        private VisualElement _rootVisualElement;
        private VisualElement _inspectorHeader;
        private VisualElement _filterComponentsContainer;
        private VisualElement _filterComponentsButton;
        private ToolbarSearchField _searchComponentsToolbar;
        private VisualElement _filterAllButton;
        private readonly List<VisualElement> _filterComponentButtons = new();
        private readonly List<VisualElement> _categoryButtons = new();

        private readonly Texture _allButtonIconTexture;
        private readonly Texture _filterButtonIconTexture;

        #endregion

        public Artifice_InspectorHeader_Dock(EditorWindow window, Object obj)
        {
            InspectorWindow = window;
            _inspectorLockedPropertyInfo =
                window.GetType().GetProperty("isLocked", BindingFlags.Public | BindingFlags.Instance);
            _inspectorWasLocked = IsInspectorLocked();

            _allButtonIconTexture = EditorGUIUtility.IconContent("ViewToolOrbit On").image;
            _filterButtonIconTexture = EditorGUIUtility.IconContent("d_align_horizontally_right_active").image;

            SetDockSelectionToObject(obj);
        }

        #region Public API

        public void Update()
        {
            if (!InspectingObjectIsValid()) return;

            _rootVisualElement ??= InspectorWindow.rootVisualElement.Q(null, InspectorListClassName);
            if (_rootVisualElement == null) return;

            if (InspectorJustUnlocked() && Selection.activeObject != _inspectingObject)
                SetDockSelectionToObject(Selection.activeObject);

            if (_inspectorHeader == null)
                _inspectorHeader = BuildUI();

            if (_inspectorHeader.parent == null)
            {
                if (!ShouldShowComponentViewerGui() && _rootVisualElement.childCount > GetComponentViewerIndex())
                    _rootVisualElement.Insert(GetComponentViewerIndex(), _inspectorHeader);
            }

            UpdateComponentVisibility();
        }

        public void SetDockSelectionToObject(Object obj)
        {
            if (_inspectingObject == null || (_inspectingObject != obj && !IsInspectorLocked()))
            {
                ResetUIState();
            }

            _inspectingObject = obj;
            if (_inspectingObject is not GameObject) return;

            RefreshNoMultiInspectVisualsSet();

            var isAsset = AssetDatabase.Contains(_inspectingObject);
            var prefabType = PrefabUtility.GetPrefabAssetType(_inspectingObject);
            _isProjectPrefab = isAsset && prefabType is PrefabAssetType.Regular or PrefabAssetType.Variant;
            _isProjectModel = isAsset && prefabType is PrefabAssetType.Model;
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

        public bool IsInspectorLocked() => (bool)_inspectorLockedPropertyInfo.GetValue(InspectorWindow);

        #endregion

        #region Filtering Logic (Separation of Concern)

        private void ToggleComponentFilter(int instanceID, bool exclusive)
        {
            if (exclusive)
            {
                _filteredComponentIDs.Clear();
                _filteredComponentIDs.Add(instanceID);
            }
            else
            {
                if (_filteredComponentIDs.Contains(instanceID))
                    _filteredComponentIDs.Remove(instanceID);
                else
                    _filteredComponentIDs.Add(instanceID);
            }

            RefreshUIState_FilterComponents();
            RefreshUIStates_CategoryButtons();
            UpdateComponentVisibility();
        }

        private void FilterByType(Type targetType, bool exclusive)
        {
            // 1. Identify all components matching this type
            var matchingIDs = _indexToComponentDictionary.Values
                .Where(c => (targetType == typeof(MonoBehaviour)) ? c is MonoBehaviour : c.GetType() == targetType)
                .Select(c => c.GetInstanceID())
                .ToList();

            if (matchingIDs.Count == 0)
                return;

            if (exclusive)
            {
                // Check if we are already exclusively filtering exactly these IDs
                var isAlreadyExclusive = _filteredComponentIDs.Count == matchingIDs.Count &&
                                         _filteredComponentIDs.All(id => matchingIDs.Contains(id));

                _filteredComponentIDs.Clear();

                // Toggle: If it was already exclusive, clear it. If not, set it.
                if (!isAlreadyExclusive)
                {
                    foreach (var id in matchingIDs)
                        _filteredComponentIDs.Add(id);
                }
            }
            else
            {
                // Additive Toggle: If the first item of this type is already in, 
                // we assume the user wants to remove the group.
                var isAlreadyFiltered = _filteredComponentIDs.Contains(matchingIDs[0]);

                foreach (var id in matchingIDs)
                {
                    if (isAlreadyFiltered)
                        _filteredComponentIDs.Remove(id);
                    else
                        _filteredComponentIDs.Add(id);
                }
            }

            RefreshUIState_FilterComponents();
            RefreshUIStates_CategoryButtons();
            UpdateComponentVisibility();
        }

        private void ClearAllFilters()
        {
            _filteredComponentIDs.Clear();
            RefreshUIState_FilterComponents();
            RefreshUIStates_CategoryButtons();
            UpdateComponentVisibility();
        }

        #endregion

        #region UI Construction

        private VisualElement BuildUI()
        {
            _indexToComponentDictionary.Clear();
            var components = GetAllVisibleComponents();
            for (var i = 0; i < components.Count; i++)
                _indexToComponentDictionary.Add(i, components[i]);

            var root = new VisualElement();
            root.styleSheets.Add(Artifice_Utilities.GetGlobalStyle());
            root.styleSheets.Add(Artifice_Utilities.GetStyle(GetType()));

            root.Add(BuildUI_QuickToolsRow());
            root.Add(BuildUI_FilterPanel());
            
            if(Artifice_InspectorHeader_Main.CategoryButtonsEnabled)
                root.Add(BuildUI_FastCategoryRow());

            return root;
        }

        private VisualElement BuildUI_QuickToolsRow()
        {
            var row = new VisualElement();
            row.AddToClassList("quick-tools-container");

            // Buttons
            var btnContainer = new VisualElement();
            btnContainer.AddToClassList("enhancer-options-container");

            var comps = _indexToComponentDictionary.Values.ToList();
            btnContainer.Add(
                new Artifice_VisualElement_LabeledButton("Collapse All", () => SetExpandState(comps, false)));
            btnContainer.Add(new Artifice_VisualElement_LabeledButton("Expand All", () => SetExpandState(comps, true)));

            _filterComponentsButton = new Artifice_VisualElement_LabeledButton("Filter",
                () => _filterComponentsContainer.ToggleInClassList("visibility-toggle"));
            _filterComponentsButton.Insert(0, new Image { image = _filterButtonIconTexture });
            btnContainer.Add(_filterComponentsButton);

            // Search
            _searchComponentsToolbar = new ToolbarSearchField();
            _searchComponentsToolbar.AddToClassList("search-bar-container");
            _searchComponentsToolbar.RegisterValueChangedCallback(evt =>
            {
                _searchedComponentPrompt = evt.newValue;
                UpdateComponentVisibility();
            });

            row.Add(btnContainer);
            row.Add(_searchComponentsToolbar);
            return row;
        }

        private VisualElement BuildUI_FilterPanel()
        {
            _filterComponentsContainer = new VisualElement();
            _filterComponentsContainer.AddToClassList("visibility-toggle");
            _filterComponentsContainer.AddToClassList("filter-components-container");

            // Inner Search
            var innerSearch = new ToolbarSearchField();
            innerSearch.RegisterValueChangedCallback(OnFilterSearchChanged);
            _filterComponentsContainer.Add(innerSearch);

            // "All" Button
            _filterAllButton = BuildUI_FilterButton("All", null);
            _filterAllButton.Insert(0, new Image { image = _allButtonIconTexture });
            _filterAllButton.RegisterCallback<MouseDownEvent>(_ => ClearAllFilters());
            _filterComponentsContainer.Add(_filterAllButton);

            // List
            var scroll = new ScrollView { mouseWheelScrollSize = 9 };
            scroll.AddToClassList("filter-components-scrollView");

            foreach (var comp in _indexToComponentDictionary.Values)
            {
                var btn = BuildUI_FilterButton(comp.GetType().Name, comp);
                btn.RegisterCallback<MouseDownEvent>(
                    evt => ToggleComponentFilter(comp.GetInstanceID(), evt.button == 1));
                _filterComponentButtons.Add(btn);
                scroll.Add(btn);
            }

            _filterComponentsContainer.Add(scroll);
            RefreshUIState_FilterComponents();
            return _filterComponentsContainer;
        }

        private VisualElement BuildUI_FastCategoryRow()
        {
            var container = new VisualElement();
            container.AddToClassList("fast-category-type-list");

            var types = _indexToComponentDictionary.Values
                .Select(c => c is MonoBehaviour ? typeof(MonoBehaviour) : c.GetType())
                .Distinct();

            foreach (var type in types)
            {
                var btn = BuildUI_CategoryButton(type);
                btn.userData = type; // Store the type here
                btn.RegisterCallback<MouseDownEvent>(evt => { FilterByType(type, evt.button == 1); });
                _categoryButtons.Add(btn);
                container.Add(btn);
            }

            return container;
        }

        #endregion

        #region Helper Methods

        private void UpdateComponentVisibility()
        {
            var startIndex = GetComponentViewerIndex() + 1;
            var skipped = 0;
            var hasFilter = _filteredComponentIDs.Count > 0;
            var hasSearch = !string.IsNullOrWhiteSpace(_searchedComponentPrompt);

            for (var i = startIndex; i < _rootVisualElement.childCount; i++)
            {
                if (_noMultiEditVisualElementsHashset.Contains(_rootVisualElement[i].name))
                {
                    skipped++;
                    continue;
                }

                var compIndex = i - startIndex - skipped;
                if (_indexToComponentDictionary.TryGetValue(compIndex, out var comp))
                {
                    var visible = true;
                    if (hasFilter && !_filteredComponentIDs.Contains(comp.GetInstanceID())) visible = false;
                    if (visible && hasSearch && !comp.GetType().Name
                            .Contains(_searchedComponentPrompt, StringComparison.OrdinalIgnoreCase)) visible = false;

                    _rootVisualElement[i].style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
                }
            }
        }

        private void RefreshUIState_FilterComponents()
        {
            var hasFilters = _filteredComponentIDs.Count > 0;

            // Update "All" button
            _filterAllButton.EnableInClassList("filter-component-button-selected", !hasFilters);
            _filterComponentsButton.EnableInClassList("filter-component-button-selected", hasFilters);

            // Update individual buttons based on IDs
            foreach (var btn in _filterComponentButtons)
            {
                var comp = btn.userData as Component;
                if (comp == null) continue;
                btn.EnableInClassList("filter-component-button-selected",
                    _filteredComponentIDs.Contains(comp.GetInstanceID()));
            }
        }

        private void RefreshUIStates_CategoryButtons()
        {
            foreach (var btn in _categoryButtons)
            {
                if (btn.userData is not Type btnType) 
                    continue;
                
                // 1. Get all components currently in the inspector that match this button's type
                var matchingComponents = _indexToComponentDictionary.Values
                    .Where(component => (btnType == typeof(MonoBehaviour)) ? component is MonoBehaviour : component.GetType() == btnType)
                    .ToList();

                if (matchingComponents.Count == 0)
                {
                    btn.EnableInClassList("fast-category-type-button-selected", false);
                    continue;
                }

                var isTypeFiltered = matchingComponents.All(c => _filteredComponentIDs.Contains(c.GetInstanceID()));
                
                btn.EnableInClassList("fast-category-type-button-selected", isTypeFiltered);
            }
        }

        private void OnFilterSearchChanged(ChangeEvent<string> evt)
        {
            foreach (var btn in _filterComponentButtons)
            {
                var label = btn.Q<Label>();
                var match = string.IsNullOrEmpty(evt.newValue) ||
                            label.text.Contains(evt.newValue, StringComparison.OrdinalIgnoreCase);
                btn.EnableInClassList("hide", !match);
            }
        }

        private VisualElement BuildUI_FilterButton(string title, Component comp)
        {
            var btn = new VisualElement();
            btn.AddToClassList("filter-component-button");
            btn.userData = comp;

            if (comp != null)
            {
                var icon = EditorGUIUtility.ObjectContent(comp, comp.GetType()).image;
                btn.Add(new Image { image = icon });
            }

            btn.Add(new Label(title));
            return btn;
        }

        private VisualElement BuildUI_CategoryButton(Type type)
        {
            var btn = new VisualElement { tooltip = $"{type.Name}\n  Left Click: Add to selections\n  Right Click: Reset selections and add." };
            btn.AddToClassList("fast-category-type-button");

            var icon = (type == typeof(MonoBehaviour))
                ? Artifice_SCR_CommonResourcesHolder.instance.ScriptIcon.texture
                : (Texture2D)EditorGUIUtility.ObjectContent(null, type).image;

            if (icon) btn.Add(new Image { image = icon });
            return btn;
        }

        private void ResetUIState()
        {
            _inspectorHeader?.Clear();
            _inspectorHeader = null;
            _searchedComponentPrompt = string.Empty;
            _filteredComponentIDs.Clear();
            _filterComponentButtons.Clear();
            _categoryButtons.Clear();
            _searchComponentsToolbar?.SetValueWithoutNotify(string.Empty);
        }

        private int GetComponentViewerIndex() => _isProjectPrefab ? 2 : 1;

        private bool InspectingObjectIsValid() =>
            _inspectingObject && _inspectingObject is GameObject && !_isProjectModel;

        private bool InspectorJustUnlocked()
        {
            var current = IsInspectorLocked();
            var changed = _inspectorWasLocked && !current;
            _inspectorWasLocked = current;
            return changed;
        }

        private void RefreshNoMultiInspectVisualsSet()
        {
            _noMultiEditVisualElementsHashset.Clear();
            if (Selection.gameObjects.Length <= 1 || _rootVisualElement == null) return;

            var splitIndex = -1;
            for (var i = 0; i < _rootVisualElement.childCount; i++)
            {
                if (_rootVisualElement[i].ClassListContains(InspectorNoMultiEditClassName))
                {
                    splitIndex = i;
                    break;
                }
            }

            if (splitIndex == -1) return;
            for (var i = splitIndex + 1; i < _rootVisualElement.childCount; i++)
                _noMultiEditVisualElementsHashset.Add(_rootVisualElement[i].name);
        }

        private List<Component> GetAllVisibleComponents()
        {
            if (!InspectingObjectIsValid()) return new List<Component>();
            var target = _inspectingObject as GameObject;

            var baseList = GetVisibleFromGameObject(target);
            if (Selection.gameObjects.Length <= 1 || IsInspectorLocked()) return baseList;

            // Intersection of components for multi-selection
            foreach (var other in Selection.gameObjects)
            {
                if (other == target) continue;
                var otherList = GetVisibleFromGameObject(other);
                baseList.RemoveAll(c => !otherList.Any(oc => oc.GetType() == c.GetType()));
            }

            return baseList;
        }

        private List<Component> GetVisibleFromGameObject(GameObject go) =>
            go.GetComponents<Component>()
                .Where(c => c && !c.hideFlags.HasFlag(HideFlags.HideInInspector))
                .ToList();

        private void SetExpandState(List<Component> comps, bool expand)
        {
            foreach (var c in comps) InternalEditorUtility.SetIsInspectorExpanded(c, expand);
            ActiveEditorTracker.sharedTracker.ForceRebuild();
        }

        private bool ShouldShowComponentViewerGui()
        {
            var idx = GetComponentViewerIndex();
            if (idx >= _rootVisualElement.childCount) return false;
            return _rootVisualElement[idx]?.name == ComponentViewerName;
        }

        #endregion
    }
}