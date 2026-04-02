using ArtificeToolkit.Attributes;

namespace ArtificeToolkit.Editor.Artifice_CustomAttributeDrawers.CustomAttributeDrawers_Groups
{
    [Artifice_CustomAttributeDrawer(typeof(HorizontalGroupBeginAttribute))]
    public class Artifice_CustomAttributeDrawer_HorizontalGroupBeginAttribute : Artifice_CustomAttributeDrawer_HorizontalGroupAttribute
    {
        protected override bool IsOpenGroupDrawer { get; } = true;
    }
}