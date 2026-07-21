using System;
using System.Collections.Generic;
using ArtificeToolkit.Attributes;
using CustomAttributes;
using UnityEngine;

namespace ArtificeToolkit.Examples
{
    using Range = ArtificeToolkit.Attributes.RangeAttribute;
    /// <summary>
    /// A small, self-contained catalogue of every concrete Artifice attribute.
    /// Open Artifice Wizard > Examples > All Attributes to interact with it.
    /// </summary>
    public class Artifice_ExampleForArtificeAttributes : MonoBehaviour
    {
        [InfoBox("Example Script for Artifice Toolkit. Feel free to explore the script.", InfoBoxAttribute.InfoMessageType.Info)]
        [InlineObject]
        public List<Artifice_SCR_Character> characters;
        
        public enum Difficulty
        {
            Easy,
            Normal,
            Hard
        }

        [Serializable]
        public class InlineDetails
        {
            public string note = "Nested fields are drawn without a foldout.";
            public int value = 3;
        }

        [Serializable]
        public class NamedItem
        {
            public string name;
            public int amount;

            public NamedItem(string name, int amount)
            {
                this.name = name;
                this.amount = amount;
            }
        }

        [Serializable]
        public class TableRow
        {
            public string item;
            public int amount;
            public bool equipped;

            public TableRow(string item, int amount, bool equipped)
            {
                this.item = item;
                this.amount = amount;
                this.equipped = equipped;
            }
        }

        [Serializable]
        public class SortedValues
        {
            [Sort(1)] public string second = "Declared first, sorted second";
            [Sort(0)] public string first = "Declared second, sorted first";
        }

        [InfoBox("A compact, editable example of every Artifice attribute. Toggle this value to try ConditionalInfoBox and EnableIf.")]
        [Sort(-100)]
        public bool showOptional = true;

        [ConditionalInfoBox("This message only appears while Show Optional is enabled.", nameof(showOptional))]
        [EnableIf(nameof(showOptional))]
        [Sort(-90)]
        public string optionalMessage = "Conditional field";

        [BoxGroup("Essentials")]
        [Title("Title + SafeTooltip")]
        [SafeTooltip("SafeTooltip works reliably inside Artifice lists and nested layouts.")]
        public string displayName = "Artifice Example";

        [BoxGroup("Essentials")]
        [Title("HideLabel")]
        [HideLabel]
        public string labelFreeValue = "This field has no label";

        [BoxGroup("Essentials")]
        [ReadOnly]
        public string readOnlyValue = "This value cannot be edited";

        [BoxGroup("Essentials")]
        [ReadOnly]
        [ForceArtifice]
        public Vector3 forcedNestedValue = Vector3.up;

        [BoxGroup("Essentials")]
        [Attributes.Space(6, 6, 4, 4)]
        [Border]
        public string themeBorder = "Native theme border";

        [BoxGroup("Essentials")]
        [Border("#2E86DE")]
        public string explicitBorder = "Explicit blue border";

        [FoldoutGroup("Numbers", GroupColor.Green)]
        [Range(0, 10)]
        public int rangeValue = 5;

        [FoldoutGroup("Numbers", GroupColor.Green)]
        [MinValue(0)]
        public int minimumValue = 1;

        [FoldoutGroup("Numbers", GroupColor.Green)]
        [MaxValue(10)]
        public int maximumValue = 8;

        [FoldoutGroup("Numbers", GroupColor.Green)]
        [MeasureUnit("m/s")]
        public float speed = 4.5f;

        [FoldoutGroup("Numbers", GroupColor.Green)]
        [LabelWidth(180)]
        public float customLabelWidth = 12f;

        [FoldoutGroup("Numbers", GroupColor.Green)]
        [EnumToggle]
        public Difficulty difficulty = Difficulty.Normal;

        [FoldoutGroup("Numbers", GroupColor.Green)]
        [OnValueChanged(nameof(ReportValueChanged))]
        public int trackedValue;

        [FoldoutGroup("Object References", GroupColor.Blue)]
        [AssetsOnly]
        public Artifice_SCR_Character assetOnly;

        [FoldoutGroup("Object References", GroupColor.Blue)]
        [SceneObjectsOnly]
        public GameObject sceneObjectOnly;

        [FoldoutGroup("Object References", GroupColor.Blue)]
        [ChildGameObjectsOnly]
        [Required("Choose a child of the example GameObject.")]
        public Transform requiredChild;

        [FoldoutGroup("Object References", GroupColor.Blue)]
        [PreviewSprite(90)]
        public Texture2D imagePreview;

        [FoldoutGroup("Object References", GroupColor.Blue)]
        [InlineObject]
        public Artifice_SCR_Character inlineObject;

#pragma warning disable 0618
        [FoldoutGroup("Object References", GroupColor.Blue)]
        [PreviewScriptable]
        public Artifice_SCR_Character legacyPreviewScriptable;
#pragma warning restore 0618

        [TabGroup("Data Examples", "Validation", GroupColor.Purple)]
        [ValidateInput(nameof(IsShortCodeValid), "Use at least three characters.")]
        public string shortCode = "ABC";

        [TabGroup("Data Examples", "Validation", GroupColor.Purple)]
        [ValidateJson]
        public string json = "{\"name\":\"Artifice\"}";

        [TabGroup("Data Examples", "Validation", GroupColor.Purple)]
        [ValidateUxml]
        public string uxml = "<ui:UXML xmlns:ui=\"UnityEngine.UIElements\"><ui:Label text=\"Hello\" /></ui:UXML>";

        [TabGroup("Data Examples", "Nested", GroupColor.Purple)]
        [InlineProperty]
        public InlineDetails inlineDetails = new();

        [TabGroup("Data Examples", "Nested", GroupColor.Purple)]
        [InlineProperty(InlinePropertyStyle.WithoutTitle)]
        public SortedValues sortedValues = new();

        [TabGroup("Data Examples", "Lists", GroupColor.Purple)]
        [ListElementName(nameof(NamedItem.name))]
        public List<NamedItem> namedItems = new()
        {
            new NamedItem("Potion", 2),
            new NamedItem("Key", 1)
        };

        [TabGroup("Data Examples", "Lists", GroupColor.Purple)]
        [TableList]
        public List<TableRow> table = new()
        {
            new TableRow("Sword", 1, true),
            new TableRow("Potion", 3, false)
        };

        [HorizontalGroup("Responsive Layout")]
        [VerticalGroup("Responsive Layout/Left")]
        [LayoutPercent(50)]
        public string percentWidth = "50% width";

        [HorizontalGroup("Responsive Layout")]
        [VerticalGroup("Responsive Layout/Left")]
        [LayoutPixels(0, 42)]
        public string pixelHeight = "42 px height";

        [HorizontalGroup("Responsive Layout")]
        [VerticalGroup("Responsive Layout/Right")]
        [LayoutPercent(50)]
        public string secondColumn = "Second column";

        [BoxGroupBegin("Begin / End Groups", GroupColor.Orange)]
        public string boxBegin = "BoxGroupBegin";

        [HorizontalGroupBegin("Begin / End Groups/Row")]
        [VerticalGroupBegin("Begin / End Groups/Row/Left")]
        public string leftTop = "Left top";

        public string leftBottom = "Left bottom";

        [GroupEnd]
        [VerticalGroupBegin("Begin / End Groups/Row/Right")]
        public string rightTop = "Right top";

        public string rightBottom = "Right bottom";

        [GroupEnd]
        [GroupEnd]
        [FoldoutGroupBegin("Begin / End Groups/More", GroupColor.Orange)]
        public string foldoutBegin = "FoldoutGroupBegin";

        [GroupEnd]
        [GroupEnd]

        [HideInArtifice]
        public string hiddenValue = "This field is intentionally hidden";

        [InfoBox("HideInArtifice removes the field above. See Artifice_ExampleIgnoredComponent.cs for the class-level ArtificeIgnore example.", InfoBoxAttribute.InfoMessageType.None)]
        [ReadOnly]
        public string hiddenAttributeNote = "Inspect the source to see both opt-out attributes.";

        [ButtonProperty(nameof(SayHello))]
        [Border]
        public ArtificeElement buttonProperty;

        private bool IsShortCodeValid()
        {
            return !string.IsNullOrWhiteSpace(shortCode) && shortCode.Length >= 3;
        }

        private void ReportValueChanged()
        {
            Debug.Log($"Artifice tracked value changed to {trackedValue}.", this);
        }

        private void SayHello()
        {
            Debug.Log($"Hello from {displayName}!", this);
        }

        [Button]
        private void SimpleButton()
        {
            Debug.Log("Artifice Button invoked.", this);
        }

        [Button(true, nameof(displayName))]
        private void ButtonWithParameter(string value)
        {
            Debug.Log($"Button parameter: {value}", this);
        }
    }
}
