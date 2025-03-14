using System;
using ArtificeToolkit.Attributes;
using ArtificeToolkit.Runtime.SerializedDictionary;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

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
            Low = 50,
            Medium = 150,
            High = 250,
            VeryHigh = 350,
            Absolute = 1_000_000
        }
        
        [field: SerializeField] 
        public bool autorun = false;
        
        [field: SerializeField, BoxGroup("Batching"), EnumToggle] 
        public BatchingPriority batchingPriority = BatchingPriority.Medium; 
        [field: SerializeField, BoxGroup("Batching")] 
        public bool useCustomBatchingValue;
        [field: SerializeField, BoxGroup("Batching"), EnableIf(nameof(useCustomBatchingValue), true)]
        public int customBatchingValue;
        
        
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