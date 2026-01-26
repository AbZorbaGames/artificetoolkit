using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ArtificeToolkit.Editor;
using ArtificeToolkit.Editor.Resources;
using ArtificeToolkit.Editor.VisualElements;
using CustomAttributes;
using JetBrains.Annotations;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Editor.Artifice_PropertyDrawer_SerializedHashSet
{
    public class ArtificeEditor_VisualElement_HashSetListView : Artifice_VisualElement_AbstractListView
    {
        #region FIELDS

        private readonly List<Artifice_VisualElement_InfoBox> _infoBoxes = new();
        
        #endregion
        
        protected override void BeforeBuildUIStart()
        {
            base.BeforeBuildUIStart();
            _infoBoxes.Clear();
        }
        
        protected override VisualElement BuildPropertyFieldUI(SerializedProperty property, int index)
        {
            // Should force artifice?
            var propertyNeedsArtifice = Property.GetCustomAttributes().Any(attribute => attribute is ListElementNameAttribute);

            var container = new VisualElement();
            container.AddToClassList("hash-set-entry");
            
            // Create info box
            var infoBox = new Artifice_VisualElement_InfoBox("", Artifice_SCR_CommonResourcesHolder.instance.ErrorIcon);
            infoBox.AddToClassList("hide");
            container.Add(infoBox);
            _infoBoxes.Add(infoBox);
            
            // Create property's GUI with ArtificeDrawer
            var propertyField = ArtificeDrawer.CreatePropertyGUI(property, ShouldForceArtifice || propertyNeedsArtifice);
            propertyField = ArtificeDrawer.CreateCustomAttributesGUI(property, propertyField, ChildrenInjectedCustomAttributes);
            propertyField.AddToClassList("property-field");
            container.Add(propertyField);

            return container;
        }
        
        protected override void OnBuildUICompleted()
        {
            base.OnBuildUICompleted();
            
            contentContainer.styleSheets.Add(Artifice_Utilities.GetStyle(typeof(ArtificeEditor_VisualElement_HashSetListView)));
            contentContainer.AddToClassList("");
            
            PerformHashSetCompareCheck();
            
            contentContainer.TrackPropertyValue(Property, _ => PerformHashSetCompareCheck());
        }

        private void PerformHashSetCompareCheck()
        {
            if (Property.arraySize == 0)
                return;
            
            var elementType = Property.GetArrayElementAtIndex(0)
                .GetTarget<object>()
                ?.GetType();

            if (elementType == null)
                return;

            // EqualityComparer<T>.Default at runtime
            var comparerType = typeof(EqualityComparer<>).MakeGenericType(elementType);
            var comparer = (IEqualityComparer)
                comparerType.GetProperty("Default").GetValue(null);

            var accepted = new List<object>();

            for (var i = 0; i < Property.arraySize; i++)
            {
                var property = Property.GetArrayElementAtIndex(i);
                var target = property.GetTarget<object>();

                var conflictIndex = -1;

                // Check for each already inserted element, if it conflicts.
                for (var j = 0; j < accepted.Count; j++)
                {
                    if (comparer.Equals(target, accepted[j]))
                    {
                        conflictIndex = j;
                        break;
                    }
                }

                if (conflictIndex >= 0)
                {
                    Set_InfoBox(_infoBoxes[i], $"Property will not be added to the Set. Conflicts with 'Element {conflictIndex}'");
                }
                else
                {
                    accepted.Add(target);
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