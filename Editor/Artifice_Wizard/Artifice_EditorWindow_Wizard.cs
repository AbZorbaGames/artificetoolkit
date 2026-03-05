using System.Collections.Generic;
using System.IO;
using System.Linq;
using ArtificeToolkit.Attributes;
using ArtificeToolkit.Attributess;
using ArtificeToolkit.Editor.Artifice_ArtificeMenuEditorWindow;
using ArtificeToolkit.Examples;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

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
                new("Changelogs", CreateAndRegister<Page_Changelog>()),
                new("Examples", null)
                {
                    Children =
                    {
                        CreateNode_Characters(),
                    }
                }
            };

            return list;
        }

        private ArtificeMenuTreeNode CreateNode_Characters()
        {
            var charactersIconContent = EditorGUIUtility.IconContent("Cloth Icon");
            var node = new ArtificeMenuTreeNode("Characters", null, charactersIconContent.image);
            
            var characters = LoadAllCharacters();
            foreach (var character in characters)
            {
                node.AddChild(new ArtificeMenuTreeNode(character.characterName, character, character.playerIcon));
            }

            var newContent = EditorGUIUtility.IconContent("CreateAddNew@2x");
            node.AddChild(new ArtificeMenuTreeNode("Create New", CreateAndRegister<Page_CreateNewScriptableObject>(), newContent.image));

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

        #endregion

        /* Sub Pages */
        private class Page_CreateNewScriptableObject : ScriptableObject
        {
            [InfoBox("After creating, you will have to `Refresh` from the context menu panel to see the newly created asset.")]
            [SerializeField, InlineObject(false), ReadOnly]
            public Artifice_SCR_Character character;

            private bool _isCreated;

            private void OnEnable()
            {
                // Create new SCR
                character = CreateInstance<Artifice_SCR_Character>();
            }

            private void OnDisable()
            {
                if (_isCreated)
                    DestroyImmediate(character);
            }

            [Button]
            public void Create()
            {
                _isCreated = true;

                const string folder = "Packages/com.abzorba.artificetoolkit/Runtime/Artifice_Example";

                // Ensure filename is valid
                var fileName = string.IsNullOrEmpty(character.characterName)
                    ? "ExampleCharacter_New Character"
                    : $"ExampleCharacter_{character.characterName}";

                var path = $"{folder}/{fileName}.asset";
                path = AssetDatabase.GenerateUniqueAssetPath(path);

                AssetDatabase.CreateAsset(character, path);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                EditorGUIUtility.PingObject(character);

                _isCreated = false;
                character = CreateInstance<Artifice_SCR_Character>();
            }
        }
        
        private class Page_Changelog : EditorWindow
        {
            private string _changelogText;
            private Vector2 _scroll;

            private const string ChangeLogPath =
                "Packages/com.abzorba.artificetoolkit/CHANGELOG.md";

            private void OnEnable()
            {
                LoadChangelog();
            }

            private void LoadChangelog()
            {
                var fullPath = Path.GetFullPath(ChangeLogPath);

                if (File.Exists(fullPath))
                {
                    _changelogText = File.ReadAllText(fullPath);
                }
                else
                {
                    _changelogText = "CHANGELOG.md not found.";
                }
            }

            private void CreateGUI()
            {
                var container = new ScrollView(ScrollViewMode.Vertical);
                container.style.width = new StyleLength(Length.Percent(100));
                container.style.height = new StyleLength(Length.Percent(100));
                container.style.marginBottom = 30;
                
                container.Add(new Label(_changelogText));
                
                rootVisualElement.Add(container);
            }
        }
    }
}