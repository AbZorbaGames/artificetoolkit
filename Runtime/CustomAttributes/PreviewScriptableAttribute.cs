using System;

namespace ArtificeToolkit.Attributes
{
    [Obsolete("Use `InlineObject` instead. Its the same for scriptable objects but also works for most UnityEngine.Object types.")]
    public class PreviewScriptableAttribute : CustomAttribute
    {
    }
}
