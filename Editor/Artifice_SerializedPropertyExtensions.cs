using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using ArtificeToolkit.Attributes;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

// ReSharper disable InvertIf

namespace ArtificeToolkit.Editor
{
    public static class Artifice_SerializedPropertyExtensions
    {
        private static readonly Regex ArrayIndexCapturePattern = new Regex(@"\[(\d*)\]");

        /// <summary> This method calls <see cref="GetTarget"/> but casts the result using generics for better usability </summary>
        public static T GetTarget<T>(this SerializedProperty property)
        {
            return (T)GetTarget(property);
        }
        
        /// <summary> This method uses reflection to return the Type of the property. </summary>
        public static Type GetTargetType(this SerializedProperty property)
        {
            // Try get direct value for optimization.
            if (GetTargetTypeDirect(property, out var value))
                return value;
            
            var propertyNames = property.propertyPath.Split('.');
            
            object target = property.serializedObject.targetObject;
            var targetType = target.GetType();
            
            var isNextPropertyArrayIndex = false;
            for (var i = 0; i < propertyNames.Length && target != null; ++i)
            {
                var propName = propertyNames[i];

                if (propName == "Array")
                {
                    isNextPropertyArrayIndex = true;
                }
                else if (isNextPropertyArrayIndex)
                {
                    isNextPropertyArrayIndex = false;
                    var arrayIndex = ParseArrayIndex(propName);
                    if (target is IList targetAsArray)
                        target = targetAsArray[arrayIndex];
                }
                else
                {
                    targetType = GetFieldType(target, propName);
                    target = GetField(target, propName);
                }
            }

            return targetType;
        }

        /// <summary> This method uses reflection to return the object reference of the property. </summary>
        private static object GetTarget(this SerializedProperty property)
        {
            // First try to use direct type access if possible for performance.
            if (GetTargetDirect(property, out var value))
                return value;
            
            // For generic types, do complex stuff.
            var propertyNames = property.propertyPath.Split('.');
            object target = property.serializedObject.targetObject;
            var isNextPropertyArrayIndex = false;
            for (var i = 0; i < propertyNames.Length && target != null; ++i)
            {
                var propName = propertyNames[i];
                if (propName == "Array")
                {
                    isNextPropertyArrayIndex = true;
                }
                else if (isNextPropertyArrayIndex)
                {
                    isNextPropertyArrayIndex = false;
                    var arrayIndex = ParseArrayIndex(propName);
                    if (target is IList targetAsArray)
                        target = targetAsArray[arrayIndex];
                }
                else
                {
                    target = GetField(target, propName);
                }
            }

            return target;
        }

        /// <summary> This method returns the object value of the property using direct means. It does not support generic types and enums. </summary>
        private static bool GetTargetDirect(this SerializedProperty property, out object value)
        {
            // Fast path for common types
            switch (property.propertyType)
            {
                case SerializedPropertyType.Integer:
                    value = property.intValue;
                    return true;
                case SerializedPropertyType.Boolean:
                    value = property.boolValue;
                    return true;
                case SerializedPropertyType.Float:
                    value = property.floatValue;
                    return true;
                case SerializedPropertyType.String:
                    value = property.stringValue;
                    return true;
                case SerializedPropertyType.ObjectReference:
                    value = property.objectReferenceValue;
                    return true;
                case SerializedPropertyType.Enum:
                    // Skip this so GetTarget will find the actual enum, and not the integer value of it. 
                    // value = property.enumValueFlag;
                    value = null;
                    return false;
                case SerializedPropertyType.Color:
                    value = property.colorValue;
                    return true;
                case SerializedPropertyType.Vector2:
                    value = property.vector2Value;
                    return true;
                case SerializedPropertyType.Vector3:
                    value = property.vector3Value;
                    return true;
                case SerializedPropertyType.Vector4:
                    value = property.vector4Value;
                    return true;
                case SerializedPropertyType.Rect:
                    value = property.rectValue;
                    return true;
                case SerializedPropertyType.LayerMask:
                    value = property.intValue;  // LayerMask is stored as an integer
                    return true;
                case SerializedPropertyType.Character:
                    value = (char)property.intValue; // Character stored as an integer
                    return true;
                case SerializedPropertyType.AnimationCurve:
                    value = property.animationCurveValue;
                    return true;
                case SerializedPropertyType.Bounds:
                    value = property.boundsValue;
                    return true;
                case SerializedPropertyType.Quaternion:
                    value = property.quaternionValue;
                    return true;
                case SerializedPropertyType.ExposedReference:
                    value = property.exposedReferenceValue;
                    return true;
                case SerializedPropertyType.FixedBufferSize:
                    value = property.intValue; // Generally represents size
                    return true;
                case SerializedPropertyType.Vector2Int:
                    value = property.vector2IntValue;
                    return true;
                case SerializedPropertyType.Vector3Int:
                    value = property.vector3IntValue;
                    return true;
                case SerializedPropertyType.RectInt:
                    value = property.rectIntValue;
                    return true;
                case SerializedPropertyType.BoundsInt:
                    value = property.boundsIntValue;
                    return true;
                case SerializedPropertyType.ManagedReference:
                    // ManagedReference is a serialized reference to a managed (non-Unity) object.
                    value = property.managedReferenceValue;
                    return true;
                case SerializedPropertyType.Hash128:
                    value = property.hash128Value;
                    return true;

                case SerializedPropertyType.ArraySize:
                    // ArraySize holds the size of an array, accessible as an integer
                    value = property.intValue;
                    return true;

                // Default and unsupported types
                case SerializedPropertyType.Generic:
                default:
                    value = null;
                    return false;
            }
        }
        
        /// <summary> This method returns the Type of the property using direct means. It does not support generic types, object references and enums. </summary>
        private static bool GetTargetTypeDirect(this SerializedProperty property, out Type value)
        {
            // Fast path for common types
            switch (property.propertyType)
            {
                case SerializedPropertyType.Integer:
                    value = typeof(int);
                    return true;
                case SerializedPropertyType.Boolean:
                    value = typeof(bool);
                    return true;
                case SerializedPropertyType.Float:
                    value = typeof(float);
                    return true;
                case SerializedPropertyType.String:
                    value = typeof(string);
                    return true;
                case SerializedPropertyType.Enum:
                    // Skip this so GetTarget will find the actual enum, and not the integer value of it.
                    value = null;
                    return false;
                case SerializedPropertyType.Color:
                    value = typeof(Color);
                    return true;
                case SerializedPropertyType.Vector2:
                    value = typeof(Vector2);
                    return true;
                case SerializedPropertyType.Vector3:
                    value = typeof(Vector3);
                    return true;
                case SerializedPropertyType.Vector4:
                    value = typeof(Vector4);
                    return true;
                case SerializedPropertyType.Rect:
                    value = typeof(Rect);
                    return true;
                case SerializedPropertyType.LayerMask:
                    value = typeof(int); // LayerMask is stored as an integer
                    return true;
                case SerializedPropertyType.Character:
                    value = typeof(char); // Character stored as an integer
                    return true;
                case SerializedPropertyType.AnimationCurve:
                    value = typeof(AnimationCurve);
                    return true;
                case SerializedPropertyType.Bounds:
                    value = typeof(Bounds);
                    return true;
                case SerializedPropertyType.Quaternion:
                    value = typeof(Quaternion);
                    return true;
                case SerializedPropertyType.ExposedReference:
                    value = typeof(Object); // ExposedReference usually holds a UnityEngine.Object reference
                    return true;
                case SerializedPropertyType.FixedBufferSize:
                    value = typeof(int); // Represents size as an integer
                    return true;
                case SerializedPropertyType.Vector2Int:
                    value = typeof(Vector2Int);
                    return true;
                case SerializedPropertyType.Vector3Int:
                    value = typeof(Vector3Int);
                    return true;
                case SerializedPropertyType.RectInt:
                    value = typeof(RectInt);
                    return true;
                case SerializedPropertyType.BoundsInt:
                    value = typeof(BoundsInt);
                    return true;
                case SerializedPropertyType.ManagedReference:
                    value = property.managedReferenceValue?.GetType();
                    return value != null;
                case SerializedPropertyType.Hash128:
                    value = typeof(Hash128);
                    return true;
                case SerializedPropertyType.ArraySize:
                    value = typeof(int); // ArraySize is an integer
                    return true;
                    
                // Default and unsupported types
                case SerializedPropertyType.ObjectReference:
                case SerializedPropertyType.Generic:
                default:
                    value = null;
                    return false;
            }
        }

        /// <summary> Utility method for GetTarget </summary>
        private static object GetField(object target, string name, Type targetType = null)
        {
            if (target == null)
                return null;
            
            if (targetType == null)
                targetType = target.GetType();

            FieldInfo fi = null;
            while (targetType != null)
            {
                fi = targetType.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (fi != null)
                    return fi.GetValue(target);

                targetType = targetType.BaseType;
            }

            return null;
        }
        
        /// <summary> Utility method for GetTarget </summary>
        private static Type GetFieldType(object target, string name)
        {
            var fi = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (fi != null)
                return fi.FieldType;

            return null;
        }
        
        /// <summary> Utility method for GetTarget </summary>
        private static int ParseArrayIndex(string propertyName)
        {
            var match = ArrayIndexCapturePattern.Match(propertyName);
            if (!match.Success)
                throw new Exception($"Invalid array index parsing in {propertyName}");

            return int.Parse(match.Groups[1].Value);
        }

        /// <summary> Returns true if property is valid and not disposed off. Solution is dirty but could not access this information otherwise. </summary>
        public static bool Verify(this SerializedProperty property)
        {
            // Test that the serialized property has not been disposed off.
            try
            {
                var propertyPath = property.propertyPath;
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        
        /// <summary> Returns an array of any <see cref="CustomAttribute"/> found in the property. Otherwise returns null. </summary>
        public static Attribute[] GetAttributes(this SerializedProperty property)
        {
            var fieldInfo = GetFieldNested(property.serializedObject.targetObject, property.propertyPath);

            if (fieldInfo != null)
            {
                // var attributes = (CustomAttribute[])fieldInfo.GetCustomAttributes(typeof(CustomAttribute), true);
                return (Attribute[])fieldInfo.GetCustomAttributes(true);
            }

            return null;
        }
        
        /// <summary> Returns an array of any <see cref="CustomAttribute"/> found in the property. Otherwise returns null. </summary>
        public static CustomAttribute[] GetCustomAttributes(this SerializedProperty property)
        {
            // Arrays actually have a sibling "size". Their parent, is the actual property I will need to search in GetFieldNested.
            if (property.name == "Array")
                property = property.FindParentProperty();
            
            var fieldInfo = GetFieldNested(property.serializedObject.targetObject, property.propertyPath);

            if (fieldInfo != null)
            {
                var attributes = (CustomAttribute[])fieldInfo.GetCustomAttributes(typeof(CustomAttribute), true);
                return attributes;
            }

            return new CustomAttribute[] { };
        }

        
        /// <summary>Gets visible children of a <see cref="SerializedProperty"/> at 1 level depth.</summary>
        public static List<SerializedProperty> GetVisibleChildren(this SerializedProperty property)
        {
            var it = property.Copy();

            var list = new List<SerializedProperty>();
            
            if (it.NextVisible(true))
            {
                // If depth is same or bigger, iterator had no children.
                if (it.depth <= property.depth)
                    return list;
                
                do
                { 
                    list.Add(it.Copy());
                }
                while (it.NextVisible(false) && it.depth > property.depth);
            }

            return list;
        }
        
        /// <summary>
        /// Sort the properties based on the SortAttribute order.
        /// </summary>
        public static List<SerializedProperty> SortProperties(this List<SerializedProperty> properties)
        {
            var sortOrderCache = new Dictionary<SerializedProperty, int>() ;
            var needsSorting = false;

            foreach (var property in properties)
            {
                if (property.name == "m_Script")
                {
                    sortOrderCache[property] = int.MinValue;
                }
                else
                {
                    var attributes = property.GetCustomAttributes();
                    var sortAttribute = attributes?.FirstOrDefault(attr => attr is SortAttribute) as SortAttribute;
                    sortOrderCache[property] = sortAttribute?.Order ?? 0;
                    needsSorting = true;
                }
            }

            return needsSorting ? properties.OrderBy(p => sortOrderCache[p]).ToList() : properties;
        }
        
        /// <summary> Returns the field info of a target object based on the path </summary>
        private static FieldInfo GetFieldNested(object target, string path)
        {
            var fields = path.Split('.');
            var isNextPropertyArrayIndex = false;

            for (int i = 0; i < fields.Length - 1; ++i)
            {
                var propName = fields[i];
                if (propName == "Array")
                {
                    isNextPropertyArrayIndex = true;
                }
                else if (isNextPropertyArrayIndex)
                {
                    isNextPropertyArrayIndex = false;
                    var index = ParseArrayIndex(propName);
                    var targetAsList = target as IList;
                    if (targetAsList != null && targetAsList.Count > index)
                        target = targetAsList[index];
                }
                else
                    target = GetField(target, propName);
            }
            
            FieldInfo fieldInfo = null;
            if (target != null)
            {
                Type targetType = target.GetType();
                while (targetType != null)
                {
                    fieldInfo = targetType.GetField(fields[^1], BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (fieldInfo != null)
                        return fieldInfo;

                    targetType = targetType.BaseType;
                }
            }

            return fieldInfo;
        }
        
        
        /// <summary> Returns a serialized property in the same scope </summary>
        public static SerializedProperty FindPropertyInSameScope(this SerializedProperty property, string propertyName)
       {
           var path = property.propertyPath.Split('.');
           path[^1] = propertyName;

           var newPath = String.Join('.', path);
           return property.serializedObject.FindProperty(newPath);
       }
       
        /// <summary> Returns a serialized property in the same scope </summary>
        public static SerializedProperty FindParentProperty(this SerializedProperty property)
        {
            var path = property.propertyPath.Split('.');
            Array.Resize(ref path, path.Length - 1);
            var newPath = String.Join('.', path);
            return property.serializedObject.FindProperty(newPath);
        }

        
        /// <summary> Returns array children type from array property. </summary>
        public static Type GetArrayChildrenType(this SerializedProperty property)
        {
            Debug.Assert(property.isArray && property.propertyType != SerializedPropertyType.String, "Property must be an array.");

            Type returnValue = null;

            if (property.arraySize == 0)
            {
                property.InsertArrayElementAtIndex(0);
                property.serializedObject.ApplyModifiedProperties();

                var list = property.GetTarget<object>();
                if (list.GetType().IsArray) // Property is an array
                {
                    returnValue = list.GetType().GetElementType();
                }
                else // Property is a list or serializable collection
                    returnValue = list.GetType().GetGenericArguments().Single();
                
                property.ClearArray();
                property.serializedObject.ApplyModifiedProperties();
            }
            else
                returnValue = property.GetArrayElementAtIndex(0).GetTarget<object>().GetType();
            
            return returnValue;
        }
        
        /// <summary> Utility method to more clearly check for array properties. This was needed because string values, are considered arrays. </summary>
        public static bool IsArray(this SerializedProperty property)
        {
            // String typed SerializedProperties are by default considered arrays.
            // So use this utility method as a cleaner way to check for actual property arrays
            return property.isArray && property.propertyType != SerializedPropertyType.String;
        }

        /// <summary> Returns true if property is part of an array </summary>
        public static bool IsArrayElement(this SerializedProperty property)
        {
            var propertyParent = property.FindParentProperty();
            return propertyParent != null && propertyParent.IsArray();
        } 
        
        /// <summary> Returns index of property in its array or -1 if its not in an array </summary>
        public static int GetIndexInArray(this SerializedProperty property)
        {
            if (!property.IsArrayElement())
                return -1;
            int startIndex = property.propertyPath.LastIndexOf('[') + 1;
            int length = property.propertyPath.LastIndexOf(']') - startIndex;
            return int.Parse(property.propertyPath.Substring(startIndex, length));
        }

        /// <summary> Returns true if property is a managed reference property type. </summary>
        public static bool IsManagedReference(this SerializedProperty property)
        {
            return property.propertyType == SerializedPropertyType.ManagedReference;
        }
        
        /// <summary> Get type from type name though assembly definitions. </summary>
        public static Type GetTypeFromFieldTypename (string typeName) {
            if (string.IsNullOrEmpty(typeName))
                return null;

            var splitIndex = typeName.IndexOf(' ');
            var assembly = Assembly.Load(typeName.Substring(0,splitIndex));
            return assembly.GetType(typeName.Substring(splitIndex + 1));
        }
        
        /// <summary> Returns the string value of the underlying serialized property. </summary>
        public static string GetValueString(this SerializedProperty property)
        {
            switch (property.propertyType)
            {
                case SerializedPropertyType.Integer:
                    return property.intValue.ToString();
                case SerializedPropertyType.Boolean:
                    return property.boolValue.ToString();
                case SerializedPropertyType.Float:
                    return property.floatValue.ToString();
                case SerializedPropertyType.String:
                    return property.stringValue;
                case SerializedPropertyType.ObjectReference:
                    return property.objectReferenceValue != null ? property.objectReferenceValue.ToString() : "null";
                case SerializedPropertyType.Enum:
                    return property.enumDisplayNames[property.enumValueIndex];
                case SerializedPropertyType.Vector2:
                    return property.vector2Value.ToString();
                case SerializedPropertyType.Vector3:
                    return property.vector3Value.ToString();
                case SerializedPropertyType.Color:
                    return property.colorValue.ToString();
                case SerializedPropertyType.Rect:
                    return property.rectValue.ToString();
                case SerializedPropertyType.Bounds:
                    return property.boundsValue.ToString();
                default:
                    return $"Unsupported {property.propertyType.ToString()}";
            }
        }
     
        
        /// <summary> Copies the value of direct value of a serialized property if their SerializedPropertyType match.</summary>
        public static void Copy(this SerializedProperty property, SerializedProperty targetProperty)
        {
            if (targetProperty == null || property.propertyType != targetProperty.propertyType)
            {
                Debug.LogWarning("Cannot paste: mismatched property types or no copied value.");
                return;
            }

            // Paste based on the type of the copied property
            switch (targetProperty.propertyType)
            {
                case SerializedPropertyType.Integer:
                    property.intValue = targetProperty.intValue;
                    break;
                case SerializedPropertyType.Boolean:
                    property.boolValue = targetProperty.boolValue;
                    break;
                case SerializedPropertyType.Float:
                    property.floatValue = targetProperty.floatValue;
                    break;
                case SerializedPropertyType.String:
                    property.stringValue = targetProperty.stringValue;
                    break;
                case SerializedPropertyType.ObjectReference:
                    property.objectReferenceValue = targetProperty.objectReferenceValue;
                    break;
                case SerializedPropertyType.Color:
                    property.colorValue = targetProperty.colorValue;
                    break;
                case SerializedPropertyType.Vector2:
                    property.vector2Value = targetProperty.vector2Value;
                    break;
                case SerializedPropertyType.Vector3:
                    property.vector3Value = targetProperty.vector3Value;
                    break;
                case SerializedPropertyType.Vector4:
                    property.vector4Value = targetProperty.vector4Value;
                    break;
                case SerializedPropertyType.Rect:
                    property.rectValue = targetProperty.rectValue;
                    break;
                case SerializedPropertyType.Bounds:
                    property.boundsValue = targetProperty.boundsValue;
                    break;
                case SerializedPropertyType.Enum:
                    property.enumValueFlag = targetProperty.enumValueFlag;
                    break;
                default:
                    Debug.LogWarning($"Unsupported property type for paste: {targetProperty.propertyType}");
                    break;
            }

            property.serializedObject.ApplyModifiedProperties();
        }
    }
}