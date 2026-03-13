using System;
using ArtificeToolkit.Attributes;

namespace CustomAttributes
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Enum | AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Struct, AllowMultiple = false)]
    public class ButtonPropertyAttribute : CustomAttribute
    {
        public readonly string MethodName;
        public readonly string[] ParameterNames;

        public ButtonPropertyAttribute(string methodName, params string[] parameterNames)
        {
            MethodName = methodName;
            ParameterNames = parameterNames;
        }
    }
}
