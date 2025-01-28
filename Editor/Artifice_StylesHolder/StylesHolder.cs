using System;
using System.Collections.Generic;
using ArtificeToolkit.Attributes;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace ArtificeToolkit.Editor
{
    /* NESTED CLASS */
    /// <summary> Holds info, regarding stylesheets/></summary>
    [Serializable]
    public class StyleData
    {
        public string name;
        public MonoScript script;
        public StyleSheet stylesheet;
    }

    [Serializable]
    public class StyleDataCategory
    {
        public string categoryName;

        [Title("Data")] public List<StyleData> styleData;
    }

    /// <summary>Holds <see cref="StyleData"/>, accessed through <see cref="Artifice_Utilities"/>.</summary>
    [CreateAssetMenu(fileName = "Styles Holder", menuName = "ScriptableObjects/ArtificeToolkit/Styles Holder")]
    public class StylesHolder : ScriptableObject
    {
        #region FIELDS

        [SerializeField] private StyleSheet globalStyle = null;
        [SerializeField] private List<StyleDataCategory> categories;

        #endregion

        /// <summary> Searches all categories for <see cref="StyleSheet"/> entry of the given Script Type. </summary>
        public StyleSheet GetStyle(Type type)
        {
            foreach (var category in categories)
            foreach (var data in category.styleData)
                if (data.script != null && data.script.GetClass() == type)
                    return data.stylesheet;

            Debug.Assert(false, $"[StyleHolderSO] Not style found for class of type ({type})");
            return null;
        }

        /// <summary> Searches all categories for <see cref="StyleSheet"/> entry of the given name.
        /// If you use this overload, make sure names are unique, or the first found will be returned.</summary>
        public StyleSheet GetStyleByName(string name)
        {
            foreach (var category in categories)
            foreach (var data in category.styleData)
                if (data.name == name)
                    return data.stylesheet;

            Debug.Assert(false, $"[StyleHolderSO] Not style found for class of type ({name})");
            return null;
        }

        /// <summary> Returns the global style set for this styles holder. </summary>
        public StyleSheet GetGlobalStyle()
        {
            return globalStyle;
        }

        /// <summary> Sets the global style preset of this styles holder. </summary>
        public void SetGlobalStyle(StyleSheet style)
        {
            globalStyle = style;
        }
    }
}