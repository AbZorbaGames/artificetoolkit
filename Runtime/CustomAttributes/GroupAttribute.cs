namespace ArtificeToolkit.Attributes
{
    public enum GroupColor
    {
        Default,
        Red,
        Blue,
        Black,
        Green,
        Yellow,
        Orange,
        Pink,
        Purple
    }
    
    /// <summary> All Group-like attributes should inherit from this class. </summary>
    public abstract class GroupAttribute : CustomAttribute, IArtifice_ArrayAppliedAttribute
    {
        public readonly string GroupName;
        public readonly GroupColor GroupColor;

        protected GroupAttribute(string groupName)
        {
            GroupName = groupName;
            GroupColor = GroupColor.Default;
        }
        protected GroupAttribute(string groupName, GroupColor groupColor)
        {
            GroupName = groupName;
            GroupColor = groupColor;
        }
    }
}
