using ArtificeToolkit.Attributes;

namespace ArtificeToolkit.Attributes
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
