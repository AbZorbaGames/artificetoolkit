using ArtificeToolkit.Runtime.SerializedDictionary;
using UnityEditor;
using UnityEngine.UIElements;

// ReSharper disable CheckNamespace

namespace ArtificeToolkit.Editor
{
    [CustomPropertyDrawer(typeof(SerializedDictionaryWrapper), true)]
    public class ArtificeEditor_PropertyDrawer_SerializedDictionary : PropertyDrawer
    {
        #region FIELDS
        
        private SerializedProperty _property;
        private SerializedProperty _pairListProperty;
        
        private VisualElement _mainContainer;
        
        #endregion
        
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            _property = property;
            Initialize();
            BuildUI();
            
            return _mainContainer;
        }

        private void Initialize()
        {
            // Fill up list of serialized properties
            _pairListProperty = _property.FindPropertyRelative("list");
        }

        private void BuildUI()
        {
            // Build UI
            _mainContainer = new VisualElement();
            _mainContainer.styleSheets.Add(Artifice_Utilities.GetGlobalStyle());
            _mainContainer.styleSheets.Add(Artifice_Utilities.GetStyle(GetType()));
            
            var listView2 = new ArtificeEditor_VisualElement_DictionaryListView();
            listView2.value = _pairListProperty;
            _mainContainer.Add(listView2);
        }
    }
}