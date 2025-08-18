namespace ArtificeToolkit.Attributes
{
    public class RequiredAttribute : ValidatorAttribute
    {
        public string Message = "";
        
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
