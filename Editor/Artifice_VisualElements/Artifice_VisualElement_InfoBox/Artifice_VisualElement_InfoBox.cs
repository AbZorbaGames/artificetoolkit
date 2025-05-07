using ArtificeToolkit.Editor.Resources;
using UnityEngine;
using UnityEngine.UIElements;

namespace ArtificeToolkit.Editor.VisualElements
{
    public class Artifice_VisualElement_InfoBox : VisualElement
    {
        private readonly Image _image;
        private readonly Label _labelMessage;

        private Artifice_VisualElement_InfoBox()
        {
            // Load stylesheet
            styleSheets.Add(Artifice_Utilities.GetStyle(GetType()));
            
            // Load container style
            AddToClassList("info-box-container");
            
            // Add image
            _image = new Image();
            _image.AddToClassList("image");
            Add(_image);

            // Add label
            _labelMessage = new Label();
            _labelMessage.AddToClassList("label");
            _labelMessage.style.whiteSpace = WhiteSpace.Normal;
            Add(_labelMessage);
        }
        public Artifice_VisualElement_InfoBox(string message) : this()
        {
            _labelMessage.text = message;
            _image.sprite = Artifice_SCR_CommonResourcesHolder.instance.CommentIcon;
        }
        public Artifice_VisualElement_InfoBox(string message, Sprite sprite) : this()
        {
            _image.sprite = sprite;
            _labelMessage.text = message;
        }

        public void Update(Sprite sprite)
        {
            _image.sprite = sprite;
        }
        public void Update(string message)
        {
            _labelMessage.text = message;
        }
        public void Update(Sprite sprite, string message)
        {
            Update(sprite);
            Update(message);
        }
    }
}
