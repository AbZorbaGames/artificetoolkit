using System;
using System.Collections.Generic;
using ArtificeToolkit.Attributes;
using ArtificeToolkit.CustomAttributes;
using CustomAttributes;
using UnityEngine;
using Range = ArtificeToolkit.Attributes.RangeAttribute;

/// <summary> Example script for <see cref="CustomAttribute"/> </summary>
public class Artifice_ExampleForArtificeAttributes : MonoBehaviour
{
    [Serializable]
    public class Character
    {
        [Serializable]
        public class AbilityScores
        {
            [Range(0, 20)] public int strength = 10;
            [Range(0, 20)] public int dexterity = 10;
            [Range(0, 20)] public int constitution = 10;
            [Range(0, 20)] public int wisdom = 10;
            [Range(0, 20)] public int intelligence = 10;
            [Range(0, 20)] public int charisma = 10;

            [Button]
            public void ResetStats()
            {
                strength = 10;
                dexterity = 10;
                constitution = 10;
                wisdom = 10;
                intelligence = 10;
                charisma = 10;
            }

            [Button]
            public void RandomizeStats()
            {
            }
        }

        [Serializable]
        public class Item
        {
            public enum WeaponType
            {
                Longsword,
                Rapier,
                Bow,
                Axe
            }

            [MinValue(0)] public int amount;

            [EnumToggle, HideLabel] public WeaponType weaponType;
        }

        [Serializable]
        public class Skills
        {
            [MinValue(0)] public int athletics;
            [MinValue(0)] public int acrobatics;
            [MinValue(0)] public int arcana;
            [MinValue(0)] public int deception;
        }
        
        [Required, PreviewSprite, HorizontalGroup("row"), EnableIf(nameof(isUsingPortrait)), LayoutPercent(30), HideLabel, Title("Portrait")]
        public Texture2D playerIcon;

        /* Notice that VerticalGroup is created under the already established HorizontalGroup 'row'" */
        /* Vertical Group created "col" */
        
        [VerticalGroup("row/col"), Title("Character Name"), HideLabel] 
        public string name;

        /* Notice that TabGroup is created under the already established HorizontalGroup 'row'" */
        /* Tabs > Ability Scores */
        
        [TabGroup("row/col/tabs", "Ability Scores"), InlineProperty(InlinePropertyStyle.WithoutTitleBorderless)]
        public AbilityScores abilityScores;

        /* Tabs > Skills */
        
        [TabGroup("row/col/tabs", "Skills"), InlineProperty(InlinePropertyStyle.WithoutTitleBorderless)]
        public Skills skills;

        /* Tabs > Items */
        
        [TabGroup("row/col/tabs", "Items"), InlineProperty(InlinePropertyStyle.WithoutTitleBorderless)]
        public List<Item> items;
        
        /* Tabs > Other */
        
        [TabGroup("row/col/tabs", "Other")]
        public bool isUsingPortrait;
    }

    [InfoBox("Example Script for Artifice Toolkit. Feel free to explore the script.", InfoBoxAttribute.InfoMessageType.Info)]
    public List<Character> characters;
}