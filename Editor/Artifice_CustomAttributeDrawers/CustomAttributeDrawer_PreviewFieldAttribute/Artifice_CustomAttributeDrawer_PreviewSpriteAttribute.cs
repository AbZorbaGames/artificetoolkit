using ArtificeToolkit.Attributes;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace ArtificeToolkit.Editor.Artifice_CustomAttributeDrawers.CustomAttributeDrawer_PreviewFieldAttribute
{
    /// <summary> Custom drawer for <see cref="PreviewSpriteAttribute"/> attribute. </summary>
    [Artifice_CustomAttributeDrawer(typeof(PreviewSpriteAttribute))]
    public class Artifice_CustomAttributeDrawer_PreviewSpriteAttribute : Artifice_CustomAttributeDrawer
    {
        #region FIELDS

        private SerializedProperty _property;
        private Image _image;
        
        #endregion
        
        public override VisualElement OnPostPropertyGUI(SerializedProperty property)
        {
            var attribute = (PreviewSpriteAttribute)Attribute;
            _property = property;
            _property.serializedObject.Update();
            
            // Add image ad track property to update
            _image = (Image)BuildImageUI(property);
            _image.styleSheets.Add(Artifice_Utilities.GetStyle(GetType()));
            _image.TrackPropertyValue(property, _ => UpdateImageView());
            
            // Set parameterized dimensions of image
            _image.style.width = attribute.Width;
            _image.style.height = attribute.Height;
            
            // Update if valid
            if(IsValidType(_property.objectReferenceValue))
                UpdateImageView();

            return _image;
        }

        #region Build UI
        
        private VisualElement BuildImageUI(SerializedProperty property)
        {
            // Create image
            var image = new Image();
            image.AddToClassList("image");
            
            // Track property to update image.
            image.TrackPropertyValue(property, _ => UpdateImageView());
            
            // Set image dimensions based on attribute
            var attribute = (PreviewSpriteAttribute)Attribute;
            image.style.width = attribute.Width;
            image.style.height = attribute.Height;

            return image;
        }
        
        #endregion
        
        #region Utility
        
        private bool IsValidType(object obj)
        {
            return obj is Sprite || obj is Texture2D;
        }
        
        private void UpdateImageView()
        {
            _property.serializedObject.Update();
            if(_property.objectReferenceValue == null)
                _image.sprite = null;
            else if (_property.objectReferenceValue is Sprite sprite)
                _image.sprite = sprite;
            else if (_property.objectReferenceValue is Texture2D texture)
                _image.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);
            else
                Artifice_Utilities.LogError($"Assignment failed. Make sure you are using type of {_property.type}");
        }
        
        #endregion
    }
}