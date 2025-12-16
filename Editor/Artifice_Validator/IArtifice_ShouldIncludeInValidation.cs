using ArtificeToolkit.Attributes;
using UnityEditor;

namespace ArtificeToolkit.Editor
{
    public interface IArtifice_ShouldIncludeInValidation
    {
        public bool ShouldIncludeInValidation(SerializedProperty property, CustomAttribute customAttribute);
    }
}
