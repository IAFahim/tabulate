// <copyright file="SheetDefinitionCreationDialog.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Tabulate.Editor.UI.Dialogs
{
    using System;
    using System.Linq;
    using BovineLabs.Tabulate.Editor.Core;
    using UnityEditor;
    using UnityEditor.Search;
    using UnityEngine;
    using UnityEngine.UIElements;

    public class SheetDefinitionCreationDialog : EditorWindow
    {
        private SheetType selectedSheetType = SheetType.GameObject;
        private AssetManagementMode selectedAssetManagementMode = AssetManagementMode.Manual;
        private string customSearchString = string.Empty;
        private TextField customSearchField = null!;

        private Action<SheetDefinition>? onSheetCreated;

        public static void ShowDialog(Action<SheetDefinition>? onCreated = null)
        {
            var window = CreateInstance<SheetDefinitionCreationDialog>();
            window.onSheetCreated = onCreated;
            window.titleContent = new GUIContent("Create Sheet Definition");
            window.Show();

            // Center the window
            var position = window.position;
            position.center = new Vector2(Screen.currentResolution.width / 2f, Screen.currentResolution.height / 2f);
            window.position = position;
        }

        public void CreateGUI()
        {
            this.minSize = new Vector2(400, 240);
            this.maxSize = new Vector2(400, 240);

            // Load UXML
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(EditorConstants.AssetPath + "SheetDefinitionCreationDialog.uxml");
            visualTree.CloneTree(this.rootVisualElement);

            // Get UI element references
            var sheetTypeField = this.rootVisualElement.Q<EnumField>("sheet-type-field");
            var assetModeField = this.rootVisualElement.Q<EnumField>("asset-mode-field");
            var modeHelpButton = this.rootVisualElement.Q<Button>("mode-help-button");
            var customSearchContainer = this.rootVisualElement.Q<VisualElement>("custom-search-container");
            this.customSearchField = this.rootVisualElement.Q<TextField>("custom-search-field");
            var searchHelpButton = this.rootVisualElement.Q<Button>("search-help-button");
            var createButton = this.rootVisualElement.Q<Button>("create-button");
            var cancelButton = this.rootVisualElement.Q<Button>("cancel-button");

            // Initialize values
            sheetTypeField.Init(this.selectedSheetType);
            assetModeField.Init(this.selectedAssetManagementMode);
            this.customSearchField.value = this.customSearchString;

            // Set up help button for asset management modes
            modeHelpButton.tooltip = "Manual: Add/remove assets manually via drag-drop or list controls\n" +
                                   "Automatic: Assets automatically discovered based on column target types\n" +
                                   "Custom: Use custom Unity search strings to find assets";

            // Set up event handlers
            sheetTypeField.RegisterValueChangedCallback(evt =>
            {
                this.selectedSheetType = (SheetType)evt.newValue;
            });

            assetModeField.RegisterValueChangedCallback(evt =>
            {
                this.selectedAssetManagementMode = (AssetManagementMode)evt.newValue;
                this.UpdateCustomSearchFieldVisibility(customSearchContainer);
            });

            this.customSearchField.RegisterValueChangedCallback(evt =>
            {
                this.customSearchString = evt.newValue;
            });

            createButton.clicked += this.CreateSheetDefinition;
            cancelButton.clicked += this.Close;

            // Set up button event handlers
            modeHelpButton.clicked += () => { /* Tooltip handles the help display */ };
            searchHelpButton.clicked += this.OpenSearchWindow;
            searchHelpButton.tooltip = "Open Unity Search window to build search queries";

            // Initial visibility update
            this.UpdateCustomSearchFieldVisibility(customSearchContainer);
        }

        private void CreateSheetDefinition()
        {
            try
            {
                // Show save file panel with sheet type specific default name
                var defaultFileName = $"{this.selectedSheetType}SheetDefinition.asset";

                var savePath = EditorUtility.SaveFilePanel("Save Sheet Definition", "Assets", defaultFileName, "asset");

                if (string.IsNullOrEmpty(savePath))
                {
                    // User cancelled
                    return;
                }

                // Convert absolute path to relative path from project root
                var relativePath = FileUtil.GetProjectRelativePath(savePath);
                if (string.IsNullOrEmpty(relativePath))
                {
                    EditorUtility.DisplayDialog("Error", "Please save the asset within the project folder.", "OK");
                    return;
                }

                // Create the SheetDefinition asset
                var sheetDefinition = CreateInstance<SheetDefinition>();
                sheetDefinition.Type = this.selectedSheetType;
                sheetDefinition.AssetManagementMode = this.selectedAssetManagementMode;
                sheetDefinition.CustomSearchString = this.customSearchString;

                // Create and save the asset
                AssetDatabase.CreateAsset(sheetDefinition, relativePath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                // Focus the created asset in the Project window
                EditorUtility.FocusProjectWindow();
                Selection.activeObject = sheetDefinition;

                // Notify callback
                this.onSheetCreated?.Invoke(sheetDefinition);

                this.Close();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to create SheetDefinition: {ex.Message}");
                EditorUtility.DisplayDialog("Error", $"Failed to create SheetDefinition:\n{ex.Message}", "OK");
            }
        }

        private void UpdateCustomSearchFieldVisibility(VisualElement customSearchContainer)
        {
            if (this.selectedAssetManagementMode == AssetManagementMode.Custom)
            {
                customSearchContainer.style.display = DisplayStyle.Flex;
            }
            else
            {
                customSearchContainer.style.display = DisplayStyle.None;
            }
        }

        private void OpenSearchWindow()
        {
            SearchService.ShowContextual("asset");
        }
    }
}