using System;
using System.Collections.Generic;
using System.Reflection;
using Artifice_Editor;
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
        private static readonly Dictionary<LogType, Label> _logLabels = new();
        
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
            
            BuildUI();
        }
        
        /* Build UI */
        private static void BuildUI()
        {
            _rootVisualElement.styleSheets.Add(Artifice_Utilities.GetGlobalStyle());
            _rootVisualElement.styleSheets.Add(Artifice_Utilities.GetStyle(typeof(Artifice_Toolbar_Validator)));
            
            var container = new VisualElement();
            container.AddToClassList("main-container");
            _rootVisualElement.Add(container);
            
            // Create log/warning/error icons and update count based on validator.
            container.Add(CreateLogButton(LogType.Log, Artifice_SCR_CommonResourcesHolder.instance.CommentIcon));
            container.Add(CreateLogButton(LogType.Warning, Artifice_SCR_CommonResourcesHolder.instance.WarningIcon));
            container.Add(CreateLogButton(LogType.Error, Artifice_SCR_CommonResourcesHolder.instance.ErrorIcon));
            
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
            
            
            // Subscribe on log counter refresh event
            Artifice_Validator.Instance.OnLogCounterRefreshedEvent.AddListener(delegate
            {
                var logCounters = Artifice_Validator.Instance.Get_LogCounters();
                
                if (_logLabels.TryGetValue(LogType.Log, out var commentsLabel))
                    commentsLabel.text = logCounters.comments.ToString();
                
                if (_logLabels.TryGetValue(LogType.Warning, out var warningsLabel))
                    warningsLabel.text = logCounters.warnings.ToString();
                
                if (_logLabels.TryGetValue(LogType.Error, out var errorsLabel))
                    errorsLabel.text = logCounters.errors.ToString();
            });
        }

        /* Build UI */
        private static VisualElement CreateLogButton(LogType type, Sprite sprite)
        {
            var container = new VisualElement();
            container.AddToClassList("log-button");

            var image = new Image();
            image.sprite = sprite;
            container.Add(image);

            var label = new Label("0");
            container.Add(label);
            
            // Add label to log label dict.
            _logLabels.TryAdd(type, label);
            
            return container;
        }
    }
}