using System.Collections;
using System.Collections.Generic;
using ArtificeToolkit.Editor.Resources;
using ArtificeToolkit.Editor.VisualElements;
using JetBrains.Annotations;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace ArtificeToolkit.Editor.Artifice_PropertyDrawer_SerializedHashSet
{
    public class ArtificeEditor_VisualElement_HashSetListView : Artifice_VisualElement_AbstractListView
    {
        private sealed class ComparerAdapter : IEqualityComparer<object>
        {
            private readonly IEqualityComparer _comparer;

            public ComparerAdapter(IEqualityComparer comparer)
            {
                _comparer = comparer;
            }

            public new bool Equals(object x, object y)
            {
                return _comparer.Equals(x, y);
            }

            public int GetHashCode(object obj)
            {
                return obj == null ? 0 : _comparer.GetHashCode(obj);
            }
        }

        #region FIELDS

        private readonly List<Artifice_VisualElement_InfoBox> _infoBoxes = new();
        
        #endregion

        public ArtificeEditor_VisualElement_HashSetListView()
        {
            styleSheets.Add(Artifice_Utilities.GetStyle(typeof(ArtificeEditor_VisualElement_HashSetListView)));
        }
        
        protected override void BeforeBuildUIStart()
        {
            base.BeforeBuildUIStart();
            _infoBoxes.Clear();
        }
        
        protected override VisualElement BuildPropertyFieldUI(SerializedProperty property, int index)
        {
            var container = new VisualElement();
            container.AddToClassList("hash-set-entry");
            
            // Create info box
            var infoBox = new Artifice_VisualElement_InfoBox("", Artifice_SCR_CommonResourcesHolder.instance.ErrorIcon);
            infoBox.AddToClassList("hide");
            container.Add(infoBox);
            _infoBoxes.Add(infoBox);
            
            // Create property's GUI with ArtificeDrawer
            var propertyField = ArtificeDrawer.CreatePropertyGUI(property, ShouldForceArtifice || HasListElementNameAttribute);
            if (propertyField == null)
                return container;

            propertyField = ArtificeDrawer.CreateCustomAttributesGUI(property, propertyField, ChildrenInjectedCustomAttributes);
            propertyField.AddToClassList("property-field");
            container.Add(propertyField);

            return container;
        }
        
        protected override void OnBuildUICompleted()
        {
            base.OnBuildUICompleted();
            
            PerformHashSetCompareCheck();
            
            // The children container is replaced on every rebuild, so its tracker is detached
            // with the old UI instead of accumulating on this long-lived root element.
            ChildrenContainer.TrackPropertyValue(Property, _ => PerformHashSetCompareCheck());
        }

        private void PerformHashSetCompareCheck()
        {
            if (Property.arraySize == 0 || _infoBoxes.Count != Property.arraySize)
                return;
            
            var elementType = Property.GetArrayChildrenType();

            if (elementType == null)
                return;

            // EqualityComparer<T>.Default at runtime
            var comparerType = typeof(EqualityComparer<>).MakeGenericType(elementType);
            var comparer = (IEqualityComparer)
                comparerType.GetProperty("Default").GetValue(null);

            var accepted = new Dictionary<object, int>(new ComparerAdapter(comparer));
            var firstNullIndex = -1;

            for (var i = 0; i < Property.arraySize; i++)
            {
                var property = Property.GetArrayElementAtIndex(i);
                var target = property.GetTarget<object>();
                int conflictIndex;

                if (target == null)
                {
                    conflictIndex = firstNullIndex;
                    if (firstNullIndex < 0)
                        firstNullIndex = i;
                }
                else if (!accepted.TryGetValue(target, out conflictIndex))
                {
                    accepted.Add(target, i);
                    conflictIndex = -1;
                }

                if (conflictIndex >= 0)
                {
                    Set_InfoBox(_infoBoxes[i], $"Property will not be added to the Set. Conflicts with 'Element {conflictIndex}'");
                }
                else
                {
                    Set_InfoBox(_infoBoxes[i], null);
                }
            }
        }

        public void Set_InfoBox(Artifice_VisualElement_InfoBox infoBox, [CanBeNull] string message)
        {
            if (message == null)
            {
                infoBox.AddToClassList("hide");
            }
            else
            {
                infoBox.RemoveFromClassList("hide");
                infoBox.Update(message);
            }
        }
    }
}
