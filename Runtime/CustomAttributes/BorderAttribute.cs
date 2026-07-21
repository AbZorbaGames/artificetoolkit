using System.Runtime.CompilerServices;
using UnityEngine;

[assembly: InternalsVisibleTo("abzorba.artificetoolkit.editor")]

namespace ArtificeToolkit.Attributes
{
    /// <summary> Wraps property in a colored border. </summary>
    public class BorderAttribute : CustomAttribute
    {
        public readonly Color Color;
        internal bool UsesThemeDefault { get; }

        public BorderAttribute()
        {
            Color = Color.black;
            UsesThemeDefault = true;
        }

        public BorderAttribute(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out Color parsedColor);
            Color = parsedColor;
        }
        
        public BorderAttribute(float r, float g, float b, float a = 1)
        {
            Color = new Color(r, g, b, a);
        }
    }
}
