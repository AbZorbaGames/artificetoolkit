using System;
using System.Collections.Generic;
using System.Linq;
using ArtificeToolkit.Attributes;
using ArtificeToolkit.Editor.VisualElements;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace ArtificeToolkit.Editor
{
    public abstract class Artifice_VisualElement_AbstractListView : BindableElement, INotifyValueChanged<SerializedProperty>, IDisposable, IArtifice_Persistence
    {
        /// <summary> Helper nested class to handle the children of this class </summary>
        private class ChildElement
        {
            public readonly VisualElement VisualElement;
            public readonly SerializedProperty Property;
            public int PropertyArrayIndex;

            public ChildElement(VisualElement visualElement, SerializedProperty property, int propertyArrayIndex)
            {
                
                VisualElement = visualElement;
                Property = property;
                PropertyArrayIndex = propertyArrayIndex;
            }
        }

        /// <summary> Helper nested class to keep track of late property array swaps after dragging child elements </summary>
        private class ArrayElementSwapRecord
        {
            public readonly int X;
            public readonly int Y;

            public ArrayElementSwapRecord(int x, int y)
            {
                X = x;
                Y = y;
            }

        }
        
        // private class 
        
        #region FIELDS

        public bool ShouldForceArtifice { get; set; }
        public event EventHandler BuildUICompleted;
        
        private Label _listViewLabel;
        private VisualElement _childrenContainer;
        
        protected SerializedProperty Property;
        protected List<CustomAttribute> ChildrenInjectedCustomAttributes = new();
        protected readonly ArtificeDrawer ArtificeDrawer = new();
        protected bool HasListElementNameAttribute => _listElementNameAttribute != null;
        protected VisualElement ChildrenContainer => _childrenContainer;
        
        private readonly UIBuilder _uiBuilder = new();
        private readonly List<ChildElement> _children = new();
        private bool _isEditable = true;

        private static SerializedPropertyCopier _serializedPropertyCopier;
        private bool _disposed;
        private bool _isAttachedToPanel;
        private bool _wasAttachedToPanel;
        private bool _isBuildingListUI;
        private bool _isRebuildScheduled;
        private bool _scheduledRebuildRequiresFullBuild;
        private bool _rebuildAfterCurrentBuild;
        private IVisualElementScheduledItem _scheduledRebuild;
        private int _renderedArraySize = -1;
        private ListElementNameAttribute _listElementNameAttribute;
        
        /* Fields used for dragging elements for reposition */
        private bool _isDraggingElement;
        private ChildElement _draggedChild;
        private float _draggedElementStartY = -1;
        private float _draggedElementHeight = -1;
        private Vector2 _mouseStartPos = Vector2.zero;
        private readonly int _animationDuration = 300; // In ms
        private readonly List<ArrayElementSwapRecord> _lateSwapRecord = new();
        private readonly HashSet<VisualElement> _isBeingAnimated = new();
        
        #endregion
        
        public Artifice_VisualElement_AbstractListView()
        {
            // Load stylesheet
            styleSheets.Add(Artifice_Utilities.GetGlobalStyle());
            styleSheets.Add(Artifice_Utilities.GetStyle(typeof(Artifice_VisualElement_AbstractListView)));
            
            // Apply main container class
            AddToClassList("artifice-list");
            
            // Handler move event
            RegisterCallback<MouseMoveEvent>(OnMouseMove, TrickleDown.TrickleDown);
            RegisterCallback<MouseUpEvent>(OnMouseUp);
            RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
        }
        
        #region BUILD UI
        
        protected void BuildListUI()
        {
            if (_disposed || Property.Verify() == false)
                return;

            if (_isBuildingListUI)
            {
                _rebuildAfterCurrentBuild = true;
                return;
            }

            _isBuildingListUI = true;
            try
            {
                Debug.Assert(Property.isArray, "ArtificeListView only works with Array properties.");
                Property.serializedObject.UpdateIfRequiredOrScript();

                _uiBuilder.Create<VisualElement>(
                    "list",
                    elem =>
                    {
                        Add(elem);
                    },
                    elem =>
                    {
                        BeforeBuildUIStart();

                        _children.Clear();
                        _renderedArraySize = Property.arraySize;

                        // Build Prefab Override Indicator
                        elem.Add(BuildPrefabOverrideIndicatorUI());

                        // Build List Header
                        elem.Add(BuildListHeaderUI());

                        _childrenContainer = new VisualElement();
                        _childrenContainer.AddToClassList("children-container");
                        _childrenContainer.SetEnabled(_isEditable);

                        // Collapsed lists do not need to construct their potentially expensive
                        // child property trees. Expanding the header performs a full rebuild.
                        if (Property.isExpanded)
                        {
                            var prePropertyElem = BuildPrePropertyUI(Property);
                            if (prePropertyElem != null)
                                elem.Add(prePropertyElem);

                            if (_renderedArraySize == 0)
                            {
                                var emptyListLabel = new Label("List is empty.");
                                emptyListLabel.AddToClassList("empty-list-label");
                                _childrenContainer.Add(emptyListLabel);
                            }
                            else
                            {
                                for (var index = 0; index < _renderedArraySize; index++)
                                {
                                    var childProperty = Property.GetArrayElementAtIndex(index);
                                    var childElem = BuildListElementUI(childProperty, index);
                                    _children.Add(new ChildElement(childElem, childProperty, index));
                                    _childrenContainer.Add(childElem);
                                }
                            }

                            elem.Add(_childrenContainer);
                        }

                        OnBuildUICompleted();
                    }
                );

                LoadPersistedData();
            }
            finally
            {
                _isBuildingListUI = false;
            }

            if (_rebuildAfterCurrentBuild)
            {
                _rebuildAfterCurrentBuild = false;
                RequestRebuild();
            }
        }

        private void RequestRebuild(bool onlyIfStructureChanged = false)
        {
            if (_disposed || Property.Verify() == false)
                return;

            if (_isRebuildScheduled)
            {
                if (!onlyIfStructureChanged)
                    _scheduledRebuildRequiresFullBuild = true;
                return;
            }

            if (_isBuildingListUI)
            {
                _rebuildAfterCurrentBuild = true;
                return;
            }

            if (!_isAttachedToPanel)
                return;

            _isRebuildScheduled = true;
            _scheduledRebuildRequiresFullBuild = !onlyIfStructureChanged;
            _scheduledRebuild = schedule.Execute(() =>
            {
                _isRebuildScheduled = false;
                _scheduledRebuild = null;
                var requiresFullBuild = _scheduledRebuildRequiresFullBuild;
                _scheduledRebuildRequiresFullBuild = false;

                if (!_isAttachedToPanel || _disposed || Property.Verify() == false)
                    return;

                Property.serializedObject.UpdateIfRequiredOrScript();
                if (requiresFullBuild || Property.arraySize != _renderedArraySize)
                    BuildListUI();
            });
        }

        private void OnAttachToPanel(AttachToPanelEvent evt)
        {
            if (_disposed)
                return;

            _isAttachedToPanel = true;
            Undo.undoRedoPerformed -= OnUndoRedoPerformed;
            Undo.undoRedoPerformed += OnUndoRedoPerformed;

            // A reused visual element may have missed changes while it was detached.
            if (_wasAttachedToPanel)
                RequestRebuild();

            _wasAttachedToPanel = true;
        }

        private void OnDetachFromPanel(DetachFromPanelEvent evt)
        {
            _isAttachedToPanel = false;
            _isRebuildScheduled = false;
            _scheduledRebuildRequiresFullBuild = false;
            _scheduledRebuild?.Pause();
            _scheduledRebuild = null;
            Undo.undoRedoPerformed -= OnUndoRedoPerformed;
        }

        private void OnUndoRedoPerformed()
        {
            // Property bindings refresh values themselves. A costly full rebuild is only needed
            // when Undo changed the array structure (normally its size).
            RequestRebuild(onlyIfStructureChanged: true);
        }

        private VisualElement BuildPrefabOverrideIndicatorUI()
        {
            // Handle dynamic depth for indicator
            var prefabOverrideIndicator = new VisualElement();
            prefabOverrideIndicator.AddToClassList("list-header-prefab-override-indicator");
            prefabOverrideIndicator.style.left = -15 * (Property.depth + 1) - 4; // 15 is the default margin for nested properties. 4 is the total margin of the artifice drawer (?!).
                    
            // Change prefab indicator based on whether the value is a prefab override or not.
            prefabOverrideIndicator.style.display = Property.prefabOverride ? DisplayStyle.Flex : DisplayStyle.None;
            prefabOverrideIndicator.TrackPropertyValue(Property, trackedProperty =>
            {
                // Check for difference in size.
                prefabOverrideIndicator.style.display = trackedProperty.prefabOverride ? DisplayStyle.Flex : DisplayStyle.None;
                if(trackedProperty.arraySize != _renderedArraySize)
                    RequestRebuild();
            });

            return prefabOverrideIndicator;
        }
        private VisualElement BuildListHeaderUI()
        {
            var listHeader = new VisualElement();
            listHeader.AddToClassList("list-header");
            
            // Arrow symbol
            var arrowSymbolLabel = new Label("\u25bc");
            arrowSymbolLabel.AddToClassList("arrow-symbol-label");
            listHeader.Add(arrowSymbolLabel);
            if(Property.isExpanded == false)
                arrowSymbolLabel.AddToClassList("rotate-90");
            
            // Title of list
            _listViewLabel = new Label(Property.displayName);
            _listViewLabel.AddToClassList("list-title-label");
            listHeader.Add(_listViewLabel);
            
            // Size field
            var sizeProperty = Property.FindPropertyRelative("Array.size");
            
            var sizeField = new VisualElement();
            sizeField.AddToClassList("size-field");
            sizeField.SetEnabled(_isEditable);
            listHeader.Add(sizeField);
            
            var sizeTitleLabel = new Label("Size");
            sizeTitleLabel.AddToClassList("size-title-label");
            sizeField.Add(sizeTitleLabel);

            var sizeValueField = new IntegerField();
            sizeValueField.value = sizeProperty.intValue;   
            sizeValueField.Q(className: TextInputBaseField<int>.inputUssClassName)?.AddToClassList("size-text");
            sizeValueField.AddToClassList("size-value-field");
            sizeField.Add(sizeValueField);

            var sizeChangeCommitted = false;
            void CommitSizeChange()
            {
                if (sizeChangeCommitted)
                    return;

                var newSize = Mathf.Max(0, sizeValueField.value);
                if (newSize == Property.arraySize)
                    return;

                sizeChangeCommitted = true;
                sizeProperty.intValue = newSize;
                Property.serializedObject.ApplyModifiedProperties();
                RequestRebuild();
            }

            sizeValueField.RegisterCallback<KeyDownEvent>(evt =>
            {
                // Revert any changes on Escape
                if (evt.keyCode == KeyCode.Escape)
                {
                    sizeValueField.SetValueWithoutNotify(Property.arraySize);
                    evt.StopPropagation();
                }
                // Apply changes on Enter
                else if (evt.keyCode is KeyCode.Return or KeyCode.KeypadEnter)
                {
                    CommitSizeChange();
                    evt.StopPropagation();
                }
            });
            sizeValueField.RegisterCallback<FocusOutEvent>(_ => CommitSizeChange());
            
            // Add button for new elements
            var addButton = new Artifice_VisualElement_LabeledButton("+", OnAddItem);
            addButton.AddToClassList("add-button");
            addButton.SetEnabled(_isEditable);
            listHeader.Add(addButton);
            
            // Change isExpanded on click
            listHeader.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.button != 0 ||
                    (evt.target != listHeader && evt.target != arrowSymbolLabel && evt.target != _listViewLabel))
                    return;
                
                Property.isExpanded = !Property.isExpanded;
                Property.serializedObject.ApplyModifiedProperties();
                
                // change USS of arrow
                if (Property.isExpanded == false)
                {
                    arrowSymbolLabel.RemoveFromClassList("rotate-0");
                    arrowSymbolLabel.AddToClassList("rotate-90");
                }
                else
                {
                    arrowSymbolLabel.RemoveFromClassList("rotate-90");
                    arrowSymbolLabel.AddToClassList("rotate-0");
                }
                
                RequestRebuild();
                evt.StopPropagation();
            });

            // Register right-click context menu
            listHeader.AddManipulator(new ContextualMenuManipulator(evt =>
            {
                // Copy property path
                evt.menu.AppendAction("Copy Property Path", _ => { GUIUtility.systemCopyBuffer = Property.propertyPath; }, DropdownMenuAction.AlwaysEnabled);

                // Prefab Overrides
                if (Property.prefabOverride)
                {
                    evt.menu.AppendSeparator();

                    // This is enforced by Property.prefabOverride statement
                    var prefabLevels = GetPrefabVariantLevels((Property.serializedObject.targetObject  as MonoBehaviour)?.gameObject);

                    for (var i = prefabLevels.Count - 1; i >= 0; i--)
                    {
                        var prefabLevel = prefabLevels[i];
                        var label = i > 0 ? $"Apply as Override in Prefab '{prefabLevel.name}'" : $"Apply to Prefab '{prefabLevel.name}'";
                        evt.menu.AppendAction(label, action => ApplyToPrefab(Property, prefabLevel), DropdownMenuAction.AlwaysEnabled);
                    }
                    
                    evt.menu.AppendAction("Revert to Prefab", action => RevertToPrefab(Property), DropdownMenuAction.AlwaysEnabled);
                }
                
                // Copy / Paste
                evt.menu.AppendSeparator();
                evt.menu.AppendAction("Copy", action => DeepCopyProperty(Property), DropdownMenuAction.AlwaysEnabled);
                evt.menu.AppendAction("Paste", action => DeepPasteProperty(Property), 
                    _serializedPropertyCopier != null ? DropdownMenuAction.AlwaysEnabled : DropdownMenuAction.AlwaysDisabled);
            }));
            
            // Implement drag-and-drop elements into list
            listHeader.RegisterCallback<DragEnterEvent>(OnDragEnterEvent);
            listHeader.RegisterCallback<DragUpdatedEvent>(OnDragUpdatedEvent);
            listHeader.RegisterCallback<DragPerformEvent>(OnDragPerformEvent);
            listHeader.RegisterCallback<DragLeaveEvent>(OnDragLeaveEvent);
            listHeader.RegisterCallback<DragExitedEvent>(OnDragExitEvent);
            
            return listHeader;
        }
        private VisualElement BuildListElementUI(SerializedProperty property, int index)
        {
            // Add zebra-based styling.
            var elementContainer = new VisualElement();
            elementContainer.AddToClassList("element-container");
            if(index % 2 == 0)
                elementContainer.AddToClassList("element-container-even");
            else
                elementContainer.AddToClassList("element-container-odd");

            // Add drag control with mouse down event
            var dragControl = new Label("=");
            dragControl.AddToClassList("drag-control");
            dragControl.RegisterCallback<MouseDownEvent>(OnMouseDown);
            dragControl.RegisterCallback<DetachFromPanelEvent>(_ => dragControl.UnregisterCallback<MouseDownEvent>(OnMouseDown));

            // Inherited Implementation of BuildPropertyFieldUI
            var propertyField = BuildPropertyFieldUI(property, index) ?? new VisualElement();
            propertyField.AddToClassList("property-field");
            // Set dynamic name based on first string and ListElementName
            SetDynamicElementLabelName(property, index, propertyField);
            
            // Create Delete Button
            var deleteButtonContainer = new VisualElement();
            deleteButtonContainer.AddToClassList("delete-button-container");
            
            var deleteButton = new Artifice_VisualElement_LabeledButton("-", () => OnRemoveItem(property.GetIndexInArray()));
            deleteButton.AddToClassList("delete-button");
            deleteButtonContainer.Add(deleteButton);
            
            // Add everything to the list element container
            elementContainer.Add(dragControl);
            elementContainer.Add(propertyField);
            elementContainer.Add(deleteButtonContainer);

            return elementContainer;
        }

        protected virtual void BeforeBuildUIStart()
        {
            // A rebuild replaces every child created by this drawer. Dispose nested lists and
            // attribute drawers now instead of retaining them until the parent inspector closes.
            ArtificeDrawer.ReleaseVisualElementResources();
            _listElementNameAttribute = Property.GetCustomAttributes()
                .OfType<ListElementNameAttribute>()
                .FirstOrDefault();
            _isDraggingElement = false;
            _draggedChild = null;
            _lateSwapRecord.Clear();
            _isBeingAnimated.Clear();
        }
        
        protected virtual void OnBuildUICompleted()
        {
            BuildUICompleted?.Invoke(this, EventArgs.Empty);
        }
        protected virtual VisualElement BuildPrePropertyUI(SerializedProperty property)
        {
            return null;
        }
        protected abstract VisualElement BuildPropertyFieldUI(SerializedProperty property, int index);
        
        #endregion
        
        #region On Add / Remove

        protected virtual void OnAddItem()
        {
            Property.arraySize++;
            Property.serializedObject.ApplyModifiedProperties();
            RequestRebuild();
        }

        protected virtual void OnRemoveItem(int index)
        {
            var previousSize = Property.arraySize;
            Property.DeleteArrayElementAtIndex(index);

            // Object-reference arrays clear the reference on the first delete and remove the
            // slot on the second in affected Unity versions.
            if (Property.arraySize == previousSize)
                Property.DeleteArrayElementAtIndex(index);

            Property.serializedObject.ApplyModifiedProperties();
            RequestRebuild();
        }
        
        #endregion
        
        #region Drag and Drop Events

        private void OnDragEnterEvent(DragEnterEvent evt)
        {
            DragAndDrop.visualMode = CanAcceptObjectDrag()
                ? DragAndDropVisualMode.Link
                : DragAndDropVisualMode.Rejected;
            var elem = (VisualElement)evt.target; 
            elem.AddToClassList("drag-hover");
        }
        private void OnDragUpdatedEvent(DragUpdatedEvent evt)
        {
            // This needs to be set every update frame otherwise it is reseted.
            // If reseted, it never calls the DragPerform event 
            DragAndDrop.visualMode = CanAcceptObjectDrag()
                ? DragAndDropVisualMode.Generic
                : DragAndDropVisualMode.Rejected;
        }
        private void OnDragPerformEvent(DragPerformEvent evt)
        {
            var elem = (VisualElement)evt.target;
            var arrayChildrenType = Property.GetArrayChildrenType();
            if (!typeof(UnityEngine.Object).IsAssignableFrom(arrayChildrenType))
            {
                elem.RemoveFromClassList("drag-hover");
                return;
            }

            DragAndDrop.AcceptDrag();

            var data = DragAndDrop.objectReferences;
            var didAddElement = false;
            foreach (var datum in data)
            {
                UnityEngine.Object valueToAdd = null;
                if (datum != null && arrayChildrenType.IsAssignableFrom(datum.GetType()))
                    valueToAdd = datum;
                else if (datum is GameObject gameObject &&
                         typeof(Component).IsAssignableFrom(arrayChildrenType))
                    valueToAdd = gameObject.GetComponent(arrayChildrenType);

                if (valueToAdd == null)
                    continue;

                Property.arraySize++;
                Property.GetArrayElementAtIndex(Property.arraySize - 1).objectReferenceValue = valueToAdd;
                didAddElement = true;
            }

            if (didAddElement)
            {
                Property.serializedObject.ApplyModifiedProperties();
                RequestRebuild();
            }
            
            // Remove darkened container
            elem.RemoveFromClassList("drag-hover");
        }

        private bool CanAcceptObjectDrag()
        {
            var arrayChildrenType = Property.GetArrayChildrenType();
            return arrayChildrenType != null &&
                   typeof(UnityEngine.Object).IsAssignableFrom(arrayChildrenType);
        }
        private void OnDragLeaveEvent(DragLeaveEvent evt)
        {
            var elem = (VisualElement)evt.target; 
            elem.RemoveFromClassList("drag-hover");
        }
        private void OnDragExitEvent(DragExitedEvent evt)
        {
            var elem = (VisualElement)evt.target; 
            elem.RemoveFromClassList("drag-hover");
        }
        
        #endregion

        #region Element Mouse Drag Events

        private void OnMouseDown(MouseDownEvent evt)
        {
            var dragControlElem = ((VisualElement)evt.target); // Drag icon will have the element as parent.
            
            _mouseStartPos = evt.mousePosition;
            _isDraggingElement = true;
            _draggedChild = _children.Find(child => child.VisualElement == dragControlElem.parent);
            _draggedChild.VisualElement.AddToClassList("currently-dragged");
            _lateSwapRecord.Clear();

            // Children will be made absolute, so assign the default height by hand to the parent
            _draggedChild.VisualElement.parent.style.height = _draggedChild.VisualElement.parent.worldBound.height;
            
            // Make all children absolute, and translate them by their current position
            var topIt = 0f;
            foreach (var child in _children)
            {
                var childElem = child.VisualElement;
                var currentY = topIt;

                // For the specific element we want to drag, keep the original y position
                if (childElem == _draggedChild.VisualElement)
                {
                    _draggedElementStartY = currentY;
                    _draggedElementHeight = childElem.worldBound.height; // Cache this to be able to change USS size while dragging.
                }

                topIt += childElem.worldBound.height;
                // var currentY = childElem.style.top;
                childElem.style.position = Position.Absolute;
                childElem.style.width = Length.Percent(100);
                childElem.style.top = currentY;
            }
            
            // Send it to front so it overlays other elements.
            _draggedChild.VisualElement.BringToFront();
        }
        private void OnMouseUp(MouseUpEvent evt)
        {
            if (_isDraggingElement == false)
                return;
            
            // Late swap things in the serialized property array
            foreach (var record in _lateSwapRecord)
            {
                // MoveArrayElement does not auto copy isExpanded, do it by hand.
                var isExpandedX = Property.GetArrayElementAtIndex(record.X).isExpanded;
                var isExpandedY = Property.GetArrayElementAtIndex(record.Y).isExpanded;
                
                // Move the data
                Property.MoveArrayElement(record.X, record.Y);
                
                // Exchange their swapped is expanded
                Property.GetArrayElementAtIndex(record.Y).isExpanded = isExpandedX;
                Property.GetArrayElementAtIndex(record.X).isExpanded = isExpandedY;
            }
            Property.serializedObject.ApplyModifiedProperties();
            _lateSwapRecord.Clear();
            
            // Remove dragged visuals
            _draggedChild.VisualElement.RemoveFromClassList("currently-dragged");
            
            // Reset utility variables
            _isDraggingElement = false;
            _draggedChild = null;

            // Reordering was using absolute positions. Restore them to relative.
            foreach (var child in _children)
            {
                child.VisualElement.style.top = 0;
                child.VisualElement.style.position = Position.Relative;
            }
            
            RequestRebuild();
        }
        private void OnMouseMove(MouseMoveEvent evt)
        {
            if (_isDraggingElement == false)
                return;

            // Get base references
            var draggedElem = _draggedChild.VisualElement;
            var draggedChildIndex = _children.IndexOf(_draggedChild);
            
            var parentElem = draggedElem.parent;
            var mouseDy = evt.mousePosition.y - _mouseStartPos.y;

            // Freely move the _draggedChild based on mouseDy. Clamp value to not allow it to pass bounds
            draggedElem.style.top = Mathf.Clamp(_draggedElementStartY + mouseDy, -1, parentElem.worldBound.height - draggedElem.worldBound.height + 1);

            // Check for element before _dragged target
            if (draggedChildIndex > 0)
            {
                var previousChild = _children[draggedChildIndex - 1];
                var previousElem = previousChild.VisualElement;
                
                // Check bounds and make sure it is not already animated
                if (
                    draggedElem.worldBound.y < previousElem.worldBound.y + previousElem.worldBound.height / 2 &&
                    !_isBeingAnimated.Contains(previousElem)
                )
                {
                    SwapChildren(draggedChildIndex, draggedChildIndex - 1);
                    AnimateSlide(previousElem, _draggedElementHeight, true);
                }
            }

            if (draggedChildIndex < _children.Count - 1)
            {
                var nextChild = _children[draggedChildIndex + 1];
                var nextElem = nextChild.VisualElement;
                
                // Check bounds and make sure it is not already animated
                if (
                    draggedElem.worldBound.y + _draggedElementHeight > nextElem.worldBound.y + nextElem.worldBound.height / 2 && 
                    !_isBeingAnimated.Contains(nextElem)
                )
                {
                    SwapChildren(draggedChildIndex, draggedChildIndex + 1);
                    AnimateSlide(nextElem, _draggedElementHeight, false);
                }
            }
        }

        /* Utility */   
        private void AnimateSlide(VisualElement target, float height, bool downDirection)
        {
            _isBeingAnimated.Add(target);
            
            var sign = downDirection ? +1f : -1f;
            // Slerp animation for changing  position of the element
            var startValue = target.style.translate.value.y.value;
            var endValue = startValue + sign * height;
            var startHeight = target.style.top.value.value;
                        
            var anim = target.experimental.animation.Start(0, 1, _animationDuration, (elem, f) =>
            {
                var currentTranslateHeight = Mathf.SmoothStep(startValue, endValue, f);
                elem.style.top = startHeight + currentTranslateHeight;
            });
            anim.onAnimationCompleted += () =>
            {
                _isBeingAnimated.Remove(target);
            };
        }
        
        /* Utility*/
        private void SwapChildren(int source, int dest)
        {
            // Add record to late swap in serialized property array
            _lateSwapRecord.Add(new ArrayElementSwapRecord(source, dest));
            
            // Replace PropertyArrayIndices and position in list
            (_children[source].PropertyArrayIndex, _children[dest].PropertyArrayIndex) = (_children[dest].PropertyArrayIndex, _children[source].PropertyArrayIndex);
            (_children[source], _children[dest]) = (_children[dest], _children[source]);
        }
        
        #endregion
        
        #region Value Binding Pattern 
        
        public void SetValueWithoutNotify(SerializedProperty newValue)
        {
            Property = newValue.Copy();
            SetViewPersistenceKey(Property);
            BuildListUI();
        }
        
        public SerializedProperty value
        {
            get => Property;
            // The setter is called when the user changes the value of the ObjectField, which calls
            // OnObjectFieldValueChanged(), which calls this.
            set
            {
                if (value == this.value)
                    return;
                
                var previous = this.value;
                SetValueWithoutNotify(value);
                
                using (var evt = ChangeEvent<SerializedProperty>.GetPooled(previous, value))
                {
                    evt.target = this;
                    SendEvent(evt);
                }
            }
        }
        
        #endregion
        
        #region Utility

        public void Set_Enabled(bool enabled)
        {
            _isEditable = enabled;
            BuildListUI();
        }
        
        public void SetTitle(string title)
        {
            _listViewLabel.text = title;
        }
        
        public void SetChildrenInjectedCustomAttributes(List<CustomAttribute> childrenInjectedCustomAttributes)
        {
            ChildrenInjectedCustomAttributes = childrenInjectedCustomAttributes;
        }

        public void SetSerializedPropertyFilter(ArtificeDrawer.SerializedPropertyFilter filter)
        {
            ArtificeDrawer.SetSerializedPropertyFilter(filter);
        }

        private void SetDynamicElementLabelName(SerializedProperty property, int index, VisualElement propertyField)
        {
            if (propertyField == null)
                return;

            // Get the first label if it exists, and apply naming.
            var label = propertyField.Query<Label>().First();
            if (label != null && label.text == property.displayName)
            {
                // Cached list that may be used through-out the lifetime of the element 
                var firstStringValue = "";
                var listElementNameValue = "";
                
                // I do not like local methods, but this is really good.
                void UpdateElementLabel()
                {
                    label.text = firstStringValue != string.Empty ? firstStringValue : $"Element {index}";
                    label.text += listElementNameValue != string.Empty ? $" ({listElementNameValue})" : string.Empty;
                }
                
                // Check whether first child property is a string and override label text accordingly
                if (property.hasVisibleChildren)
                {
                    var firstChild = property.Copy();
                    if (
                        firstChild.NextVisible(true) &&
                        firstChild.propertyType == SerializedPropertyType.String
                    )
                    {
                        // Cache first string value
                        firstStringValue = firstChild.stringValue;
                        
                        // Call update method on change.
                        propertyField.TrackPropertyValue(firstChild, trackedProperty =>
                        {
                            firstStringValue = firstChild.stringValue;
                            UpdateElementLabel();
                        });;
                    }
                }   
                
                // Append custom list name after wards.
                if (_listElementNameAttribute != null)
                {
                    var fieldPropertyName = _listElementNameAttribute.FieldName;
                    var fieldProperty = property.FindPropertyRelative(fieldPropertyName);
                    if (fieldProperty != null)
                    {
                        listElementNameValue = fieldProperty.GetValueString();
                        
                        // Subscribe to change event to update value
                        label.TrackPropertyValue(fieldProperty, trackedProperty =>
                        {
                            listElementNameValue = fieldProperty.GetValueString();
                            UpdateElementLabel();
                        });
                    }
                    else
                        Artifice_Utilities.LogError($"Issue in abstract list view. Cannot find nested property <b>\"{fieldPropertyName}\"</b> of type <b>\"{Property.type}\"</b>");
                }
                
                // After everything has been hashed for the first time
                UpdateElementLabel();
            }
        }

        #endregion
        
        #region Context Menu Options
        
        private static List<GameObject> GetPrefabVariantLevels(GameObject instance)
        {
            var prefabLevels = new List<GameObject>();
            if (instance == null)
                return prefabLevels;

            var current = instance;
            while (true)
            {
                if(PrefabUtility.IsAnyPrefabInstanceRoot(current))
                    prefabLevels.Add(PrefabUtility.GetCorrespondingObjectFromSource(current));

                if (current.transform.parent == null)
                    break;
                
                current = current.transform.parent.gameObject;
            }

            return prefabLevels;
        }
        
        private void ApplyToPrefab(SerializedProperty property, GameObject prefabRoot)
        {
            // Apply changes to the prefab
            PrefabUtility.ApplyPropertyOverride(property, PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(prefabRoot), InteractionMode.UserAction);
            Property.serializedObject.ApplyModifiedProperties();
            RequestRebuild();
        }

        private void RevertToPrefab(SerializedProperty property)
        {
            // Revert changes to match the prefab
            PrefabUtility.RevertPropertyOverride(property, InteractionMode.UserAction);
            Property.serializedObject.Update();
            RequestRebuild();
        }
        
        /// <summary> Deep copies the list of a serialized property. </summary>
        private void DeepCopyProperty(SerializedProperty source)
        {
            if (_serializedPropertyCopier == null)
                _serializedPropertyCopier = new SerializedPropertyCopier();
            
            _serializedPropertyCopier.Copy(source);
        }

        private void DeepPasteProperty(SerializedProperty destination)
        {
            if (_serializedPropertyCopier == null)
                _serializedPropertyCopier = new SerializedPropertyCopier();
            
            _serializedPropertyCopier.Paste(destination);
            RequestRebuild();
        }
        
        #endregion
        
        #region Dispose Pattern
        
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                // Handler move event
                UnregisterCallback<MouseMoveEvent>(OnMouseMove, TrickleDown.TrickleDown);
                UnregisterCallback<MouseUpEvent>(OnMouseUp);
                UnregisterCallback<AttachToPanelEvent>(OnAttachToPanel);
                UnregisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
                
                // Unregister to undo for rebuild
                Undo.undoRedoPerformed -= OnUndoRedoPerformed;
                ArtificeDrawer.ReleaseVisualElementResources();
                _lateSwapRecord.Clear();
                _isBeingAnimated.Clear();
            }

            _disposed = true;
        }
        
        #endregion
        
        #region Save/Load Persistence
        
        public string ViewPersistenceKey { get; set; }
        
        public virtual void SavePersistedData()
        {
            
        }

        public virtual void LoadPersistedData()
        {
                
        }

        protected virtual void SetViewPersistenceKey(SerializedProperty property)
        {
            ViewPersistenceKey = property.propertyPath;
        }
        
        #endregion
    }
}
