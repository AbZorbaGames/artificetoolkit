using System;

namespace ArtificeToolkit.Attributes
{
    /// <summary> This attribute changes the way default Enums are rendered to have them as toggle buttons </summary>
    /// <remarks> Automatically detects and handles enums which are marked as Flagged. </remarks>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Enum, AllowMultiple = true)]
    public class EnumToggleAttribute : CustomAttribute
    {
        public readonly bool HideLabel = false;
        
        public EnumToggleAttribute(bool hideLabel = false) : base()
        {
            HideLabel = hideLabel;
        }
    }
}
