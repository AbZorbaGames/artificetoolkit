namespace ArtificeToolkit.Attributes
{
    /// <summary> As <see cref="BoxGroupAttribute"/>, but uses horizontal alignment. </summary>
    /// <remarks> Current implementation does not cope well with input fields. </remarks>
    public class HorizontalGroupAttribute : BoxGroupAttribute
    {
        public readonly float WidthPercent = -1;
        
        public HorizontalGroupAttribute() : base("Horizontal Group", GroupColor.Transparent)
        {
            
        }
        public HorizontalGroupAttribute(string groupName) : base(groupName, GroupColor.Transparent)
        {
            
        }
    }
}
