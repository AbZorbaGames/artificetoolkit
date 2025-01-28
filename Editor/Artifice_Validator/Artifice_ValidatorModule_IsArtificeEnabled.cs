using System.Collections;
using Artifice_Editor;
using ArtificeToolkit.Editor.Resources;
using UnityEngine;

namespace ArtificeToolkit.Editor
{
    using ValidatorLog = Artifice_Validator.ValidatorLog;

    public class Artifice_ValidatorModule_IsArtificeEnabled : Artifice_ValidatorModule
    {
        public override string DisplayName { get; protected set; } = "Artifice Enabled";
        public override bool DisplayOnFilters { get; protected set; } = false;

        private readonly ValidatorLog _cachedLog;

        
        public Artifice_ValidatorModule_IsArtificeEnabled()
        {
            _cachedLog = new ValidatorLog(
                Artifice_SCR_CommonResourcesHolder.instance.WarningIcon,
                "ArtificeDrawer is not enabled",
                LogType.Warning,
                typeof(Artifice_ValidatorModule_IsArtificeEnabled),
                hasAutoFix: true,
                autoFixAction: () => Artifice_Utilities.ToggleArtificeDrawer(true)
            );
        }

        public override IEnumerator ValidateCoroutine(int batchSize)
        {
            Logs.Clear();
            
            if (Artifice_Utilities.ArtificeDrawerEnabled == false)
                Logs.Add(_cachedLog);

            yield break;
        }
    }
}