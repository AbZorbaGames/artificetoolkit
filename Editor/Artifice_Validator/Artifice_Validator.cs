using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ArtificeToolkit.Attributes;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace ArtificeToolkit.Editor
{
    public class Artifice_Validator : IDisposable
    {
        #region NESTED

        /// <summary> Logs structure for validators </summary>
        public struct ValidatorLog
        {
            public readonly string Message;
            public readonly LogType LogType;
            public readonly Sprite Sprite;
            public readonly Object OriginObject;
            public readonly string OriginLocationName;
            public readonly Type OriginValidatorType;

            public readonly bool HasAutoFix;
            public readonly Action AutoFixAction;

            public ValidatorLog(
                Sprite sprite,
                string message,
                LogType logType,
                Type originValidatorType,
                // Optional Parameters (Metadata)
                Object originObject = null,
                string originLocationName = "",
                bool hasAutoFix = false,
                Action autoFixAction = null
            )
            {
                Sprite = sprite;
                Message = message;
                LogType = logType;
                OriginObject = originObject;
                OriginLocationName = originLocationName;
                OriginValidatorType = originValidatorType;

                HasAutoFix = hasAutoFix;
                AutoFixAction = hasAutoFix ? autoFixAction : null;
            }
        }

        /// <summary> Helper struct to keep counter of logs per category </summary>
        public struct ValidatorLogCounters
        {
            public uint comments;
            public uint warnings;
            public uint errors;

            public readonly Dictionary<string, uint> scenesMap;
            public readonly Dictionary<string, uint> assetPathsMap;
            public readonly Dictionary<string, uint> validatorTypesMap;

            public ValidatorLogCounters(bool ignore = false)
            {
                comments = 0;
                warnings = 0;
                errors = 0;
                scenesMap = new Dictionary<string, uint>();
                assetPathsMap = new Dictionary<string, uint>();
                validatorTypesMap = new Dictionary<string, uint>();
            }

            public void IncreaseCount(ValidatorLog log)
            {
                switch (log.LogType)
                {
                    case LogType.Log:
                        ++comments;
                        break;
                    case LogType.Warning:
                        ++warnings;
                        break;
                    case LogType.Error:
                        ++errors;
                        break;
                    default:
                        break;
                }

                // Add 0 if key does not exist
                if (scenesMap.ContainsKey(log.OriginLocationName))
                    scenesMap[log.OriginLocationName] += 1;
                else
                {
                    var copy = assetPathsMap.Keys.ToList();
                    foreach (var key in copy)
                        if (log.OriginLocationName.Contains(key))
                            assetPathsMap[key] += 1;
                }

                if (validatorTypesMap.ContainsKey(log.OriginValidatorType.Name))
                    validatorTypesMap[log.OriginValidatorType.Name] += 1;
            }

            public void DecreaseCount(ValidatorLog log)
            {
                switch (log.LogType)
                {
                    case LogType.Log:
                        --comments;
                        break;
                    case LogType.Warning:
                        --warnings;
                        break;
                    case LogType.Error:
                        --errors;
                        break;
                    default:
                        break;
                }

                // Add 0 if key does not exist
                if (scenesMap.ContainsKey(log.OriginLocationName))
                    scenesMap[log.OriginLocationName] -= 1;
                else
                {
                    var copy = assetPathsMap.Keys.ToList();
                    foreach (var key in copy)
                        if (log.OriginLocationName.Contains(key))
                            assetPathsMap[key] -= 1;
                }

                if (validatorTypesMap.ContainsKey(log.OriginValidatorType.Name))
                    validatorTypesMap[log.OriginValidatorType.Name] -= 1;
            }

            public void Reset()
            {
                comments = 0;
                warnings = 0;
                errors = 0;
                foreach (var key in scenesMap.Keys.ToList())
                    scenesMap[key] = 0;
                foreach (var key in assetPathsMap.Keys.ToList())
                    assetPathsMap[key] = 0;
                foreach (var key in validatorTypesMap.Keys.ToList())
                    validatorTypesMap[key] = 0;
            }
        }

        #endregion

        #region SINGLETON

        private static Artifice_Validator _instance = null;

        private Artifice_Validator()
        {
        }

        public static Artifice_Validator Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new Artifice_Validator();

                return _instance;
            }
        }

        #endregion

        #region FIELD

        // Public events to notify on refresh and log counter refresh.
        public UnityEvent OnLogsRefreshEvent = new();
        public UnityEvent OnLogCounterRefreshedEvent = new();

        // Config, Logs and LogCounters
        private Artifice_SCR_ValidatorConfig _config;
        private readonly List<ValidatorLog> _logs = new();
        private ValidatorLogCounters _logCounters;

        // Validator Module List
        private List<Artifice_ValidatorModule> _validatorModules;

        // Refresh and Performance Limiter
        private bool _isRefreshing = false;
        private EditorCoroutine _refreshCoroutine;

        public const string ConfigPathKey = "ArtificeValidator/SettingsPath";
        public const string ConfigFolderPath = "Assets/Editor/ArtificeToolkit";

        #endregion
        
        [InitializeOnLoadMethod]
        private static void Init()
        {
            EditorApplication.delayCall -= DelayedInit;
            EditorApplication.delayCall += DelayedInit;
        }
        
        /// <summary> [InitializeOnLoadMethod] is too early time-wise, so initialize on delayed call. </summary>
        private static void DelayedInit()
        {
            Instance.Initialize();

            AssemblyReloadEvents.beforeAssemblyReload -= Instance.Dispose;
            AssemblyReloadEvents.beforeAssemblyReload += Instance.Dispose;
            EditorApplication.quitting -= Instance.Dispose;
            EditorApplication.quitting += Instance.Dispose;
        }

        /// <summary> Initializes singleton instances. </summary>
        private void Initialize()
        {
            // Load Artifice Validator State
            if (EditorPrefs.HasKey(ConfigPathKey))
                _config = AssetDatabase.LoadAssetAtPath<Artifice_SCR_ValidatorConfig>(
                    EditorPrefs.GetString(ConfigPathKey));

            // if config is still null, try to find any config file.
            if (_config == null)
            {
                // Use as path the path of the editor window
                if (!Directory.Exists(ConfigFolderPath))
                    Directory.CreateDirectory(ConfigFolderPath);

                _config = (Artifice_SCR_ValidatorConfig)ScriptableObject.CreateInstance(
                    typeof(Artifice_SCR_ValidatorConfig));
                AssetDatabase.CreateAsset(_config, ConfigFolderPath + "/Default Validator Config.asset");
                EditorPrefs.SetString(ConfigPathKey, ConfigFolderPath + "/Default Validator Config.asset");
            }

            _isRefreshing = false;
            _logCounters = new ValidatorLogCounters(false);

            // Initialize/Get Scenes
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                _logCounters.scenesMap[scene.name] = 0;
                _config.scenesMap.TryAdd(scene.name, true);
            }

            // In case a new scene was added, mark as dirty 
            EditorUtility.SetDirty(_config);

            // Initialize Asset paths
            foreach (var assetPath in _config.assetPathsMap.Keys)
                _logCounters.assetPathsMap[assetPath] = 0;

            // Initialize keys for log states (load this from state later on)
            if (_config.logTypesMap.ContainsKey(LogType.Log) == false)
                _config.logTypesMap[LogType.Log] = true;
            if (_config.logTypesMap.ContainsKey(LogType.Warning) == false)
                _config.logTypesMap[LogType.Warning] = true;
            if (_config.logTypesMap.ContainsKey(LogType.Error) == false)
                _config.logTypesMap[LogType.Error] = true;

            // Get Validator Module Types
            _validatorModules = new List<Artifice_ValidatorModule>();
            foreach (var type in TypeCache.GetTypesDerivedFrom<Artifice_ValidatorModule>())
            {
                if (type.IsAbstract)
                    continue;

                if (_config.validatorTypesMap.ContainsKey(type.Name) == false)
                    _config.validatorTypesMap[type.Name] = true;

                _validatorModules.Add((Artifice_ValidatorModule)Activator.CreateInstance(type));
                _logCounters.validatorTypesMap[type.Name] = 0;
            }

            // Add Update callback to editor application update.
            EditorApplication.update -= Update;
            EditorApplication.update += Update;
        }

        /// <summary> Called every editor update. </summary>
        private void Update()
        {
            if (_config == null)
                return;

            if (_config.autorun && _isRefreshing == false)
            {
                _isRefreshing = true;
                
                //  Stop coroutine just in case
                if(_refreshCoroutine != null)
                    EditorCoroutineUtility.StopCoroutine(_refreshCoroutine);
                
                // Start coroutine.
                _refreshCoroutine = EditorCoroutineUtility.StartCoroutine(RefreshLogsCoroutine(), this);
            }
        }

        /// <summary> Calls <see cref="RefreshLogsCoroutine"/> but blocks main thread to run faster. </summary>
        public void RefreshLogs()
        {
            if (_refreshCoroutine != null)
            {
                EditorCoroutineUtility.StopCoroutine(_refreshCoroutine);
                EditorUtility.ClearProgressBar();
            }

            _refreshCoroutine = EditorCoroutineUtility.StartCoroutine(RefreshLogsCoroutine(true), this);
        }

        /// <summary> Iterates every nested property of gameobject to detect <see cref="ValidatorAttribute"/> and logs their validity. </summary>
        private IEnumerator RefreshLogsCoroutine(bool fullScan = false)
        {
            _isRefreshing = true;

            var currentBatchCount = 0;
            var batchSize = _config.useCustomBatchingValue ? _config.customBatchingValue : (int)_config.batchingPriority;
            if (fullScan)
                batchSize = (int)Artifice_SCR_ValidatorConfig.BatchingPriority.Absolute;

            if (_logs == null)
                throw new ArgumentException($"[{GetType()}] FilteredLogs not initialized properly.");

            // Gather all root gameObjects.
            var rootGameObjects = GetAllRootGameObjects();

            // Run validate for each module and add to list
            _logs.Clear();
            for (var i = 0; i < _validatorModules.Count; i++)
            {
                var module = _validatorModules[i];
                module.Reset();

                // Unless blocking search. skip on demand only modules
                if (module.OnFullScanOnly && fullScan == false)
                    continue;

                // If blocking, progress bar
                if (fullScan)
                    EditorUtility.DisplayProgressBar("Artifice Validator Scan", $"Running {module.GetType().Name}",
                        (float)(i + 1) / (float)(_validatorModules.Count + 1));

                // Validate and add logs
                var coroutine = module.ValidateCoroutine(rootGameObjects);
                while (coroutine.MoveNext())
                {
                    if (++currentBatchCount > batchSize)
                    {
                        currentBatchCount = 0;
                        yield return null;
                    }
                }
                
                _logs.AddRange(module.Logs);
            }
            
            // Emit refresh
            OnLogsRefreshEvent.Invoke();
            // Refresh log counters.
            RefreshLogCounters();
            
            _isRefreshing = false;

            // If method was called as blocking, do not change isRefreshing since its auto-refresh's job.
            if (fullScan)
                EditorUtility.ClearProgressBar();
        }

        /// <summary> Iterates all logs and sets the log counter's value. </summary>
        private void RefreshLogCounters()
        {
            _logCounters.Reset();
            foreach (var log in _logs)
                _logCounters.IncreaseCount(log);

            OnLogCounterRefreshedEvent.Invoke();
        }

        /// <summary> Disposes listeners </summary>
        public void Dispose()
        {
            EditorApplication.update -= Update;
            OnLogsRefreshEvent?.RemoveAllListeners();
            OnLogCounterRefreshedEvent?.RemoveAllListeners();
        }

        /// <summary> Returns original list of validator logs. </summary>
        public List<ValidatorLog> Get_Logs()
        {
            return _logs;
        }

        /// <summary> Returns list of log counters. </summary>
        public ValidatorLogCounters Get_LogCounters()
        {
            return _logCounters;
        }

        /// <summary> Returns list of all validator modules found. </summary>
        public List<Artifice_ValidatorModule> Get_ValidatorModules()
        {
            return _validatorModules;
        }

        /// <summary> Returns <see cref="Artifice_SCR_ValidatorConfig"/></summary>
        public Artifice_SCR_ValidatorConfig Get_ValidatorConfig()
        {
            return _config;
        }

        #region Utility
            
        private List<GameObject> GetAllRootGameObjects()
        {
            var rootGameObjects = new List<GameObject>();

            // Check if we are in Prefab Mode
            var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage != null)
            {
                rootGameObjects.Add(prefabStage.prefabContentsRoot);
                return rootGameObjects;
            }

            // If not in prefab stage, get root objects from all loaded scenes
            var sceneCount = SceneManager.sceneCount;
            for (var i = 0; i < sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded)
                    rootGameObjects.AddRange(scene.GetRootGameObjects());
            }

            return rootGameObjects;
        }

        #endregion
    }
}