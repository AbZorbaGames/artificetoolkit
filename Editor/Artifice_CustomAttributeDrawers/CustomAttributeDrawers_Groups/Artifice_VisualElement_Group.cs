using System;
using ArtificeToolkit.Attributes;
using UnityEngine.UIElements;

namespace ArtificeToolkit.Editor.Artifice_CustomAttributeDrawers.CustomAttributeDrawers_Groups
{
    /// <summary> Base class for all Group visual elements. Handles content container logic for the subclasses </summary>
    public abstract class Artifice_VisualElement_Group : VisualElement, IArtifice_Persistence
    {
        #region FIELDS

        private const string ExplicitColorClass = "group-color-explicit";
        
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
            foreach (GroupColor color in Enum.GetValues(typeof(GroupColor)))
                RemoveFromClassList(GetGroupColorClass(color));

            RemoveFromClassList(ExplicitColorClass);
            AddToClassList(GetGroupColorClass(groupColor));

            if (groupColor is not GroupColor.Default and not GroupColor.Transparent)
                AddToClassList(ExplicitColorClass);
        }

        private static string GetGroupColorClass(GroupColor groupColor)
        {
            switch (groupColor)
            {
                case GroupColor.Default:
                    return "group-color-default";
                case GroupColor.Black:
                    return "group-color-black";
                case GroupColor.Blue:
                    return "group-color-blue";
                case GroupColor.Red:
                    return "group-color-red";
                case GroupColor.Green:
                    return "group-color-green";
                case GroupColor.Orange:
                    return "group-color-orange";
                case GroupColor.Yellow:
                    return "group-color-yellow";
                case GroupColor.Pink:
                    return "group-color-pink";
                case GroupColor.Purple:
                    return "group-color-purple";
                case GroupColor.Transparent:
                    return "group-color-transparent";
                default:
                    throw new ArgumentOutOfRangeException(nameof(groupColor), groupColor, null);
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
