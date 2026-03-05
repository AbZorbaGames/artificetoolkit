using ArtificeToolkit.Attributes;

namespace ArtificeToolkit.Attributess
{
    public class InlineObjectAttribute : CustomAttribute
    {
        public readonly bool IsShrinkable;

        public InlineObjectAttribute(bool isShrinkable = true)
        {
            IsShrinkable = isShrinkable;
        }
    }
}