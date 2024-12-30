using ArtificeToolkit.Editor.Artifice_CustomAttributeDrawers.CustomAttributeDrawers_Groups;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;

namespace ArtificeToolkit.Editor.VisualElements
{
    /// <summary>
    /// This element provides a sliding container which is enabled or disabled by a button.
    /// </summary>
    public class Artifice_VisualElement_SlidingGroup : Artifice_VisualElement_Group
    {
        public bool IsExpanded;
        
        private Button _handeElem;

        private ValueAnimation<float> _valueAnimation = new();

        private const int ToggleAnimationDurationMillis = 350;
        
        public Artifice_VisualElement_SlidingGroup() : base()
        {
            styleSheets.Add(Artifice_Utilities.GetStyle(GetType()));
            
            // Default content is created in base constructor.
            DefaultContentContainer.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);
            
            // Create handle button
            _handeElem = new Button(ToggleSlidingContainer);
            _handeElem.text = "Sliding Group";
            _handeElem.AddToClassList("handle");
            hierarchy.Insert(0, _handeElem);
            
            // Make default state be slided off for now.
            IsExpanded = false;
            SlideOff();
        }

        public override void SetTitle(string title)
        {
            _handeElem.text = title;
        }

        private void ToggleSlidingContainer()
        {
            if (IsExpanded)
                SlideOff();
            else
                SlideOn();

            IsExpanded = !IsExpanded;
        }

        private void SlideOn()
        {
            if(_valueAnimation != null && _valueAnimation.isRunning)
                _valueAnimation.Stop();
            
            // Make visible and flex.
            DefaultContentContainer.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.Flex);
            DefaultContentContainer.style.position = new StyleEnum<Position>(Position.Absolute);
            
            #if UNITY_2022_1_OR_NEWER
            
            // Begin animation!
            var width = resolvedStyle.width;
            _valueAnimation = DefaultContentContainer.experimental.animation.Start(1f, 0f, ToggleAnimationDurationMillis, (elem, value) =>
            {
                elem.style.translate = new StyleTranslate(new Translate(value * width, 0f));
            });
            
            #else
            
            // Begin animation!
            var width = resolvedStyle.width;
            _valueAnimation = DefaultContentContainer.experimental.animation.Start(0f, 1f, ToggleAnimationDurationMillis, (elem, value) =>
            {
                elem.style.scale = new StyleScale(new Scale(new Vector2(value, value)));
            });
            
            #endif
        }

        private void SlideOff()
        {
            if(_valueAnimation != null && _valueAnimation.isRunning)
                _valueAnimation.Stop();
            
#if UNITY_2022_1_OR_NEWER
            
            // Begin animation!
            var width = resolvedStyle.width;
            _valueAnimation = DefaultContentContainer.experimental.animation.Start(0f, 1f, ToggleAnimationDurationMillis, (elem, value) =>
            {
                elem.style.translate = new StyleTranslate(new Translate(value * width, 0f));
            })
                .OnCompleted(() => DefaultContentContainer.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None));;
            
#else
            
            // Begin animation!
            var width = resolvedStyle.width;
            _valueAnimation = DefaultContentContainer.experimental.animation.Start(1f, 0f, ToggleAnimationDurationMillis, (elem, value) =>
            {
                elem.style.scale = new StyleScale(new Scale(new Vector2(value, value)));
            })
                .OnCompleted(() => DefaultContentContainer.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None));
            
#endif
        }
    }
}