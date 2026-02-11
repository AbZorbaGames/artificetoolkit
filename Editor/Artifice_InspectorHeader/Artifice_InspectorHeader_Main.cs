using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;

namespace ArtificeToolkit.Editor.Artifice_InspectorHeader
{
    /// <summary> The main class responsible for handling the inspector Docks. Initializes on load. </summary>
    public static class Artifice_InspectorHeader_Main
    {
        #region FIELDS

        public static bool IsEnabled
        {
            get => EditorPrefs.GetBool("InspectorHeader EnabledState");
            set => EditorPrefs.SetBool("InspectorHeader EnabledState", value);
        }
        
        public static bool CategoryButtonsEnabled
        {
            get => EditorPrefs.GetBool("InspectorHeader CategoryButtonsEnabled");
            set => EditorPrefs.SetBool("InspectorHeader CategoryButtonsEnabled", value);
        }
        
        private static readonly List<Artifice_InspectorHeader_Dock> Docks = new();

        private static readonly Type InspectorWindowType =
            typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.InspectorWindow");

        private static readonly FieldInfo AllInspectorsFieldInfo =
            InspectorWindowType.GetField("m_AllInspectors", BindingFlags.NonPublic | BindingFlags.Static);

        #endregion

        public static List<Artifice_InspectorHeader_Dock> GetDocks() => Docks;

        [InitializeOnLoadMethod]
        private static void Init()
        {
            if (IsEnabled == false)
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

        public static void Set_IsEnabled(bool enabled)
        {
            IsEnabled = enabled;
            
            if(IsEnabled)
                OnInit();
            else
                DeInit();
            
            Artifice_Utilities.TriggerNextFrameReselection();
        }
        
        #endregion
    }
}