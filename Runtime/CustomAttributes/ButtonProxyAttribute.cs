using System;
using ArtificeToolkit.Attributes;

namespace CustomAttributes
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Enum | AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Struct, AllowMultiple = false)]
    public class ButtonProxyAttribute : CustomAttribute
    {
        public readonly string MethodName;
        public readonly string[] ParameterNames;

        public ButtonProxyAttribute(string methodName, params string[] parameterNames)
        {
            MethodName = methodName;
            ParameterNames = parameterNames;
        }
    }
}
