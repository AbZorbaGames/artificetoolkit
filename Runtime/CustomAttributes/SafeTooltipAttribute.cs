using ArtificeToolkit.Attributes;

namespace ArtificeToolkit.Attributes
{
    public class SafeTooltipAttribute : CustomAttribute, IArtifice_ArrayAppliedAttribute
    {
        public readonly string Tooltip;

        public SafeTooltipAttribute(string tooltip)
        {
            Tooltip = tooltip;
        }
    }
}