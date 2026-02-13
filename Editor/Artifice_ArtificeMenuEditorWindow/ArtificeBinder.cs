using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace ArtificeToolkit.Editor.Artifice_ArtificeMenuEditorWindow
{
    public static class ArtificeBinder
    {
        // Safety limit to prevent infinite recursion (e.g., Object A has Object B has Object A)
        private const int MaxDepth = 10;
        
        /// <summary>
        /// Draws an inspector for a standalone object instance.
        /// Use this for your root objects or nested classes.
        /// </summary>
        public static VisualElement CreateField(object instance)
        {
            if (instance == null) 
                return new Label("Instance is null");
        
            var root = new VisualElement();
        
            // 1. Reflect on the INSTANCE itself to find its members
            var fields = instance.GetType().GetFields(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            foreach (var field in fields)
                root.Add(CreateField(instance, field, 0));

            return root;
        }
        
        public static VisualElement CreateField(object target, FieldInfo field, int depth = 0)
        {
            if (depth > MaxDepth)
                return new Label("Max Depth Reached");

            var type = field.FieldType;
            var label = ObjectNames.NicifyVariableName(field.Name);

            if (IsLeafType(type))
            {
                return CreateLeafField(target, field, type, label);
            }
        
            if (typeof(System.Collections.IList).IsAssignableFrom(type))
                return new Label($"{label} (List/Array support requires generic iterators)");

            return CreateNestedGroup(target, field, label, depth);
        }

        private static VisualElement CreateNestedGroup(object parentTarget, FieldInfo parentField, string label, int depth)
        {
            var foldout = new Foldout
            {
                text = label, 
                value = false,
                style =
                {
                    marginLeft = 15
                }
            };

            // Container for the children
            var contentContainer = new VisualElement();
            foldout.Add(contentContainer);

            // Get instance and iterate over fields.
            var currentInstance = parentField.GetValue(parentTarget);
            var childFields = currentInstance.GetType().GetFields(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            foreach (var childField in childFields)
            {
                // Skip hidden/compiler generated backing fields if you want cleaner UI
                if (childField.Name.Contains("k__BackingField")) continue;

                // Recursive Call: 
                // Target is now 'currentInstance', not 'parentTarget'
                var childUI = CreateField(currentInstance, childField, depth + 1);
                contentContainer.Add(childUI);
            }

            return foldout;
        }
    
        private static VisualElement CreateLeafField(object target, FieldInfo field, Type type, string label)
        {
            // ... (Insert the Switch/If block from the previous response here) ...
            // Example:
            if (type == typeof(int)) return Bind(new IntegerField(label), target, field);
            if (type == typeof(float)) return Bind(new FloatField(label), target, field);
            if (type == typeof(string)) return Bind(new TextField(label), target, field);
            if (typeof(UnityEngine.Object).IsAssignableFrom(type))
            {
                var objField = new ObjectField(label) { objectType = type };
                return BindGeneric(objField, target, field);
            }
        
            return new Label($"Unknown Leaf: {type.Name}");
        }
        
        private static bool IsLeafType(Type t)
        {
            return t.IsPrimitive || t == typeof(string) || t.IsEnum || 
                   typeof(UnityEngine.Object).IsAssignableFrom(t) ||
                   t == typeof(Vector3) || t == typeof(Vector2) || t == typeof(Color);
        }

        private static VisualElement Bind<T>(BaseField<T> control, object target, FieldInfo field)
        {
            var val = field.GetValue(target);
            if(val != null) control.value = (T)val;

            control.RegisterValueChangedCallback(evt => {
                field.SetValue(target, evt.newValue);
                if(target is UnityEngine.Object uo) EditorUtility.SetDirty(uo);
            });

            // Polling for value updates
            control.schedule.Execute(() => {
                var actual = (T)field.GetValue(target);
                if (!EqualityComparer<T>.Default.Equals(actual, control.value))
                    control.SetValueWithoutNotify(actual);
            }).Every(100);

            return control;
        }
    
        private static VisualElement BindGeneric<T>(BaseField<T> control, object target, FieldInfo field)
        {
            return Bind(control, target, field);
        }
    }
}