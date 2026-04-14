using ArtificeToolkit.Attributes;
using ArtificeToolkit.Editor.Artifice_CustomAttributeDrawers;
using ArtificeToolkit.Editor.Artifice_CustomAttributeDrawers.CustomAttributeDrawers_Groups;

namespace ArtificeToolkit.Editor.Artifice_CustomAttributeDrawers.CustomAttributeDrawers_Groups
{
    [Artifice_CustomAttributeDrawer(typeof(VerticalGroupBeginAttribute))]
    public class Artifice_CustomAttributeDrawer_VerticalGroupBeginAttribute : Artifice_CustomAttributeDrawer_VerticalGroupAttribute
    {
        protected override bool IsOpenGroupDrawer { get; } = true;
    }
}
