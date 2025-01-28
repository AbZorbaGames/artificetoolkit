    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using ArtificeToolkit.Editor;
    using Unity.EditorCoroutines.Editor;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.Events;
    using UnityEngine.SceneManagement;
    using Object = UnityEngine.Object;

    namespace Artifice_Editor
    {
        public class Artifice_Validator : IDisposable
        {
            #region NESTED
            
            /// <summary> Logs structure for validators </summary>
            public struct ValidatorLog
            {
                public readonly string message;
                public readonly LogType logType;
                public readonly Sprite sprite;
                public readonly Object originObject;
                public readonly string originLocationName;
                public readonly Type originValidatorType;

                public readonly bool hasAutoFix;
                public readonly Action autoFixAction;
                
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
                    this.sprite = sprite;
                    this.message = message;
                    this.logType = logType;
                    this.originObject = originObject;
                    this.originLocationName = originLocationName;
                    this.originValidatorType = originValidatorType;

                    this.hasAutoFix = hasAutoFix;
                    this.autoFixAction = hasAutoFix ? autoFixAction : null;
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
                    switch (log.logType)
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
                    if (scenesMap.ContainsKey(log.originLocationName))
                        scenesMap[log.originLocationName] += 1;
                    else
                    {
                        var copy = assetPathsMap.Keys.ToList(); 
                        foreach(var key in copy)
                            if (log.originLocationName.Contains(key))
                                assetPathsMap[key] += 1;
                    }

                    if (validatorTypesMap.ContainsKey(log.originValidatorType.Name))
                        validatorTypesMap[log.originValidatorType.Name] += 1;
                }
                public void DecreaseCount(ValidatorLog log)
                {
                    switch (log.logType)
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
                    if (scenesMap.ContainsKey(log.originLocationName))
                        scenesMap[log.originLocationName] -= 1;
                    else
                    {
                        var copy = assetPathsMap.Keys.ToList(); 
                        foreach(var key in copy)
                            if (log.originLocationName.Contains(key))
                                assetPathsMap[key] -= 1;
                    }
                    
                    if (validatorTypesMap.ContainsKey(log.originValidatorType.Name))
                        validatorTypesMap[log.originValidatorType.Name] -= 1;
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
            
            private static Artifice_Validator instance = null;

            private Artifice_Validator()
            {
            }

            public static Artifice_Validator Instance
            {
                get
                {
                    if (instance == null)
                    {
                        instance = new Artifice_Validator();
                    }
                    return instance;
                }
            }
            
            #endregion
            
            #region FIELD
            
            // Public events to notify on refresh and log counter refresh.
            public UnityEvent OnLogsRefreshEvent;
            public UnityEvent OnLogCounterRefreshedEvent;
            
            // Config, Logs and LogCounters
            private Artifice_SCR_ValidatorConfig _config;
            private readonly List<ValidatorLog> _logs = new();
            private ValidatorLogCounters _logCounters;
            
            // Validator Module List
            private List<Artifice_ValidatorModule> _validatorModules;
            
            // Performance Limiter
            private bool _isRefreshing = false;
            
            public const string ConfigPathKey = "ArtificeValidator/SettingsPath";
            public const string ConfigFolderPath = "Assets/Editor/ArtificeToolkit";
            
            #endregion

            [InitializeOnLoadMethod]
            private static void Init()
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
                if(EditorPrefs.HasKey(ConfigPathKey))
                    _config = AssetDatabase.LoadAssetAtPath<Artifice_SCR_ValidatorConfig>(EditorPrefs.GetString(ConfigPathKey));
                
                // if config is still null, try to find any config file.
                if (_config == null)
                {
                    // Use as path the path of the editor window
                    if (!System.IO.Directory.Exists(ConfigFolderPath))
                        System.IO.Directory.CreateDirectory(ConfigFolderPath);
                    
                    _config = (Artifice_SCR_ValidatorConfig)ScriptableObject.CreateInstance(typeof(Artifice_SCR_ValidatorConfig));
                    AssetDatabase.CreateAsset(_config, ConfigFolderPath + "/Default Validator Config.asset");
                    EditorPrefs.SetString(ConfigPathKey, ConfigFolderPath + "/Default Validator Config.asset");
                }
                
                _isRefreshing = false;
                _logCounters = new ValidatorLogCounters(false);
                OnLogsRefreshEvent = new UnityEvent();
                OnLogCounterRefreshedEvent = new UnityEvent();

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
                if(_config.logTypesMap.ContainsKey(LogType.Log) == false)
                    _config.logTypesMap[LogType.Log] = true;
                if(_config.logTypesMap.ContainsKey(LogType.Warning) == false)
                    _config.logTypesMap[LogType.Warning] = true;
                if(_config.logTypesMap.ContainsKey(LogType.Error) == false)
                    _config.logTypesMap[LogType.Error] = true;
                
                // Get Validator Module Types
                _validatorModules = new List<Artifice_ValidatorModule>();
                foreach (var type in TypeCache.GetTypesDerivedFrom<Artifice_ValidatorModule>())
                {
                    if(type.IsAbstract)
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
                    EditorCoroutineUtility.StartCoroutine(RefreshLogsCoroutine(), this);
                }
            }
            
            /// <summary> Calls <see cref="RefreshLogsCoroutine"/> but blocks main thread to run faster. </summary>
            public void RefreshLogs()
            {
                EditorCoroutineUtility.StartCoroutine(RefreshLogsCoroutine(true), this);
            }
            
            /// <summary> Iterates every nested property of gameobject to detect <see cref="Abz_ValidatorAttribute"/> and logs their validity. </summary>
            private IEnumerator RefreshLogsCoroutine(bool isBlocking = false)
            {
                _isRefreshing = true;
                
                var batchSize = (int)_config.batchingPriority;
                if (isBlocking)
                    batchSize = (int)Artifice_SCR_ValidatorConfig.BatchingPriority.Absolute;
                
                if (_logs == null)
                    throw new ArgumentException($"[{GetType()}] FilteredLogs not initialized properly.");

                // Run validate for each module and add to list
                _logs.Clear();
                for(var i = 0; i < _validatorModules.Count; i++)
                {
                    var module = _validatorModules[i];
                    
                    // Unless blocking search. skip on demand only modules
                    if(module.OnDemandOnlyModule && isBlocking == false)
                        continue;
                    
                    // If blocking, progress bar
                    if(isBlocking)
                        EditorUtility.DisplayProgressBar("Artifice Validator Scan", $"Running {module.GetType().Name}", (float)(i + 1) / (float)(_validatorModules.Count + 1));
                        
                    // Validate and add logs
                    yield return module.ValidateCoroutine(batchSize);

                    var logs = module.Logs;
                    _logs.AddRange(logs);
                }
                
                // Refresh log counters.
                RefreshLogCounters();

                // Emit refresh
                OnLogsRefreshEvent.Invoke();
                
                _isRefreshing = false;
                
                // If method was called as blocking, do not change isRefreshing since its auto-refresh's job.
                if(isBlocking)
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
        }
    }
