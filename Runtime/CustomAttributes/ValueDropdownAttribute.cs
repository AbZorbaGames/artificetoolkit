using System;
using UnityEngine;

namespace ArtificeToolkit.Attributes
{
    /// <summary>
    /// Attribute to specify that a string field should present a dropdown list of values
    /// retrieved from a specified method, property, or field.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class ValueDropdownAttribute : PropertyAttribute
    {
        public string MethodName { get; private set; }

        public ValueDropdownAttribute(string methodName)
        {
            MethodName = methodName;
        }
    }
}