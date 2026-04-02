using System;
using ArtificeToolkit.Attributes;
using UnityEngine;
using UnityEngine.UIElements;

namespace ArtificeToolkit.Editor.Artifice_CustomAttributeDrawers.CustomAttributeDrawers_Groups
{
    /// <summary> Base class for all Group visual elements. Handles content container logic for the subclasses </summary>
    public abstract class Artifice_VisualElement_Group : VisualElement, IArtifice_Persistence
    {
        #region FIELDS
        
        // Public override for nested groups
        public override VisualElement contentContainer => _customContentContainer;
        
        protected readonly VisualElement DefaultContentContainer;
        
        private VisualElement _customContentContainer;
        
        #endregion

        protected Artifice_VisualElement_Group()
        {
            styleSheets.Add(Artifice_Utilities.GetGlobalStyle());
            styleSheets.Add(Artifice_Utilities.GetStyle(typeof(Artifice_VisualElement_Group)));

            AddToClassList("group-container");

            // Create content container
            DefaultContentContainer = new VisualElement();
            DefaultContentContainer.AddToClassList("default-content-container");
            DefaultContentContainer.AddToClassList("content-container");
            hierarchy.Add(DefaultContentContainer);
            
            // Retarget overriden content container, to use Add on custom container
            ResetContentContainer();
        }

        public void SetContentContainer(VisualElement elem)
        {
            if(elem == this)
                ResetContentContainer();
            else
                _customContentContainer = elem;
        }

        public void ResetContentContainer()
        {
            SetContentContainer(DefaultContentContainer);
        }
        
        public virtual void SetTitle(string title)
        {
        }

        public virtual void SetGroupColor(GroupColor groupColor)
        {
            DefaultContentContainer.style.backgroundColor = DispatchGroupColor_Content(groupColor);
        }
        
        protected Color DispatchGroupColor_Content(GroupColor groupColor)
        {
            switch (groupColor)
            {
                case GroupColor.Default:
                    return new Color(0.25f, 0.25f, 0.25f, 1f);
                case GroupColor.Black:
                    return new Color(0.141f, 0.141f, 0.141f, 1);
                case GroupColor.Blue:
                    return new Color(0.314f, 0.4f, 0.49f, 1);
                case GroupColor.Red:
                    return new Color(0.49f, 0.314f, 0.314f, 1);
                case GroupColor.Green:
                    return new Color(0.333f, 0.49f, 0.314f, 1);
                case GroupColor.Orange:
                    return new Color(0.588f, 0.486f, 0.361f, 1);
                case GroupColor.Yellow:
                    return new Color(0.482f, 0.49f, 0.314f, 1);
                case GroupColor.Pink:
                    return new Color(0.588f, 0.361f, 0.482f, 1);
                case GroupColor.Purple:
                    return new Color(0.486f, 0.361f, 0.588f, 1);
                case GroupColor.Transparent:
                    return new Color(0f, 0f, 0f, 0f);
                default:
                    throw new ArgumentException();
            }
        }
        
        #region Artifice Persistence Interface

        public string ViewPersistenceKey { get; set; }
        
        public virtual void SavePersistedData()
        {
        }

        public virtual void LoadPersistedData()
        {
        }
        
        #endregion
    }
}