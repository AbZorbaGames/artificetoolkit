using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

namespace ArtificeToolkit.Editor.Artifice_CustomAttributeDrawers.CustomAttributeDrawers_Groups
{
    /// <summary> VisualElement that drives the TaGroup logic and style </summary>
    public class Artifice_VisualElement_TabGroup : Artifice_VisualElement_Group
    {
        #region FIELDS

        private readonly Dictionary<string, VisualElement> _tabMapHeaders = new();
        private readonly Dictionary<string, VisualElement> _tabMapContainers = new();
        private readonly VisualElement _headersContainer;

        private string _selectedTabKey;
        
        #endregion

        public Artifice_VisualElement_TabGroup()
        {
            styleSheets.Add(Artifice_Utilities.GetStyle(typeof(Artifice_VisualElement_TabGroup)));
            
            _headersContainer = new VisualElement();
            _headersContainer.AddToClassList("headers-container");
            hierarchy.Insert(0, _headersContainer);
            
            ResetContentContainer();
        }

        public bool ContainsTab(string tabSection)
        {
            return _tabMapHeaders.ContainsKey(tabSection);
        }
        public void CreateTab(string tabSection)
        {
            // Add header
            var tabTitleContainer = new VisualElement();
            tabTitleContainer.name = $"{tabSection}-title-container";
            tabTitleContainer.AddToClassList("tab-title-container");
            _headersContainer.Add(tabTitleContainer);
            _tabMapHeaders.Add(tabSection, tabTitleContainer);
            
            // register event to change tabs
            tabTitleContainer.RegisterCallback<MouseDownEvent>(evt =>
            {
                SelectTab(tabSection);
                SavePersistedData();
            });

            var tabTitleLabel = new Label(tabSection);
            tabTitleLabel.AddToClassList("tab-title-label");
            tabTitleContainer.Add(tabTitleLabel);
            
            // Add Container
            var tabContainer = new VisualElement();
            tabContainer.name = $"{tabSection}-container";
            tabContainer.AddToClassList("tab-container");
            _tabMapContainers.Add(tabSection, tabContainer);
            DefaultContentContainer.Add(tabContainer);
        }
        public void SelectTab(string tabSection, bool setAsContentContainer = true)
        {
            // Change style of selected title
            foreach(var pair in _tabMapHeaders)
                if(pair.Key == tabSection)
                    pair.Value.AddToClassList("tab-title-container-selected");
                else
                    pair.Value.RemoveFromClassList("tab-title-container-selected");

            // Show only the selected tab container
            foreach (var pair in _tabMapContainers)
                if (pair.Key == tabSection)
                {
                    pair.Value.RemoveFromClassList("hide");
                    if(setAsContentContainer)
                        SetContentContainer(pair.Value);
                }
                else
                    pair.Value.AddToClassList("hide");

            _selectedTabKey = tabSection;
        }

        #region Save/Load Persisted Data
        
        public override void SavePersistedData()
        {
            Artifice_SCR_PersistedData.instance.SaveData(ViewPersistenceKey, "selectedTabKey", _selectedTabKey);
        }

        public override void LoadPersistedData()
        {
            _selectedTabKey = Artifice_SCR_PersistedData.instance.LoadData(ViewPersistenceKey, "selectedTabKey");

            if (_selectedTabKey == null)
            {
                SelectTab(_tabMapHeaders.Keys.First(), false);
                return;
            }
            
            // This may not exist of tab names changed through compilation.
            if(_tabMapHeaders.ContainsKey(_selectedTabKey))
                SelectTab(_selectedTabKey, false);
            else
                SelectTab(_tabMapHeaders.Keys.First(), false);
        }
        
        #endregion
    }
}
