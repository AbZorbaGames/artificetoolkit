namespace ArtificeToolkit.Attributes
{
    /// <summary> Bounds the property's max value to a given value </summary>
    public class MaxValueAttribute : CustomAttribute
    {
        public float Value;
        
        public MaxValueAttribute(float value)
        {
            Value = value;
        }
    }
}
