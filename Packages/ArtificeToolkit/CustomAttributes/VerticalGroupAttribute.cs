namespace CustomAttributes
{
    /// <summary> This attribute on its own is not useful, but comes into play in parallel with the <see cref="HorizontalGroupAttribute"/> </summary>
    public class VerticalGroupAttribute : GroupAttribute
    {
        public VerticalGroupAttribute() : base("Vertical Group")
        {
            
        }
        public VerticalGroupAttribute(string groupName) : base(groupName)
        {
        }
    }
}
