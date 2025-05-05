namespace ArtificeToolkit.Attributes
{
    /// <summary> Renders a Title section above the point it is attributed to.</summary>
    public class TitleAttribute : CustomAttribute, IArtifice_ArrayAppliedAttribute
    {
        public readonly string Title;
        
        public TitleAttribute(string title)
        {
            Title = title;
        }        
    }
}
