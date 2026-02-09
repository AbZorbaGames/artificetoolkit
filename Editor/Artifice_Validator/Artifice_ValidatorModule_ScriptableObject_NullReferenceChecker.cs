using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

        private readonly string[] _foldersToSearch = { "Assets" }; // Only search under Assets which is over user's control.
        
        public override IEnumerator ValidateCoroutine(List<GameObject> rootGameObjects)
        {
            var allAssetPaths = AssetDatabase.GetAllAssetPaths();
            var iterationCounter = 0;

            var pathsToSearch = allAssetPaths.Where(IsUnderSearchFolders);
            foreach (var path in pathsToSearch)
            {
                iterationCounter++;
                if (iterationCounter % 100 == 0)
                    yield return null; // avoid editor freeze
                
                if (!path.EndsWith(".asset"))
                    continue;

                var obj = AssetDatabase.LoadMainAssetAtPath(path);
                if (obj == null)
                    continue;

                // Case 1: Script rebound to MonoScript
                if (obj is MonoScript)
                {
                    Logs.Add(CreateMissingScriptableObjectValidatorLog(path, $"Corrupted ScriptableObject (script rebound): {obj.name}"));
                    continue;
                }

                SerializedObject so;
                try
                {
                    so = new SerializedObject(obj);
                }
                catch
                {
                    Logs.Add(CreateMissingScriptableObjectValidatorLog(path, $"Corrupted ScriptableObject (cannot deserialize): {obj.name}"));
                    continue;
                }

                var scriptProp = so.FindProperty("m_Script");

                // Case 2: ScriptableObject lost its script field entirely
                if (scriptProp == null)
                {
                    Logs.Add(CreateMissingScriptableObjectValidatorLog(path, $"Corrupted ScriptableObject (no m_Script): {obj.name}"));
                    continue;
                }
                
                // Case 3: Script field exists but is null
                if (scriptProp.objectReferenceValue == null)
                {
                    Logs.Add(CreateMissingScriptableObjectValidatorLog(path, $"Corrupted ScriptableObject (missing Script): {obj.name}"));
                    continue;
                }
            }
        }

        private Artifice_Validator.ValidatorLog CreateMissingScriptableObjectValidatorLog(string path, string message = "")
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
        
        private bool IsUnderSearchFolders(string path)
        {
            foreach (var folder in _foldersToSearch)
            {
                if (path.StartsWith(folder + "/", StringComparison.Ordinal))
                    return true;
            }
            return false;
        }
    }
}