using ArtificeToolkit.Attributes;

public class FoldoutGroupBeginAttribute : FoldoutGroupAttribute
{
    public FoldoutGroupBeginAttribute() : base("Foldout Group")
    {
            
    }
    public FoldoutGroupBeginAttribute(string groupName) : base(groupName)
    {
    }
    public FoldoutGroupBeginAttribute(string groupName, GroupColor groupColor) : base(groupName, groupColor)
    {
    }
}
