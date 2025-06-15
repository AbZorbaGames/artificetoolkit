using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ArtificeToolkit.Attributes;
using ArtificeToolkit.Editor.Artifice_CustomAttributeDrawers;
using ArtificeToolkit.Editor.Artifice_CustomAttributeDrawers.CustomAttributeDrawer_ButtonAttribute;
using ArtificeToolkit.Editor.Artifice_CustomAttributeDrawers.CustomAttributeDrawers_Groups;
using ArtificeToolkit.Editor.Resources;
using ArtificeToolkit.Editor.VisualElements;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

// ReSharper disable GCSuppressFinalizeForTypeWithoutDestructor
// ReSharper disable CanSimplifyDictionaryLookupWithTryGetValue
// ReSharper disable MemberCanBeMadeStatic.Local
// ReSharper disable RedundantIfElseBlock

namespace ArtificeToolkit.Editor
{
    [InitializeOnLoad]
    public sealed class ArtificeDrawer : IDisposable
    {
        #region FIELDS

        private readonly Stack<IDisposable> _disposableStack = new();
        private bool _disposed;

        // Cached results for custom attribute usage
        private readonly Dictionary<SerializedProperty, bool> _doesRequireVisualElementsCache = new();
        
        // Type cache for performance
        private static readonly Dictionary<string, Type> TypeCache = new();

        /// <summary> String properties that should be ignored from Artifice. </summary>
        private static readonly HashSet<string> PropertyIgnoreSet;

        // Delegate declaration for serialized property filter method.
        public delegate bool SerializedPropertyFilter(SerializedProperty property);
        private SerializedPropertyFilter _serializedPropertyFilter = property => true;
        
        #endregion

        /// <summary> Static constructor initializes ArrayAppliedCustomAttributes variable since its reused for all artifice drawer instances. </summary>
        static ArtificeDrawer()
        {
            // Refresh toggle of artifice drawer to secure consistency throughout package updates.
            Artifice_Utilities.ToggleArtificeDrawer(Artifice_Utilities.ArtificeDrawerEnabled);
            
            PropertyIgnoreSet = new HashSet<string>()
            {
                // "Serialized Data Mode Controller",
                "Serialized Data Mode Controller",
            };
        }
        
        /// <summary> Returns the ArtificeInspector of a SerializedObject. </summary>
        public VisualElement CreateInspectorGUI(SerializedObject serializedObject)
        {
            // Do nothing while compiling.
            if (EditorApplication.isCompiling)
                return new VisualElement();
            
            // Make sure serialized object is updated
            serializedObject.Update(); 
            
            // Create initialized artifice inspector container
            var artificeInspector = CreateArtificeInspectorContainerGUI(serializedObject);
            
            // Check whether target object is missing
            if (serializedObject.targetObject == null)
            {
                artificeInspector.Add(CreateScriptMissingUI(serializedObject));
                return artificeInspector;
            }
            
            // Fully render out its visible children properties
            foreach (var property in serializedObject.GetIterator().GetVisibleChildren().SortProperties())
            {
                if (PropertyIgnoreSet.Contains(property.displayName))
                    continue;
                
                artificeInspector.Add(CreatePropertyGUI(property.Copy()));
            }

            // Create optional method buttons Foldout Group for serializedObject
            artificeInspector.Add(CreateMethodsGUI(serializedObject));
            
            // Add artifice indicator if artifice has been used.
            if (_doesRequireVisualElementsCache.Any(pair => pair.Value))
                artificeInspector.Add(CreateArtificeIndicatorGUI(serializedObject));
            
            // Apply any modified property
            serializedObject.ApplyModifiedProperties(); 

            return artificeInspector;
        }

        /// <summary> Returns an initialized VisualElement container to be used for the Artifice inspector </summary>
        private VisualElement CreateArtificeInspectorContainerGUI(SerializedObject serializedObject)
        {
            var artificeContainer = new VisualElement
            {
                name = serializedObject.GetHashCode().ToString()
            };

            // If for some reason this occurs, the Inspector would be empty and not easily debuggable.
            // Hopefully, the thrown exception will help pinpoint what went wrong.
            if (Artifice_Utilities.GetGlobalStyle() == null || Artifice_Utilities.GetStyle(GetType()) == null)
                throw new Exception("GlobalStyle or ArtificeStyle not found.");

            artificeContainer.styleSheets.Add(Artifice_Utilities.GetGlobalStyle()); // This propagates to all children.
            artificeContainer.styleSheets.Add(Artifice_Utilities.GetStyle(GetType())); // Supports
            
            return artificeContainer;
        }

        /// <summary> Receives a SerializedProperty as a parameter and returns its Artifice GUI </summary>
        public VisualElement CreatePropertyGUI(SerializedProperty property, bool forceArtificeStyle = false, bool useFoldoutForVisibleChildren = true)
        {
            var container = new VisualElement();
            container.AddToClassList("property-container");

            // If filtered, return empty container.
            if (_serializedPropertyFilter.Invoke(property) == false)
                return null;
            
            // Check if property enforces Artifice in following calls.
            var customAttributes = property.GetCustomAttributes();
            if (customAttributes != null && customAttributes.Any(attribute => attribute is ForceArtificeAttribute))
                forceArtificeStyle = true;

            if (forceArtificeStyle || DoesRequireArtificeRendering(property))
            {
                // Arrays need to use custom Artifice List Views (and not a string value!)
                if (property.IsArray())
                {
                    // Discern which properties are to be applied to the list and which to its children.
                    SplitCustomPropertiesForArrays(property, out var arrayCustomAttributes, out var childrenCustomAttributes);
                    
                    // Check whether it should be drawn with table list
                    var isTableList = property.GetAttributes().Any(attribute => attribute.GetType() == typeof(TableListAttribute));
                        
                    // Spawn either ListView or TableView
                    var listView = isTableList ? (Artifice_VisualElement_AbstractListView)new Artifice_VisualElement_TableListView() : new Artifice_VisualElement_ListView();
                    listView.SetSerializedPropertyFilter(_serializedPropertyFilter);
                    listView.SetChildrenInjectedCustomAttributes(childrenCustomAttributes);
                    listView.ShouldForceArtifice = forceArtificeStyle;
                    listView.value = property;
                    container.Add(CreateCustomAttributesGUI(property, listView, arrayCustomAttributes));
                    
                    _disposableStack.Push(listView); // Add to disposable list
                }
                // If property is a serialized reference of an interface, allow to select which type of interface inheritors to spawn
                else if (property.IsManagedReference())
                {
                    container.Add(CreateSerializeReferenceFieldGUI(property));
                }
                // If property has visible children, wrap it in a foldout to mimic unity's default behaviour or use any potential implemented custom property drawer.
                else if (property.hasVisibleChildren)
                {
                    var hasCustomPropertyDrawer = Artifice_CustomDrawerUtility.HasCustomDrawer(property);
                    if (hasCustomPropertyDrawer)
                    {
                        var customPropertyField = Artifice_CustomDrawerUtility.CreatePropertyGUI(property);
                        
                        // In case the custom property utility fails, fallback to a default property field (this was seen in issue #29)
                        customPropertyField = customPropertyField ?? new PropertyField(property);
                        
                        customPropertyField = CreateCustomAttributesGUI(property, customPropertyField);
                        container.Add(customPropertyField);
                    }
                    else
                    {
                        VisualElement childrenContainer;

                        // Optionally use foldout for visible children, or have them just placed in order.
                        if (useFoldoutForVisibleChildren)
                        {
                            childrenContainer = new Foldout
                            {
                                value = property.isExpanded,
                                text = property.displayName
                            };
                            childrenContainer.AddToClassList("nested-field-property");
                            ((Foldout)childrenContainer).BindProperty(property); // Bind to make foldout state (open-closed) be persistent
                        }
                        else
                            childrenContainer = new VisualElement();

                        // Create property for each child
                        foreach (var child in property.GetVisibleChildren().SortProperties())
                            childrenContainer.Add(CreatePropertyGUI(child, forceArtificeStyle));
                        
                        // Create methods group
                        childrenContainer.Add(CreateMethodsGUI(property));
                        
                        container.Add(CreateCustomAttributesGUI(property, childrenContainer));
                    }
                }
                else
                {
                    var defaultPropertyField = new PropertyField(property);
                    defaultPropertyField.BindProperty(property);
                    container.Add(CreateCustomAttributesGUI(property, defaultPropertyField));
                }
            }
            else
            {
#if UNITY_2022_2_OR_NEWER
                var field = new PropertyField(property);
                field.Bind(property.serializedObject);
                container.Add(field);
#else
                container.Add(CreateIMGUIField(property));
#endif
            }
            
            return container;
        }

        /// <summary> Uses <see cref="CustomAttribute"/> and <see cref="Artifice_CustomAttributeDrawer"/> to change how the parameterized <see cref="VisualElement"/> will look like using the property's custom attributes. </summary>
        public VisualElement CreateCustomAttributesGUI(SerializedProperty property, VisualElement propertyField)
        {
            var customAttributes = property.GetCustomAttributes();
            if (DoesRequireArtificeRendering(property) == false)
                return propertyField;

            return CreateCustomAttributesGUI(property, propertyField, customAttributes.ToList());
        }
        
        /// <summary> Uses <see cref="CustomAttribute"/> and <see cref="Artifice_CustomAttributeDrawer"/> to change how the parameterized <see cref="VisualElement"/> will look like with any parameterized custom attributes. </summary>
        public VisualElement CreateCustomAttributesGUI(SerializedProperty property, VisualElement propertyField, List<CustomAttribute> customAttributes)
        {
            var attributeDrawers = new List<Artifice_CustomAttributeDrawer>();
            var drawerMap = Artifice_Utilities.GetDrawerTypesMap();
            foreach (var customAttribute in customAttributes)
            {
                // Skip if drawer does not exist for custom attribute
                if(drawerMap.ContainsKey(customAttribute.GetType()) == false)
                    continue;
                
                // Create instance of drawer
                var attributeDrawer = (Artifice_CustomAttributeDrawer)Activator.CreateInstance(drawerMap[customAttribute.GetType()]);
                attributeDrawer.Attribute = customAttribute;
                attributeDrawers.Add(attributeDrawer);
                _disposableStack.Push(attributeDrawer); // Add to disposable stack
            }

            // Copy property because param is an iterator which will move on.
            var rootVisualElement = new VisualElement
            {
                name = property.propertyPath
            };

            // PRE GUI
            foreach (var eachAttributeDrawer in attributeDrawers)
                rootVisualElement.Add(eachAttributeDrawer.OnPrePropertyGUI(property));

            // ON GUI       (Adds first OnPropertyGUI implementation only)
            var propertyReplacementDrawer = attributeDrawers.FirstOrDefault(drawer => drawer.IsReplacingPropertyField);
            propertyField = propertyReplacementDrawer != null ? propertyReplacementDrawer.OnPropertyGUI(property) : propertyField;
            rootVisualElement.Add(propertyField);

            // POST GUI  
            foreach (var drawer in attributeDrawers)
                rootVisualElement.Add(drawer.OnPostPropertyGUI(property));

            // WRAP GUI     (Order matters a lot!)
            // // Reverse order does not break it but not 100% sure why. In correct order it just loops.
            var wrapper = rootVisualElement;
            for (var i = attributeDrawers.Count - 1; i >= 0; i--)
                wrapper = attributeDrawers[i].OnWrapGUI(property, wrapper);

            // ON PROPERTY BOUND GUI
            propertyField?.schedule.Execute(() =>
            {
                foreach (var drawer in attributeDrawers)
                    drawer.OnPropertyBoundGUI(property, propertyField);
            });

            return wrapper;
        }

        /// <summary> Uses <see cref="IMGUIContainer"/> to create the default UI implementation Unity would have offered. </summary>
        private VisualElement CreateIMGUIField(SerializedProperty property)
        {
            // Fallback to default IMGUI properties
            var guiContainer = new IMGUIContainer();
            guiContainer.onGUIHandler = () => CreateIMGUIFieldHandler(property);
            
            return guiContainer;
        }

        /// <summary> Used by <see cref="CreateIMGUIField"/> to create IMGUI. </summary>
        private void CreateIMGUIFieldHandler(SerializedProperty property)
        {
            property.serializedObject.Update();
            
            EditorGUI.BeginChangeCheck();
            
            // Create dummy rect of zero height, to get width of current available rect
            var rect = EditorGUILayout.GetControlRect(true, 0f);
            var viewWidth = rect.width;

            // Cache previous label width
            var previousLabelWidth = EditorGUIUtility.labelWidth;

            // Minimum label width is 123. Else, set 33% of the available width as label.
            EditorGUIUtility.labelWidth = Mathf.Max((viewWidth) * 0.33f, 123);

            // IMGUI handler is called every editor frame. This is innately incompatible with UI toolkit which works in a persistent manner.
            // To avoid timing errors, this try catch is needed unfortunately. In the future, further investigation should be done to avoid this.
            try
            {
                EditorGUILayout.PropertyField(property);
            }
            catch (Exception)
            {
                // Noop
            }
            
            // Restore label width for custom IMGUI implementations like lists
            EditorGUIUtility.labelWidth = previousLabelWidth;   
                
            if (EditorGUI.EndChangeCheck())
                property.serializedObject.ApplyModifiedProperties();
        }
        
        /// <summary> Uses property's managed reference type to provide options of what to instantiate and then draws it on the inspector. </summary>
        private VisualElement CreateSerializeReferenceFieldGUI(SerializedProperty property)
        {
            var typeName = property.managedReferenceFieldTypename;
            var baseType = Artifice_SerializedPropertyExtensions.GetTypeFromFieldTypename(typeName);

            // If its a case we do not cover fall back to normal case.
            if (baseType.IsInterface == false && baseType.IsAbstract == false)
                return new PropertyField(property);
            
            // Get all derived types and create string map for easy accessing.
            var types = UnityEditor.TypeCache.GetTypesDerivedFrom(baseType).OrderBy(type => type.Name).ToList();
            var typeMap = new Dictionary<string, Type>();
            foreach (var type in types)
            {
                // MonoBehaviour types cannot be instantiated in runtime like c# objects.
                if(type == typeof(MonoBehaviour) || type.IsSubclassOf(typeof(MonoBehaviour)) || type.IsAbstract || type.IsInterface)
                    continue;
                
                typeMap.Add(type.Name, type);
            }
            
            // Create base container for property.
            var container = new VisualElement();
            container.AddToClassList("property-container");
            
            // Create the custom attributes GUI
            container = CreateCustomAttributesGUI(property, container);

            // Selector container
            var selectorContainer = new VisualElement();
            selectorContainer.AddToClassList("serialize-reference-selector");
            container.Add(selectorContainer);
            
            // Create label of property
            var label = new Label(property.displayName);
            selectorContainer.Add(label);

            // Create dropdown for selections.
            var dropdown = new DropdownField();
            dropdown.choices.Add("Null");
            foreach (var pair in typeMap)
                dropdown.choices.Add(pair.Key);
            selectorContainer.Add(dropdown);
            
            // Create container for drawing selected inherited property. This will be cleared and drawn again upon change.
            var referenceContainer = new Foldout();
            referenceContainer.AddToClassList("reference-container");
            referenceContainer.BindProperty(property);
            referenceContainer.text = "Reference Value";
            container.Add(referenceContainer);
            
            // Initialize UI based on current value.
            RebuildReferenceContainerGUI();
            if (property.managedReferenceValue != null)
            {
                var managedReference = property.managedReferenceValue;
                dropdown.value = managedReference.GetType().Name;
            }
            else   
                dropdown.value = "Null";
            
            // Reference container will constantly track property for value changes (Supports undo and object reset this way) to redraw it self.
            referenceContainer.TrackPropertyValue(property, trackedProperty =>
            {
                RebuildReferenceContainerGUI();
            });
            
            // Dropdown should also track the property in order to update its label on external updates.
            dropdown.TrackPropertyValue(property, trackedProperty => {
                trackedProperty.serializedObject.Update();
                
                if (trackedProperty.managedReferenceValue != null)
                    dropdown.value = trackedProperty.managedReferenceValue.GetType().Name;
                else
                    dropdown.value = "Null";
            });
            
            // On dropdown value changed, update managed reference object of property. This will trigger reference redraw.
            dropdown.RegisterValueChangedCallback(evt =>
            {
                Undo.RecordObject(property.serializedObject.targetObject, "Managed Reference Change");
                
                // Get value from type map, create instance and draw from artifice.
                if (typeMap.TryGetValue(evt.newValue, out var type))
                    property.managedReferenceValue = Activator.CreateInstance(type);
                else
                    property.managedReferenceValue = null;
                
                property.serializedObject.ApplyModifiedProperties();
            });

            void RebuildReferenceContainerGUI()
            {
                property.serializedObject.Update();
             
                if (property == null)
                    return;
                
                // Clear reference container.
                referenceContainer.Clear();
                referenceContainer.RemoveFromClassList("reference-container");
                
                // Get value from type map, create instance and draw from artifice.
                if (property.managedReferenceValue != null && property.hasVisibleChildren)
                {
                    referenceContainer.RemoveFromClassList("hide");
                    referenceContainer.AddToClassList("reference-container");
                    
                    foreach(var childProperty in property.GetVisibleChildren())
                        referenceContainer.Add(CreatePropertyGUI(childProperty));
                }
                else
                    referenceContainer.AddToClassList("hide");
            }

            return container;
        }
        
        /// <summary> Returns a <see cref="VisualElement"/> with buttons which invoke the methods marked with the <see cref="ButtonAttribute"/>. </summary>
        /// <remarks> Unfortunately, there is not unified structure for SerializedObject and SerializedProperty. A template is used here to avoid deduplicate method overloads. </remarks>
        private VisualElement CreateMethodsGUI<T>(T serializedData) where T : class
        {
            // Obtain the target type depending on the serializedData type.
            var targetType = serializedData switch
            {
                SerializedObject serializedObject => serializedObject.targetObject.GetType(),
                SerializedProperty serializedProperty => serializedProperty.GetTarget<object>().GetType(),
                _ => throw new ArgumentException("Invalid serialized data type.")
            };

            // Get name to show in sliding group title based on serialized data type.
            var slidingGroupTitle = serializedData switch
            {
                SerializedObject serializedObject => serializedObject.targetObject.GetType().Name,
                SerializedProperty serializedProperty => serializedProperty.displayName,
                _ => throw new ArgumentException("Invalid serialized data type.")
            };
            
            // Create main container to return, containing both a list of buttons and a sliding group.
            var container = new VisualElement();
            container.AddToClassList("property-container");

            // Some methods may be in a sliding group. Optional.
            var slidingGroup = new Artifice_VisualElement_SlidingGroup();
            slidingGroup.SetTitle($"{slidingGroupTitle}: Actions");
            slidingGroup.AddToClassList("method-group-container");
            
            // Create reusable button drawer.
            var buttonCustomDrawer = new Artifice_CustomAttributeDrawer_ButtonAttribute();
            
            var methods = targetType.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var method in methods)
            {
                // Get button attribute
                var buttonAttribute = method.GetCustomAttribute<ButtonAttribute>();
                if (buttonAttribute == null)
                    continue;

                // Create dedicated drawer for it
                buttonCustomDrawer.Attribute = buttonAttribute;

                // Create the method GUI using serializedData
                var button = buttonCustomDrawer.CreateMethodGUI(serializedData, method);
                button.name = method.Name;
                button.AddToClassList("method-button");

                // Check whether a Sort or a Group attribute were used with the button.
                var groupAttribute = method.GetCustomAttribute<GroupAttribute>();
                if (groupAttribute != null)
                {
                    // From drawer map, get the type visual element group type.
                    var drawerMap = Artifice_Utilities.GetDrawerTypesMap();
                    if (drawerMap.TryGetValue(groupAttribute.GetType(), out var drawerType) == false)
                        Debug.Assert(false,
                            $"GroupAttribute {groupAttribute.GetType().Name} does not have a respective drawer.");

                    var groupAttributeDrawer =
                        (Artifice_CustomAttributeDrawer)Activator.CreateInstance(drawerType) as
                        Artifice_CustomAttributeDrawer_GroupAttribute;
                    Debug.Assert(groupAttributeDrawer != null, "GroupAttribute drawer cannot be null here.");
                    
                    groupAttributeDrawer.Attribute = groupAttribute;
                    _disposableStack.Push(groupAttributeDrawer);
                    
                    if (serializedData is SerializedObject serializedObject)
                    {
                        groupAttributeDrawer.OnWrapGUI(serializedObject.GetIterator(), button);
                    }
                    else if (serializedData is SerializedProperty serializedProperty)
                    {
                        // A method can only be contained in a serialized property. So the SerializedProperty we 
                        // need for the group holder, is any of the first children.
                        var visibleChildren = serializedProperty.GetVisibleChildren();
                        if (visibleChildren.Count == 0)
                        {
                            var infoBox = new Artifice_VisualElement_InfoBox(
                                "Cannot add method to a non-existing group container",
                                Artifice_SCR_CommonResourcesHolder.instance.WarningIcon);
                            container.Add(infoBox);
                        }
                        else
                        {
                            // var isAlreadyAdded = Artifice_CustomAttributeUtility_GroupsHolder.Instance.Contains(serializedp)
                            var element = groupAttributeDrawer.OnWrapGUI(visibleChildren.First(), button);
                            container.Add(element);
                        }
                    }

                }
                else if (buttonAttribute.ShouldAddOnSlidingPanel)
                    slidingGroup.Add(button);
                else
                    container.Add(button);
            }

            // If sliding group is not empty, add it to the container last.
            if (slidingGroup.childCount > 0)
                container.Add(slidingGroup);

            return container.childCount > 0 ? container : null;
        }
        
        /// <summary> Returns an interactable visual indicator to determine whether ArtificeDrawer is enabled or not </summary>
        private VisualElement CreateArtificeIndicatorGUI(SerializedObject serializedObject)
        {
            var indicator = new VisualElement();
            indicator.AddToClassList("artifice-indicator");
            indicator.AddToClassList(Artifice_Utilities.ArtificeDrawerEnabled ? "indicator-enabled" : "indicator-disabled");

            indicator.RegisterCallback<ClickEvent>(evt =>
            {
                Artifice_Utilities.ToggleArtificeDrawer(!Artifice_Utilities.ArtificeDrawerEnabled);
                indicator.RemoveFromClassList(!Artifice_Utilities.ArtificeDrawerEnabled ? "indicator-enabled" : "indicator-disabled");
                indicator.AddToClassList(Artifice_Utilities.ArtificeDrawerEnabled ? "indicator-enabled" : "indicator-disabled");
            });
            
            indicator.tooltip = "Green: ArtificeDrawer is enabled.\nRed: ArtificeDrawer is disabled\n\nClick to toggle.\nNote: Inspector redraw is required.";
            
            return indicator;
        }
        
        #region Utility Methods
        
        public void SetSerializedPropertyFilter(SerializedPropertyFilter filter)
        {
            _serializedPropertyFilter = filter;
        }
        
        /// <summary> Checks property and its visible children. If any use custom attributes, this method returns true. False otherwise. </summary>
        private bool DoesRequireArtificeRendering(SerializedProperty property)
        {
            if (_doesRequireVisualElementsCache.TryGetValue(property, out var cachedResult))
                return cachedResult;
            
            // Check self
            if (IsUsingCustomAttributesDirectly(property))
            {
                _doesRequireVisualElementsCache[property] = true;
                return true;   
            }

            // Check children (no reason to skip as this check will be called for children as well).
            foreach (var childProperty in property.GetVisibleChildren())
            {
                if (DoesRequireArtificeRendering(childProperty))
                {
                    _doesRequireVisualElementsCache[property] = true;
                    return true;
                }
            }

            _doesRequireVisualElementsCache[property] = false;
            return false;
        }
        
        /// <summary> Returns true if the property is directly using any <see cref="CustomAttribute"/> </summary>
        private bool IsUsingCustomAttributesDirectly(SerializedProperty property)
        {
            // Check if property directly has a custom attribute
            var customAttributes = property.GetCustomAttributes();
            if (customAttributes is { Length: > 0 })
                return true;
            
            if (property.IsArray() && property.arraySize == 0)
            {
                var typeName = property.arrayElementType.Replace("PPtr<$", "").Replace(">", "");

                // Return cached if found. Otherwise search assemblies.
                if (TypeCache.TryGetValue(typeName, out var arrayElementType) == false) 
                {
                    arrayElementType = AppDomain.CurrentDomain.GetAssemblies()
                        .SelectMany(a => {
                            try { return a.GetTypes(); } catch { return Array.Empty<Type>(); }
                        })
                        .FirstOrDefault(t => t.FullName == typeName || t.Name == typeName);

                    TypeCache[typeName] = arrayElementType;
                }

                return arrayElementType != null && DoChildrenOfTypeUseCustomAttributes(arrayElementType);
            }

            
            // Otherwise, maybe some method of the object uses custom attributes.
            var obj = property.GetTarget<object>();
            if (obj != null)
            {
                foreach(var method in obj.GetType().GetMethods())
                    if (method.GetCustomAttributes().Any(attribute => attribute is CustomAttribute))
                    {
                        _doesRequireVisualElementsCache[property] = true;
                        return true;
                    }
            }
          
            return false;
        }
        
        /// <summary> Returns true if any nested field is using any <see cref="CustomAttribute"/> </summary>
        private bool DoChildrenOfTypeUseCustomAttributes(Type type)
        {
            // Create  queue and already-searched structures for BFS
            var queue = new Queue<FieldInfo>();
            var alreadySearched = new HashSet<FieldInfo>();
            
            // Inject into the queue all the direct children fields of type.
            foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
                queue.Enqueue(field);

            while (queue.Count > 0)
            {
                var currentField = queue.Dequeue();

                // Check if already searched to skip
                if (alreadySearched.Contains(currentField))
                    continue;
                alreadySearched.Add(currentField);
                
                // Check if it uses custom attributes
                var customAttributes = currentField.GetCustomAttributes().ToArray();
                if (customAttributes.Length > 0)
                    return true;
                
                // Add nested fields in queue to search
                foreach(var field in currentField.FieldType.GetFields())
                    queue.Enqueue(field);
            }

            return false;
        }
        
        /// <summary> Some <see cref="CustomAttribute"/> on lists are meant to be passed along its children, instead of the list it self. This method splits them and provides them as out parameters. </summary>
        private void SplitCustomPropertiesForArrays(SerializedProperty property, out List<CustomAttribute> arrayCustomAttributes, out List<CustomAttribute> childrenCustomAttributes)
        {
            // Create new lists
            arrayCustomAttributes = new List<CustomAttribute>();
            childrenCustomAttributes = new List<CustomAttribute>();
            
            // Get property attributes and parse-split them
            var attributes = property.GetCustomAttributes();
            foreach (var attribute in attributes)
                if (attribute is IArtifice_ArrayAppliedAttribute)
                    arrayCustomAttributes.Add(attribute);
                else
                    childrenCustomAttributes.Add(attribute);
        }
        
        /// <summary> Returns a VisualElement notifying for a missing script error </summary>
        private VisualElement CreateScriptMissingUI(SerializedObject serializedObject)
        {
            var container = new VisualElement();

            container.Add(new PropertyField(serializedObject.FindProperty("m_Script")));

            var labelContainer = new VisualElement();
            labelContainer.AddToClassList("label-container");
            container.Add(labelContainer);

            var flavorIconLabel = new Label(":'(");
            // var flavorIconLabel = new Label(":(");
            flavorIconLabel.AddToClassList("flavor-icon");
            labelContainer.Add(flavorIconLabel);

            var textLabel = new Label("The associated script can not be loaded. Please fix any compile errors and assign a valid script.");
            textLabel.AddToClassList("text");
            labelContainer.Add(textLabel);

            return container;
        }
        
        #endregion

        #region Dispose Pattern

        ~ArtificeDrawer()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true); // Dispose of unmanaged resources.
            GC.SuppressFinalize(this); // Suppress finalization.
        }

        private void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            // Dispose managed resources
            if (disposing)
            {
                while (_disposableStack.Count > 0)
                    _disposableStack.Pop().Dispose();

                _doesRequireVisualElementsCache.Clear();
            }

            _disposed = true;
        }

        #endregion
    }
}