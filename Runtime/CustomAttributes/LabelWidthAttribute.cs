namespace ArtificeToolkit.Attributes
{
    /// <summary> Controls the width of Label Fields </summary>
    public class LabelWidthAttribute : CustomAttribute
    {
        public readonly int WidthPixels;

        public LabelWidthAttribute(int widthPixels)
        {
            WidthPixels = widthPixels;
        }
    }
}
