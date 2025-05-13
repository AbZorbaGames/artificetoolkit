using System;
using System.Linq;
using System.Reflection;
using ArtificeToolkit.Attributes;
using ArtificeToolkit.Editor.Resources;
using UnityEditor;
using UnityEditor.UIElements;
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

        private LogType _logType = LogType.Error;
        public override LogType LogType => _logType;

        private Sprite _logSprite = Artifice_SCR_CommonResourcesHolder.instance.ErrorIcon;
        public override Sprite LogSprite => _logSprite;

        protected override bool IsApplicableToProperty(SerializedProperty property) => true;

        public override bool IsValid(SerializedProperty property)
        {
            ResetValues();

            var (propertyParentTarget, propertyMemberInfo) = Artifice_SerializedPropertyExtensions
                .ResolveNestedMember(property.propertyPath, property.serializedObject.targetObject);
            var fieldInfo = (FieldInfo)propertyMemberInfo;

            if (fieldInfo == null)
            {
                _logMessage = $"ValidateInput: Invalid property: '{property.name}'";
                return false;
            }

            var validateAttribute = fieldInfo.GetCustomAttribute<ValidateInputAttribute>();
            if (validateAttribute == null)
                Debug.Assert(false, "CustomAttributeDrawer_ValidateInput should never be called on a property which does not have a ValidateInputAttribute");
            var conditionPath = validateAttribute.Condition;

            _logMessage = validateAttribute.Message;
            _logType = validateAttribute.LogType;
            _logSprite = Artifice_Utilities.LogIconFromType(_logType);
            InfoBox?.Update(_logSprite, _logMessage);

            // Check for literal strings
            switch (conditionPath.Trim())
            {
                case var s when string.Equals(s, "true", StringComparison.OrdinalIgnoreCase):
                    return true;
                case var s when string.Equals(s, "false", StringComparison.OrdinalIgnoreCase):
                    return false;
            }

            // Get nested member
            object validationParentTarget;
            MemberInfo validationMemberInfo;
            try
            {
                (validationParentTarget, validationMemberInfo) =
                    Artifice_SerializedPropertyExtensions
                        .ResolveNestedMember(conditionPath, propertyParentTarget);
            }
            catch (Exception ex)
            {
                _logMessage = ex.Message.Insert(0, "ValidateInput: ");
                return false;
            }

            // If validation member info is of supported type, execute and return validation
            if (validationMemberInfo is FieldInfo or PropertyInfo or MethodInfo)
            {
                // Track entire serialized object to reevaluate on changes.
                InfoBox.TrackSerializedObjectValue(property.serializedObject, _ =>
                {
                    var isValid = ExecuteValidation(validationMemberInfo, validationParentTarget, fieldInfo,
                        propertyParentTarget);
                    
                    // Hide or show InfoBox based on reevaluated value.
                    if(isValid)
                        InfoBox?.AddToClassList("hide");
                    else
                        InfoBox?.RemoveFromClassList("hide");
                });
                
                // Return validation result
                return ExecuteValidation(validationMemberInfo, validationParentTarget, fieldInfo, propertyParentTarget);
            }
            
            _logMessage = $"ValidateInput: Invalid validation condition: '{conditionPath}'";
            return false;
        }

        private void ResetValues()
        {
            _logMessage = "";
            _logType = LogType.Error;
            _logSprite = Artifice_SCR_CommonResourcesHolder.instance.ErrorIcon;
        }

        private bool ExecuteValidation(MemberInfo validationMemberInfo, object validationParentTarget, FieldInfo fieldInfo, object propertyParentTarget)
        {
            return validationMemberInfo switch
            {
                FieldInfo field => ExecuteValidationField(field, validationParentTarget),
                PropertyInfo prop => ExecuteValidationProperty(prop, validationParentTarget),
                MethodInfo method => ExecuteValidationMethod(method, validationParentTarget, fieldInfo,
                    propertyParentTarget),
                _ => throw new ArgumentException("Control flow should never reach this point.")
            };
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
            var paramValues = new object[parameters.Length];
            var assignedField = false;
            var assignedMessage = false;
            var assignedType = false;
            var i = 0;

            for (; i < Mathf.Min(parameters.Length, 3); i++)
            {
                var paramType = parameters[i].ParameterType;

                if (!assignedField && paramType.IsAssignableFrom(fieldInfo.FieldType))
                {
                    paramValues[i] = fieldInfo.GetValue(fieldObject);
                    assignedField = true;
                }
                else if (!assignedMessage && paramType == typeof(string).MakeByRefType())
                {
                    paramValues[i] = _logMessage;
                    assignedMessage = true;
                }
                else if (!assignedType && paramType == typeof(LogType).MakeByRefType())
                {
                    paramValues[i] = _logType;
                    assignedType = true;
                }
                else
                {
                    if (!parameters[i].HasDefaultValue)
                    {
                        _logMessage =
                            $"ValidateInput: Parameter is not assignable from any of the" +
                            $" ValidateInput properties and isn't optional:"              +
                            $"\n'Method: {methodName}', Parameter: '{parameters[i].Name}'";
                        return false;
                    }

                    break;
                }
            }

            for (; i < parameters.Length; i++)
            {
                if (parameters[i].HasDefaultValue) paramValues[i] = parameters[i].DefaultValue;
                else
                {
                    _logMessage =
                        $"ValidateInput: Validation method parameters, other than the first," +
                        $" must be optional."                                                 +
                        $"\n'Method: {methodName}', Parameter: '{parameters[i].Name}'";
                    return false;
                }
            }

            try
            {
                var result = validationMethod.Invoke(validationObject, paramValues);

                // Retrieve log message and type
                if (assignedMessage)
                    _logMessage = (string)paramValues.FirstOrDefault(p => p is string);
                if (assignedType)
                {
                    _logType = (LogType)paramValues.FirstOrDefault(p => p is LogType)!;
                    _logSprite = Artifice_Utilities.LogIconFromType(_logType);
                }

                if (assignedMessage || assignedType)
                    InfoBox?.Update(_logSprite, _logMessage);

                if (result is bool isValid) return isValid;
            }
            catch (Exception ex)
            {
                var fieldName = fieldInfo.Name;
                if (ex is TargetInvocationException targetEx)
                {
                    _logMessage =
                        $"ValidateInput: Exception occurred while invoking validation method" +
                        $" '{methodName}' from '{validationObject}' for field"                +
                        $" '{fieldName}' in {fieldObject}."                                   +
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