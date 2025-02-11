namespace ArtificeToolkit.Attributes
{
    /// <summary> This attribute receives a method name which invokes after the property tracks a change.
    /// Temporary attribute, until expression are added in the Artifice system. </summary>
    public class OnValueChanged : CustomAttribute
    {
        public readonly string MethodName;

        public OnValueChanged(string methodName)
        {
            MethodName = methodName;
        }
    }
}
