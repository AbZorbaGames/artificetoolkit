using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Debug = UnityEngine.Debug;

namespace ArtificeToolkit.Editor
{
    /// <summary> This class is used to render arrays and lists in a way the supports CustomAttributes and offers more functionality than Unity's default lists. </summary>
    /// <remarks><see cref="ArtificeDrawer"/></remarks>
    public class Artifice_VisualElement_TableListView : Artifice_VisualElement_AbstractListView
    {
        private class FieldColumnData
        {
            public string Name;
            public float WidthPercent;

            public VisualElement HeaderElement;
            public List<VisualElement> FieldElements;
            
            public FieldColumnData(string name)
            {
                Name = name;
                WidthPercent = -1;

                FieldElements = new List<VisualElement>();
            }

            public void Refresh()
            {
                HeaderElement.style.width = Length.Percent(WidthPercent);
                foreach (var fieldElem in FieldElements)
                    fieldElem.style.width = Length.Percent(WidthPercent);
            }
        }
        
        #region FIELDS
        
        private readonly List<FieldColumnData> _fieldColumns;
        private bool _disposed;

        private VisualElement _headerContainer;
        private List<VisualElement> _dragHandlers;
        
        private bool _isClicked;
        private int _selectedLeftColumnIndex;
        private const float MinimumColumnWidthPercent = 5f;
        private const string ElementValueColumnName = "$value";
        
        #endregion
        
        public Artifice_VisualElement_TableListView()
        {
            _fieldColumns = new List<FieldColumnData>();
            _dragHandlers = new List<VisualElement>();
            
            styleSheets.Add(Artifice_Utilities.GetStyle(GetType()));
            
            RegisterCallback<MouseMoveEvent>(OnMouseMoveEventHandler);
            RegisterCallback<MouseUpEvent>(OnMouseUpEventHandler);
        }

        protected override void BeforeBuildUIStart()
        {
            base.BeforeBuildUIStart();
            _fieldColumns.Clear();
            _dragHandlers.Clear();
            _headerContainer = null;
            _isClicked = false;
            _selectedLeftColumnIndex = -1;
        }

        protected override VisualElement BuildPrePropertyUI(SerializedProperty property)
        {
            _headerContainer = new VisualElement();
            _headerContainer.AddToClassList("header-container");

            var childType = property.GetArrayChildrenType();
            
            // Create field columns and label elements
            var fieldNames = childType
                .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(field =>
                    !field.IsStatic &&
                    !field.IsNotSerialized &&
                    (field.IsPublic || field.GetCustomAttribute<SerializeField>() != null))
                .Select(field => field.Name)
                .ToArray();

            // TableList on a primitive or otherwise fieldless type should still render values.
            if (fieldNames.Length == 0)
                fieldNames = new[] { ElementValueColumnName };

            foreach (var fieldName in fieldNames)
            {
                var data = new FieldColumnData(fieldName);
                data.WidthPercent = 100f / fieldNames.Length;
                _fieldColumns.Add(data);
                
                var labelContainer = new VisualElement();
                labelContainer.AddToClassList("column-label-container");
                labelContainer.style.width = Length.Percent(data.WidthPercent);
                _headerContainer.Add(labelContainer);
                data.HeaderElement = labelContainer;
                
                var label = new Label(fieldName == ElementValueColumnName ? "Value" : fieldName);
                labelContainer.Add(label);
            }
            
            // Create in between elements
            var percentTotal = 0f;
            for (var i = 0; i < _fieldColumns.Count - 1; i++)
            {
                // Create handler element
                var dragHandler = new VisualElement();
                dragHandler.AddToClassList("drag-handler");
                _dragHandlers.Add(dragHandler);
                
                // Add header container
                _headerContainer.Add(dragHandler);

                // Set position of drag handler based on previous width percents
                percentTotal += _fieldColumns[i].WidthPercent;
                dragHandler.style.left = Length.Percent(percentTotal);

                // Set callbacks
                var capturedI = i;
                dragHandler.RegisterCallback<MouseDownEvent>(evt =>
                {
                    OnMouseDownEventHandler(capturedI, evt);
                });
            }
                
            return _headerContainer;
        }
        
        protected override VisualElement BuildPropertyFieldUI(SerializedProperty property, int index)
        {
            // Iterate
            var propertyContainer = new VisualElement();
            propertyContainer.AddToClassList("property-container");

            foreach (var field in _fieldColumns)
            {
                // Create field for sub-property
                var fieldContainer = new VisualElement();
                fieldContainer.AddToClassList("field-container");
                propertyContainer.Add(fieldContainer);
                
                // Create sub property field
                var subProperty = field.Name == ElementValueColumnName
                    ? property
                    : property.FindPropertyRelative(field.Name);
                if (subProperty == null)
                {
                    fieldContainer.Add(new HelpBox(
                        $"Serialized field '{field.Name}' was not found.",
                        HelpBoxMessageType.Error));
                    continue;
                }

                var subPropertyField = ArtificeDrawer.CreatePropertyGUI(subProperty, true);
                if (subPropertyField == null)
                    continue;

                subPropertyField = ArtificeDrawer.CreateCustomAttributesGUI(subProperty, subPropertyField, ChildrenInjectedCustomAttributes);
                subPropertyField.AddToClassList("sub-property-field");
                
                // Add subPropertyField to its respective list.
                field.FieldElements.Add(fieldContainer);

                // Set width from field.Width
                fieldContainer.style.width = Length.Percent(field.WidthPercent);
                
                fieldContainer.Add(subPropertyField);
            }
            
            return propertyContainer;
        }

        private void OnMouseDownEventHandler(int leftColumnIndex, MouseDownEvent evt)
        {
            _isClicked = true;
            _selectedLeftColumnIndex = leftColumnIndex;
        }
        private void OnMouseMoveEventHandler(MouseMoveEvent evt)
        {
            if (!_isClicked)
                return;
            
            // Assert selected
            Debug.Assert(
                _selectedLeftColumnIndex >= 0 && _selectedLeftColumnIndex < _fieldColumns.Count - 1,
                $"SelectedLeftColumnIndex must be between 0 and {+_fieldColumns.Count - 1}"
            );

            // Get mouse deltaX;
            var mouseDelta = evt.mouseDelta;
            
            // Analogous percent
            var maxSize = _headerContainer.resolvedStyle.width;
            if (maxSize <= 0)
                return;

            var percentChange = 100f * mouseDelta.x / maxSize;
            var leftColumn = _fieldColumns[_selectedLeftColumnIndex];
            var rightColumn = _fieldColumns[_selectedLeftColumnIndex + 1];
            percentChange = Mathf.Clamp(
                percentChange,
                MinimumColumnWidthPercent - leftColumn.WidthPercent,
                rightColumn.WidthPercent - MinimumColumnWidthPercent);

            // Change width percents and refresh
            leftColumn.WidthPercent += percentChange;
            rightColumn.WidthPercent -= percentChange;
            leftColumn.Refresh();
            rightColumn.Refresh();
            
            // Update drag handler position
            var dragHandler =_dragHandlers[_selectedLeftColumnIndex];
            dragHandler.style.left = Length.Percent(dragHandler.style.left.value.value + percentChange);
        }
        private void OnMouseUpEventHandler(MouseUpEvent evt)
        {
            // Save load
            SavePersistedData();
            
            _isClicked = false;
            _selectedLeftColumnIndex = -1;
        }
        
        // Protected implementation of Dispose pattern.
        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    ArtificeDrawer.Dispose();
                    UnregisterCallback<MouseMoveEvent>(OnMouseMoveEventHandler);
                    UnregisterCallback<MouseUpEvent>(OnMouseUpEventHandler);
                }

                _disposed = true;
            }

            // Call base class implementation.
            base.Dispose(disposing);
        }

        #region Save/Load Persistence
        
        public override void SavePersistedData()
        {
            // Set foreach field, the width
            foreach (var fieldColumn in _fieldColumns)
                Artifice_SCR_PersistedData.instance.SaveData(ViewPersistenceKey, fieldColumn.Name, fieldColumn.WidthPercent.ToString());
        }

        public override void LoadPersistedData()
        {
            foreach (var fieldColumn in _fieldColumns)
            {
                var savedWidth = Artifice_SCR_PersistedData.instance.LoadData(ViewPersistenceKey, fieldColumn.Name);
                if(float.TryParse(savedWidth, out var width))
                {
                    fieldColumn.WidthPercent = width; 
                    fieldColumn.Refresh();
                }
            }

            var percentTotal = 0f;
            for(var i = 0; i < _dragHandlers.Count; i++)
            {
                percentTotal += _fieldColumns[i].WidthPercent;
                _dragHandlers[i].style.left = Length.Percent(percentTotal);
            }
        }

        protected override void SetViewPersistenceKey(SerializedProperty property)
        {
            ViewPersistenceKey = property.GetArrayChildrenType().ToString();
        }

        #endregion
    }
}
