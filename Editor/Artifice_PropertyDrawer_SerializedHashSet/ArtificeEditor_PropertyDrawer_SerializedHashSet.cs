using ArtificeToolkit.Runtime.SerializedHashSet;
using UnityEditor;
using UnityEngine.UIElements;

namespace ArtificeToolkit.Editor.Artifice_PropertyDrawer_SerializedHashSet
{
    [CustomPropertyDrawer(typeof(SerializedHashSetWrapper), true)]
    public class ArtificeEditor_PropertyDrawer_SerializedHashSet : PropertyDrawer
    {
        #region FIELDS

        private SerializedProperty _property;
        private SerializedProperty _listProperty;

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
            _listProperty = _property.FindPropertyRelative("list");
        }

        private void BuildUI()
        {
            // Build UI
            _mainContainer = new VisualElement();
            var listView = new ArtificeEditor_VisualElement_HashSetListView();
            listView.value = _listProperty;
            _mainContainer.Add(listView);
        }
    }
}
