using System;
using System.Reflection;
using ArtificeToolkit.Attributes;
using ArtificeToolkit.Editor.Resources;
using UnityEditor;
using UnityEngine;

namespace ArtificeToolkit.Editor.Artifice_CustomAttributeDrawers.CustomAttributeDrawer_Validators
{
    [Artifice_CustomAttributeDrawer(typeof(ValidateInputAttribute))]
    public class
        Artifice_CustomAttributeDrawer_ValidateInputAttribute :
        Artifice_CustomAttributeDrawer_Validator_BASE
    {
        private string _logMessage = "";
        public override string LogMessage => _logMessage;
        public override Sprite LogSprite { get; } = Artifice_SCR_CommonResourcesHolder.instance.ErrorIcon;
        public override LogType LogType { get; } = LogType.Error;

        protected override bool IsApplicableToProperty(SerializedProperty property) => true;

        public override bool IsValid(SerializedProperty property)
        {
            object targetObject = property.serializedObject.targetObject;
            var targetType = targetObject.GetType();
            var fieldName = property.propertyPath.Split('.')[0];
            var fieldInfo = targetType.GetField(fieldName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (fieldInfo == null)
            {
                _logMessage = $"ValidateInput: Invalid property: '{property.name}'";
                return false;
            }

            var validateAttribute = fieldInfo.GetCustomAttribute<ValidateInputAttribute>();
            var condition = validateAttribute.Condition;
            _logMessage = validateAttribute.Message;

            switch (condition.ToLower())
            {
                case "true": return true;
                case "false": return false;
            }

            var validationMethod = targetType.GetMethod(condition,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (validationMethod != null)
                return ExecuteValidationMethod(
                    validationMethod, targetObject, targetType, fieldName);
            else
            {
                _logMessage = $"ValidateInput: Invalid validation condition: '{condition}'";
                return false;
            }
        }

        private bool ExecuteValidationMethod(MethodInfo validationMethod, object targetObject,
                                             Type targetType, string fieldName)
        {
            var methodName = validationMethod.Name;

            if (validationMethod.ReturnType != typeof(bool))
            {
                _logMessage =
                    $"ValidateInput: Validation method must return a bool: '{methodName}'";
                return false;
            }

            try
            {
                var result = validationMethod.Invoke(targetObject, null);
                if (result is bool isValid) return isValid;
            }
            catch (TargetInvocationException ex)
            {
                _logMessage =
                    $"ValidateInput: Exception occurred while invoking validation method" +
                    $"'{methodName}' for field '{fieldName}'." +
                    $"\nException: {ex.InnerException?.Message ?? ex.Message}";
                return false;
            }
            catch (Exception ex)
            {
                _logMessage =
                    $"ValidateInput: Exception occured while executing validation method" +
                    $"'{methodName}' for field '{fieldName}'.\nException: {ex.Message}";
                return false;
            }

            return false;
        }
    }
}