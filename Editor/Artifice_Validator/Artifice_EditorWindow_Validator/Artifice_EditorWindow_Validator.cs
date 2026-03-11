using System;
using System.Collections.Generic;
using System.Linq;
using ArtificeToolkit.Editor.Resources;
using ArtificeToolkit.Editor.VisualElements;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

// ReSharper disable InconsistentNaming

namespace ArtificeToolkit.Editor
{
    public class Artifice_EditorWindow_Validator : EditorWindow, IHasCustomMenu
    {   
        #region Constants
        
        private const string MenuItemPath = "Artifice Toolkit/\u2713 Artifice Validator %&v";
        private const int MenuItemPriority = 23;

        #endregion
        
        #region Nested VisualElements
        
        /// <summary> Helper class to render repeatable list element </summary>
        private class ListItem : VisualElement
        {
            private readonly Image _image;
            private readonly Label _text;
            private readonly ScrollView _scrollView;

            public ListItem()
            {
                AddToClassList("list-item");

                _image = new Image();
                _image.AddToClassList("icon");

                // Create vertical scroll view for the text
                _scrollView = new ScrollView(ScrollViewMode.Vertical);
                _scrollView.verticalScrollerVisibility = ScrollerVisibility.Hidden;
                _scrollView.AddToClassList("text-scroll");

                _text = new Label();
                _text.AddToClassList("label");

                _scrollView.Add(_text);

                Add(_image);
                Add(_scrollView);
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

        /// <summary> Extends <see cref="ListItem"/> by representing more information for <see cref="Artifice_Validator.ValidatorLog"/> </summary>
        private class ValidatorLogListItem : ListItem
        {
            private readonly Label _objectNameLabel;
            private readonly Artifice_VisualElement_LabeledButton _autoFixButton;

            public ValidatorLogListItem() : base()
            {
                styleSheets.Add(Artifice_Utilities.GetGlobalStyle());
                
                // Create object label
                _objectNameLabel = new Label("Undefined");
                _objectNameLabel.AddToClassList("object-name-label");
                Add(_objectNameLabel);
                
                // Create autoFixButton
                _autoFixButton = new Artifice_VisualElement_LabeledButton("Auto Fix", null);
                _autoFixButton.AddToClassList("hide");
                Add(_autoFixButton);
            }

            public void Set(Artifice_Validator.ValidatorLog log)
            {
                base.Set(log.Sprite, log.Message);
                _objectNameLabel.text = log.OriginObject == null ? "" : log.OriginObject.name;

                if (log.HasAutoFix)
                {
                    // Show button and set callback
                    _autoFixButton.style.display = DisplayStyle.Flex;
                    _autoFixButton.SetAction(log.AutoFixAction);
                }
                else
                    _autoFixButton.style.display = DisplayStyle.None;
            }
        }
        
        #endregion
        
        #region FIELDS
 
        // Logs
        private readonly List<Artifice_Validator.ValidatorLog> _filteredLogs = new();
        
        // Filter mechanism
        private List<Func<Artifice_Validator.ValidatorLog, bool>> _filters;
        
        // Dynamic VisualElement References
        private ListView _logsListView;
        
        // Used for editor prefs
        public const string PrefabStageKey = "PrefabStage";
        
        public Artifice_SCR_ValidatorConfig _config;
        
        #endregion
            
        [MenuItem(MenuItemPath, priority = MenuItemPriority)]
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
            if (_config == null)
            {
                Close();
                return;
            }

            // Create GUI
            BuildUI();
        }
        
        /* Mono */
        private void Initialize()
        {
            _config = Artifice_Validator.Instance.Get_ValidatorConfig();
            
            // Initialize Filters
            _filters = new List<Func<Artifice_Validator.ValidatorLog, bool>>();
            // _filters.Add(log => OnSelectedScenesFilter(log) || OnSelectedAssetPathFilter(log));
            _filters.Add(OnSelectedValidatorTypesFilter);
            _filters.Add(OnLogTypeTogglesFilter);
            
            Artifice_Validator.Instance.OnLogsRefreshEvent.AddListener(OnLogsRefresh);
        }
        
        /// <summary> Visually refreshes log counters and filtered logs. </summary>
        private void OnLogsRefresh()
        {
            // Refresh Filtered logs
            RefreshFilteredLogs();
        }
        
        /// <summary> Clears and fills filtered logs based on all logs. </summary>
        private void RefreshFilteredLogs()
        {
            // Get logs from instance
            var logs = Artifice_Validator.Instance.Get_Logs();
            
            // Refresh filtered logs.
            _filteredLogs.Clear();
            foreach (var log in logs)
            {
                if(_filters.All(filter => filter.Invoke(log)))
                    _filteredLogs.Add(log);
            }
            _logsListView?.RefreshItems();
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
            // trackedContainers.Add(BuildTrackedAssetFoldersUI()); // Removed for now. It seems kind of useless with many validations being conflicting from asset space to scene space.
            trackedContainers.Add(BuildTrackedValidatorTypesUI());
            // Add to split-pane
            splitPane.Add(trackedContainers);
            
            // Build Error logs and add
            splitPane.Add(BuildLogsUI());
            
            RefreshFilteredLogs();
        }
        
        private VisualElement BuildHeaderUI()
        {
            var container = new VisualElement();
            container.AddToClassList("header-container");

            // Manual scan button
            var runScan = new Artifice_VisualElement_LabeledButton("Run Scan", () =>
            {
                Artifice_Validator.Instance.RefreshLogs();
            });
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
            Artifice_Validator.Instance.OnLogCounterRefreshedEvent.AddListener(() =>
            {
                var logCounters = Artifice_Validator.Instance.Get_LogCounters();
                infoToggle.Text = logCounters.comments.ToString();
                warningToggle.Text = logCounters.warnings.ToString();
                errorToggle.Text = logCounters.errors.ToString();
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
            var validatorModules = Artifice_Validator.Instance.Get_ValidatorModules();
            
            var listView = new ListView(
                validatorModules,
                26,
                () => new ToggleListItem(),
                (elem, i) =>
                {
                    var validatorTypeName = validatorModules[i].GetType().Name;

                    // Change display mode based on display on filters.
                    if (validatorModules[i].DisplayOnFiltersList)
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
                    Artifice_Validator.Instance.OnLogCounterRefreshedEvent.AddListener(() =>
                    {
                        var logCounters = Artifice_Validator.Instance.Get_LogCounters();
                        if (logCounters.validatorTypesMap.ContainsKey(validatorTypeName))
                            itemElem.CountLabel.text = logCounters.validatorTypesMap[validatorTypeName].ToString();
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
                27,
                () => new ValidatorLogListItem(),
                (elem, i) =>
                {
                    var itemElem = (ValidatorLogListItem)elem;
                    itemElem.Set(_filteredLogs[i]);
                }
            );
            
            _logsListView.selectionChanged += items =>
            {
                if (!items.Any())
                    return;
                
                var selected = (Artifice_Validator.ValidatorLog)items.First();

                var originObject = selected.OriginObject;
                
                if (originObject != null)
                {
                    Selection.SetActiveObjectWithContext(originObject, originObject);
                    EditorGUIUtility.PingObject(originObject);

                    if (originObject is Component originComponent)
                    {
                        // Collapse all other components instead of our focused component.
                        var components = originComponent.gameObject.GetComponents<Component>();
                        foreach (var component in components)
                            InternalEditorUtility.SetIsInspectorExpanded(component, component == originObject);
                    }

                    ActiveEditorTracker.sharedTracker.ForceRebuild();
                }
            };
            
            container.Add(_logsListView);
            
            return container;
        }
        
        #endregion
        
        #region Filter Methods
        
        private bool OnSelectedValidatorTypesFilter(Artifice_Validator.ValidatorLog log)
        {
            return _config.validatorTypesMap[log.OriginValidatorType.Name];
        }

        private bool OnLogTypeTogglesFilter(Artifice_Validator.ValidatorLog log)
        {
            return _config.logTypesMap[log.LogType];
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
            
            EditorPrefs.SetString(Artifice_Validator.ConfigPathKey, relativePath);
         
            // TODO: [zack] this does not apply the settings now.
            Close();
            OpenWindow();
        }
        
        #endregion
    }
}
