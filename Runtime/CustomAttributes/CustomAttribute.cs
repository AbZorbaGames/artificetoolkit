using System;
using System.Diagnostics;

namespace ArtificeToolkit.Attributes
{
    /// <summary>Customizes how a field is rendered in the inspector.</summary>
    [Conditional("UNITY_EDITOR")]
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Enum | AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Struct, AllowMultiple = true)]
    public abstract class CustomAttribute : Attribute
    {
    }
}