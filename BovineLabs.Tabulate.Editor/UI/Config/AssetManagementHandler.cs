// <copyright file="AssetManagementPanel.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Tabulate.Editor.UI.Config
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using BovineLabs.Tabulate.Editor.Core;
    using BovineLabs.Tabulate.Editor.Utilities;
    using UnityEditor;
    using UnityEditor.Search;
    using UnityEngine;
    using UnityEngine.UIElements;
    using Object = UnityEngine.Object;
    using ObjectField = UnityEditor.UIElements.ObjectField;

    /// <summary>
    /// UI component for managing the asset list in sheet definitions.
    /// </summary>
    public class AssetManagementHandler : VisualElement
    {
        private readonly SheetDefinition sheetDefinition;
        private readonly ListView assetListView;
        private readonly Button cleanupButton;
        private readonly Button clearAllButton;
        private readonly Label assetCountLabel;
        private readonly Label panelTitleLabel;
        private readonly VisualElement dropOverlay;
        private readonly EnumField assetModeField;
        private readonly TextField customSearchField;
        private readonly Button searchHelpButton;
        private readonly VisualElement customSearchContainer;
        private readonly Action? onAssetsChanged;

        private readonly List<Object?> workingAssetList = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="AssetManagementHandler"/> class.
        /// </summary>
        /// <param name="sheetDefinition">The sheet definition to manage assets for.</param>
        /// <param name="onAssetsChanged">Callback invoked when assets are modified.</param>
        public AssetManagementHandler(SheetDefinition sheetDefinition, Action? onAssetsChanged = null)
        {
            this.LoadUXML();

            this.sheetDefinition = sheetDefinition;
            this.onAssetsChanged = onAssetsChanged;

            this.assetListView = this.Q<ListView>("asset-list-view");
            this.cleanupButton = this.Q<Button>("cleanup-button");
            this.clearAllButton = this.Q<Button>("clear-all-button");
            this.assetCountLabel = this.Q<Label>("asset-count-label");
            this.panelTitleLabel = this.Q<Label>("panel-title");
            this.dropOverlay = this.Q<VisualElement>("drop-overlay");
            this.assetModeField = this.Q<EnumField>("asset-mode-field");
            this.customSearchField = this.Q<TextField>("custom-search-field");
            this.searchHelpButton = this.Q<Button>("search-help-button");
            this.customSearchContainer = this.Q<VisualElement>("custom-search-container");

            this.style.flexGrow = 1;

            this.SetupListView();
            this.SetupEventHandlers();
            this.SetupDragAndDrop();
            this.SetupAssetModeControls();

            this.UpdatePanelTitle();
            this.LoadWorkingAssetList();
            this.UpdateUI();
        }

        /// <summary>
        /// Applies the working asset list to the sheet definition.
        /// Only stores assets for Manual mode - Automatic and Custom modes use live discovery.
        /// </summary>
        public void ApplyChanges()
        {
            // Only store assets for Manual mode
            if (this.sheetDefinition.AssetManagementMode == AssetManagementMode.Manual)
            {
                // Clear existing managed assets
                this.sheetDefinition.ClearManagedAssets();

                // Add all assets from working list
                foreach (var asset in this.workingAssetList)
                {
                    if (asset == null)
                    {
                        continue;
                    }

                    if (asset is GameObject gameObject)
                    {
                        this.sheetDefinition.AddGameObject(gameObject);
                    }
                    else if (asset is ScriptableObject scriptableObject)
                    {
                        this.sheetDefinition.AddScriptableObject(scriptableObject);
                    }
                }

                EditorUtility.SetDirty(this.sheetDefinition);
            }

            this.onAssetsChanged?.Invoke();
        }

        private void LoadUXML()
        {
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(EditorConstants.AssetPath + "AssetManagementPanel.uxml");
            visualTree.CloneTree(this);
        }

        private void SetupListView()
        {
            this.assetListView.reorderable = true;
            this.assetListView.reorderMode = ListViewReorderMode.Animated;

            this.assetListView.makeItem = () =>
            {
                var container = new VisualElement();
                container.AddToClassList("asset-list-item");

                var objectField = new ObjectField();
                objectField.AddToClassList("asset-object-field");
                objectField.allowSceneObjects = false;
                container.Add(objectField);

                return container;
            };

            this.assetListView.bindItem = (element, index) =>
            {
                var objectField = element.Q<ObjectField>();

                if (objectField != null && index < this.workingAssetList.Count)
                {
                    var asset = this.workingAssetList[index];
                    objectField.value = asset;
                    objectField.objectType = this.GetExpectedAssetType();

                    // Handle value changes
                    objectField.RegisterValueChangedCallback(evt =>
                    {
                        if (index < this.workingAssetList.Count)
                        {
                            if (this.IsValidAsset(evt.newValue))
                            {
                                this.workingAssetList[index] = evt.newValue;
                                this.UpdateAssetCount();
                                this.ApplyChanges();
                            }
                            else
                            {
                                // Revert to previous value if invalid
                                objectField.value = evt.previousValue;
                                if (evt.newValue != null)
                                {
                                    var expectedType = this.GetExpectedAssetType().Name;
                                    Debug.LogWarning($"Invalid asset type. Expected {expectedType}, got {evt.newValue.GetType().Name}");
                                }
                            }
                        }
                    });
                }
            };

            this.assetListView.fixedItemHeight = 22;
            this.assetListView.itemsSource = this.workingAssetList;

            // Handle add/remove from built-in footer
            this.assetListView.itemsAdded += this.OnItemsAdded;
            this.assetListView.itemsRemoved += this.OnItemsRemoved;
        }

        private void SetupEventHandlers()
        {
            this.cleanupButton.clicked += this.OnCleanupClicked;
            this.clearAllButton.clicked += this.OnClearAllClicked;
            this.searchHelpButton.clicked += this.OpenSearchWindow;
        }

        private void LoadWorkingAssetList()
        {
            this.workingAssetList.Clear();

            if (this.sheetDefinition == null)
            {
                return;
            }

            if (this.sheetDefinition.AssetManagementMode == AssetManagementMode.Manual)
            {
                // For Manual mode, load the stored managed assets
                var managedAssets = this.sheetDefinition.GetManagedAssets();
                this.workingAssetList.AddRange(managedAssets.Cast<Object?>());
            }
            else
            {
                // For Automatic and Custom modes, show live discovery results
                var discoveredAssets = AssetPopulationHelper.PopulateFromCurrentRules(this.sheetDefinition);
                this.workingAssetList.AddRange(discoveredAssets.Cast<Object?>());
            }
        }

        private void UpdateUI()
        {
            this.UpdateAssetCount();
            this.UpdateButtonStates();
            this.assetListView.Rebuild();
        }

        private void UpdateAssetCount()
        {
            var validAssets = this.workingAssetList.Count(asset => asset != null);
            var totalAssets = this.workingAssetList.Count;
            this.assetCountLabel.text = totalAssets == validAssets
                ? $"Assets: {totalAssets}"
                : $"Assets: {validAssets}/{totalAssets} (with {totalAssets - validAssets} missing)";
        }

        private void UpdatePanelTitle()
        {
            if (this.sheetDefinition != null)
            {
                this.panelTitleLabel.text = $"Asset Management - {this.sheetDefinition.Type}";
            }
        }

        private void UpdateButtonStates()
        {
            var hasAssets = this.workingAssetList.Count > 0;
            var isManualMode = this.sheetDefinition.AssetManagementMode == AssetManagementMode.Manual;

            this.cleanupButton.SetEnabled(isManualMode && hasAssets);
            this.clearAllButton.SetEnabled(isManualMode && hasAssets);

            // List view is read-only for Automatic/Custom modes (shows preview), editable for Manual mode
            this.assetListView.SetEnabled(isManualMode);
        }

        private void OnItemsAdded(IEnumerable<int> indices)
        {
            // ListView automatically adds null items, so we just need to update UI
            this.UpdateUI();
            this.ApplyChanges();
        }

        private void OnItemsRemoved(IEnumerable<int> indices)
        {
            // Items are already removed by ListView, just update UI
            this.UpdateUI();
            this.ApplyChanges();
        }

        private void OnCleanupClicked()
        {
            var removedCount = this.workingAssetList.RemoveAll(asset => asset == null);
            if (removedCount > 0)
            {
                this.UpdateUI();
                this.ApplyChanges();
            }
        }

        private void OnClearAllClicked()
        {
            if (EditorUtility.DisplayDialog("Clear All Assets",
                "Are you sure you want to remove all managed assets? This will revert to auto-discovery mode.",
                "Clear All", "Cancel"))
            {
                this.workingAssetList.Clear();
                this.UpdateUI();
                this.ApplyChanges();
            }
        }

        private bool IsValidAsset(Object? asset)
        {
            if (asset == null)
            {
                return true; // Allow null for removal
            }

            return this.sheetDefinition.Type switch
            {
                SheetType.GameObject => asset is GameObject,
                SheetType.ScriptableObject => asset is ScriptableObject,
                _ => false,
            };
        }

        private Type GetExpectedAssetType()
        {
            return this.sheetDefinition.Type switch
            {
                SheetType.GameObject => typeof(GameObject),
                SheetType.ScriptableObject => typeof(ScriptableObject),
                _ => typeof(Object),
            };
        }

        private void SetupDragAndDrop()
        {
            // Initially hidden
            this.dropOverlay.style.display = DisplayStyle.None;

            // Add drag event handlers to the root element
            this.RegisterCallback<DragEnterEvent>(_ =>
            {
                var hasValidAssets = DragAndDrop.objectReferences.Any(obj =>
                    obj != null && !string.IsNullOrEmpty(AssetDatabase.GetAssetPath(obj)) && this.IsValidAsset(obj));

                if (hasValidAssets)
                {
                    DragAndDrop.AcceptDrag();
                    this.dropOverlay.style.display = DisplayStyle.Flex;
                }
            });

            this.RegisterCallback<DragLeaveEvent>(_ =>
            {
                this.dropOverlay.style.display = DisplayStyle.None;
            });

            this.RegisterCallback<DragUpdatedEvent>(_ =>
            {
                var hasValidAssets = DragAndDrop.objectReferences.Any(obj =>
                    obj != null && !string.IsNullOrEmpty(AssetDatabase.GetAssetPath(obj)) && this.IsValidAsset(obj));

                DragAndDrop.visualMode = hasValidAssets ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Rejected;
            });

            this.RegisterCallback<DragPerformEvent>(_ =>
            {
                this.dropOverlay.style.display = DisplayStyle.None;

                var allObjects = DragAndDrop.objectReferences;
                var validAssets = allObjects.Where(obj =>
                    obj != null && !string.IsNullOrEmpty(AssetDatabase.GetAssetPath(obj)) && this.IsValidAsset(obj)).ToArray();

                if (validAssets.Length > 0)
                {
                    var addedCount = 0;
                    foreach (var asset in validAssets)
                    {
                        if (!this.workingAssetList.Contains(asset))
                        {
                            this.workingAssetList.Add(asset);
                            addedCount++;
                        }
                    }

                    if (addedCount > 0)
                    {
                        this.UpdateUI();
                        this.ApplyChanges();
                    }
                }
                else if (allObjects.Length > 0)
                {
                    var expectedType = this.GetExpectedAssetType().Name;
                    Debug.LogWarning($"Invalid asset types dropped. Expected {expectedType} assets only");
                }
            });
        }

        private void SetupAssetModeControls()
        {
            this.assetModeField.Init(this.sheetDefinition.AssetManagementMode);
            this.assetModeField.RegisterValueChangedCallback(this.OnAssetModeChanged);

            this.customSearchField.value = this.sheetDefinition.CustomSearchString;
            this.customSearchField.isDelayed = true;
            this.customSearchField.tooltip = "Enter Unity search string (e.g., 't:AudioClip', 'name:Player', 'l:Environment')";
            this.searchHelpButton.tooltip = "Open Unity Search window to build search queries";
            this.customSearchField.RegisterValueChangedCallback(evt =>
            {
                if (this.sheetDefinition.AssetManagementMode == AssetManagementMode.Custom)
                {
                    this.sheetDefinition.CustomSearchString = evt.newValue;
                    EditorUtility.SetDirty(this.sheetDefinition);

                    // Auto-refresh the working list
                    this.LoadWorkingAssetList();
                    this.UpdateUI();
                }
            });

            this.UpdateCustomSearchFieldVisibility();
        }

        private void OnAssetModeChanged(ChangeEvent<Enum> evt)
        {
            var newMode = (AssetManagementMode)evt.newValue;
            var currentMode = this.sheetDefinition.AssetManagementMode;

            if (newMode == currentMode)
            {
                return;
            }

            var modeNames = new Dictionary<AssetManagementMode, string>
            {
                { AssetManagementMode.Manual, "Manual" },
                { AssetManagementMode.Automatic, "Automatic" },
                { AssetManagementMode.Custom, "Custom" },
            };

            var hasCurrentAssets = this.workingAssetList.Count > 0;

            var shouldConfirm = hasCurrentAssets && currentMode == AssetManagementMode.Manual;

            if (!shouldConfirm || EditorUtility.DisplayDialog("Change Asset Management Mode",
                $"Switching to {modeNames[newMode]} mode will clear all currently managed assets.\n\n" + "Are you sure you want to continue?", "Continue",
                "Cancel"))
            {
                this.sheetDefinition.AssetManagementMode = newMode;
                EditorUtility.SetDirty(this.sheetDefinition);

                // Clear managed assets when leaving Manual mode
                if (currentMode == AssetManagementMode.Manual)
                {
                    this.sheetDefinition.ClearManagedAssets();
                }

                // Reload the working asset list based on the new mode
                this.LoadWorkingAssetList();

                this.UpdateCustomSearchFieldVisibility();
                this.ApplyChanges();
                this.UpdateUI();
            }
            else
            {
                // Revert the UI field back to the current mode
                this.assetModeField.value = currentMode;
            }
        }

        private void UpdateCustomSearchFieldVisibility()
        {
            var isCustomMode = this.sheetDefinition.AssetManagementMode == AssetManagementMode.Custom;
            this.customSearchContainer.style.display = isCustomMode ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void OpenSearchWindow()
        {
            SearchService.ShowContextual("asset");
        }
    }
}