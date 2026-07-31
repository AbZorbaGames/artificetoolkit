using System;
using UnityEditor;
using UnityEngine.UIElements;

namespace ArtificeToolkit.Editor.Artifice_CustomAttributeDrawers
{
    /// <summary> Base class for all custom AttributeDrawers.</summary>
    public abstract class Artifice_CustomAttributeDrawer : IDisposable
    {
        #region FIELDS

        public Attribute Attribute;
        public virtual bool IsReplacingPropertyField { get; } = false;

        private bool _disposed = false;

        #endregion

        public virtual VisualElement OnPrePropertyGUI(SerializedProperty property)
        {
            return null;
        }
        public virtual VisualElement OnPropertyGUI(SerializedProperty property)
        {
            return null;
        }
        public virtual VisualElement OnPostPropertyGUI(SerializedProperty property)
        {
            return null;
            
        }
        public virtual VisualElement OnWrapGUI(SerializedProperty property, VisualElement root)
        {
            return root;
        }
        public virtual void OnPropertyBoundGUI(SerializedProperty property, VisualElement propertyField)
        {
            
        }

        #region Dispose Pattern

        ~Artifice_CustomAttributeDrawer()
        {
            Dispose(false);
        }
        public void Dispose()
        {
            // Dispose of unmanaged resources.
            Dispose(true);
            // Suppress finalization.
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            // Free managed state
            if (disposing)
            {
                       
            }
            
            // Free unmanaged resources

            _disposed = true;
        }
        
        #endregion
    }
}
