using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ArtificeToolkit.Editor.Resources;
using ArtificeToolkit.Editor.VisualElements;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Image = UnityEngine.UIElements.Image;
using Object = UnityEngine.Object;
using Toggle = UnityEngine.UIElements.Toggle;

// ReSharper disable InconsistentNaming

namespace ArtificeToolkit.Editor
{
    public class Artifice_EditorWindow_Validator : EditorWindow, IHasCustomMenu
    {   
        #region Constants
        
        private const string MenuItemPath = "Artifice Toolkit/Validator %&v";

        #endregion
        
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
        private struct ValidatorLogCounters
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
        
        #region Nested VisualElements
        
        /// <summary> Helper class to render repeatable list element </summary>
        private class ListItem : VisualElement
        {
            private Image _image;
            private Label _text;
            
            public ListItem()
            {
                AddToClassList("list-item");

                _image = new Image();
                _image.AddToClassList("icon");
                _text = new Label();
                _text.AddToClassList("label");
                
                Add(_image);
                Add(_text);
            }

            public void Set(Sprite sprite, string text)
            {
                _image.sprite = sprite;
                _text.text = text;
            }

            public string Get_Text()
            {
                return _text.text;
            }
        }
        
        /// <summary> Extends <see cref="ListItem"/> by having a toggle </summary>
        private class ToggleListItem : ListItem
        {
            public readonly Toggle Toggle;
            public readonly Label CountLabel;
            
            public ToggleListItem() : base()
            {
                Toggle = new Toggle();
                Add(Toggle);
                Toggle.SendToBack();

                var countLabelContainer = new VisualElement();
                countLabelContainer.AddToClassList("count-label-container");
                Add(countLabelContainer);
                
                CountLabel = new Label("0");
                CountLabel.AddToClassList("count-label");
                countLabelContainer.Add(CountLabel);
            }

            public void Set(bool state, Sprite sprite, string text)
            {
                base.Set(sprite, text);
                Toggle.value = state;
            }
        }

        /// <summary> Extends <see cref="ListItem"/> by representing more information for <see cref="ValidatorLog"/> </summary>
        private class ValidatorLogListItem : ListItem
        {
            private readonly Label _objectNameLabel;
            private readonly Artifice_VisualElement_LabeledButton _autoFixButton;

            private Object _originObject;

            public ValidatorLogListItem() : base()
            {
                styleSheets.Add(Artifice_Utilities.GetGlobalStyle());
                
                // Create object label
                _objectNameLabel = new Label("GameObject");
                _objectNameLabel.AddToClassList("object-name-label");
                Add(_objectNameLabel);
                
                // Create autoFixButton
                _autoFixButton = new Artifice_VisualElement_LabeledButton("Auto Fix", null);
                _autoFixButton.AddToClassList("hide");
                Add(_autoFixButton);
                
                RegisterCallback<ClickEvent>(evt =>
                {
                    var listItem = (ValidatorLogListItem)evt.currentTarget;
                    if(evt.clickCount == 2 && listItem._originObject != null)
                       Selection.SetActiveObjectWithContext(listItem._originObject, listItem._originObject);
                });
            }

            public void Set(ValidatorLog log)
            {
                base.Set(log.sprite, log.message);
                _objectNameLabel.text = log.originObject == null ? "" : log.originObject.name;
                _originObject = log.originObject;

                if (log.hasAutoFix)
                {
                    // Show button and set callback
                    _autoFixButton.style.display = DisplayStyle.Flex;
                    _autoFixButton.SetAction(log.autoFixAction);
                }
                else
                    _autoFixButton.style.display = DisplayStyle.None;
            }
        }
        
        #endregion
        
        #region FIELDS
 
        // Logs
        private readonly List<ValidatorLog> _logs = new();
        private readonly List<ValidatorLog> _filteredLogs = new();
        
        // Filter mechanism
        private List<Func<ValidatorLog, bool>> _filters;
        
        // Logs counter
        private ValidatorLogCounters _logCounters;
        
        // Validator Module List
        private List<Artifice_ValidatorModule> _validatorModules;
        
        // Dynamic VisualElement References
        private ListView _logsListView;
        private UnityEvent OnLogsRefreshEvent;
        private UnityEvent OnLogCounterRefreshedEvent;
        
        // Performance Bounds
        private bool _isRefreshing = false;

        // Used for editor prefs
        public const string PrefabStageKey = "PrefabStage";
        public const string ConfigPathKey = "ArtificeValidator/SettingsPath";
        private const string ConfigFolderPath = "Assets/Editor/ArtificeToolkit";
        
        public Artifice_SCR_ValidatorConfig _config;
        
        #endregion
        
        [MenuItem(MenuItemPath)]
        public static void OpenWindow()
        {
            var wnd = GetWindow<Artifice_EditorWindow_Validator>();
            wnd.titleContent = new GUIContent("Artifice Validator");
            wnd.minSize = new Vector2(750, 450);
        }
        
        /* Mono */
        private void CreateGUI()
        {   
            // Initialize
            Initialize();

            // Create GUI
            BuildUI();
        }
        
        /* Mono */
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
                
                _config = (Artifice_SCR_ValidatorConfig)CreateInstance(typeof(Artifice_SCR_ValidatorConfig));
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
            
            // Initialize Filters
            _filters = new List<Func<ValidatorLog, bool>>();
            _filters.Add(log => OnSelectedScenesFilter(log) || OnSelectedAssetPathFilter(log));
            _filters.Add(OnSelectedValidatorTypesFilter);
            _filters.Add(OnLogTypeTogglesFilter);
        }
        
        /* Mono */
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
        
        /* Mono */
        private void OnDisable()
        {
            OnLogsRefreshEvent?.RemoveAllListeners();
            OnLogCounterRefreshedEvent?.RemoveAllListeners();
        }

        /// <summary> Calls <see cref="RefreshLogsCoroutine"/> but blocks main thread to run faster. </summary>
        private void RefreshLogs()
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
            
            // Refresh counters
            RefreshLogCounters();
            
            // Refresh Filtered logs
            RefreshFilteredLogs();
            
            // Emit refresh
            OnLogsRefreshEvent.Invoke();
            
            _isRefreshing = false;
            
            // If method was called as blocking, do not change isRefreshing since its auto-refresh's job.
            if(isBlocking)
                EditorUtility.ClearProgressBar();
        }
        
        /// <summary> Clears and fills filtered logs based on all logs. </summary>
        private void RefreshFilteredLogs()
        {
            _filteredLogs.Clear();
            foreach (var log in _logs)
            {
                // If a prefab stage is open, use prefab stage filter.
                if (PrefabStageUtility.GetCurrentPrefabStage() != null)
                {
                    if(OnPrefabStageFilter(log))
                        _filteredLogs.Add(log);
                }
                // Else, use all normal filters.
                else if(_filters.All(filter => filter.Invoke(log)))
                    _filteredLogs.Add(log);
            }
            _logsListView?.RefreshItems();
        }

        /// <summary> Calling this method recalculates the log count for each counter map </summary>
        private void RefreshLogCounters()
        {
            _logCounters.Reset();
            foreach (var log in _logs)
                _logCounters.IncreaseCount(log);
            
            OnLogCounterRefreshedEvent.Invoke();
        }
        
        #region Build UI

        private void BuildUI()
        {
            // Add styles 
            rootVisualElement.styleSheets.Add(Artifice_Utilities.GetGlobalStyle());
            rootVisualElement.styleSheets.Add(Artifice_Utilities.GetStyle(GetType()));

            // Draw
            var header = BuildHeaderUI();
            rootVisualElement.Add(header);

            // Splits tracked containers and error logs
            var splitPane = new TwoPaneSplitView(0, 300, TwoPaneSplitViewOrientation.Horizontal);
            splitPane.viewDataKey = "Artifice_EditorWindow_Validator";
            splitPane.AddToClassList("align-horizontal");
            rootVisualElement.Add(splitPane);

            // Build Tracked locations
            var trackedContainers = new ScrollView();
            trackedContainers.AddToClassList("tracked-containers-container");
            // trackedContainers.Add(BuildTrackedScenesUI()); // Removed for now. It seemed obnoxious and useless. 
            trackedContainers.Add(BuildTrackedAssetFoldersUI());
            trackedContainers.Add(BuildTrackedValidatorTypesUI());
            // Add to split-pane
            splitPane.Add(trackedContainers);
            
            // Build Error logs and add
            splitPane.Add(BuildLogsUI());
        }
        
        private VisualElement BuildHeaderUI()
        {
            var container = new VisualElement();
            container.AddToClassList("header-container");

            // Manual scan button
            var runScan = new Artifice_VisualElement_LabeledButton("Run Scan", RefreshLogs);
            container.Add(runScan);
            
            // Autorun toggle
            var autorunButton = new Artifice_VisualElement_ToggleButton(
                "Autorun",
                Artifice_SCR_CommonResourcesHolder.instance.PauseIcon,
                Artifice_SCR_CommonResourcesHolder.instance.PlayIcon,
                _config.autorun
            );
            var configSerializedObject = new SerializedObject(_config);
            autorunButton.BindProperty(configSerializedObject.FindProperty(nameof(_config.autorun)));
            container.Add(autorunButton);
            
            // Settings btton
            var settingsButton = new Artifice_VisualElement_LabeledButton("Settings", () =>
            {
                Selection.activeObject = _config;
            });
            container.Add(settingsButton);
            
            // Create container for toggles 
            var logFilterToggles = new VisualElement();
            logFilterToggles.AddToClassList("log-toggles-container");
            container.Add(logFilterToggles);

            // Simple Log
            var infoToggle = new Artifice_VisualElement_ToggleButton("0", Artifice_SCR_CommonResourcesHolder.instance.CommentIcon, _config.logTypesMap[LogType.Log]);
            infoToggle.OnButtonPressed += value => {
                _config.logTypesMap[LogType.Log] = value;
                RefreshFilteredLogs();
            };
            logFilterToggles.Add(infoToggle);
            
            // Warning Log
            var warningToggle = new Artifice_VisualElement_ToggleButton("0", Artifice_SCR_CommonResourcesHolder.instance.WarningIcon, _config.logTypesMap[LogType.Warning]);
            warningToggle.OnButtonPressed += value => {
                _config.logTypesMap[LogType.Warning] = value;
                RefreshFilteredLogs();
            };
            logFilterToggles.Add(warningToggle);
            
            // Error Log
            var errorToggle = new Artifice_VisualElement_ToggleButton("0", Artifice_SCR_CommonResourcesHolder.instance.ErrorIcon, _config.logTypesMap[LogType.Error]);
            errorToggle.OnButtonPressed += value => {
                _config.logTypesMap[LogType.Error] = value;
                RefreshFilteredLogs();
            };
            logFilterToggles.Add(errorToggle);
            
            // Subscribe on refresh to increase counters
            OnLogCounterRefreshedEvent.AddListener(() =>
            {
                infoToggle.Text = _logCounters.comments.ToString();
                warningToggle.Text = _logCounters.warnings.ToString();
                errorToggle.Text = _logCounters.errors.ToString();
            });
            
            return container;
        }
        
        private VisualElement BuildTrackedScenesUI() // Has been removed from the UI for now, but keep it for now in case we want to simply refactor the visuals in the future.
        {
            var container = new VisualElement();
            container.AddToClassList("tracked-list-container");
            
            // Add list title
            container.Add(BuildTrackedListTitleUI("Scenes"));
            
            // Add list view
            var scenes = _config.scenesMap.Keys.ToList(); 
            var listView = new ListView(
                scenes,
                26,
                () => new ToggleListItem(),
                (elem, i) =>
                {
                    var itemElem = (ToggleListItem)elem;
                    itemElem.Set(   
                        _config.scenesMap[scenes[i]],
                        Artifice_SCR_CommonResourcesHolder.instance.UnityIcon,
                        scenes[i]
                    );
            
                    // Refresh is never called unless the window is rebuild, so I can consider binding to be permanent
                    itemElem.Toggle.RegisterValueChangedCallback(evt =>
                    {
                        _config.scenesMap[scenes[i]] = evt.newValue;
                        RefreshFilteredLogs();
                    });

                    // Subscribe to increase count
                    OnLogCounterRefreshedEvent.AddListener(() =>
                    {
                        if (_logCounters.scenesMap.ContainsKey(scenes[i]))
                            itemElem.CountLabel.text = _logCounters.scenesMap[scenes[i]].ToString();
                        else
                            itemElem.CountLabel.text = "0";
                    });
                }
            );
            
            container.Add(listView);
            
            return container;
        }
        
        private VisualElement BuildTrackedAssetFoldersUI()
        {
            var container = new VisualElement();
            container.AddToClassList("tracked-list-container");
            container.AddToClassList("space-bottom");
            
            // Add list title
            var header = BuildTrackedListTitleUI("Assets");
            container.Add(header);
            
            // Add list view
            var assetPaths = _config.assetPathsMap.Keys.ToList();
            ListView listView = null;
            listView = new ListView(
                assetPaths,
                26,
                () => new ToggleListItem(),
                (elem, i) =>
                {
                    var itemElem = (ToggleListItem)elem;
                    itemElem.Set(
                        _config.assetPathsMap[assetPaths[i]],
                        Artifice_SCR_CommonResourcesHolder.instance.FolderIcon,
                        assetPaths[i]
                    );
                    
                    // Subscribe change, to refresh filters
                    itemElem.Toggle.RegisterValueChangedCallback(evt =>
                    {
                        // Get reference to validator
                        _config.assetPathsMap[assetPaths[i]] = evt.newValue;
                        RefreshFilteredLogs();
                    });
                    
                    // Subscribe for right click, to allow removing
                    itemElem.RegisterCallback<MouseDownEvent>(evt =>
                    {
                        if (evt.button == 1)
                        {
                            var genericMenu = new GenericMenu();
                            genericMenu.AddItem(new GUIContent("Remove path"), false, () => AssetPaths_RemoveItem(listView, assetPaths[i]));
                            genericMenu.ShowAsContext();
                        }
                    });
                }
            );
            container.Add(listView);
            
            OnLogCounterRefreshedEvent.AddListener(() =>
            {
                var children = listView.Query(className: "unity-list-view__item").ToList();
                for (var i = 0; i < children.Count; i++)
                {
                    var child = (ToggleListItem)children[i];
                    var assetPath = child.Get_Text();
                    if (string.IsNullOrEmpty(assetPath))
                        continue;
                    
                    child.CountLabel.text = _logCounters.assetPathsMap[assetPath].ToString();
                }
            });
            
            // Add context menu options
            var validatorWeakReference = new WeakReference<Artifice_EditorWindow_Validator>(this);
            header.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (evt.button == 1)
                {
                    var genericMenu = new GenericMenu();
                    genericMenu.AddItem(new GUIContent("Add Asset Path"), false, () =>
                    {
                        var path = EditorUtility.OpenFolderPanel("Select Folder", "Assets", "");
                        var relativePath = Artifice_Utilities.ConvertGlobalToRelativePath(path);
                        if (string.IsNullOrEmpty(relativePath))
                            return;
                        
                        if(validatorWeakReference.TryGetTarget(out var validator))
                            validator.AssetPaths_AddItem(listView, relativePath);
                    });
                    genericMenu.ShowAsContext();
                }
            });
            
            return container;
        }
        
        private VisualElement BuildTrackedValidatorTypesUI()
        {
            var container = new VisualElement();
            container.AddToClassList("tracked-list-container");
            container.AddToClassList("space-top");
            
            // Add list title
            container.Add(BuildTrackedListTitleUI("Validator Types"));
            
            // Add list view
            var validatorModules = _validatorModules;
            
            var listView = new ListView(
                validatorModules,
                26,
                () => new ToggleListItem(),
                (elem, i) =>
                {
                    var validatorTypeName = validatorModules[i].GetType().Name;

                    // Change display mode based on display on filters.
                    if (validatorModules[i].DisplayOnFilters)
                        elem.style.display = DisplayStyle.Flex;
                    else
                        elem.style.display = DisplayStyle.None;
                    
                    var itemElem = (ToggleListItem)elem;
                    itemElem.Set(
                        _config.validatorTypesMap[validatorTypeName],
                        Artifice_SCR_CommonResourcesHolder.instance.ScriptIcon,
                        validatorModules[i].DisplayName
                    );
                    
                    itemElem.Toggle.RegisterValueChangedCallback(evt =>
                    {
                        _config.validatorTypesMap[validatorTypeName] = evt.newValue;
                        RefreshFilteredLogs();
                    });
                    
                    // Subscribe to increase count
                    OnLogCounterRefreshedEvent.AddListener(() =>
                    {
                        if (_logCounters.validatorTypesMap.ContainsKey(validatorTypeName))
                            itemElem.CountLabel.text = _logCounters.validatorTypesMap[validatorTypeName].ToString();
                        else
                            itemElem.CountLabel.text = "0";
                    });
                }
            );
            container.Add(listView);
            
            return container;
        }
        
        private VisualElement BuildTrackedListTitleUI(string headerTitle)
        {
            var container = new VisualElement();
            container.AddToClassList("list-title-container");
            container.Add(new Label(headerTitle));
            return container;
        }
        
        private VisualElement BuildLogsUI()
        {
            var container = new VisualElement();
            container.AddToClassList("logs-list-container");
            
            _logsListView = new ListView(
                _filteredLogs,
                26,
                () => new ValidatorLogListItem(),
                (elem, i) =>
                {
                    var itemElem = (ValidatorLogListItem)elem;
                    itemElem.Set(_filteredLogs[i]);
                }
            );
            container.Add(_logsListView);
            
            return container;
        }
        
        #endregion
        
        #region Filter Methods

        private bool OnSelectedScenesFilter(ValidatorLog log)
        {
            return _config.scenesMap.TryGetValue(log.originLocationName, out var value) && value
                || log.originLocationName == "";
        }

        private bool OnSelectedValidatorTypesFilter(ValidatorLog log)
        {
            return _config.validatorTypesMap[log.originValidatorType.Name];
        }

        private bool OnLogTypeTogglesFilter(ValidatorLog log)
        {
            return _config.logTypesMap[log.logType];
        }

        private bool OnSelectedAssetPathFilter(ValidatorLog log)
        {
            foreach (var (folderPath, shouldShow) in _config.assetPathsMap)
                if (log.originLocationName.Contains(folderPath) && shouldShow)
                    return true;

            if (log.originLocationName == "")
                return true;

            return false;
        }
            
        private bool OnPrefabStageFilter(ValidatorLog log)
        {
            return log.originLocationName == PrefabStageKey &&
                OnSelectedValidatorTypesFilter(log) &&
                OnLogTypeTogglesFilter(log);
        }
        
        #endregion
        
        #region Custom Menu Implementation
        
        public void AddItemsToMenu(GenericMenu menu)
        {
            menu.AddItem(new GUIContent("Load Settings"), false, LoadSettingsFile);
        }

        private void LoadSettingsFile()
        {
            var defaultPath = AssetDatabase.GetAssetPath(_config);
            
            var globalPath = EditorUtility.OpenFilePanel("Find settings", defaultPath, "asset");
            var relativePath = Artifice_Utilities.ConvertGlobalToRelativePath(globalPath);
            if (string.IsNullOrEmpty(relativePath))
                return;
            
            EditorPrefs.SetString(ConfigPathKey, relativePath);
         
            Close();
            OpenWindow();
        }
        
        #endregion
        
        #region Utility
        
        private void AssetPaths_AddItem(ListView listView, string assetPath)
        {
            _logCounters.assetPathsMap[assetPath] = 0;
            
            _config.assetPathsMap.TryAdd(assetPath, true);
            EditorUtility.SetDirty(_config);
            
            listView.itemsSource.Add(assetPath);
            listView.RefreshItems();
        }
        private void AssetPaths_RemoveItem(ListView listView, string assetPath)
        {
            _logCounters.assetPathsMap[assetPath] = 0;
            
            _config.assetPathsMap.Remove(assetPath);
            EditorUtility.SetDirty(_config);
            
            listView.itemsSource.Remove(assetPath);
            listView.RefreshItems();
        }
        
        #endregion
    }
}
