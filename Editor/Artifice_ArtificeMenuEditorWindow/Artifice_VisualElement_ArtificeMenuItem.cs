using UnityEditor;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace Editor.Artifice_ArtificeMenuEditorWindow
{
    public class Artifice_VisualElement_ArtificeMenuItem : VisualElement
    {
        public readonly UnityEvent<ArtificeMenuTreeNode> OnClick = new();
        public ArtificeMenuTreeNode Node { get; }
        private Artifice_VisualElement_ArtificeMenuItem Parent { get; set; }

        private readonly VisualElement _headerContainer;
        private readonly VisualElement _childrenContainer;
        private Image _collapseImage;

        public Artifice_VisualElement_ArtificeMenuItem(ArtificeMenuTreeNode node)
        {
            Node = node;
            AddToClassList("menu-item");

            _headerContainer = CreateHeader(node);
            _childrenContainer = CreateChildrenContainer();

            hierarchy.Add(_headerContainer);
            hierarchy.Add(_childrenContainer);
        }

        private VisualElement CreateHeader(ArtificeMenuTreeNode node)
        {
            var container = new VisualElement();
            container.AddToClassList("menu-item-header-container");
            container.RegisterCallback<MouseDownEvent>(_ => OnClick?.Invoke(node));

            if (node.ScriptableObject != null)
                container.AddToClassList("menu-item-header-container-allowed-hover");

            if (node.Get_Children().Count > 0 || node.ScriptableObject == null)
            {
                _collapseImage = new Image { image = EditorGUIUtility.IconContent("d_icon dropdown@2x").image };
                _collapseImage.AddToClassList("menu-item-collapse-button");
                _collapseImage.RegisterCallback<MouseDownEvent>(evt =>
                {
                    evt.StopImmediatePropagation();
                    ToggleExpanded();
                });
                container.Add(_collapseImage);
            }

            if (node.Sprite != null)
            {
                var icon = new Image { image = node.Sprite.texture };
                icon.AddToClassList("menu-item-header-icon");
                container.Add(icon);
            }

            var label = new Label(node.Title);
            label.AddToClassList("menu-item-header-label");
            container.Add(label);
            return container;
        }

        private VisualElement CreateChildrenContainer()
        {
            var container = new VisualElement();
            container.AddToClassList("menu-item-children-container");
            container.AddToClassList("hide");
            return container;
        }

        public void AddChild(Artifice_VisualElement_ArtificeMenuItem menuItem)
        {
            _childrenContainer.Add(menuItem);
            menuItem.SetParent(this);
        }

        public void SetSelected(bool selected)
        {
            _headerContainer.EnableInClassList("menu-item--selected", selected);
            if (selected) ExpandPath();
        }

        private void ExpandPath()
        {
            var iterator = Parent;
            while (iterator != null)
            {
                iterator.ToggleExpanded(true);
                iterator = iterator.Parent;
            }
        }

        public void SetParent(Artifice_VisualElement_ArtificeMenuItem parent)
        {
            Parent = parent;
            _headerContainer.style.paddingLeft = 10 + (10 * GetDepth());
        }

        public void ToggleExpanded(bool? forceState = null)
        {
            bool expand = forceState ?? _childrenContainer.ClassListContains("hide");
            _childrenContainer.EnableInClassList("hide", !expand);
            _collapseImage?.EnableInClassList("menu-item-collapse-button--expanded", expand);
        }

        private int GetDepth() => Parent == null ? 0 : 1 + Parent.GetDepth();
    }
}