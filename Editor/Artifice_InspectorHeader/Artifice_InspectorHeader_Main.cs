using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Mono.Cecil;
using UnityEditor;

namespace ArtificeToolkit.Editor.Artifice_InspectorHeader
{
    /// <summary> The main class responsible for handling the inspector Docks. Initializes on load. </summary>
    public static class Artifice_InspectorHeader_Main
    {
        #region FIELDS

        private static readonly List<Artifice_InspectorHeader_Dock> Docks = new();

        private static readonly Type InspectorWindowType =
            typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.InspectorWindow");

        private static readonly FieldInfo AllInspectorsFieldInfo =
            InspectorWindowType.GetField("m_AllInspectors", BindingFlags.NonPublic | BindingFlags.Static);

        private const string MenuPath = "Artifice Toolkit/";
        private const string MenuInspectorHeaderOn = MenuPath + "\u2610 Toggle Inspector Header/On";
        private const string MenuInspectorHeaderOff = MenuPath + "\u2610 Toggle Inspector Header/Off";
        private const int MenuItemPriority = 12;
        private const string EditorPrefKeyForToolEnabledState = "InspectorHeader EnabledState";
        private static bool _isEnabled;

        #endregion

        public static List<Artifice_InspectorHeader_Dock> GetDocks() => Docks;

        [InitializeOnLoadMethod]
        private static void Init()
        {
            _isEnabled = EditorPrefs.GetBool(EditorPrefKeyForToolEnabledState, false);
            if (_isEnabled == false)
                return;
            EditorApplication.delayCall -= OnInit;
            EditorApplication.delayCall += OnInit;
        }

        private static void OnInit()
        {
            try
            {
                SubscribeToCallbacks();
            }
            catch
            {
                EditorApplication.delayCall -= OnInit;
                EditorApplication.delayCall += OnInit;
            }
        }

        private static void DeInit()
        {
            UnSubscribeToCallbacks();
            foreach (var dock in Docks)
                dock.RemoveGUI();

            Docks.Clear();
        }

        private static void SubscribeToCallbacks()
        {
            EditorApplication.update -= RefreshInspectorWindows;
            EditorApplication.update += RefreshInspectorWindows;
            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.update += OnEditorUpdate;
            Selection.selectionChanged -= OnSelectionChanged;
            Selection.selectionChanged += OnSelectionChanged;
        }

        private static void UnSubscribeToCallbacks()
        {
            EditorApplication.update -= RefreshInspectorWindows;
            EditorApplication.update -= OnEditorUpdate;
            Selection.selectionChanged -= OnSelectionChanged;
        }


        private static void RefreshInspectorWindows()
        {
            var inspectorWindows = (IList)AllInspectorsFieldInfo.GetValue(InspectorWindowType);

            if (inspectorWindows is not { Count: > 0 })
            {
                Docks.Clear();
                return;
            }

            foreach (EditorWindow inspectorWindow in inspectorWindows)
                if (!InspectorHasDock(inspectorWindow))
                    Docks.Add(new Artifice_InspectorHeader_Dock(inspectorWindow, Selection.activeObject));

            for (var i = Docks.Count - 1; i >= 0; i--)
                if (!Docks[i].InspectorWindow)
                    Docks.RemoveAt(i);
        }

        private static void OnEditorUpdate()
        {
            foreach (var dock in Docks)
                dock.Update();
        }

        private static void OnSelectionChanged()
        {
            foreach (var dock in Docks)
            {
                if (!dock.IsInspectorLocked())
                    dock.SetDockSelectionToObject(Selection.activeObject);

                dock.Update();
            }
        }

        private static bool InspectorHasDock(EditorWindow inspector)
        {
            foreach (var dock in Docks)
            {
                if (dock.InspectorWindow.GetInstanceID() == inspector.GetInstanceID())
                    return true;
            }

            return false;
        }

        #region Utilities

        private static void EnableTool()
        {
            _isEnabled = true;
            EditorPrefs.SetBool(EditorPrefKeyForToolEnabledState, _isEnabled);
            OnInit();
        }

        private static void DisableTool()
        {
            _isEnabled = false;
            EditorPrefs.SetBool(EditorPrefKeyForToolEnabledState, _isEnabled);
            DeInit();
        }

        public static bool IsEnabled()
        {
            return _isEnabled;
        }

        public static void SetEnabled(bool option)
        {
            if(option)
                EnableTool();
            else
                DisableTool();
        }
        
        #endregion
    }
}