using System;
using System.Collections.Generic;
using System.Linq;
using ArtificeToolkit.Attributes;
using ArtificeToolkit.Editor.VisualElements;
using CustomAttributes;
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

            public override int GetHashCode()
            {
                return X * 13 + Y * 17;
            }
        }
        
        #region FIELDS

        private Label _listViewLabel;
        
        protected SerializedProperty Property;
        protected List<CustomAttribute> ChildrenInjectedCustomAttributes = new();
        protected readonly ArtificeDrawer ArtificeDrawer = new();
        
        private readonly UIBuilder _uiBuilder = new();
        private readonly List<ChildElement> _children = new();

        private static SerializedProperty _copiedProperty; 
        private bool _disposed;
        
        /* Fields used for dragging elements for reposition */
        private bool _isDraggingElement;
        private ChildElement _draggedChild;
        private float _draggedElementStartY = -1;
        private float _draggedElementHeight = -1;
        private Vector2 _mouseStartPos = Vector2.zero;
        private readonly int _animationDuration = 300; // In ms
        private readonly HashSet<ArrayElementSwapRecord> _lateSwapRecord = new();
        private readonly HashSet<VisualElement> _isBeingAnimated = new();
        
        #endregion
        
        public Artifice_VisualElement_AbstractListView()
        {
            // Load stylesheet
            styleSheets.Add(Artifice_Utilities.GetStyle(typeof(Artifice_VisualElement_AbstractListView)));
            
            // Apply main container class
            AddToClassList("artifice-list");
            
            // Handler move event
            RegisterCallback<MouseMoveEvent>(OnMouseMove, TrickleDown.TrickleDown);
            RegisterCallback<MouseUpEvent>(OnMouseUp);
            
            // Register to undo for rebuild
            Undo.undoRedoPerformed -= BuildListUI;
            Undo.undoRedoPerformed += BuildListUI;
        }
        
        #region BUILD UI
        
        private void BuildListUI()
        {
            if (Property.Verify() == false)
                return;
            
            Debug.Assert(Property.isArray, "ArtificeListView only works with Array properties.");
            _uiBuilder.Create<VisualElement>(
                "list",
                elem =>
                {
                    Add(elem);
                },
                elem =>
                {
                    Property.serializedObject.Update();
                    
                    // Refresh lists
                    _children.Clear();
                    elem.Clear();

                    // Build Prefab Override Indicator
                    elem.Add(BuildPrefabOverrideIndicatorUI());
                    
                    // Build List Header
                    elem.Add(BuildListHeaderUI());
                    
                    // PreChildren Build
                    var prePropertyElem = BuildPrePropertyUI(Property);
                    elem.Add(prePropertyElem);
                    
                    // Add children
                    var index = 0;
                    var childrenContainer = new VisualElement();
                    childrenContainer.AddToClassList("children-container");

                    var childrenProperties = Property.GetVisibleChildren();
                    if (childrenProperties.Count == 1) // childProperty for list size will always exist, so Count == 1 means the list is empty
                    {
                        var emptyListLabel = new Label("List is empty.");
                        emptyListLabel.AddToClassList("empty-list-label");
                        childrenContainer.Add(emptyListLabel);
                    }
                    else
                    {
                        foreach (var childProperty in childrenProperties)
                        {
                            if (childProperty.propertyType == SerializedPropertyType.ArraySize)
                                continue;

                            var childElem = BuildListElementUI(childProperty, index);
                            _children.Add(new ChildElement(childElem, childProperty, index++));
                            childrenContainer.Add(childElem);
                        }
                    }
                    elem.Add(childrenContainer);

                    // Set children container hide state based on isExpanded
                    if (Property.isExpanded == false)
                    {
                        prePropertyElem?.AddToClassList("hide");
                        childrenContainer.AddToClassList("hide");
                    }
                    
                    Property.serializedObject.ApplyModifiedProperties();
                    OnBuildUICompleted();
                }
            );
            
            LoadPersistedData();
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
                prefabOverrideIndicator.style.display = Property.prefabOverride ? DisplayStyle.Flex : DisplayStyle.None;                            
                if(trackedProperty.arraySize != _children.Count)
                    BuildListUI();
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
            _listViewLabel.tooltip = "Artifice List:\nTreat this as exactly as you would treat Unity's default list. If you find missing functionality that you would like to add, contact the ArtificeDrawer developers.";
            
            // Size field
            var sizeProperty = Property.FindPropertyRelative("Array.size");
            
            var sizeField = new VisualElement();
            sizeField.AddToClassList("size-field");
            listHeader.Add(sizeField);
            
            var sizeTitleLabel = new Label("Size");
            sizeTitleLabel.AddToClassList("size-title-label");
            sizeField.Add(sizeTitleLabel);

            var sizeValueField = new IntegerField();
            sizeValueField.value = sizeProperty.intValue;   
            sizeValueField.Children().ToList()[0].AddToClassList("size-text");
            sizeValueField.AddToClassList("size-value-field");
            sizeField.Add(sizeValueField);

            sizeValueField.RegisterCallback<KeyDownEvent>(evt =>
            {
                // Revert any changes on Escape
                if (evt.keyCode == KeyCode.Escape)
                {
                    sizeProperty.intValue = _children.Count;
                }
                // Apply changes on Enter
                if (evt.keyCode == KeyCode.KeypadEnter && sizeValueField.value != _children.Count)
                {
                    sizeProperty.intValue = sizeValueField.value;
                    Property.serializedObject.ApplyModifiedProperties();
                    BuildListUI();
                }
            });
            sizeValueField.RegisterCallback<FocusOutEvent>(evt =>
            {
                if (sizeValueField.value == _children.Count)
                    return;
                
                // Apply changes on focus lose (click out of value)
                sizeProperty.intValue = sizeValueField.value;
                Property.serializedObject.ApplyModifiedProperties();
                BuildListUI();
            });
            
            // Add button for new elements
            var addButton = new Artifice_VisualElement_LabeledButton("+", OnAddItem);
            addButton.AddToClassList("add-button");
            listHeader.Add(addButton);
            
            // Change isExpanded on click
            listHeader.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.button != 0)
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
                
                BuildListUI();
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
                evt.menu.AppendAction("Copy", action => CopyProperty(), DropdownMenuAction.AlwaysEnabled);
                evt.menu.AppendAction("Paste", action => PastePropertyNested(Property, _copiedProperty), 
                    _copiedProperty != null ? DropdownMenuAction.AlwaysEnabled : DropdownMenuAction.AlwaysDisabled);
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
            var propertyField = BuildPropertyFieldUI(property, index);
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
        
        protected virtual void OnBuildUICompleted()
        {
            
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
            Property.serializedObject.Update();
            BuildListUI();
        }

        protected virtual void OnRemoveItem(int index)
        {
            Property.DeleteArrayElementAtIndex(index);
            Property.serializedObject.ApplyModifiedProperties();
            BuildListUI();
        }
        
        #endregion
        
        #region Drag and Drop Events

        private void OnDragEnterEvent(DragEnterEvent evt)
        {
            DragAndDrop.visualMode = DragAndDropVisualMode.Link;
            var elem = (VisualElement)evt.target; 
            elem.AddToClassList("drag-hover");
        }
        private void OnDragUpdatedEvent(DragUpdatedEvent evt)
        {
            // This needs to be set every update frame otherwise it is reseted.
            // If reseted, it never calls the DragPerform event 
            DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
        }
        private void OnDragPerformEvent(DragPerformEvent evt)
        {
            DragAndDrop.AcceptDrag();
            
            // Check if types match exactly
            var arrayChildrenType = Property.GetArrayChildrenType();
            
            var data = DragAndDrop.objectReferences;
            foreach (var datum in data)
            {
                // Add element to last position
                Property.InsertArrayElementAtIndex(Property.arraySize > 0 ? Property.arraySize - 1 : 0);
                
                // Get the last element
                var newProperty = Property.GetArrayElementAtIndex(Property.arraySize -1);
                var wasNewElementAdded = false;
                
                // If array children type is same with datum, simply assign it.
                if (arrayChildrenType == datum.GetType())
                {
                    newProperty.objectReferenceValue = datum;
                    wasNewElementAdded = true;
                }
                else if(datum is GameObject)  // Otherwise, if datum is a GameObject, search components for match
                {
                    var component = (datum as GameObject).GetComponent(arrayChildrenType);
                    if (component != null)
                    {
                        newProperty.objectReferenceValue = component;
                        wasNewElementAdded = true;
                    }
                }
                
                // In case nothing was added, remove newly added item and maybe console some error?
                if(!wasNewElementAdded)
                    Property.DeleteArrayElementAtIndex(Property.arraySize - 1);
                
                // Apply/Update
                Property.serializedObject.ApplyModifiedProperties();
                Property.serializedObject.Update();
            }
            
            // Rebuild list after loop
            BuildListUI();
            
            // Remove darkened container
            var elem = (VisualElement)evt.target;
            elem.RemoveFromClassList("drag-hover");
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
            
            BuildListUI();
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
            // Get the first label if it exists, and apply naming.
            var label = propertyField.Query<Label>().First();
            if (label != null && label.text == property.displayName)
            {
                // Cached values that may be used through-out the lifetime of the element 
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
                var listElementName = (ListElementNameAttribute)(Property.GetCustomAttributes().FirstOrDefault(attribute => attribute is ListElementNameAttribute));
                if (listElementName != null)
                {
                    var fieldPropertyName = listElementName.FieldName;
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
                        Debug.LogError($"[ArtificeDrawer][ListElementName] Cannot find nested property <b>\"{fieldPropertyName}\"</b> of type <b>\"{Property.type}\"</b>");
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
            BuildListUI();
        }

        private void RevertToPrefab(SerializedProperty property)
        {
            // Revert changes to match the prefab
            PrefabUtility.RevertPropertyOverride(property, InteractionMode.UserAction);
            Property.serializedObject.Update();
            BuildListUI();
        }
        
        private void CopyProperty()
        {
            // Store the current property as the "copied" property
            _copiedProperty = Property.Copy();
        }

        private void PastePropertyNested(SerializedProperty destination, SerializedProperty source)
        {
            if (source.isArray)
            {
                destination.ClearArray();
                destination.arraySize = source.arraySize;
                for (var i = 0; i < destination.arraySize; i++)
                    PastePropertyNested(destination.GetArrayElementAtIndex(i), source.GetArrayElementAtIndex(i));
            }
            else if (source.hasChildren)
            {
                // Use SerializedProperty iteration to handle children
                var sourceChild = source.Copy();
                var destinationChild = destination.Copy();

                var enterChildren = true;

                while (sourceChild.Next(enterChildren))
                {
                    // // Ensure we only copy properties within this object, not siblings
                    // if (!SerializedProperty.EqualContents(sourceChild, source))
                    //     break;

                    // Move destination child to match source's path
                    destinationChild = destination.FindPropertyRelative(sourceChild.name);

                    if (destinationChild != null)
                    {
                        PastePropertyNested(destinationChild, sourceChild);
                    }

                    enterChildren = false; // Only enter children for the first time
                }            
            }
            else
            {
                destination.Copy(source);
            }

            destination.serializedObject.ApplyModifiedProperties();
            destination.serializedObject.Update();
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
                
                // Unregister to undo for rebuild
                Undo.undoRedoPerformed -= BuildListUI;
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
