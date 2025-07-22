using System;

namespace ArtificeToolkit.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Field)]
    public class ArtificeIgnoreAttribute : CustomAttribute
    {
    }
}
