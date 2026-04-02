using ArtificeToolkit.Attributes;

namespace ArtificeToolkit.Editor.Artifice_CustomAttributeDrawers.CustomAttributeDrawers_Groups
{
    [Artifice_CustomAttributeDrawer(typeof(BoxGroupBeginAttribute))]
    public class Artifice_CustomAttributeDrawer_BoxGroupBeginAttribute : Artifice_CustomAttributeDrawer_BoxGroupAttribute
    {
        protected override bool IsOpenGroupDrawer { get; } = true;
    }
}