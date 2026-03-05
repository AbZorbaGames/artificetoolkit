using ArtificeToolkit.Attributes;

namespace CustomAttributes
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