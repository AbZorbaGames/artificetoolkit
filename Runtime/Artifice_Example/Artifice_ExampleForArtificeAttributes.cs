using System.Collections.Generic;
using ArtificeToolkit.Attributes;
using ArtificeToolkit.Attributess;
using UnityEngine;

namespace ArtificeToolkit.Examples
{
    /// <summary> Example script for <see cref="CustomAttribute"/> </summary>
    public class Artifice_ExampleForArtificeAttributes : MonoBehaviour
    {
        [InfoBox("Example Script for Artifice Toolkit. Feel free to explore the script.", InfoBoxAttribute.InfoMessageType.Info)]
        [InlineObject]
        public List<Artifice_SCR_Character> characters;
    }
}