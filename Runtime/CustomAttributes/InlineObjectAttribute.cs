using ArtificeToolkit.Attributes;

namespace ArtificeToolkit.Attributes
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