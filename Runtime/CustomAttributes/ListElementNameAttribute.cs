using ArtificeToolkit.Attributes;

namespace CustomAttributes
{
    public class ListElementNameAttribute : CustomAttribute, IArtifice_ArrayAppliedAttribute
    {
        public readonly string FieldName;

        public ListElementNameAttribute(string fieldName)
        {
            FieldName = fieldName;
        }
    }
}
