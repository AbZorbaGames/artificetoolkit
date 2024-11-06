using UnityEngine.UIElements;

namespace ArtificeToolkit.Editor.Artifice_CustomAttributeDrawers.CustomAttributeDrawers_Groups
{
    // ReSharper disable once ClassNeverInstantiated.Global
    /// <summary> Inherits from <see cref="Artifice_VisualElement_BoxGroup"/> but offers foldout capabilities </summary>
    public class Artifice_VisualElement_FoldoutGroup : Artifice_VisualElement_BoxGroup
    {
        #region FIELDS

        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                _isExpanded = value;
                UpdateDropdown();
            }
        }
        
        private readonly VisualElement _dropdownSymbol;
        private bool _isExpanded = true;
        
        #endregion

        public Artifice_VisualElement_FoldoutGroup() : base()
        {
            styleSheets.Add(Artifice_Utilities.GetStyle(typeof(Artifice_VisualElement_FoldoutGroup)));
            
            var titleContainer = this.Query<VisualElement>(classes: "title-container").First();

            _dropdownSymbol = new Label("\u25bc");
            _dropdownSymbol.AddToClassList("dropdown-symbol");
            _dropdownSymbol.AddToClassList("title-label");
            titleContainer.Insert(0, _dropdownSymbol);
            
            UpdateDropdown();
            
            titleContainer.RegisterCallback<ClickEvent>(evt =>
            {
                _isExpanded = !_isExpanded;
                UpdateDropdown();
            });
        }

        private void UpdateDropdown()
        {
            if (_isExpanded)
            {
                // Unhide default container if previously hidden
                DefaultContentContainer.RemoveFromClassList("hide");
                
                // Symbol
                _dropdownSymbol.RemoveFromClassList("rotate-90");
                _dropdownSymbol.AddToClassList("rotate-0");;
                
                // Content Container
                DefaultContentContainer.RemoveFromClassList("foldout-close");
            }
            else
            {
                // Hide default container
                DefaultContentContainer.AddToClassList("hide");
                
                // Symbol
                _dropdownSymbol.RemoveFromClassList("rotate-0");
                _dropdownSymbol.AddToClassList("rotate-90");;
                
                // Content Container
                DefaultContentContainer.RemoveFromClassList("foldout-open");
                DefaultContentContainer.AddToClassList("foldout-close");
            }
            
            SavePersistedData();
        }

        #region Save/Load Persistence
        
        public override void SavePersistedData()
        {
            Artifice_SCR_PersistedData.instance.SaveData(ViewPersistenceKey, "isExpanded", _isExpanded ? "true" : "false");
        }

        public override void LoadPersistedData()
        {
            // Load data
            var value = Artifice_SCR_PersistedData.instance.LoadData(ViewPersistenceKey, "isExpanded");
            if (value == null)
                return;
            
            _isExpanded = value == "true";
            UpdateDropdown();
        }
        
        #endregion
    }
}
