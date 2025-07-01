using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using ArtificeToolkit.Attributes;
using UnityEditor;
using UnityEngine.UIElements;

// ReSharper disable UseObjectOrCollectionInitializer

namespace ArtificeToolkit.Editor.Artifice_CustomAttributeDrawers.CustomAttributeDrawer_ButtonAttribute
{
    [Serializable]
    [Artifice_CustomAttributeDrawer(typeof(ButtonAttribute))]
    public class Artifice_CustomAttributeDrawer_ButtonAttribute : Artifice_CustomAttributeDrawer
    {
        /// <summary> Returns button for method button GUI from a serialized object or property. Works with multiselect as well. </summary>
        public VisualElement CreateMethodGUI<T>(T serializedData, MethodInfo methodInfo) where T : class
        {
            var button = new Button(() =>
            {
                var serializedObject = serializedData switch
                {
                    SerializedObject obj => obj,
                    SerializedProperty property => property.serializedObject,
                    _ => throw new ArgumentException("Invalid serialized data type.")
                };
                
                var targets = serializedObject.targetObjects;
                serializedObject.Update();

                foreach (var target in targets)
                {
                    // We need to find the invocation target of the Button method, since it can belong to nested property of the SerializedObject. Thus target is not enough.
                    object invocationTarget = null;
                    var invocationSerializedObject = new SerializedObject(target);
                    if (serializedData is SerializedObject)
                        invocationTarget = invocationSerializedObject.targetObject;
                    else
                    {
                        var serializedProperty = serializedData as SerializedProperty;
                        invocationTarget = invocationSerializedObject.FindProperty(serializedProperty.propertyPath).GetTarget<object>();
                    }
                 
                    // Get parameter values specific to this target (you may need to refactor GetParameterList to support this)
                    var parametersList = GetParameterListForTarget(invocationTarget);
                    
                    if (methodInfo.GetParameters().Length != parametersList.Count)
                        throw new ArgumentException(
                            $"[Artifice/Button] Parameters count do not match with method {methodInfo.Name}");

                    methodInfo.Invoke(invocationTarget, parametersList.ToArray());
                }

                serializedObject.ApplyModifiedProperties();
            });

            button.text = AddSpacesBeforeCapitals(methodInfo.Name);
            button.styleSheets.Add(Artifice_Utilities.GetStyle(GetType()));
            button.AddToClassList("button");

            return button;
        }

        /// <summary> Retrieves a list of parameters for the method invocation based on the attribute parameter names. </summary>
        private List<object> GetParameterListForTarget(object target)
        {
            var attribute = (ButtonAttribute)Attribute;
            var parametersList = new List<object>();

            foreach (var parameterName in attribute.ParameterNames)
            {
                var field = target.GetType().GetField(parameterName,
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                if (field == null)
                {
                    var property = target.GetType().GetProperty(parameterName,
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                    if (property == null)
                        throw new ArgumentException($"[Artifice/Button] Cannot find parameter '{parameterName}' on {target}");

                    parametersList.Add(property.GetValue(target));
                }
                else
                {
                    parametersList.Add(field.GetValue(target));
                }
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