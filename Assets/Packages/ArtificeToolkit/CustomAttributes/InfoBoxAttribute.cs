namespace Packages.ArtificeToolkit.CustomAttributes
{
    /// <summary> Shows in the inspector an info box the parameterized message </summary>
    public class InfoBoxAttribute : CustomAttribute
    {
        public string Message;
        public InfoMessageType Type;
        
        public enum InfoMessageType
        {
            Info, Warning, Error, None
        }
        
        public InfoBoxAttribute(string message)
        {
            Message = message;
            Type = InfoMessageType.Info;
        }

        public InfoBoxAttribute(string message, InfoMessageType type)
        {
            Message = message;
            Type = type;
        }
    }
}