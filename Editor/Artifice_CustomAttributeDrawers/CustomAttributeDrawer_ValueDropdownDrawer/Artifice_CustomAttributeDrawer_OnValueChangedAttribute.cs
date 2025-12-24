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
            if (property.propertyType != SerializedPropertyType.String)
            {
                EditorGUI.LabelField(position, label.text, "Use [ValueDropdown] with strings only.");
                return;
            }

            // Get the attribute instance
            ValueDropdownAttribute dropdownAttribute = (ValueDropdownAttribute)attribute;
            object target = property.serializedObject.targetObject;
            string memberName = dropdownAttribute.MethodName;

            // Attempt to get the list of strings
            List<string> stringList = GetListFromMember(target, memberName);

            // Handle null or empty list
            if (stringList == null || stringList.Count == 0)
            {
                EditorGUI.LabelField(position, label.text, $"Member '{memberName}' not found/empty.");
                return;
            }

            // Determine the current index
            int currentIndex = stringList.IndexOf(property.stringValue);

            // Handle missing/custom values (e.g., if the string was set to something not in the list currently)
            if (currentIndex == -1)
            {
                if (!string.IsNullOrEmpty(property.stringValue))
                {
                    stringList.Insert(0, $"{property.stringValue} (Current)");
                    currentIndex = 0;
                }
                else
                {
                    currentIndex = 0;
                }
            }

            // Draw the Popup
            EditorGUI.BeginChangeCheck();
            int newIndex = EditorGUI.Popup(position, label.text, currentIndex, stringList.ToArray());

            // Apply changes if selection changed
            if (EditorGUI.EndChangeCheck())
            {
                if (newIndex >= 0 && newIndex < stringList.Count)
                {
                    string selection = stringList[newIndex];
                    // Remove the temporary visual tag if selected
                    if (selection.EndsWith(" (Current)"))
                    {
                        selection = selection.Replace(" (Current)", "");
                    }
                    property.stringValue = selection;
                    property.serializedObject.ApplyModifiedProperties();
                }
            }
        }

        /// <summary>
        /// Tries to find a Field, Property, or Method that returns IEnumerable<string>
        /// </summary>
        private List<string> GetListFromMember(object target, string memberName)
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

            // Convert the result to List<string> safely
            if (rawValue is IEnumerable<string> enumerable)
            {
                return enumerable.ToList();
            }

            // If we reach here, we failed to get a valid list, so return null
            return null;
        }
    }
}