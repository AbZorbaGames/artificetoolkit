using System.Linq;
using CustomAttributes;
using UnityEditor;
using UnityEngine.UIElements;

namespace ArtificeToolkit.Editor
{
    /// <summary> This class is used to render arrays and lists in a way the supports CustomAttributes and offers more functionality than Unity's default lists. </summary>
    /// <remarks><see cref="ArtificeDrawer"/></remarks>
    public class Artifice_VisualElement_ListView : Artifice_VisualElement_AbstractListView
    {
        #region FIELDS
        
        private bool _disposed;

        #endregion

        protected override VisualElement BuildPropertyFieldUI(SerializedProperty property, int index)
        {
            // Should force artifice?
            var propertyNeedsArtifice = Property.GetCustomAttributes().Any(attribute => attribute is ListElementNameAttribute);
            
            // Create property's GUI with ArtificeDrawer
            var propertyField = ArtificeDrawer.CreatePropertyGUI(property, ShouldForceArtifice || propertyNeedsArtifice);
            propertyField = ArtificeDrawer.CreateCustomAttributesGUI(property, propertyField, ChildrenInjectedCustomAttributes);
            propertyField.AddToClassList("property-field");

            return propertyField;
        }

        // Protected implementation of Dispose pattern.
        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    ArtificeDrawer.Dispose();
                }

                _disposed = true;
            }

            // Call base class implementation.
            base.Dispose(disposing);
        }
    }
}
