using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ArtificeToolkit.Editor
{
    /// <summary> Enables deep copy and pasting of serialized properties. </summary>
    public class SerializedPropertyCopier
    {
        private readonly List<PropertyData> _copiedData = new();

        private class PropertyData
        {
            public string RelativePath;
            public object Value;
            public SerializedPropertyType Type;
        }

        public void Copy(SerializedProperty property)
        {
            _copiedData.Clear();
            var copy = property.Copy();
            var end = copy.GetEndProperty();

            while (copy.NextVisible(true) && !SerializedProperty.EqualContents(copy, end))
            {
                _copiedData.Add(new PropertyData
                {
                    RelativePath = copy.propertyPath.Substring(property.propertyPath.Length + 1),
                    Value = GetValue(copy),
                    Type = copy.propertyType
                });
            }
        }

        public void Paste(SerializedProperty property)
        {
            foreach (var data in _copiedData)
            {
                var targetProp = property.FindPropertyRelative(data.RelativePath);
                if (targetProp == null) continue;

                SetValue(targetProp, data.Value, data.Type);
            }

            property.serializedObject.ApplyModifiedProperties();
        }

        private object GetValue(SerializedProperty prop)
        {
            return prop.propertyType switch
            {
                SerializedPropertyType.Integer => prop.intValue,
                SerializedPropertyType.Boolean => prop.boolValue,
                SerializedPropertyType.Float => prop.floatValue,
                SerializedPropertyType.String => prop.stringValue,
                SerializedPropertyType.Color => prop.colorValue,
                SerializedPropertyType.ObjectReference => prop.objectReferenceValue,
                SerializedPropertyType.LayerMask => prop.intValue,
                SerializedPropertyType.Enum => prop.enumValueIndex,
                SerializedPropertyType.Vector2 => prop.vector2Value,
                SerializedPropertyType.Vector3 => prop.vector3Value,
                SerializedPropertyType.Vector4 => prop.vector4Value,
                SerializedPropertyType.Rect => prop.rectValue,
                SerializedPropertyType.ArraySize => prop.intValue,
                SerializedPropertyType.Character => prop.intValue,
                SerializedPropertyType.AnimationCurve => prop.animationCurveValue,
                SerializedPropertyType.Bounds => prop.boundsValue,
                SerializedPropertyType.Quaternion => prop.quaternionValue,
                _ => null
            };
        }

        private void SetValue(SerializedProperty prop, object value, SerializedPropertyType type)
        {
            switch (type)
            {
                case SerializedPropertyType.Integer: prop.intValue = (int)value; break;
                case SerializedPropertyType.Boolean: prop.boolValue = (bool)value; break;
                case SerializedPropertyType.Float: prop.floatValue = (float)value; break;
                case SerializedPropertyType.String: prop.stringValue = (string)value; break;
                case SerializedPropertyType.Color: prop.colorValue = (Color)value; break;
                case SerializedPropertyType.ObjectReference: prop.objectReferenceValue = (Object)value; break;
                case SerializedPropertyType.LayerMask: prop.intValue = (int)value; break;
                case SerializedPropertyType.Enum: prop.enumValueIndex = (int)value; break;
                case SerializedPropertyType.Vector2: prop.vector2Value = (Vector2)value; break;
                case SerializedPropertyType.Vector3: prop.vector3Value = (Vector3)value; break;
                case SerializedPropertyType.Vector4: prop.vector4Value = (Vector4)value; break;
                case SerializedPropertyType.Rect: prop.rectValue = (Rect)value; break;
                case SerializedPropertyType.ArraySize: prop.intValue = (int)value; break;
                case SerializedPropertyType.Character: prop.intValue = (int)value; break;
                case SerializedPropertyType.AnimationCurve: prop.animationCurveValue = (AnimationCurve)value; break;
                case SerializedPropertyType.Bounds: prop.boundsValue = (Bounds)value; break;
                case SerializedPropertyType.Quaternion: prop.quaternionValue = (Quaternion)value; break;
            }
        }
    }
}