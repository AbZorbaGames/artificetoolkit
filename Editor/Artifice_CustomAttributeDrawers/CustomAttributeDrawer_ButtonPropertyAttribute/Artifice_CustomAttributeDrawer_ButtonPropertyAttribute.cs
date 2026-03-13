using System;
using ArtificeToolkit.Editor.Artifice_CustomAttributeDrawers.CustomAttributeDrawer_ButtonAttribute;
using ArtificeToolkit.Editor.VisualElements;
using CustomAttributes;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace ArtificeToolkit.Editor.Artifice_CustomAttributeDrawers.CustomAttributeDrawer_ButtonPropertyAttribute
{
    [Artifice_CustomAttributeDrawer(typeof(ButtonPropertyAttribute))]
    public class Artifice_CustomAttributeDrawer_ButtonPropertyAttribute : Artifice_CustomAttributeDrawer
    {
        public override bool IsReplacingPropertyField { get; } = true;

        public override VisualElement OnPropertyGUI(SerializedProperty property)
        {
            var attribute = (ButtonPropertyAttribute)Attribute;

            var buttonLabel = Artifice_CustomAttributeDrawer_ButtonAttribute.AddSpacesBeforeCapitals(attribute.MethodName);
            var button = new Artifice_VisualElement_LabeledButton(buttonLabel, () =>
            {
                // Cover for multiple selection
                var targets = property.serializedObject.targetObjects;
                foreach (var target in targets)
                {
                    var serializedObject = new SerializedObject(target);
                    serializedObject.Update();
                    
                    // We need to find the invocation target of the Button method, since it can belong to nested property of the SerializedObject. Thus target is not enough.
                    object invocationTarget = null;
                    if (property.depth > 0)
                        invocationTarget = serializedObject.FindProperty(property.propertyPath).FindParentProperty().GetTarget<object>();
                    else
                        invocationTarget = target;
                 
                    // Get parameter values specific to this target (you may need to refactor GetParameterList to support this)
                    var parametersList = Artifice_CustomAttributeDrawer_ButtonAttribute.GetParameterListForTarget(invocationTarget, attribute.ParameterNames);
                    
                    // Get method info
                    var methodInfo = invocationTarget.GetType().GetMethod(attribute.MethodName);
                    if (methodInfo == null)
                    {
                        Debug.Log($"Could not find method ({attribute.MethodName}) relating to property [{invocationTarget.ToString()}]");
                        return;
                    }
                    
                    // Fill in any default parameters that may exist and are not defined in the attribute.
                    var parameterInfo = methodInfo.GetParameters();
                    for (var i = parametersList.Count; i < parameterInfo.Length; i++)
                    {
                        var excessParameterInfo = parameterInfo[i];
                        if (excessParameterInfo.HasDefaultValue)
                            parametersList.Add(excessParameterInfo.DefaultValue);
                        else
                        {
                            Artifice_Utilities.LogError( $"Parameter \'<b>{excessParameterInfo.Name}\' in method \'{methodInfo.Name}\'</b> is not provided, nor has a default value. Aborting method invocation...");
                            return;
                        }
                    }
                    
                    if (methodInfo.GetParameters().Length != parametersList.Count)
                        throw new ArgumentException(
                            $"[Artifice/Button] Parameters count do not match with method {methodInfo.Name}");

                    methodInfo.Invoke(invocationTarget, parametersList.ToArray());

                    serializedObject.ApplyModifiedProperties();
                }
            });
            button.AddToClassList("button");
            
            // Reuse the same stylesheet as the ButtonAttribute for consistency.
            button.styleSheets.Add(Artifice_Utilities.GetGlobalStyle());
            button.styleSheets.Add(Artifice_Utilities.GetStyle(typeof(Artifice_CustomAttributeDrawer_ButtonAttribute)));
            
            // Find 
            return button;
        }
    }
}
