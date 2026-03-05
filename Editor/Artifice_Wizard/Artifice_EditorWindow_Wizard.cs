using System.Collections.Generic;
using System.Linq;
using ArtificeToolkit.Artifice_Example;
using ArtificeToolkit.Attributes;
using ArtificeToolkit.Editor.Artifice_ArtificeMenuEditorWindow;
using ArtificeToolkit.Examples;
using CustomAttributes;
using UnityEditor;
using UnityEngine;

namespace ArtificeToolkit.Editor.Artifice_Wizard
{
    public class Artifice_EditorWindow_Wizard : ArtificeMenuEditorWindow
    {
        #region FIELDS

        public override string ViewPersistenceKey { get; set; } = "Artifice_EditorWindow_Wizard";

        #endregion

        public static void ShowWindow()
        {
            var win = GetWindow<Artifice_EditorWindow_Wizard>();
            win.titleContent = new GUIContent("Artifice Wizard");
            win.minSize = new Vector2(750, 700);
        }

        protected override List<ArtificeMenuTreeNode> BuildMenuTree()
        {
            var list = new List<ArtificeMenuTreeNode>
            {
                new("Home", CreateAndRegister<Artifice_EditorWindow_HomeSettings>()),
                new("Examples", null)
                {
                    Children =
                    {
                        CreateNode_Characters(),
                        CreateNode_Items()
                    }
                },
                new("Ignore List", CreateAndRegister<Artifice_EditorWindow_IgnoreList>())
            };

            return list;
        }

        private ArtificeMenuTreeNode CreateNode_Characters()
        {
            var node = new ArtificeMenuTreeNode("Characters", null);
            
            var characters = LoadAllCharacters();
            foreach (var character in characters)
                node.AddChild(new ArtificeMenuTreeNode(character.characterName, character));

            node.AddChild(new ArtificeMenuTreeNode("Create New", CreateAndRegister<Page_CreateNewScriptableObject>()));
            
            return node;
        }

        private ArtificeMenuTreeNode CreateNode_Items()
        {
            var node = new ArtificeMenuTreeNode("Items", null);
            
            var items = LoadAllItems();
            foreach (var item in items)
                node.AddChild(new ArtificeMenuTreeNode(item.name, item));

            return node;
        }
        
        #region Utilities
        
        private List<Artifice_SCR_Character> LoadAllCharacters()
        {
            var searchFolders = new[]
            {
                "Packages/com.abzorba.artificetoolkit/Runtime/Artifice_Example"
            };

            var guids = AssetDatabase.FindAssets("t:Artifice_SCR_Character", searchFolders);

            var characters = guids
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<Artifice_SCR_Character>)
                .Where(asset => asset != null)
                .ToList();

            return characters;
        }

        private List<Artifice_SCR_Item> LoadAllItems()
        {
            var searchFolders = new[]
            {
                "Packages/com.abzorba.artificetoolkit/Runtime/Artifice_Example"
            };

            var guids = AssetDatabase.FindAssets("t:Artifice_SCR_Item", searchFolders);

            var items = guids
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<Artifice_SCR_Item>)
                .Where(asset => asset != null)
                .ToList();

            return items;
        }
        
        #endregion
        
        /* Sub Pages */
        private class Page_CreateNewScriptableObject : ScriptableObject
        {
            [SerializeField, InlineObject]
            public Artifice_SCR_Character character;

            [Button]
            public void Create()
            {
                
            }
        }
    }
}