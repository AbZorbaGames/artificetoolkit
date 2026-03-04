using ArtificeToolkit.Attributes;

namespace ArtificeToolkit.CustomAttributes
{
    public class LayoutPercentAttribute : CustomAttribute
    {
        public readonly int? WidthPercent;
        public readonly int? HeightPercent;
        
        /// <summary>
        /// Using width or height of value 0 will cause the value to be ignored.
        /// </summary>
        public LayoutPercentAttribute(int widthPercent = 0, int heightPercent = 0)
        {
            WidthPercent = widthPercent > 0 ? widthPercent : null;
            HeightPercent = heightPercent > 0 ? heightPercent : null;
        }
    }
}
