using ArtificeToolkit.Attributes;

namespace ArtificeToolkit.Attributess
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