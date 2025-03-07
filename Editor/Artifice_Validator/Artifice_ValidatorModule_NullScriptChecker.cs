using ArtificeToolkit.Editor.Resources;
using UnityEngine;

namespace ArtificeToolkit.Editor
{
    public class Artifice_ValidatorModule_NullScriptChecker : Artifice_ValidatorModule_GameObjectBatching
    {
        public override string DisplayName { get; protected set; } = "Null Script Checker";

        public override bool DisplayOnFiltersList { get; protected set; } = false;

        public override bool OnFullScanOnly { get; protected set; } = false;

        protected override void ValidateGameObject(GameObject gameObject)
        {
            foreach (var component in gameObject.GetComponents<Component>())
            {
                if (component == null)
                {
                    // Create validation
                    var log = new Artifice_Validator.ValidatorLog(
                        Artifice_SCR_CommonResourcesHolder.instance.ErrorIcon,
                        "Component has corrupted script reference",
                        LogType.Error,
                        GetType(),
                        gameObject.transform, // Since component is null, add the transform of the gameobject, in order to be able to jump to the gameobject.
                        gameObject.name
                    );
                    Logs.Add(log);
                }
            }
        }
    }
}
