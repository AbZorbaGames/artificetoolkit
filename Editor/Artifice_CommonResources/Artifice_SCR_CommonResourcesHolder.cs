using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ArtificeToolkit.Editor.Resources
{
    [FilePath("Artifice/CommonResourcesHolder.holder", FilePathAttribute.Location.PreferencesFolder)]
    public class Artifice_SCR_CommonResourcesHolder : ScriptableSingleton<Artifice_SCR_CommonResourcesHolder>
    {
        public Sprite CommentIcon { get; private set; }
        public Sprite WarningIcon { get; private set; }
        public Sprite ErrorIcon { get; private set; }

        public Sprite MagnifyingGlassIcon { get; private set; }
        public Sprite GameObjectIcon { get; private set; }
        public Sprite TransformIcon { get; private set; }
        public Sprite ScriptIcon { get; private set; }
        public Sprite BarsIcon { get; private set; }
        
        public Sprite UnityIcon { get; private set; }
        public Sprite FolderIcon { get; private set; }
        
        public Sprite GearIcon { get; private set; }
        
        public Sprite PlayIcon { get; private set; }
        public Sprite PauseIcon { get; private set; }
        public Sprite RefreshIcon { get; private set; }
        
        
        public Sprite StarIcon { get; private set; }
        
        public Sprite MaximizeIcon { get; private set; }


        private void Awake()
        {
            CommentIcon = FindAndLoadSprite("comment");
            WarningIcon = FindAndLoadSprite("triangle-exclamation");
            ErrorIcon = FindAndLoadSprite("circle-exclamation");
            
            MagnifyingGlassIcon = FindAndLoadSprite("magnifying-glass");
            GameObjectIcon = FindAndLoadSprite("gameobject");
            TransformIcon = FindAndLoadSprite("transform");
            ScriptIcon = FindAndLoadSprite("script");
            BarsIcon = FindAndLoadSprite("bars");
            
            UnityIcon = FindAndLoadSprite("unity");
            FolderIcon = FindAndLoadSprite("folder");
            
            GearIcon = FindAndLoadSprite("gear");
            PlayIcon = FindAndLoadSprite("play");
            PauseIcon = FindAndLoadSprite("pause");
            RefreshIcon = FindAndLoadSprite("refresh");
            
            StarIcon = FindAndLoadSprite("star");
            MaximizeIcon = FindAndLoadSprite("maximize");
        }

        /// <summary>Searches asset database with artifice_ prefix and t:Sprite parameters.</summary>
        private Sprite FindAndLoadSprite(string spriteName)
        {
            var guid = AssetDatabase.FindAssets($"artifice_{spriteName} t:Sprite").FirstOrDefault();
            var path = AssetDatabase.GUIDToAssetPath(guid);
            return AssetDatabase.LoadAssetAtPath<Sprite>(path); 
        }

        internal bool IsNeutralIcon(Sprite sprite)
        {
            return sprite == CommentIcon ||
                   sprite == MagnifyingGlassIcon ||
                   sprite == ScriptIcon ||
                   sprite == BarsIcon ||
                   sprite == UnityIcon ||
                   sprite == FolderIcon ||
                   sprite == GearIcon ||
                   sprite == PauseIcon ||
                   sprite == RefreshIcon ||
                   sprite == MaximizeIcon;
        }
    }
}
