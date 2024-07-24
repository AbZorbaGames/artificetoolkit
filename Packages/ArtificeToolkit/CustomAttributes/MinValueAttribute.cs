namespace CustomAttributes
{
    /// <summary> Bounds the property's minimum value to a given value </summary>
    public class MinValueAttribute : CustomAttribute
    {
        public float Value;
        
        public MinValueAttribute(float value)
        {
            Value = value;
        }
    }
}