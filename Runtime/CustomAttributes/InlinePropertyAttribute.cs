using ArtificeToolkit.Attributes;

namespace CustomAttributes
{
    public enum InlinePropertyStyle
    {
        WithTitle,
        WithoutTitle,
        WithoutTitleBorderless
    }

    public class InlinePropertyAttribute : CustomAttribute
    {
        public readonly InlinePropertyStyle Type;

        public InlinePropertyAttribute(InlinePropertyStyle type = InlinePropertyStyle.WithTitle)
        {
            Type = type;
        }
    }
}