using ArtificeToolkit.Attributes;

namespace ArtificeToolkit.Attributess
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
