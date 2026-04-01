namespace ArtificeToolkit.Attributes
{
    public class RequiredAttribute : ValidatorAttribute
    {
        public readonly string Message = "";
        
        public RequiredAttribute()
        {
            Message = "Property is required.";
        }

        public RequiredAttribute(string message)
        {
            Message = message;
        }
    }
}
