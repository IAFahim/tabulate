// <copyright file="DomainReloadWindowManager.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Tabulate.Editor.Core
{
    using BovineLabs.Tabulate.Editor.UI;
    using BovineLabs.Tabulate.Editor.UI.Config;
    using BovineLabs.Tabulate.Editor.UI.Dialogs;
    using UnityEditor;
    using UnityEngine;

    /// <summary>
    /// Manages window lifecycle during domain reload events.
    /// Closes non-base windows to prevent lost references and invalid states.
    /// </summary>
    [InitializeOnLoad]
    public static class DomainReloadWindowManager
    {
        static DomainReloadWindowManager()
        {
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
        }

        private static void OnBeforeAssemblyReload()
        {
            SaveAllColumnLayouts();
            CloseSecondaryWindows();
        }

        /// <summary>
        /// Saves column layouts from all open UnitySheetEditor windows before domain reload.
        /// This ensures column order and width changes are persisted.
        /// </summary>
        private static void SaveAllColumnLayouts()
        {
            foreach (var editor in Resources.FindObjectsOfTypeAll<UnitySheetEditor>())
            {
                if (editor != null)
                {
                    try
                    {
                        editor.SyncColumnLayoutToAsset();
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogWarning($"Failed to save column layout during domain reload: {ex.Message}");
                    }
                }
            }

            // Save all assets to persist the changes
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// Closes all secondary windows (non-base UnitySheetEditor windows).
        /// This prevents references from being lost and ensures clean state after domain reload.
        /// </summary>
        private static void CloseSecondaryWindows()
        {
            foreach (var dialog in Resources.FindObjectsOfTypeAll<SheetConfigurationDialog>())
            {
                Close(dialog);
            }

            foreach (var dialog in Resources.FindObjectsOfTypeAll<QuickFormulaDialog>())
            {
                Close(dialog);
            }

            foreach (var dialog in Resources.FindObjectsOfTypeAll<SheetDefinitionCreationDialog>())
            {
                Close(dialog);
            }

            // Note: We deliberately keep UnitySheetEditor windows open as they contain
            // serialized state that persists through domain reload and are the base windows
        }

        private static void Close(EditorWindow editorWindow)
        {
            if (editorWindow == null)
            {
                return;
            }

            try
            {
                editorWindow.Close();
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"Failed to close {editorWindow.GetType()} during domain reload: {ex.Message}");
            }
        }
    }
}