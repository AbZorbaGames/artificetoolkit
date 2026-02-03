using ArtificeToolkit.Attributes;

namespace CustomAttributes
{
    public class SafeTooltipAttribute : CustomAttribute
    {
        public readonly string Tooltip;

        public SafeTooltipAttribute(string tooltip)
        {
            Tooltip = tooltip;
        }
    }
}