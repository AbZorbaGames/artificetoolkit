using System;
using System.Collections.Generic;
using ArtificeToolkit.Attributes;
using ArtificeToolkit.Attributess;
using UnityEngine;


namespace ArtificeToolkit.Examples
{
    using Range = ArtificeToolkit.Attributes.RangeAttribute;
    
    [CreateAssetMenu(menuName = "ScriptableObjects/ArtificeToolkit/Examples/Character", fileName = "New Character")]
    public class Artifice_SCR_Character : ScriptableObject
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
            public void RandomizeStats() // Simulates a point buy in system.
            {
                var stats = new int[6];
                for (var i = 0; i < stats.Length; i++)
                    stats[i] = 8;

                var points = 27;

                var cost = new int[]
                {
                    0, //8
                    1, //9
                    1, //10
                    1, //11
                    1, //12
                    1, //13
                    2, //14
                    2  //15
                };

                var rng = new System.Random();

                while (points > 0)
                {
                    var statIndex = rng.Next(0, 6);

                    var current = stats[statIndex];
                    if (current >= 15)
                        continue;

                    var costIndex = current - 8 + 1;
                    var upgradeCost = cost[costIndex];

                    if (points >= upgradeCost)
                    {
                        stats[statIndex]++;
                        points -= upgradeCost;
                    }
                    else
                    {
                        break;
                    }
                }

                strength = stats[0];
                dexterity = stats[1];
                constitution = stats[2];
                wisdom = stats[3];
                intelligence = stats[4];
                charisma = stats[5];
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

        [HorizontalGroup("row")] [Required, PreviewSprite, HideLabel, EnableIf(nameof(isUsingPortrait)), LayoutPercent(30)]
        public Texture2D playerIcon;

        /* Notice that VerticalGroup is created under the already established HorizontalGroup 'row'" */
        /* Vertical Group created "col" */

        [VerticalGroup("row/col"), Title("Character Name"), HideLabel]
        public string characterName;

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

        [TabGroup("row/col/tabs", "Other")] public bool isUsingPortrait = false;

        [Button, TabGroup("row/col/tabs", "Other")]
        public void RandomizePortrait()
        {
            isUsingPortrait = true;
        }
    }
}