using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ArtificeToolkit.Editor
{
    public abstract class Artifice_ValidatorModule
    {
        #region FIELDS

        /// <summary>Display name of module</summary>
        public virtual string DisplayName { get; protected set; } = "Undefined";

        /// <summary>If true, its displayed on the filters list. Otherwise its hidden, but always runs.</summary>
        public virtual bool DisplayOnFiltersList { get; protected set; } = true;

        /// <summary>When set to true, module will only run with dedicated button call</summary>
        public virtual bool OnFullScanOnly { get; protected set; } = false;
        
        /// <summary>Each module will empty and fill this list with its validations when <see cref="ValidateCoroutine"/> is called</summary>
        public readonly List<Artifice_Validator.ValidatorLog> Logs = new();
        
        #endregion

        /* Main Abstract Method for Validation */
        public abstract IEnumerator ValidateCoroutine(List<GameObject> rootGameObjects);

        /// <summary> Resets logs and sets finished to false. </summary>
        public void Reset()
        {
            Logs.Clear();
        }
        
        #region Utility
        
        protected Artifice_SCR_ValidatorConfig GetConfig()
        {
            // Do this on every call, since its possible for the selected config to be changed
            const string configKeyPath = Artifice_Validator.ConfigPathKey;
            return AssetDatabase.LoadAssetAtPath<Artifice_SCR_ValidatorConfig>(EditorPrefs.GetString(configKeyPath));
        } 
        
        #endregion
    }
}