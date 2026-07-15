using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
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
            
            public VisualElement KeyContainer => _keyContainer;
            public HelpBox WarningHelpBox { get; }
            
            public SerializedPairElement(int index, ArtificeDrawer artificeDrawer)
            {
                _index = index;
                
                _artificeDrawer = artificeDrawer;
                AddToClassList("serializedPair-container");
                
                WarningHelpBox = new HelpBox("Duplicate key will not be added to the dictionary", HelpBoxMessageType.Warning);
                WarningHelpBox.AddToClassList("duplicate-warning-helpbox");
                WarningHelpBox.style.display = DisplayStyle.None;
                Add(WarningHelpBox);

                var mainContainer = new VisualElement();
                mainContainer.AddToClassList("serializedPair-content");
                Add(mainContainer);

                _keyContainer = new VisualElement();
                _keyContainer.AddToClassList("key-container");
                mainContainer.Add(_keyContainer);
                
                _valueContainer = new VisualElement();
                _valueContainer.AddToClassList("value-container");
                mainContainer.Add(_valueContainer);
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

        #region Duplicate Tracking

        /// <summary> Tracks a key property and its pair element for duplicate detection. </summary>
        private class DuplicateTrackingEntry
        {
            public readonly SerializedProperty KeyProperty;
            public readonly SerializedPairElement PairElement;

            public DuplicateTrackingEntry(SerializedProperty keyProperty, SerializedPairElement pairElement)
            {
                KeyProperty = keyProperty;
                PairElement = pairElement;
            }
        }

        private readonly List<DuplicateTrackingEntry> _duplicateTrackingEntries = new();

        #endregion

        protected override void OnBuildUICompleted()
        {
            base.OnBuildUICompleted();
            SetTitle(Property.FindParentProperty().displayName);
            
            // Initial duplicate scan after UI is fully built
            RefreshDuplicateWarnings();
        }

        protected override VisualElement BuildPropertyFieldUI(SerializedProperty property, int index)
        {
            var serializedKey = property.FindPropertyRelative("Key");
            var serializedValue = property.FindPropertyRelative("Value");

            var pairElement = new SerializedPairElement(index, ArtificeDrawer);
            pairElement.SetKey(serializedKey);
            pairElement.SetValue(serializedValue);
            
            // Register duplicate tracking immediately so it's available for the initial scan in OnBuildUICompleted
            RegisterDuplicateTracking(serializedKey, pairElement);
            
            return pairElement;
        }

        protected override void BeforeBuildUIStart()
        {
            base.BeforeBuildUIStart();
            
            // Clear tracking entries before rebuilding UI
            _duplicateTrackingEntries.Clear();
        }

        /// <summary>
        /// Registers a key property for duplicate tracking and sets up real-time
        /// change detection via TrackPropertyValue.
        /// </summary>
        private void RegisterDuplicateTracking(SerializedProperty keyProperty, SerializedPairElement pairElement)
        {
            var entry = new DuplicateTrackingEntry(keyProperty, pairElement);
            _duplicateTrackingEntries.Add(entry);

            // Real-time duplicate detection: refresh all warnings when any key value changes
            pairElement.TrackPropertyValue(keyProperty, _ => RefreshDuplicateWarnings());
        }

        /// <summary>
        /// Scans all tracked key properties to identify duplicates and applies/removes
        /// the appropriate USS classes and HelpBox warnings.
        /// </summary>
        private void RefreshDuplicateWarnings()
        {
            if (_duplicateTrackingEntries.Count == 0)
                return;

            // Build a map of key-value-string -> list of indices that share that key
            var keyOccurrences = new Dictionary<string, List<int>>();
            for (var i = 0; i < _duplicateTrackingEntries.Count; i++)
            {
                var entry = _duplicateTrackingEntries[i];
                if (entry.KeyProperty == null || !entry.KeyProperty.Verify())
                    continue;

                var keyString = entry.KeyProperty.GetValueString();
                if (!keyOccurrences.ContainsKey(keyString))
                    keyOccurrences[keyString] = new List<int>();
                
                keyOccurrences[keyString].Add(i);
            }

            // Clear all existing duplicate classes and HelpBoxes
            for (var i = 0; i < _duplicateTrackingEntries.Count; i++)
            {
                var entry = _duplicateTrackingEntries[i];
                var keyContainer = entry.PairElement.KeyContainer;
                
                // Remove existing duplicate classes
                keyContainer.RemoveFromClassList("key-duplicate-first");
                keyContainer.RemoveFromClassList("key-duplicate-skipped");
                
                // Hide HelpBox
                entry.PairElement.WarningHelpBox.style.display = DisplayStyle.None;
            }

            // Now apply the appropriate styles for duplicates
            foreach (var kvp in keyOccurrences)
            {
                if (kvp.Value.Count <= 1)
                    continue; // No duplicates for this key

                // First occurrence gets orange highlight
                var firstIndex = kvp.Value[0];
                var firstEntry = _duplicateTrackingEntries[firstIndex];
                firstEntry.PairElement.KeyContainer.AddToClassList("key-duplicate-first");

                // Subsequent occurrences get red highlight + HelpBox
                for (var j = 1; j < kvp.Value.Count; j++)
                {
                    var dupIndex = kvp.Value[j];
                    var dupEntry = _duplicateTrackingEntries[dupIndex];
                    
                    dupEntry.PairElement.KeyContainer.AddToClassList("key-duplicate-skipped");
                    
                    // Show HelpBox warning
                    dupEntry.PairElement.WarningHelpBox.style.display = DisplayStyle.Flex;
                }
            }
        }
    }
}