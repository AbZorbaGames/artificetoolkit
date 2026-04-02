using System;
using ArtificeToolkit.Attributes;
using ArtificeToolkit.Editor.Artifice_CustomAttributeDrawers;

namespace ArtificeToolkit.Editor.Artifice_CustomAttributeDrawers.CustomAttributeDrawers_Groups
{
    /// <summary> Custom VisualAttribute drawer for <see cref="FoldoutGroupAttribute"/> </summary>
    [Artifice_CustomAttributeDrawer(typeof(FoldoutGroupAttribute))]
    public class Artifice_CustomAttributeDrawer_FoldoutGroupAttribute : Artifice_CustomAttributeDrawer_GroupAttribute
    {
        protected override Type VisualElementType { get; } = typeof(Artifice_VisualElement_FoldoutGroup);
    }
}