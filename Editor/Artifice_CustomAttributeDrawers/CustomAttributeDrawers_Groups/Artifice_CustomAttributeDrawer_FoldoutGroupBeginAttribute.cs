using ArtificeToolkit.Editor.Artifice_CustomAttributeDrawers;

namespace ArtificeToolkit.Editor.Artifice_CustomAttributeDrawers.CustomAttributeDrawers_Groups
{
    [Artifice_CustomAttributeDrawer(typeof(FoldoutGroupBeginAttribute))]
    public class Artifice_CustomAttributeDrawer_FoldoutGroupBeginAttribute : Artifice_CustomAttributeDrawer_FoldoutGroupAttribute
    {
        protected override bool IsOpenGroupDrawer { get; } = true;
    }
}