using ArtificeToolkit.Attributes;

namespace ArtificeToolkit.Attributes
{
    public class VerticalGroupBeginAttribute : VerticalGroupAttribute
    {
        public VerticalGroupBeginAttribute() : base("Vertical Group")
        {
            
        }
        public VerticalGroupBeginAttribute(string groupName) : base(groupName)
        {
        }
    }
}
