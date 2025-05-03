using System;
using System.Linq;
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

        public override Sprite LogSprite { get; } =
            Artifice_SCR_CommonResourcesHolder.instance.ErrorIcon;

        public override LogType LogType { get; } = LogType.Error;

        protected override bool IsApplicableToProperty(SerializedProperty property) => true;

        public override bool IsValid(SerializedProperty property)
        {
            object fieldObject = property.serializedObject.targetObject;
            var fieldObjectType = fieldObject.GetType();
            var fieldName = property.name;
            var fieldInfo = fieldObjectType.GetField(fieldName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            
            if (fieldInfo == null)
            {
                _logMessage = $"ValidateInput: Invalid property: '{property.name}'";
                return false;
            }

            var validateAttribute = fieldInfo.GetCustomAttribute<ValidateInputAttribute>();
            _logMessage = validateAttribute.Message;
            var unresolvedCondition = validateAttribute.Condition;

            // Check for literal strings
            switch (unresolvedCondition.Trim())
            {
                case var s when string.Equals(s, "true", StringComparison.OrdinalIgnoreCase):
                    return true;
                case var s when string.Equals(s, "false", StringComparison.OrdinalIgnoreCase):
                    return false;
            }
            
            // Get nested member
            object validationObject;
            MemberInfo validationMember;
            try
            {
                (validationObject, validationMember) = Artifice_Utilities.ResolveNestedMember(
                    fieldObject, unresolvedCondition);
            }
            catch (Exception ex)
            {
                _logMessage = ex.Message.Insert(0, "ValidateInput: ");
                return false;
            }

            switch (validationMember)
            {
                case FieldInfo field:
                    return ExecuteValidationField(field, validationObject);
                case PropertyInfo prop:
                    return ExecuteValidationProperty(prop, validationObject);
                case MethodInfo method:
                    return ExecuteValidationMethod(
                        method, validationObject, fieldInfo, fieldObject);
            }

            _logMessage = $"ValidateInput: Invalid validation condition: '{unresolvedCondition}'";
            return false;
        }

        private bool ExecuteValidationField(FieldInfo validationField, object validationObject)
        {
            if (validationField.FieldType != typeof(bool))
            {
                _logMessage =
                    $"ValidateInput: Validation field must be a bool: '{validationField.Name}'";
                return false;
            }

            var value =
                validationField.GetValue(validationField.IsStatic ? null : validationObject);
            return value != null && (bool)value;
        }

        private bool ExecuteValidationProperty(
            PropertyInfo validationProperty, object validationObject)
        {
            if (!validationProperty.CanRead)
            {
                _logMessage =
                    $"ValidateInput: Validation property must be readable:" +
                    $" '{validationProperty.Name}'";
                return false;
            }
            
            if (validationProperty.PropertyType != typeof(bool))
            {
                _logMessage =
                    $"ValidateInput: Validation property must be a bool:" +
                    $" '{validationProperty.Name}'";
                return false;
            }

            var value = validationProperty.GetValue(
                validationProperty.GetMethod.IsStatic ? null : validationObject);
            return value != null && (bool)value;
        }

        private bool ExecuteValidationMethod(MethodInfo validationMethod, object validationObject,
            FieldInfo fieldInfo, object fieldObject)
        {
            var methodName = validationMethod.Name;
            if (validationMethod.ReturnType != typeof(bool))
            {
                _logMessage =
                    $"ValidateInput: Validation method must return a bool: '{methodName}'";
                return false;
            }

            var parameters = validationMethod.GetParameters();
            object[] paramValues = null;
            
            // Get parameter values
            if (parameters.Length > 0)
            {
                var firstParamType = parameters[0].ParameterType;
                if (!firstParamType.IsAssignableFrom(fieldInfo.FieldType))
                {
                    _logMessage =
                        $"ValidateInput: First parameter type mismatch in '{methodName}'." +
                        $"\nExpected: {firstParamType}, Got: {fieldInfo.FieldType}";
                    return false;
                }
                
                paramValues    = new object[parameters.Length];
                paramValues[0] = fieldInfo.GetValue(fieldObject);

                for (int i = 1; i < parameters.Length; i++)
                {
                    if (parameters[i].HasDefaultValue) paramValues[i] = parameters[i].DefaultValue;
                    else
                    {
                        _logMessage =
                            $"ValidateInput: Validation method parameters, other than the first," +
                            $" must be optional.\n" +
                            $"'Method: {methodName}', Parameter: '{parameters[i].Name}'";
                        return false;
                    }
                }
            }

            try
            {
                var result = validationMethod.Invoke(validationObject, paramValues);
                if (result is bool isValid) return isValid;
            }
            catch (Exception ex)
            {
                var fieldName = fieldInfo.Name;
                if (ex is TargetInvocationException targetEx)
                {
                    _logMessage =
                        $"ValidateInput: Exception occurred while invoking validation method" +
                        $" '{methodName}' from '{validationObject}' for field" +
                        $" '{fieldName}' in {fieldObject}." +
                        $"\nException: {targetEx.InnerException?.Message ?? targetEx.Message}";
                }
                else
                {
                    _logMessage =
                        $"ValidateInput: Exception occurred while executing validation method" +
                        $" '{methodName}' for field '{fieldName}'.\nException: {ex.Message}";
                }
            }

            return false;
        }
    }
}