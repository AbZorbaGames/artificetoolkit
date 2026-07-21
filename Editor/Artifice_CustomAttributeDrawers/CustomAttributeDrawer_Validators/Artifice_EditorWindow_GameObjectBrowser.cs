using System;
using System.Collections.Generic;
using ArtificeToolkit.Editor.Resources;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace ArtificeToolkit.Editor.Artifice_CustomAttributeDrawers.CustomAttributeDrawer_Validators
{
    public class Artifice_EditorWindow_GameObjectBrowser : EditorWindow
    {
        private class VisualElement_BrowserElement : VisualElement
        {
            #region FIELDS
            
            public bool IsExpanded;
            
            private readonly GameObject _gameObject;
            private readonly int _depth; 
            private readonly Type _searchType;
            private readonly UnityEvent<Object> _onSelectEvent;
            
            private readonly List<VisualElement_BrowserElement> _childrenElem;
            private string _searchText = "";

            private const int PaddingPerDepth = 20;
            
            #endregion

            public VisualElement_BrowserElement(GameObject gameObject, int depth, Type searchType, UnityEvent<Object> onSelectEvent)
            {
                _gameObject = gameObject;
                _childrenElem = new List<VisualElement_BrowserElement>();
                _onSelectEvent = onSelectEvent;
                _depth = depth;
                _searchType = searchType;
                
                // Build UI
                BuildUI();
            }

            #region Build UI

            private void BuildUI()
            {
                // Draw self
                Add(BuildSelfUI());

                // Build Children UI and Add
                var childrenElem = BuildChildrenUI();
                foreach (var childElem in childrenElem)
                {
                    _childrenElem.Add(childElem);
                    Add(childElem);
                }
                
                // Update children visibility to default
                UpdateChildrenVisibility();
            }
            
            private VisualElement BuildSelfUI()
            {
                // Create base container for it
                var container = new VisualElement();
                container.AddToClassList("browser-element-self-container");
                
                // Padding based in depth 
                container.style.paddingLeft = _depth * PaddingPerDepth;
                
                // Dropdown symbols
                if (_gameObject.transform.childCount > 0 && AreChildrenValidForBrowse(_gameObject))
                {
                    var dropdownSymbol = new Label("\u25bc");
                    container.Add(dropdownSymbol);
                    // Update rotation
                    HandleDropDownSymbolRotation(dropdownSymbol);
                    // Register events
                    dropdownSymbol.RegisterCallback<MouseDownEvent>(evt =>
                    {
                        IsExpanded = !IsExpanded;
                        UpdateChildrenVisibility();
                        HandleDropDownSymbolRotation(dropdownSymbol);
                    });
                }
                else // If not foldout symbol exists, compensate with extra padding.
                    container.style.paddingLeft = container.style.paddingLeft.value.value + 15;
                
                container.RegisterCallback<MouseDownEvent>(evt =>
                {
                    if (evt.clickCount == 2 && IsValidForBrowse(_gameObject))
                    {
                        if(_searchType == typeof(GameObject))
                            _onSelectEvent?.Invoke(_gameObject);
                        else
                            _onSelectEvent?.Invoke(_gameObject.GetComponent(_searchType));
                    }
                });
                
                // Add icon
                var image = new Image();
                image.sprite = GetRespectiveSprite();
                image.AddToClassList("image");
                if (Artifice_SCR_CommonResourcesHolder.instance.IsNeutralIcon(image.sprite))
                    image.AddToClassList("artifice-icon--neutral");
                container.Add(image);
                
                // Add Label
                var label = new Label(_gameObject.name);
                container.Add(label);
                
                return container;
            }

            private List<VisualElement_BrowserElement> BuildChildrenUI()
            {
                var children = new List<VisualElement_BrowserElement>();
                
                // Draw children
                for (var i = 0; i < _gameObject.transform.childCount; i++)
                {
                    var child = _gameObject.transform.GetChild(i).gameObject;
                    if (IsValidForBrowse(child, true))
                    {
                        var childBrowserElement = new VisualElement_BrowserElement(child, _depth + 1, _searchType, _onSelectEvent);
                        children.Add(childBrowserElement);
                    }
                }

                return children;
            }
            
            #endregion
            
            #region Search Validation
            
            private bool IsValidForBrowse(GameObject gameObject, bool includeChildren = false)
            {
                // If I fulfill either search type condition AND I am through the search text -> true
                if (IsValidForSearchTypeCondition(gameObject) && IsValidForSearchTextCondition(gameObject))
                    return true;

                // Otherwise, if a child fulfills this conditions -> true
                if (includeChildren && AreChildrenValidForBrowse(gameObject))
                    return true;
                
                return false;
            }

            private bool AreChildrenValidForBrowse(GameObject gameObject)
            {
                for (var i = 0; i < gameObject.transform.childCount; i++)
                {
                    var child = gameObject.transform.GetChild(i);
                    if (IsValidForBrowse(child.gameObject, true))
                        return true;
                }

                return false;
            }

            private bool IsValidForSearchTypeCondition(GameObject gameObject)
            {
                return _searchType == typeof(GameObject) || _searchType.IsSubclassOf(typeof(Component)) && gameObject.GetComponent(_searchType) != null;
            }

            private bool IsValidForSearchTextCondition(GameObject gameObject)
            {
                var gameObjectNameToLower = gameObject.name.ToLower();
                // _searchText is always to lower
                
                return _searchText == "" || gameObjectNameToLower.Contains(_searchText);
            }
            
            public void UpdateSearchText(string searchText)
            {
                _searchText = searchText.ToLower();
                // Update for children as well
                foreach (var childElem in _childrenElem)
                    childElem.UpdateSearchText(_searchText);
                
                UpdateChildrenVisibility();
            }
            
            #endregion
            
            #region Utility
            
            public void UpdateChildrenVisibility()
            {
                foreach (var childElem in _childrenElem)
                {
                    if (IsExpanded && IsValidForBrowse(childElem._gameObject, true))
                    {
                        childElem.RemoveFromClassList("hide");
                        childElem.UpdateChildrenVisibility();   
                    }
                    else
                        childElem.AddToClassList("hide");
                }
            }

            private void HandleDropDownSymbolRotation(VisualElement dropdownSymbol)
            {
                if (!IsExpanded)
                {
                    dropdownSymbol.RemoveFromClassList("rotate-0");
                    dropdownSymbol.AddToClassList("rotate-90");
                }
                else
                {
                    dropdownSymbol.RemoveFromClassList("rotate-90");
                    dropdownSymbol.AddToClassList("rotate-0");
                }
            }

            private Sprite GetRespectiveSprite()
            {
                if (_searchType.IsSubclassOf(typeof(Component)) && _gameObject.GetComponent(_searchType) != null)
                {
                    return _searchType == typeof(Transform) ? Artifice_SCR_CommonResourcesHolder.instance.TransformIcon : Artifice_SCR_CommonResourcesHolder.instance.ScriptIcon;
                }
                else
                    return Artifice_SCR_CommonResourcesHolder.instance.GameObjectIcon;
            }
            
            #endregion
        }

        #region FIELDS
        
        public readonly UnityEvent<Object> OnObjectSelected = new();
        
        private VisualElement _contentElem;
        private VisualElement_BrowserElement _rootElement;
        
        #endregion

        public void Browse(GameObject gameObject, Type searchType, Vector2 screenPosition)
        {
            var size = new Vector2(400, 600);
            
            var win = GetWindow<Artifice_EditorWindow_GameObjectBrowser>("GameObject Browser");
            win.position = new Rect(screenPosition - new Vector2(size.x, 0), size);
            win.minSize = new Vector2(300, 300);
            
            // Create root browser element
            _rootElement = new VisualElement_BrowserElement(gameObject, 1, searchType, OnObjectSelected);
            _rootElement.IsExpanded = true;
            _rootElement.UpdateChildrenVisibility();
            
            // Clear and add to content
            _contentElem.Clear();
            _contentElem.Add(_rootElement);
        }
     
        /* Mono */
        private void CreateGUI()
        {
            
            
            name = "Object Browser";
            
            rootVisualElement.styleSheets.Add(Artifice_Utilities.GetGlobalStyle());
            rootVisualElement.styleSheets.Add(Artifice_Utilities.GetStyle(GetType()));
            rootVisualElement.AddToClassList("gameobject-browser-container");
            
            // Create search box
            var searchBoxContainer = new VisualElement();
            searchBoxContainer.AddToClassList("search-box-container");
            rootVisualElement.Add(searchBoxContainer);

            var textField = new TextField();
            textField.value = "Search...";
            textField.AddToClassList("search-field");
            searchBoxContainer.Add(textField);
            textField.RegisterValueChangedCallback(evt =>
            {
                if (_rootElement != null)
                {
                    _rootElement.UpdateSearchText(evt.newValue);
                }
            });
            
            // Content elem
            _contentElem = new ScrollView();
            rootVisualElement.Add(_contentElem);
        }
        
        /* Mono */
        private void OnDisable()
        {
            OnObjectSelected.RemoveAllListeners();
        }
    }
}
