using ArtificeToolkit.Attributes;
using UnityEngine.UIElements;

namespace ArtificeToolkit.Editor.Artifice_CustomAttributeDrawers.CustomAttributeDrawers_Groups
{
    /// <summary> Utility VisualElement used for <see cref="Artifice_CustomAttributeDrawer_BoxGroupAttribute"/> </summary>
    /// <remarks> Offers API for setting and reseting its content container. </remarks>
    public class Artifice_VisualElement_BoxGroup : Artifice_VisualElement_Group
    {
        private readonly VisualElement _titleContainer;
        private readonly Label _titleLabel;

        protected Artifice_VisualElement_BoxGroup() : base()
        {
            styleSheets.Add(Artifice_Utilities.GetStyle(typeof(Artifice_VisualElement_Group)));
            styleSheets.Add(Artifice_Utilities.GetStyle(typeof(Artifice_VisualElement_BoxGroup)));
            
            // Create title container and its label
            _titleContainer = new VisualElement();
            _titleContainer.AddToClassList("align-horizontal");
            _titleContainer.AddToClassList("title-container");
            hierarchy.Insert(0, _titleContainer);
                
            _titleLabel = new Label("Undefined");
            _titleLabel.AddToClassList("title-label");
            _titleContainer.Add(_titleLabel);
        }

        public override void SetTitle(string title)
        {
            _titleLabel.text = title;
        }

        public override void SetGroupColor(GroupColor groupColor)
        {
            base.SetGroupColor(groupColor);
        }
    }
}
