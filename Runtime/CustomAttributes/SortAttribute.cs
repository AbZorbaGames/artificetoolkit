namespace ArtificeToolkit.Attributes
{
    /// <summary>Customizes the order of the renderer in the inspector</summary>
    /// <remarks>Lower number shows first</remarks>
    /// <example>[Sort(1)]</example>
    public class SortAttribute: CustomAttribute
    {
        public readonly int Order;
        
        public SortAttribute(int order)
        {
            Order = order;
        }
    }
}