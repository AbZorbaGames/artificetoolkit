using ArtificeToolkit.Attributes;

namespace ArtificeToolkit.Attributes
{
    public class BoxGroupBeginAttribute : BoxGroupAttribute
    {
        public BoxGroupBeginAttribute() : base("Box Group")
        {
        }
        public BoxGroupBeginAttribute(string groupName) : base(groupName)
        {
        }
        public BoxGroupBeginAttribute(string groupName, GroupColor groupColor) : base(groupName, groupColor)
        {
        }
    }
}