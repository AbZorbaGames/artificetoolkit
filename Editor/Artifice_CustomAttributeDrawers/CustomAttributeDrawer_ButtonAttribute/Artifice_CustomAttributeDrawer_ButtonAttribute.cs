using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using ArtificeToolkit.Attributes;
using UnityEditor;
using UnityEngine.UIElements;

namespace ArtificeToolkit.Editor.Artifice_CustomAttributeDrawers.CustomAttributeDrawer_ButtonAttribute
{
    [Serializable]
    [Artifice_CustomAttributeDrawer(typeof(ButtonAttribute))]
    public class Artifice_CustomAttributeDrawer_ButtonAttribute : Artifice_CustomAttributeDrawer
    {
        /// <summary> Returns button for method button GUI from a serialized object or property. </summary>
        public VisualElement CreateMethodGUI<T>(T serializedData, MethodInfo methodInfo) where T : class
        {
            var button = new Button(() =>
            {
                // Retrieve the underlying SerializedObject regardless of whether serializedData is SerializedObject or SerializedProperty
                var serializedObject = serializedData switch
                {
                    SerializedObject obj => obj,
                    SerializedProperty property => property.serializedObject,
                    _ => throw new ArgumentException("Invalid serialized data type.")
                };
                
                // Retrieve the underlying target object regardless of whether serializedData is SerializedObject or SerializedProperty
                var targetObject = serializedData switch
                {
                    SerializedObject obj => obj.targetObject,
                    SerializedProperty property => property.GetTarget<object>(),
                    _ => throw new ArgumentException("Invalid serialized data type.")
                };

                // Begin update and call method
                serializedObject.Update();

                // Generate parameters list
                var parametersList = GetParameterList(serializedData);

                // Validate parameter count
                if (methodInfo.GetParameters().Length != parametersList.Count)
                    throw new ArgumentException($"[Artifice/Button] Parameters count do not match with method {methodInfo.Name}");

                // Invoke the method
                methodInfo.Invoke(targetObject, parametersList.ToArray());

                // Apply changes
                serializedObject.ApplyModifiedProperties();
            });
    
            button.text = AddSpacesBeforeCapitals(methodInfo.Name);
            button.styleSheets.Add(Artifice_Utilities.GetStyle(GetType()));
            button.AddToClassList("button");

            return button;
        }
        
        /// <summary> Retrieves a list of parameters for the method invocation based on the attribute parameter names. </summary>
        private List<object> GetParameterList<T>(T serializedData) where T : class
        {
            var attribute = (ButtonAttribute)Attribute;
            var parametersList = new List<object>();

            // Iterate through the parameter names specified in the attribute
            foreach (var parameterName in attribute.ParameterNames)
            {
                var parameterProperty = serializedData switch
                {
                    // Find the appropriate property based on the type of serializedData
                    SerializedObject serializedObject => serializedObject.FindProperty(parameterName),
                    SerializedProperty property => property.FindPropertyRelative(parameterName),
                    _ => throw new ArgumentException("Invalid serialized data type.")
                };

                // Check if the property exists
                if (parameterProperty == null)
                    throw new ArgumentException($"[Artifice/Button] Cannot find parameter name {parameterName}");

                // Add the target value of the property to the parameter list
                parametersList.Add(parameterProperty.GetTarget<object>());
            }

            return parametersList;
        }
        
        private string AddSpacesBeforeCapitals(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            var spacedString = new StringBuilder();
            spacedString.Append(input[0]);

            for (int i = 1; i < input.Length; i++)
            {
                if (char.IsUpper(input[i]))
                {
                    spacedString.Append(' '); // Add a space before capital letter
                }
                spacedString.Append(input[i]);
            }

            return spacedString.ToString();
        }
    }
}
