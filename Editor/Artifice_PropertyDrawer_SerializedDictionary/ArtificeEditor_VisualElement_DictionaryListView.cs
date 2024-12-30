using UnityEditor;
using UnityEngine.UIElements;

namespace ArtificeToolkit.Editor
{
    public class ArtificeEditor_VisualElement_DictionaryListView : Artifice_VisualElement_AbstractListView
    {
        #region Serialized Entry Element

        private class SerializedPairElement : VisualElement
        {
            private readonly int _index;
            private readonly VisualElement _keyContainer;
            private readonly VisualElement _valueContainer;
            private readonly ArtificeDrawer _artificeDrawer;
            
            public SerializedPairElement(int index, ArtificeDrawer artificeDrawer)
            {
                _index = index;
                
                _artificeDrawer = artificeDrawer;
                AddToClassList("serializedPair-container");
                
                _keyContainer = new VisualElement();
                _keyContainer.AddToClassList("key-container");
                Add(_keyContainer);
                
                _valueContainer = new VisualElement();
                _valueContainer.AddToClassList("value-container");
                Add(_valueContainer);
            }

            public void SetKey(SerializedProperty property)
            {
                _keyContainer.Clear();

                var keyElement = _artificeDrawer.CreatePropertyGUI(property, useFoldoutForVisibleChildren: false);
                if(keyElement is Foldout foldout)
                    foldout.text = $"Key {_index}";
                
                _keyContainer.Add(keyElement);
            }

            public void SetValue(SerializedProperty property)
            {
                _valueContainer.Clear();
                _valueContainer.Add(_artificeDrawer.CreatePropertyGUI(property, useFoldoutForVisibleChildren: false));
            }
        }
        
        #endregion

        protected override void OnBuildUICompleted()
        {
            SetTitle(Property.FindParentProperty().displayName);
        }

        protected override VisualElement BuildPropertyFieldUI(SerializedProperty property, int index)
        {
            var serializedKey = property.FindPropertyRelative("Key");
            var serializedValue = property.FindPropertyRelative("Value");

            var pairElement = new SerializedPairElement(index, ArtificeDrawer);
            pairElement.SetKey(serializedKey);
            pairElement.SetValue(serializedValue);
            
            return pairElement;
        }
    }
}