using UnityEditor;
using UnityEngine.UIElements;
using System.Reflection;
using System;
using UnityEditor.UIElements;
using UnityEngine;

namespace ArtificeToolkit.Editor
{
    /// <summary> Code written with the help of Unity Support. </summary>
    public static class Artifice_CustomDrawerUtility
    {
        private static Type ScriptAttributeUtility => Type.GetType("UnityEditor.ScriptAttributeUtility, UnityEditor");

        private static MethodInfo _getHandler = null;
        private static MethodInfo GetHandlerMethod
        {
            get
            {
                if (_getHandler == null)
                {
                    _getHandler = ScriptAttributeUtility.GetMethod("GetHandler", BindingFlags.Static | BindingFlags.NonPublic);
                }
                return _getHandler;
            }
        }

        private static object _nullHandler = null;
        private static object NullHandler
        {
            get
            {
                if (_nullHandler == null)
                {
                    var field = ScriptAttributeUtility.GetField("s_SharedNullHandler", BindingFlags.Static | BindingFlags.NonPublic);
                    if (field == null)
                        throw new ArgumentException("Could not find \'s_SharedNullHandler\'.");
                    
                    _nullHandler = field.GetValue(null);
                }
                return _nullHandler;
            }
        }

        /// <summary>
        /// Checks if the SerializedProperty has a custom property drawer.
        /// </summary>
        /// <param name="property">The serialized property.</param>
        /// <returns>True if the property has a custom drawer, false otherwise.</returns>
        public static bool HasCustomDrawer(SerializedProperty property)
        {
            var handler = GetHandlerMethod.Invoke(null, new object[] { property });
            return handler != NullHandler;
        }

        /// <summary>
        /// Creates the property GUI using the custom drawer of the SerializedProperty.
        /// </summary>
        /// <param name="property">The serialized property.</param>
        /// <returns>A VisualElement representing the custom drawer GUI for the property.</returns>
        public static VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            // Assert that the property has a custom drawer
            if (!HasCustomDrawer(property))
                throw new InvalidOperationException($"Property '{property.name}' does not have a custom property drawer.");

            // Retrieve the handler using reflection
            var handler = GetHandlerMethod.Invoke(null, new object[] { property });

            // Check if handler has the method for creating the property GUI in the new UI Toolkit (VisualElement system)
            var handlerType = handler.GetType();
            
            // Get property drawer method and create property GUI if not null.
            var getPropertyDrawerMethod = handlerType.GetMethod("get_propertyDrawer", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (getPropertyDrawerMethod != null)
            {
                var propertyDrawer = (PropertyDrawer)getPropertyDrawerMethod.Invoke(handler, new object[]{});
                return propertyDrawer.CreatePropertyGUI(property);
            }
            
            // Fallback to a default property field if no CreatePropertyGUI method is found
            return new PropertyField(property);
        }
    }
    
}