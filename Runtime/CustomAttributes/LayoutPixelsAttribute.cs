using ArtificeToolkit.Attributes;

namespace ArtificeToolkit.Attributes
{
    public class LayoutPixelsAttribute : CustomAttribute
    {
        public readonly int? WidthPixels;
        public readonly int? HeightPixels;
        
        /// <summary>
        /// Using width or height of value 0 will cause the value to be ignored.
        /// </summary>
        public LayoutPixelsAttribute(int widthPixels = 0, int heightPixels = 0)
        {
            WidthPixels = widthPixels > 0 ? widthPixels : null;
            HeightPixels = heightPixels > 0 ? heightPixels : null;
        }
    }
}
