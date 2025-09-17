using System.Collections;
using System.Collections.Generic;
using ArtificeToolkit.Editor.Resources;
using UnityEditor;
using UnityEngine;

namespace ArtificeToolkit.Editor
{
    public class Artifice_ValidatorModule_ScriptableObject_NullReferenceChecker : Artifice_ValidatorModule
    {
        public override string DisplayName { get; protected set; } = "[ScriptableObject] Null Script Checker";
        public override bool DisplayOnFiltersList { get; protected set; } = true;
        public override bool OnFullScanOnly { get; protected set; } = false;

        public override IEnumerator ValidateCoroutine(List<GameObject> rootGameObjects)
        {
            var guids = AssetDatabase.FindAssets("", new[] { "Assets" }); // Only search under Assets which is over user's control.

            for (var i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var asset = AssetDatabase.LoadMainAssetAtPath(path);

                // Check if it's a scriptable object with a missing script
                if (asset != null && asset is ScriptableObject scriptableObject)
                {
                    var script = MonoScript.FromScriptableObject(scriptableObject);
                    if (script == null)
                    {
                        Logs.Add(CreateMissingScriptableObjectValidatorLog(path));
                    }
                }
                else if (asset == null && path.EndsWith(".asset"))
                {
                    // Unity returns null if the script is missing entirely
                    Logs.Add(CreateMissingScriptableObjectValidatorLog(path));
                }

                if (i % 50 == 0)
                    yield return null; // avoid editor freeze
            }
        }

        private Artifice_Validator.ValidatorLog CreateMissingScriptableObjectValidatorLog(string path)
        {
            return new Artifice_Validator.ValidatorLog(
                Artifice_SCR_CommonResourcesHolder.instance.ErrorIcon,
                $"Missing script on scriptable object at path {path}",
                LogType.Error,
                GetType(),
                null,
                path
            );
        }
    }
}