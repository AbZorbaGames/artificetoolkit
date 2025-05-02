namespace ArtificeToolkit.Attributes
{
    public class ConditionalInfoBoxAttribute : InfoBoxAttribute, IArtifice_ArrayAppliedAttribute
    {
        public readonly string PropertyName;
        public readonly object[] Values;

        public ConditionalInfoBoxAttribute(
            string message,
            string propertyName,
            params object[] values
        ) : base(message, InfoMessageType.Warning)
        {
            PropertyName = propertyName;
            Values = values;
        }
        
        public ConditionalInfoBoxAttribute(
            string message,
            InfoMessageType type,
            string propertyName,
            params object[] values
        ) : base(message, type)
        {
            PropertyName = propertyName;
            Values = values;
        }
    }
}
