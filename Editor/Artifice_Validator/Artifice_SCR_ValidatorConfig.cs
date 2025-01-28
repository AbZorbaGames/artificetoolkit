using System;
using ArtificeToolkit.Attributes;
using ArtificeToolkit.Runtime.SerializedDictionary;
using UnityEngine;

// ReSharper disable InconsistentNaming

namespace ArtificeToolkit.Editor
{
    /// <summary> Searches the entire scene and gathers validation logs. </summary>
    [Serializable]
    [CreateAssetMenu(menuName = "ScriptableObjects/ArtificeToolkit/Validator Settings")]
    public class Artifice_SCR_ValidatorConfig : ScriptableObject
    {
        public enum BatchingPriority
        {
            Low = 10,
            Medium = 25,
            High = 40,
            VeryHigh = 100,
            Absolute = 1_000_000
        }
        
        [field: SerializeField] 
        public bool autorun = true;
        [field: SerializeField, EnumToggle] 
        public BatchingPriority batchingPriority = BatchingPriority.Medium; 
        
        [field: SerializeField] 
        public SerializedDictionary<string, bool> scenesMap = new();
        [field: SerializeField] 
        public SerializedDictionary<string, bool> validatorTypesMap = new();
        [field: SerializeField] 
        public SerializedDictionary<LogType, bool> logTypesMap = new();
        [field: SerializeField] 
        public SerializedDictionary<string, bool> assetPathsMap = new();
    }
}