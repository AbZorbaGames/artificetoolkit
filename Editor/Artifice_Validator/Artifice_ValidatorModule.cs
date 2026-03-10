using System;
using System.Collections;
using System.Collections.Generic;
using ArtificeToolkit.Attributes;
using UnityEngine;
using BatchingPriority = ArtificeToolkit.Editor.Artifice_SCR_ValidatorConfig.BatchingPriority;

namespace ArtificeToolkit.Editor
{
    public abstract class Artifice_ValidatorModule
    {
        [Serializable]
        public struct Configuration
        {
            [ReadOnly, HideLabel]
            public string typeName;
            
            public bool isOverridingDefaultConfiguration;
         
            [BoxGroup("Overrides Configuration")]
            public bool isAutorun;
            
            [field: SerializeField, BoxGroup("Overrides Configuration/Batching"), EnumToggle] 
            public BatchingPriority batchingPriority; 
            [field: SerializeField, BoxGroup("Overrides Configuration/Batching")] 
            public bool useCustomBatchingValue;
            [field: SerializeField, BoxGroup("Overrides Configuration/Batching"), EnableIf(nameof(useCustomBatchingValue), true)]
            public int customBatchingValue;
            
            [EnableIf(nameof(isOverridingDefaultConfiguration)), BoxGroup("Overrides Configuration")]
            public List<string> assetPathsToInclude;
        }
        
        #region FIELDS
        
        /// <summary>Display name of module</summary>
        public virtual string DisplayName { get; protected set; } = "Undefined";

        /// <summary>If true, its displayed on the filters list. Otherwise its hidden, but always runs.</summary>
        public virtual bool DisplayOnFiltersList { get; protected set; } = true;

        /// <summary>When set to true, module will only run with dedicated button call</summary>
        public virtual bool OnFullScanOnly { get; protected set; } = false;
        
        /// <summary>Each module will empty and fill this list with its validations when <see cref="ValidateCoroutine"/> is called</summary>
        public readonly List<Artifice_Validator.ValidatorLog> Logs = new();

        protected Configuration ModuleConfiguration; 
        
        #endregion

        /* Main Abstract Method for Validation */
        public abstract IEnumerator ValidateCoroutine(List<GameObject> rootGameObjects);

        /// <summary> Resets logs and sets finished to false. </summary>
        public void Reset()
        {
            Logs.Clear();
        }

        /// <summary> Called at every run, setting a new configuration for the run. </summary>
        /// <remarks> This would be better to be added at ValidateCoroutine but would require major version update. Skip for now. </remarks>
        public void Set_Configuration(Configuration configuration)
        {
            ModuleConfiguration = configuration;
        }
    }
}