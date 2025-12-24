using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ArtificeToolkit.Attributes;
using UnityEditor;
using UnityEngine;

namespace ArtificeToolkit.Editor.Artifice_CustomAttributeDrawers.CustomAttributeDrawer_ValueDropdownDrawer
{
    /// <summary>
    /// Custom PropertyDrawer for <see cref="ValueDropdownAttribute"/>
    /// </summary>
    [CustomPropertyDrawer(typeof(ValueDropdownAttribute))]
    public class ValueDropdownDrawer : PropertyDrawer
    {
        /// <summary>
        /// Draw the property in the Inspector GUI
        /// </summary>
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // Get the attribute instance
            ValueDropdownAttribute dropdownAttribute = (ValueDropdownAttribute)attribute;
            object target = property.serializedObject.targetObject;
            string memberName = dropdownAttribute.MethodName;

            // Get the field type
            var fieldType = GetFieldType(property);
            var options = GetListFromMember(target, memberName, fieldType);
            if (options == null || options.Count == 0)
            {
                EditorGUI.LabelField(position, label.text, $"Member '{memberName}' not found/empty.");
                return;
            }

            // Convert options to display names and find current index
            int currentIndex = 0;
            object currentValue = GetPropertyValue(property);
            var displayNames = options.Select(o => o?.ToString() ?? "<null>").ToArray();
            for (int i = 0; i < options.Count; i++)
            {
                if ((currentValue == null && options[i] == null) || (currentValue != null && currentValue.Equals(options[i])))
                {
                    currentIndex = i;
                    break;
                }
            }

            EditorGUI.BeginChangeCheck();
            int newIndex = EditorGUI.Popup(position, label.text, currentIndex, displayNames);
            if (EditorGUI.EndChangeCheck())
            {
                if (newIndex >= 0 && newIndex < options.Count)
                {
                    SetPropertyValue(property, options[newIndex]);
                    property.serializedObject.ApplyModifiedProperties();
                }
            }
        }

        private System.Type GetFieldType(SerializedProperty property)
        {
            var parent = property.serializedObject.targetObject.GetType();
            var field = parent.GetField(property.name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            return field != null ? field.FieldType : typeof(string);
        }

        private object GetPropertyValue(SerializedProperty property)
        {
            switch (property.propertyType)
            {
                case SerializedPropertyType.Integer:
                    return property.intValue;
                case SerializedPropertyType.Boolean:
                    return property.boolValue;
                case SerializedPropertyType.Float:
                    return property.floatValue;
                case SerializedPropertyType.String:
                    return property.stringValue;
                case SerializedPropertyType.Enum:
                    return property.enumNames.Length > 0 ? property.enumNames[property.enumValueIndex] : null;
                default:
                    return null;
            }
        }

        private void SetPropertyValue(SerializedProperty property, object value)
        {
            switch (property.propertyType)
            {
                case SerializedPropertyType.Integer:
                    property.intValue = value is int i ? i : 0;
                    break;
                case SerializedPropertyType.Boolean:
                    property.boolValue = value is bool b && b;
                    break;
                case SerializedPropertyType.Float:
                    property.floatValue = value is float f ? f : 0f;
                    break;
                case SerializedPropertyType.String:
                    property.stringValue = value?.ToString() ?? string.Empty;
                    break;
                case SerializedPropertyType.Enum:
                    if (value != null)
                    {
                        var idx = System.Array.IndexOf(property.enumNames, value.ToString());
                        if (idx >= 0) property.enumValueIndex = idx;
                    }
                    break;
            }
        }

        /// <summary>
        /// Tries to find a Field, Property, or Method that returns IEnumerable<T>, T[], or a single T for the dropdown.
        /// </summary>
        private List<object> GetListFromMember(object target, string memberName, System.Type fieldType)
        {
            System.Type type = target.GetType();
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            object rawValue = null;

            // Try getting Field
            FieldInfo field = type.GetField(memberName, flags);
            if (field != null)
            {
                rawValue = field.GetValue(target);
            }
            else
            {
                // Try getting Property
                PropertyInfo prop = type.GetProperty(memberName, flags);
                if (prop != null)
                {
                    rawValue = prop.GetValue(target, null);
                }
                // Try getting Method
                else
                {
                    MethodInfo method = type.GetMethod(memberName, flags);
                    if (method != null)
                    {
                        rawValue = method.Invoke(target, null);
                    }
                }
            }

            if (rawValue == null)
                return null;

            // Handle arrays and IEnumerable<T>
            if (rawValue is System.Collections.IEnumerable enumerable && !(rawValue is string))
            {
                var list = new List<object>();
                foreach (var item in enumerable)
                {
                    if (item == null || fieldType.IsAssignableFrom(item.GetType()) || item.GetType().IsPrimitive || item is string)
                        list.Add(item);
                }
                return list;
            }

            // Handle single value
            if (fieldType.IsAssignableFrom(rawValue.GetType()) || rawValue.GetType().IsPrimitive || rawValue is string)
            {
                return new List<object> { rawValue };
            }

            return null;
        }
    }
}