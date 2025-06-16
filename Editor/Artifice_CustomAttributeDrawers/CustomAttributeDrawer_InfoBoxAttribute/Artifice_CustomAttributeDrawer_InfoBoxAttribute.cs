using System;
using ArtificeToolkit.Attributes;
using ArtificeToolkit.Editor.Resources;
using ArtificeToolkit.Editor.VisualElements;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace ArtificeToolkit.Editor.Artifice_CustomAttributeDrawers.CustomAttributeDrawer_InfoBoxAttribute
{
    [Artifice_CustomAttributeDrawer(typeof(InfoBoxAttribute))]
    public class Artifice_CustomAttributeDrawer_InfoBoxAttribute : Artifice_CustomAttributeDrawer
    {
        public override VisualElement OnWrapGUI(SerializedProperty property, VisualElement root)
        {
            var attribute = (InfoBoxAttribute)Attribute;
            
            var wrapper = new VisualElement();
            wrapper.name = "InfoBox Wrapper";
            
            wrapper.Add(new Artifice_VisualElement_InfoBox(attribute.Message, LoadSpriteByType(attribute.Type))); 
            wrapper.Add(root);
            
            return wrapper;
        }

        private Sprite LoadSpriteByType(InfoBoxAttribute.InfoMessageType type)
        {
            switch (type)
            {
                case InfoBoxAttribute.InfoMessageType.Info:
                    return Artifice_SCR_CommonResourcesHolder.instance.CommentIcon;
                case InfoBoxAttribute.InfoMessageType.Warning:
                    return Artifice_SCR_CommonResourcesHolder.instance.WarningIcon;
                case InfoBoxAttribute.InfoMessageType.Error:
                    return Artifice_SCR_CommonResourcesHolder.instance.ErrorIcon;
                case InfoBoxAttribute.InfoMessageType.None:
                    return null;
                default:
                    throw new ArgumentException();
            }
        }
    }
}
