namespace ArtificeToolkit.Attributes
{
    /// <summary> Adds space in the given direction. </summary>
    public class SpaceAttribute : CustomAttribute, IArtifice_ArrayAppliedAttribute
    {
        public readonly int ValueTop;
        public readonly int ValueBottom;
        public readonly int ValueLeft;
        public readonly int ValueRight;

        public SpaceAttribute(int top)
        {
            ValueTop = top;
        }

        public SpaceAttribute(int top, int bottom)
        {
            ValueTop = top;
            ValueBottom = bottom;
        }
        
        public SpaceAttribute(int top, int bottom, int left, int right)
        {
            ValueTop = top;
            ValueBottom = bottom;
            ValueLeft = left;
            ValueRight = right;
        }
    }
}
