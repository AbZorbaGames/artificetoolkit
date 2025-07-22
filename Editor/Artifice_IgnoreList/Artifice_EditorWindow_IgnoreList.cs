using System;
using System.Collections.Generic;
using System.Linq;
using ArtificeToolkit.Editor.VisualElements;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace ArtificeToolkit.Editor
{
    /// <summary> Editor window to preview ignored components and types. </summary>
    public class Artifice_EditorWindow_IgnoreList : ArtificeEditorWindow
    {
        #region NESTED

        private class TypesContainerElement : VisualElement
        {
            public override VisualElement contentContainer  => _scrollView;

            private readonly ScrollView _scrollView;
            
            public TypesContainerElement(string title)
            {
                AddToClassList("types-container");

                var label = new Label(title);
                label.AddToClassList("title");
                hierarchy.Add(label);

                _scrollView = new ScrollView();
                hierarchy.Add(_scrollView);
            }
        }
        
        #endregion
        
        #region FIELDS
        
        private static HashSet<string> _ignoreSet;
        private List<Type> _cachedAllTypes = null;

        private VisualElement _notIgnoredTypesContainer;
        private VisualElement _ignoredTypesContainer;

        #endregion

        public static void ShowWindow()
        {
            var wnd = GetWindow(typeof(Artifice_EditorWindow_IgnoreList));
            wnd.titleContent.text = "Artifice Ignore List";
        }
        
        protected override void CreateGUI()
        {
            Initialize();     

            // Base
            base.CreateGUI();
            rootVisualElement.Add(BuildUI_IgnoreHandlingContainers());
        }

        #region BUILD UI
        
        private void Initialize()
        {
            // Get style
            rootVisualElement.styleSheets.Add(Artifice_Utilities.GetGlobalStyle());
            rootVisualElement.styleSheets.Add(Artifice_Utilities.GetStyle(GetType()));
            
            // cache types from app domain
            if (_cachedAllTypes == null)
                _cachedAllTypes = AppDomain.CurrentDomain.GetAssemblies().SelectMany(assembly => assembly.GetTypes())
                    .ToList();
            
            // Find soIgnoreList
            _ignoreSet = new HashSet<string>(Artifice_Utilities.GetIgnoredTypeNames());
        }
        
        private VisualElement BuildUI_IgnoreHandlingContainers()
        {
            var container = new VisualElement();
            container.AddToClassList("ignored-types-container");
            
            var searchField = new ToolbarSearchField();
            container.Add(searchField);

            var typeContainers = new VisualElement();
            typeContainers.AddToClassList("align-horizontal");
            container.Add(typeContainers);

            _notIgnoredTypesContainer = new TypesContainerElement("Not Ignored");
            typeContainers.Add(_notIgnoredTypesContainer);
            _ignoredTypesContainer = new TypesContainerElement("Ignored");
            typeContainers.Add(_ignoredTypesContainer);

            // Set all ignored types to ignore map.
            foreach (var ignoredType in _ignoreSet)
            {
                var entry = BuildUI_CreateTypeEntry(ignoredType);
                _ignoredTypesContainer.Add(entry);
            }
            
            var buttonSearch = new Artifice_VisualElement_LabeledButton("Search", () =>
            {
                if (string.IsNullOrEmpty(searchField.value))
                    return;

                var filteredList = _cachedAllTypes.Where(type => type.Name.Contains(searchField.value));

                _notIgnoredTypesContainer.Clear();
                foreach (var type in filteredList)
                {
                    if(_ignoreSet.Contains(type.Name))
                        continue;
                    
                    _notIgnoredTypesContainer.Add(BuildUI_CreateTypeEntry(type.Name));
                }
            });
            buttonSearch.AddToClassList("btn-search");
            container.Add(buttonSearch);

            return container;
        }

        private Label BuildUI_CreateTypeEntry(string typeName)
        {
            var entry = new Label(typeName);
            entry.AddToClassList("type-label");

            // On Entry click, add it to type ignore list.
            entry.RegisterCallback<MouseDownEvent>(evt =>
            {
                // Toggle position in lists.
                if (entry.parent == _notIgnoredTypesContainer)
                {
                    Artifice_Utilities.AddIgnoredTypeName(entry.text);
                    _ignoreSet.Add(entry.text);
                    _ignoredTypesContainer.Add(entry);
                }
                else
                {
                    Artifice_Utilities.RemoveIgnoredTypeName(entry.text);
                    _ignoreSet.Remove(entry.text);
                    _notIgnoredTypesContainer.Add(entry);
                }
            });

            return entry;
        }

        #endregion
    }
}