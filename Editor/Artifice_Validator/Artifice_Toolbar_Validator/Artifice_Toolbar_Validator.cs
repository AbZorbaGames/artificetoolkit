using System;
using System.Collections.Generic;
using System.Reflection;
using ArtificeToolkit.Editor;
using ArtificeToolkit.Editor.Resources;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Artifice.Editor
{
    public class Artifice_Toolbar_Validator
    {
        #region FIELDS

        private static readonly Type ToolbarType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.Toolbar");
        private static ScriptableObject _currentToolbar;
        private const string ToolbarLeft = "ToolbarZoneLeftAlign";
        
        private static bool _isEnabled;
        private static VisualElement _rootVisualElement;
        private static VisualElement _imGUIParentElement;
        private static IMGUIContainer _imGUIContainer;

        // Log Counter Labels
        private static readonly Dictionary<LogType, Label> _logLabelsMap = new();
        private static readonly Dictionary<LogType, VisualElement> _logIntensityElemMap = new();

        private const int MaxIntensityCounter = 8;
        private const string StylesheetNameForUnity6 = "Toolbar Validator for Unity 6";
        
        #endregion

        [InitializeOnLoadMethod]
        private static void Init()
        {
            EditorApplication.delayCall -= DelayedInit;
            EditorApplication.delayCall += DelayedInit;
        }
        
        /// <summary> VisualElement Toolbar wont be build on [InitializeOnLoadMethod] time so initialize on delayed call. </summary>
        private static void DelayedInit()
        {
            if (_currentToolbar != null)
                return;

            var toolbars = Resources.FindObjectsOfTypeAll(ToolbarType);
            _currentToolbar = toolbars.Length > 0 ? (ScriptableObject)toolbars[0] : null;
            
            if (_currentToolbar == null)
                return;

            var rootFieldInfo = _currentToolbar.GetType().GetField("m_Root", BindingFlags.NonPublic | BindingFlags.Instance);
            var rootVisualElement = rootFieldInfo!.GetValue(_currentToolbar) as VisualElement;
            _rootVisualElement = rootVisualElement.Q<VisualElement>(ToolbarLeft);
         
            // Build UI
            BuildUI();
            
            // Subscribe on log counter refresh event
            Artifice_Validator.Instance.OnLogCounterRefreshedEvent.AddListener(delegate
            {
                var logCounters = Artifice_Validator.Instance.Get_LogCounters();
                UpdateLogButton(LogType.Log, logCounters.comments, new Color(0.95f, 0.95f, 0.92f, 1f));
                UpdateLogButton(LogType.Warning, logCounters.warnings, new Color(0.98f, 0.85f, 0.25f, 1f));
                UpdateLogButton(LogType.Error, logCounters.errors, new Color(0.85f, 0.2f, 0.2f, 1f));
            });
        }

        /// <summary> Updates based on LogType the corresponding elements with given values. </summary>
        private static void UpdateLogButton(LogType type, uint count, Color color)
        {
            // Log Type
            var logLabel = _logLabelsMap[type];
            logLabel.text = count.ToString();
                
            // Update Log Intensity
            var logIntensity = _logIntensityElemMap[type];
            var normalizedAlpha = Mathf.Clamp01(count / (float)MaxIntensityCounter); // Normalize to [0,1]
            logIntensity.style.backgroundColor = new StyleColor(new Color(color.r, color.g, color.b, normalizedAlpha));
        }
        
        #region BUILD UI
        
        /* Build UI */
        private static void BuildUI()
        {
            _rootVisualElement.styleSheets.Add(Artifice_Utilities.GetGlobalStyle());
            
#if UNITY_6000_0_OR_NEWER
            _rootVisualElement.styleSheets.Add(Artifice_Utilities.GetStyleByName(StylesheetNameForUnity6));
#else
            _rootVisualElement.styleSheets.Add(Artifice_Utilities.GetStyle(typeof(Artifice_Toolbar_Validator)));
#endif

            var container = new VisualElement();
            container.AddToClassList("main-container");
            _rootVisualElement.Add(container);
            
            // Create log/warning/error icons and update count based on validator.
            container.Add(BuildUI_LogButton(LogType.Log, Artifice_SCR_CommonResourcesHolder.instance.CommentIcon));
            container.Add(BuildUI_LogButton(LogType.Warning, Artifice_SCR_CommonResourcesHolder.instance.WarningIcon));
            container.Add(BuildUI_LogButton(LogType.Error, Artifice_SCR_CommonResourcesHolder.instance.ErrorIcon));
            
            container.RegisterCallback<MouseDownEvent>(evt =>
            {
                if (EditorWindow.HasOpenInstances<Artifice_EditorWindow_Validator>())
                {
                    var window = EditorWindow.GetWindow<Artifice_EditorWindow_Validator>();
                    window.Close();
                }
                else
                   EditorWindow.GetWindow<Artifice_EditorWindow_Validator>();
            });
        }

        /* Build UI */
        private static VisualElement BuildUI_LogButton(LogType type, Sprite sprite)
        {
            var container = new VisualElement();
            container.AddToClassList("log-button");

            var image = new Image();
            image.sprite = sprite;
            container.Add(image);

            var label = new Label("0");
            container.Add(label);
            
            // Add label to log label dict.
            _logLabelsMap.TryAdd(type, label);
            
            // Add bottom line intensity elem
            var intensityElem = new VisualElement();
            intensityElem.AddToClassList("intensity-bar");
            container.Add(intensityElem);

            _logIntensityElemMap.TryAdd(type, intensityElem);
            
            return container;
        }
        
        #endregion
    }
}