// <copyright file="UnitySheetEditor.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Tabulate.Editor.UI
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Reflection;
    using BovineLabs.Core.Editor;
    using BovineLabs.Core.Editor.Internal;
    using BovineLabs.Tabulate.Editor.Core;
    using BovineLabs.Tabulate.Editor.Formula;
    using BovineLabs.Tabulate.Editor.Handlers;
    using BovineLabs.Tabulate.Editor.Services;
    using BovineLabs.Tabulate.Editor.UI.Components;
    using BovineLabs.Tabulate.Editor.UI.Config;
    using BovineLabs.Tabulate.Editor.UI.Dialogs;
    using BovineLabs.Tabulate.Editor.Utilities;
    using BovineLabs.Tabulate.Editor.Variables;
    using UnityEditor;
    using UnityEditor.Callbacks;
    using UnityEditor.UIElements;
    using UnityEngine;
    using UnityEngine.UIElements;
    using Object = UnityEngine.Object;

    [SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1214:Readonly fields should appear before non-readonly fields", Justification = "Grouping")]
    public class UnitySheetEditor : EditorWindow
    {
        // Persistence data
        private const string LastSheetDefinitionKey = "UnitySheetEditor.LastSheetDefinition";

        private static readonly PropertyInfo DisplayIndexProperty = typeof(Column).GetProperty("displayIndex", BindingFlags.Instance | BindingFlags.NonPublic)!;

        private SheetDefinition? currentSheetDefinition;

        private ObjectField sheetDefinitionField = null!;
        private VisualElement noSheetMessage = null!;
        private VisualElement sheetContentContainer = null!;

        // Toolbar UI elements
        private ToolbarButton newButton = null!;
        private ToolbarButton refreshButton = null!;
        private ToolbarSearchField searchField = null!;
        private ToolbarMenu settingsMenu = null!;

        // Main content UI elements
        private MultiColumnListView sheetListView = null!;
        private VisualElement noDataMessage = null!;

        // Status bar UI elements
        private Label statusLabel = null!;

        // Variables sidebar controller
        private VariablesSidebarController variablesSidebarController = null!;

        // Data and controllers
        private readonly List<SheetRowData> rowData = new();
        private readonly Dictionary<int, FormulaColumnHandler> formulaHandlers = new();
        private readonly Dictionary<int, DataColumnHandler> dataHandlers = new();

        // Real-time formula system
        private readonly FormulaDependencyGraph dependencyGraph = new();
        private readonly Dictionary<int, PropertyColumnHandler> propertyHandlers = new();
        private bool isRecalculatingFormulas;

        // SerializedObject cache for performance
        private readonly Dictionary<Object, SerializedObject> serializedObjectCache = new();

        // Variable system
        private VariableEvaluator variableEvaluator = null!;
        private readonly VariableDependencyGraph variableDependencyGraph = new();

        private ColumnFormulaProcessor columnFormulaProcessor = null!;

        // Track formula display labels for direct updates
        private readonly Dictionary<(int ColumnId, int ObjectIndex), TextField> formulaLabels = new();

        // Search functionality
        private string currentSearchText = string.Empty;
        private readonly List<SheetRowData> filteredRowData = new();

        // Error handling system
        private ErrorController errorController = null!;

        [MenuItem(EditorMenus.RootMenu + "Tabulate/Editor")]
        public static void ShowWindow()
        {
            var window = GetWindow<UnitySheetEditor>("Unity Sheet Editor");
            window.Show();
        }

        [OnOpenAsset]
#if UNITY_6000_3_OR_NEWER
        public static bool OnOpenAsset(EntityId instanceID, int line)
        {
            var obj = EditorUtility.EntityIdToObject(instanceID);
#else
        public static bool OnOpenAsset(int instanceID, int line)
        {
            var obj = EditorUtility.InstanceIDToObject(instanceID);
#endif
            if (obj is SheetDefinition sheetDefinition)
            {
                var window = GetWindow<UnitySheetEditor>("Unity Sheet Editor");
                window.LoadSheetDefinition(sheetDefinition);
                window.Show();
                window.Focus();
                return true;
            }

            return false;
        }

        public void CreateGUI()
        {
            this.titleContent = new GUIContent("Unity Sheet Editor");

            this.variableEvaluator = new VariableEvaluator(this.GetOrCreatePropertyHandler);
            this.columnFormulaProcessor = new ColumnFormulaProcessor(this.GetOrCreatePropertyHandler);

            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(EditorConstants.AssetPath + "UnitySheetEditor.uxml");
            visualTree.CloneTree(this.rootVisualElement);

            this.SetupUIReferences();
            this.SetupToolbar();
            this.SetupEventHandlers();

            var sheetDefinition = this.GetLastUsedSheetDefinition();
            if (sheetDefinition != null)
            {
                this.LoadSheetDefinition(sheetDefinition);
            }
            else
            {
                this.UpdateUI();
            }
        }

        /// <summary>
        /// Syncs the current column layout (order and widths) from the UI to the asset.
        /// </summary>
        internal void SyncColumnLayoutToAsset()
        {
            if (this.sheetListView.columns == null || this.currentSheetDefinition?.Columns == null)
            {
                return;
            }

            // Create a list to hold the reordered columns
            var uiColumns = new Dictionary<int, ColumnDefinition>();

            // Skip the first column (index 0) as it's the "Object" column, not a data column
            foreach (var column in this.sheetListView.columns)
            {
                if (column.title == "Object")
                {
                    continue;
                }

                var columnDef = this.GetColumnDefinitionFromColumn(column);

                // Update width values from current UI state
                columnDef.ColumnWidth = column.width.value;

                var displayIndex = GetColumnDisplayIndex(column);
                uiColumns.Add(displayIndex, columnDef);
            }

            // Update the columns array in the asset to match UI order
            this.currentSheetDefinition.Columns = uiColumns.OrderBy(i => i.Key).Select(i => i.Value).ToArray();
            EditorUtility.SetDirty(this.currentSheetDefinition);
        }

        private void LoadSheetDefinition(SheetDefinition? sheetDefinition)
        {
            this.currentSheetDefinition = sheetDefinition;
            this.SaveLastUsedSheetDefinition();
            this.UpdateObjectFieldSelection();
            this.UpdateControllers();
            this.InitializeControllers();
            this.UpdateUI();
        }

        /// <summary>
        /// Recalculates all formulas in the sheet.
        /// </summary>
        private void RecalculateFormulas()
        {
            if (this.isRecalculatingFormulas || this.currentSheetDefinition?.Columns == null)
            {
                return;
            }

            var formulaColumns = this.currentSheetDefinition.Columns.Where(c => c.Type == ColumnType.Formula).Select(c => c.ColumnId).ToList();

            this.RecalculateSpecificFormulas(formulaColumns);
        }

        private void OnVariableValueChanged(VariableDefinition variable)
        {
            // Trigger targeted UI updates in the variables sidebar
            this.variablesSidebarController.UpdateVariableDisplays(variable, this.variableDependencyGraph);

            // This method now handles the response to a variable change
            this.RecalculateFormulas();
        }

        private void SetupUIReferences()
        {
            this.sheetDefinitionField = this.rootVisualElement.Q<ObjectField>("sheet-definition-field");
            this.noSheetMessage = this.rootVisualElement.Q<VisualElement>("no-sheet-message");
            this.sheetContentContainer = this.rootVisualElement.Q<VisualElement>("sheet-content");

            // Toolbar UI elements
            this.newButton = this.rootVisualElement.Q<ToolbarButton>("new-button");
            this.refreshButton = this.rootVisualElement.Q<ToolbarButton>("refresh-button");
            this.searchField = this.rootVisualElement.Q<ToolbarSearchField>("search-field");
            this.settingsMenu = this.rootVisualElement.Q<ToolbarMenu>("settings-menu");

            // Main content UI elements
            this.sheetListView = this.rootVisualElement.Q<MultiColumnListView>("sheet-list-view");
            this.noDataMessage = this.rootVisualElement.Q<VisualElement>("no-data-message");

            // Status bar UI elements
            this.statusLabel = this.rootVisualElement.Q<Label>("status-label");

            // Initialize variables sidebar controller
            this.variablesSidebarController = new VariablesSidebarController(this.variableEvaluator, this.rootVisualElement);
            this.variablesSidebarController.VariableValueChanged += this.OnVariableValueChanged;

            // Initialize error controller
            this.errorController = new ErrorController(this.rootVisualElement);
        }

        private void SetupEventHandlers()
        {
            this.sheetDefinitionField.RegisterValueChangedCallback(this.OnSheetDefinitionChanged);

            // Toolbar event handlers
            this.newButton.clicked += this.ShowCreateNewSheetDialog;

            this.refreshButton.clicked += this.RefreshSheetData;
            this.searchField.RegisterValueChangedCallback(evt =>
            {
                this.currentSearchText = evt.newValue;
                this.ApplySearchFilter();
            });

            this.sheetListView.headerContextMenuPopulateEvent += this.OnHeaderContextMenuPopulate;
        }

        private void SetupToolbar()
        {
            // Set initial search field value
            this.searchField.value = this.currentSearchText;

            // Setup settings menu
            this.SetupSettingsMenu();
        }

        private void SetupSettingsMenu()
        {
            this.settingsMenu.menu.AppendAction("Columns", _ =>
            {
                this.OpenConfigurationTab(SheetConfigurationDialog.TabType.Columns);
            });

            this.settingsMenu.menu.AppendAction("Variables", _ =>
            {
                this.OpenConfigurationTab(SheetConfigurationDialog.TabType.Variables);
            });

            this.settingsMenu.menu.AppendAction("Assets", _ =>
            {
                this.OpenConfigurationTab(SheetConfigurationDialog.TabType.Assets);
            });
        }

        private void OpenConfigurationTab(SheetConfigurationDialog.TabType tabType)
        {
            if (this.currentSheetDefinition != null)
            {
                // Sync current UI state to asset before opening config dialog
                this.SyncColumnLayoutToAsset();
                AssetDatabase.SaveAssets();

                SheetConfigurationDialog.ShowDialog(this.currentSheetDefinition, tabType, this.OnColumnsConfigured);
            }
        }

        private void ApplySearchFilter()
        {
            if (this.currentSheetDefinition == null)
            {
                return;
            }

            // Filter the row data based on search text
            this.filteredRowData.Clear();

            foreach (var row in this.rowData)
            {
                if (string.IsNullOrEmpty(this.currentSearchText) || row.ObjectName.ToLowerInvariant().Contains(this.currentSearchText.ToLowerInvariant()))
                {
                    this.filteredRowData.Add(row);
                }
            }

            // Update the list view to use filtered data
            this.ConfigureListView();
            this.UpdateStatusBar();
        }

        private void UpdateStatusBar()
        {
            if (this.currentSheetDefinition == null)
            {
                this.statusLabel.text = "No sheet definition loaded";
                return;
            }

            var totalCount = this.rowData.Count;
            var filteredCount = this.filteredRowData.Count;
            var assetSource = this.currentSheetDefinition.AssetManagementMode == AssetManagementMode.Manual ? "Managed" : "Discovered";

            var statusText = $"{assetSource}: {totalCount} objects";

            if (!string.IsNullOrEmpty(this.currentSearchText))
            {
                statusText = $"{assetSource}: {filteredCount} of {totalCount} objects";
            }

            this.statusLabel.text = statusText;
        }

        /// <summary>
        /// Sets a placeholder text in a cell element when column has errors.
        /// </summary>
        /// <param name="element">The cell element.</param>
        /// <param name="placeholderText">The placeholder text to show.</param>
        private void SetCellPlaceholder(VisualElement element, string placeholderText)
        {
            switch (element)
            {
                case TextField textField:
                    textField.value = placeholderText;
                    textField.SetEnabled(false);
                    break;
                case Label label:
                    label.text = placeholderText;
                    break;
                case PropertyField propertyField:
                    // For property fields, we need to clear binding and show placeholder
                    propertyField.Unbind();
                    propertyField.SetEnabled(false);

                    // Add a tooltip to explain the issue
                    propertyField.tooltip = "Column configuration error - check error panel for details";
                    break;
                default:
                    // For other element types, try to find a text-based child
                    var labelElement = element.Q<Label>();
                    var textFieldElement = element.Q<TextField>();

                    if (labelElement != null)
                    {
                        labelElement.text = placeholderText;
                    }
                    else if (textFieldElement != null)
                    {
                        textFieldElement.value = placeholderText;
                        textFieldElement.SetEnabled(false);
                    }

                    break;
            }
        }

        /// <summary>
        /// Runs the complete validation process including sheet-level cycle detection and individual column validation.
        /// </summary>
        private void RunValidation()
        {
            // Ensure all controllers and the dependency graph are up-to-date first.
            this.InitializeControllers();

            // 1. Perform the sheet-level cycle check ONCE.
            if (this.dependencyGraph.TryFindCycle(out var cyclePath))
            {
                // A cycle was found. Create a detailed error message.
                var columnNames = cyclePath.Select(id => this.currentSheetDefinition?.Columns.FirstOrDefault(c => c.ColumnId == id)?.EffectiveDisplayName ?? $"C{id}");
                var errorMessage = $"Circular dependency detected involving: {string.Join(" -> ", columnNames)}";

                // Add a column-level error to every column involved in the cycle.
                foreach (var columnId in cyclePath)
                {
                    var columnDef = this.currentSheetDefinition?.Columns.First(c => c.ColumnId == columnId);
                    if (columnDef != null)
                    {
                        this.errorController.AddColumnError(
                            columnId,
                            columnDef.EffectiveDisplayName,
                            ColumnErrorType.CircularDependency,
                            ErrorSeverity.Critical,
                            errorMessage);
                    }
                }
            }

            // 2. Proceed with individual column validation (syntax, etc.), which no longer checks for cycles.
            this.ValidateAllColumns();
        }

        /// <summary>
        /// Validates all columns and creates column-level errors for invalid configurations.
        /// This prevents cell evaluation for columns with fundamental configuration issues.
        /// </summary>
        private void ValidateAllColumns()
        {
            if (this.currentSheetDefinition?.Columns == null)
            {
                return;
            }

            // Get a sample object for validation if available
            var sampleObject = this.rowData.FirstOrDefault()?.TargetObject;

            foreach (var columnDef in this.currentSheetDefinition.Columns)
            {
                try
                {
                    ValidationResult? validationResult = null;

                    if (columnDef.Type == ColumnType.Property)
                    {
                        // Validate property columns using unified validation services
                        var propertyHandler = this.GetOrCreatePropertyHandler(columnDef);
                        validationResult = propertyHandler.ValidateColumn(sampleObject);

                        // Also check for duplicate property columns
                        if (validationResult.IsValid)
                        {
                            validationResult = PropertyValidationService.ValidateDuplicateColumns(columnDef, this.currentSheetDefinition.Columns);
                        }
                    }
                    else if (columnDef.Type == ColumnType.Formula)
                    {
                        // Validate formula columns
                        var formulaHandler = new FormulaColumnHandler(columnDef, this.GetOrCreatePropertyHandler);
                        validationResult = formulaHandler.ValidateColumn(this.currentSheetDefinition.Columns, this.currentSheetDefinition.Variables);

                        // Also check for duplicate formula columns if they write to properties
                        if (validationResult.IsValid && !string.IsNullOrEmpty(columnDef.TargetTypeName) && !string.IsNullOrEmpty(columnDef.PropertyPath))
                        {
                            validationResult = PropertyValidationService.ValidateDuplicateColumns(columnDef, this.currentSheetDefinition.Columns);
                        }
                    }

                    // If validation failed, create a column error
                    if (validationResult is { IsValid: false })
                    {
                        var errorType = ErrorController.DetermineColumnErrorType(columnDef, validationResult.ErrorMessage);
                        var severity = ErrorController.DetermineErrorSeverity(errorType);

                        this.errorController.AddColumnError(columnDef.ColumnId, columnDef.EffectiveDisplayName, errorType, severity, validationResult.ErrorMessage,
                            detailedDescription: validationResult.DetailedDescription,
                            suggestion: validationResult.Suggestion, affectedRowCount: this.rowData.Count);
                    }
                }
                catch (Exception ex)
                {
                    // Catch any unexpected validation errors
                    this.errorController.AddColumnError(columnDef.ColumnId, columnDef.EffectiveDisplayName, ColumnErrorType.FormulaSystemError, ErrorSeverity.Error,
                        $"Validation failed: {ex.Message}", affectedRowCount: this.rowData.Count);
                }
            }
        }

        private void OnSheetDefinitionChanged(ChangeEvent<Object> evt)
        {
            this.LoadSheetDefinition(evt.newValue as SheetDefinition);
        }

        private void ShowCreateNewSheetDialog()
        {
            SheetDefinitionCreationDialog.ShowDialog(createdSheetDefinition =>
            {
                if (createdSheetDefinition != null)
                {
                    this.LoadSheetDefinition(createdSheetDefinition);
                }
            });
        }

        private void OnColumnsConfigured(SheetDefinition sheetDefinition)
        {
            // Refresh the UI after columns have been configured
            this.InitializeControllers();
            this.UpdateUI();
        }

        private void InitializeControllers()
        {
            if (this.currentSheetDefinition == null)
            {
                this.formulaHandlers.Clear();
                this.dataHandlers.Clear();
                return;
            }

            try
            {
                // Initialize formula and data handlers
                this.InitializeFormulaHandlers();
                this.InitializeDataHandlers();

                // Initialize variable dependencies
                this.InitializeVariableDependencies();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to initialize sheet controllers: {ex.Message}");
                this.formulaHandlers.Clear();
                this.dataHandlers.Clear();
            }
        }

        private void InitializeFormulaHandlers()
        {
            // Clear existing handlers and dependency graph
            this.CleanupRealTimeSystem();

            if (this.currentSheetDefinition?.Columns == null)
            {
                return;
            }

            // Initialize formula handlers
            foreach (var column in this.currentSheetDefinition.Columns)
            {
                if (column.Type == ColumnType.Formula)
                {
                    var handler = new FormulaColumnHandler(column, this.GetOrCreatePropertyHandler);
                    this.formulaHandlers[column.ColumnId] = handler;

                    // Register with dependency graph for real-time updates
                    handler.RegisterWithDependencyGraph(this.dependencyGraph);

                    // Also register Data dependencies in the dependency graph
                    var dependencies = handler.GetColumnDependencies();
                    foreach (var dependency in dependencies)
                    {
                        // Parse dependency as column reference (e.g., "C0", "C1")
                        if (dependency.StartsWith("C") && int.TryParse(dependency.Substring(1), out var depColumnId))
                        {
                            var dependencyColumn = this.currentSheetDefinition.Columns.FirstOrDefault(c => c.ColumnId == depColumnId);

                            if (dependencyColumn?.Type == ColumnType.Data)
                            {
                                this.dependencyGraph.AddDependency(column.ColumnId, depColumnId);
                            }
                        }
                    }
                }
            }
        }

        private void InitializeDataHandlers()
        {
            this.dataHandlers.Clear();

            if (this.currentSheetDefinition?.Columns == null)
            {
                return;
            }

            foreach (var column in this.currentSheetDefinition.Columns)
            {
                if (column.Type == ColumnType.Data)
                {
                    var handler = new DataColumnHandler(column);
                    this.dataHandlers[column.ColumnId] = handler;
                }
            }
        }

        private SheetDefinition? GetLastUsedSheetDefinition()
        {
            var lastSheetPath = EditorPrefs.GetString(LastSheetDefinitionKey, string.Empty);
            if (!string.IsNullOrEmpty(lastSheetPath))
            {
                return AssetDatabase.LoadAssetAtPath<SheetDefinition>(lastSheetPath);
            }

            return null;
        }

        private void SaveLastUsedSheetDefinition()
        {
            if (this.currentSheetDefinition != null)
            {
                var path = AssetDatabase.GetAssetPath(this.currentSheetDefinition);
                EditorPrefs.SetString(LastSheetDefinitionKey, path);
            }
            else
            {
                EditorPrefs.DeleteKey(LastSheetDefinitionKey);
            }
        }

        private void UpdateObjectFieldSelection()
        {
            this.sheetDefinitionField.SetValueWithoutNotify(this.currentSheetDefinition);
        }

        private void UpdateControllers()
        {
            this.variablesSidebarController.SetSheetDefinition(this.currentSheetDefinition);
        }

        private void UpdateUI()
        {
            if (this.currentSheetDefinition != null)
            {
                this.noSheetMessage.AddToClassList("no-sheet-message--hidden");
                this.noSheetMessage.RemoveFromClassList("no-sheet-message");

                this.sheetContentContainer.AddToClassList("sheet-content-section--visible");
                this.sheetContentContainer.RemoveFromClassList("sheet-content-section");

                // Enable toolbar buttons
                this.refreshButton.SetEnabled(true);
                this.settingsMenu.SetEnabled(true);

                this.RefreshSheetData();
            }
            else
            {
                this.noSheetMessage.RemoveFromClassList("no-sheet-message--hidden");
                this.noSheetMessage.AddToClassList("no-sheet-message");

                this.sheetContentContainer.RemoveFromClassList("sheet-content-section--visible");
                this.sheetContentContainer.AddToClassList("sheet-content-section");

                // Disable toolbar buttons
                this.refreshButton.SetEnabled(false);
                this.settingsMenu.SetEnabled(false);

                this.UpdateStatusBar();
            }
        }

        private void RefreshSheetData()
        {
            if (this.currentSheetDefinition == null)
            {
                return;
            }

            // Clear formula label tracking on refresh
            this.formulaLabels.Clear();

            // Clear SerializedObject cache and dispose old objects
            foreach (var so in this.serializedObjectCache.Values)
            {
                so.Dispose();
            }

            this.serializedObjectCache.Clear();

            // Clear all previous errors
            this.errorController.ClearAllErrors();

            this.SetupColumns();
            this.LoadRowData();

            // Run complete validation including cycle detection
            this.RunValidation();

            this.ConfigureListView();

            // Refresh variables sidebar
            this.variablesSidebarController.RefreshVariablesSidebar();
        }

        private void SetupColumns()
        {
            if (this.currentSheetDefinition?.Columns == null)
            {
                return;
            }

            var columns = new List<Column>();

            // Add object name column first
            var nameColumn = new Column
            {
                name = "object-name",
                title = "Object",
                width = 150,
                resizable = true,
                sortable = true,
                makeCell = () => new ObjectField { allowSceneObjects = true },
                bindCell = (element, index) =>
                {
                    if (element is ObjectField objectField && index < this.filteredRowData.Count)
                    {
                        objectField.value = this.filteredRowData[index].TargetObject;
                        objectField.SetEnabled(false); // Make it readonly
                    }
                },
            };

            columns.Add(nameColumn);

            // Add columns from sheet definition
            foreach (var columnDef in this.currentSheetDefinition.Columns)
            {
                Column column = new Column
                {
                    name = columnDef.ColumnId.ToString(),
                    title = columnDef.EffectiveDisplayName,
                    width = columnDef.ColumnWidth, // Use stored width
                    resizable = true,
                    sortable = true,
                    makeCell = () => this.CreateCellElement(columnDef),
                    bindCell = (element, index) => this.BindCellElement(element, index, columnDef),
                };

                columns.Add(column);
            }

            this.sheetListView.columns.Clear();
            foreach (var column in columns)
            {
                this.sheetListView.columns.Add(column);
            }
        }

        private void LoadRowData()
        {
            this.rowData.Clear();
            this.filteredRowData.Clear();

            var objects = this.DiscoverObjects();

            if (objects.Length == 0)
            {
                this.noDataMessage.AddToClassList("no-data-message--visible");
                this.noDataMessage.RemoveFromClassList("no-data-message");
                this.UpdateStatusBar();
                return;
            }

            this.noDataMessage.RemoveFromClassList("no-data-message--visible");
            this.noDataMessage.AddToClassList("no-data-message");

            foreach (var obj in objects)
            {
                this.rowData.Add(new SheetRowData(obj));
            }

            // Apply current search filter to populate filteredRowData
            this.ApplySearchFilter();
        }

        private void ConfigureListView()
        {
            this.sheetListView.itemsSource = this.filteredRowData;
            this.sheetListView.fixedItemHeight = 25;
            this.sheetListView.showBorder = true;
            this.sheetListView.showAlternatingRowBackgrounds = AlternatingRowBackground.ContentOnly;
            this.sheetListView.reorderable = false; // Keep row reordering disabled

            // Column reordering events are not available in Unity's MultiColumnListView API
            this.sheetListView.Rebuild();
        }

        private Object[] DiscoverObjects()
        {
            if (this.currentSheetDefinition == null)
            {
                return Array.Empty<Object>();
            }

            // For Automatic and Custom modes, re-discover assets using current rules
            if (this.currentSheetDefinition.AssetManagementMode == AssetManagementMode.Automatic ||
                this.currentSheetDefinition.AssetManagementMode == AssetManagementMode.Custom)
            {
                return AssetPopulationHelper.PopulateFromCurrentRules(this.currentSheetDefinition);
            }

            // For Manual mode, use the managed assets
            var managedAssets = this.currentSheetDefinition.GetManagedAssets();

            return this.currentSheetDefinition.Type switch
            {
                SheetType.GameObject => managedAssets.OfType<GameObject>().Cast<Object>().ToArray(),
                SheetType.ScriptableObject => managedAssets.OfType<ScriptableObject>().Cast<Object>().ToArray(),
                _ => Array.Empty<Object>(),
            };
        }

        private void OnHeaderContextMenuPopulate(ContextualMenuPopulateEvent evt, Column column)
        {
            if (this.currentSheetDefinition?.Columns == null)
            {
                return;
            }

            // Find the column definition that matches this Column
            var columnDef = this.GetColumnDefinitionFromColumn(column);

            // Only show formula option for Data and Property columns
            if (columnDef.Type is ColumnType.Data or ColumnType.Property)
            {
                evt.menu.AppendSeparator();
                evt.menu.AppendAction("Apply Formula to Column", _ => this.OnColumnRightClick(columnDef), DropdownMenuAction.AlwaysEnabled);
            }
        }

        private ColumnDefinition GetColumnDefinitionFromColumn(Column column)
        {
            // Find matching column definition by title
            return this.currentSheetDefinition == null
                ? throw new InvalidOperationException()
                : this.currentSheetDefinition.Columns.First(c => c.EffectiveDisplayName == column.title);
        }

        private static int GetColumnDisplayIndex(Column column)
        {
            return (int)DisplayIndexProperty.GetValue(column);
        }

        private void OnColumnRightClick(ColumnDefinition? column)
        {
            if (column == null || this.currentSheetDefinition == null)
            {
                return;
            }

            // Get all objects currently displayed in the sheet
            var objects = this.filteredRowData.Select(row => row.TargetObject).Where(obj => obj != null).ToArray();

            if (objects.Length == 0)
            {
                Debug.LogWarning("No objects available to apply formula to.");
                return;
            }

            // Show the quick formula dialog
            QuickFormulaDialog.ShowDialog(this.currentSheetDefinition, this.columnFormulaProcessor, column, objects);
        }

        private VisualElement CreateCellElement(ColumnDefinition columnDef)
        {
            ErrorAwareVisualElement errorAwareElement = columnDef.Type switch
            {
                ColumnType.Property => new ErrorAwareVisualElement(new PropertyField(null, string.Empty)),
                ColumnType.Formula => new ErrorAwareVisualElement(new TextField { isReadOnly = true }),
                ColumnType.Data => new ErrorAwareVisualElement(this.CreateDataFieldElement(columnDef)),
                _ => new ErrorAwareVisualElement(new Label()),
            };

            return errorAwareElement;
        }

        private VisualElement CreateDataFieldElement(ColumnDefinition columnDef)
        {
            return DataFieldElementFactory.CreateElement(columnDef.DataFieldType, null, columnDef.UseSlider, columnDef.MinValue, columnDef.MaxValue);
        }

        private void BindDataColumn(VisualElement element, ColumnDefinition columnDef, SheetRowData row)
        {
            if (!this.dataHandlers.TryGetValue(columnDef.ColumnId, out var handler))
            {
                return;
            }

            if (this.currentSheetDefinition == null)
            {
                return;
            }

            var currentValue = handler.GetValue(row.TargetObject, this.currentSheetDefinition) ?? string.Empty;

            // Use the unified binder with a callback that updates the data handler and triggers recalculation
            DataFieldValueBinder.BindElement(element, columnDef.DataFieldType, currentValue, newValue =>
            {
                if (this.currentSheetDefinition != null)
                {
                    handler.SetValue(row.TargetObject, this.currentSheetDefinition, newValue);
                }

                this.OnDataChanged(columnDef.ColumnId);
            });
        }

        private void BindCellElement(VisualElement element, int index, ColumnDefinition columnDef)
        {
            if (index >= this.filteredRowData.Count)
            {
                return;
            }

            var row = this.filteredRowData[index];

            // Register error-aware cell for error tracking
            ErrorAwareVisualElement? errorAwareElement = null;
            if (element is ErrorAwareVisualElement errorAware)
            {
                errorAwareElement = errorAware;
                this.errorController.RegisterErrorAwareCell(columnDef.ColumnId, index, errorAwareElement);

                // Apply any existing error state (column or cell)
                var columnError = this.errorController.ColumnErrorManager.GetColumnError(columnDef.ColumnId);
                this.errorController.UpdateCellErrorState(columnDef.ColumnId, index, columnError, row.ObjectName);

                // Get the actual content element for binding
                element = errorAwareElement.Content;
            }

            // Check if column has validation errors - if so, skip evaluation and show placeholder
            var columnErrorState = this.errorController.ColumnErrorManager.GetColumnError(columnDef.ColumnId);
            if (columnErrorState != null)
            {
                // Column has errors - show placeholder and skip evaluation
                this.SetCellPlaceholder(element, columnErrorState.CellPlaceholder);
                return;
            }

            try
            {
                if (columnDef.Type == ColumnType.Property)
                {
                    Object? targetForProperty = null;

                    // For GameObjects, we need to get the component first
                    if (this.currentSheetDefinition?.Type == SheetType.GameObject && row.TargetObject is GameObject gameObject)
                    {
                        targetForProperty = this.GetTargetComponent(gameObject, columnDef.TargetTypeName);
                    }
                    else if (this.currentSheetDefinition?.Type == SheetType.ScriptableObject)
                    {
                        // For ScriptableObjects, use directly if the type matches
                        if (!string.IsNullOrEmpty(columnDef.TargetTypeName))
                        {
                            var targetType = Type.GetType(columnDef.TargetTypeName);
                            if (targetType != null && targetType.IsInstanceOfType(row.TargetObject))
                            {
                                targetForProperty = row.TargetObject;
                            }
                        }
                        else
                        {
                            // Fallback to direct use if no specific target type is specified
                            targetForProperty = row.TargetObject;
                        }
                    }

                    if (targetForProperty != null && element is PropertyField propertyField)
                    {
                        // Use Unity's PropertyField with automatic binding
                        if (!this.serializedObjectCache.TryGetValue(targetForProperty, out var serializedObject))
                        {
                            serializedObject = new SerializedObject(targetForProperty);
                            this.serializedObjectCache[targetForProperty] = serializedObject;
                        }

                        if (columnDef.DisableInspector)
                        {
                            serializedObject.InspectorMode(InspectorMode.Debug);
                        }

                        var property = serializedObject.FindProperty(columnDef.PropertyPath);

                        if (property != null)
                        {
                            propertyField.bindingPath = property.propertyPath;
                            propertyField.Bind(serializedObject);
                            propertyField.SetEnabled(!columnDef.IsReadOnly);

                            // Set up change callback for real-time formula updates
                            if (this.propertyHandlers.TryGetValue(columnDef.ColumnId, out var handler))
                            {
                                propertyField.RegisterValueChangeCallback(_ =>
                                {
                                    handler.OnPropertyChanged(columnDef.PropertyPath);
                                });
                            }
                        }
                        else
                        {
                            Debug.LogWarning($"Property '{columnDef.PropertyPath}' not found on {targetForProperty.name}");
                        }
                    }
                }
                else if (columnDef.Type == ColumnType.Formula)
                {
                    // Handle formula columns
                    if (element is TextField textField)
                    {
                        // Track this label for direct updates during recalculation
                        var key = (columnDef.ColumnId, index);
                        this.formulaLabels[key] = textField;

                        var formulaResult = this.EvaluateFormulaColumn(columnDef, row.TargetObject);
                        if (formulaResult != null)
                        {
                            textField.value = formulaResult.ToString();

                            // Write the result to the target property if configured
                            this.WriteFormulaResult(columnDef, row.TargetObject, formulaResult);
                        }
                        else
                        {
                            textField.value = "[Error]";
                        }

                        // Update error visual state after evaluation
                        if (errorAwareElement != null)
                        {
                            var columnErrorForCell = this.errorController.ColumnErrorManager.GetColumnError(columnDef.ColumnId);
                            this.errorController.UpdateCellErrorState(columnDef.ColumnId, index, columnErrorForCell, this.rowData[index].ObjectName);
                        }
                    }
                }
                else if (columnDef.Type == ColumnType.Data)
                {
                    // Handle data columns
                    this.BindDataColumn(element, columnDef, row);
                }
                else
                {
                    // For other non-property columns, show placeholder
                    if (element is Label label)
                    {
                        label.text = $"[{columnDef.Type}]";
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to bind cell for column '{columnDef.EffectiveDisplayName}' at index {index}: {ex.Message}");

                if (element is Label label)
                {
                    label.text = "[Error]";
                }
            }
        }

        private Component? GetTargetComponent(GameObject gameObject, string? targetTypeName = null)
        {
            if (string.IsNullOrEmpty(targetTypeName))
            {
                return null;
            }

            var targetType = Type.GetType(targetTypeName);
            if (targetType == null || !typeof(MonoBehaviour).IsAssignableFrom(targetType))
            {
                return null;
            }

            return gameObject.GetComponent(targetType);
        }

        private object? EvaluateFormulaColumn(ColumnDefinition columnDef, Object targetObject)
        {
            if (!this.formulaHandlers.TryGetValue(columnDef.ColumnId, out var handler))
            {
                Debug.LogWarning(
                    $"No formula handler found for ColumnId {columnDef.ColumnId} (DisplayName: '{columnDef.EffectiveDisplayName}'). Available handlers: {string.Join(", ", this.formulaHandlers.Keys)}");

                return null;
            }

            // Find the row index for error tracking
            var rowIndex = this.rowData.FindIndex(r => r.TargetObject == targetObject);

            try
            {
                // First validate the formula syntax and dependencies
                var formulaEngine = new FormulaEngine();
                var syntaxValidation = formulaEngine.ValidateSyntax(columnDef.Formula);

                if (!syntaxValidation.IsValid)
                {
                    if (rowIndex >= 0)
                    {
                        this.errorController.AddError(columnDef.EffectiveDisplayName, columnDef.ColumnId, rowIndex, ErrorSeverity.Error, "Formula syntax error",
                            syntaxValidation.ErrorMessage, "Check the formula syntax and ensure all operators and parentheses are correct.");
                    }

                    return null;
                }

                // Check for unknown column references
                var columnReferences = formulaEngine.GetColumnReferences(columnDef.Formula);
                var availableColumnIds = this.currentSheetDefinition?.Columns.Select(c => c.ColumnId).ToHashSet() ?? new HashSet<int>();
                var unknownColumns = columnReferences
                    .Where(colRef =>
                    {
                        // Parse column reference (e.g., "C0", "C1") and check if the ColumnId exists
                        if (colRef.StartsWith("C") && int.TryParse(colRef.Substring(1), out var columnId))
                        {
                            return !availableColumnIds.Contains(columnId);
                        }

                        return true; // Invalid format is considered unknown
                    })
                    .ToList();

                if (unknownColumns.Count > 0)
                {
                    if (rowIndex >= 0)
                    {
                        var unknownList = string.Join(", ", unknownColumns);
                        this.errorController.AddError(columnDef.EffectiveDisplayName, columnDef.ColumnId, rowIndex, ErrorSeverity.Error, "Unknown column references",
                            $"Formula references unknown columns: {unknownList}", "Ensure all referenced column names match existing columns exactly.");
                    }

                    return null;
                }

                // Evaluate variables before evaluating formula
                this.EvaluateVariablesForRow(targetObject);

                var result = handler.EvaluateFormula(targetObject, this.currentSheetDefinition?.Columns ?? Array.Empty<ColumnDefinition>(), this.GetColumnValue,
                    this.currentSheetDefinition?.Variables ?? Array.Empty<VariableDefinition>(), this.variableEvaluator.GetVariableValue);

                if (result != null && rowIndex >= 0)
                {
                    // Clear any existing error since evaluation succeeded
                    this.errorController.RemoveError(columnDef.ColumnId, rowIndex);
                }
                else if (result == null && rowIndex >= 0)
                {
                    // If result is null but no validation error was caught, it might be a runtime evaluation issue
                    this.errorController.AddError(columnDef.EffectiveDisplayName, columnDef.ColumnId, rowIndex, ErrorSeverity.Warning, "Formula evaluation returned null",
                        "The formula could not be evaluated with the current data.", "Check that all referenced columns have valid values.");
                }

                return result;
            }
            catch (FormulaException ex)
            {
                if (rowIndex >= 0)
                {
                    this.errorController.AddError(columnDef.EffectiveDisplayName, columnDef.ColumnId, rowIndex, ErrorSeverity.Error, "Formula evaluation failed", ex.Message,
                        "Check the formula syntax and ensure all referenced columns exist.");
                }

                return null;
            }
            catch (Exception ex)
            {
                if (rowIndex >= 0)
                {
                    this.errorController.AddError(columnDef.EffectiveDisplayName, columnDef.ColumnId, rowIndex, ErrorSeverity.Critical, "Unexpected error during formula evaluation",
                        ex.Message, "This may indicate a bug in the formula engine. Please report this issue.");
                }

                return null;
            }
        }

        private void WriteFormulaResult(ColumnDefinition columnDef, Object targetObject, object? result)
        {
            if (!this.formulaHandlers.TryGetValue(columnDef.ColumnId, out var handler))
            {
                return;
            }

            handler.WriteFormulaResult(targetObject, result);
        }

        private object? GetColumnValue(ColumnDefinition columnDef, Object targetObject)
        {
            if (columnDef.Type == ColumnType.Property)
            {
                // Use cached handler from registry
                var propertyHandler = this.GetOrCreatePropertyHandler(columnDef);

                Object? targetForProperty = null;

                // For GameObjects, we need to get the component first
                if (this.currentSheetDefinition?.Type == SheetType.GameObject && targetObject is GameObject gameObject)
                {
                    targetForProperty = this.GetTargetComponent(gameObject, columnDef.TargetTypeName);
                }
                else if (this.currentSheetDefinition?.Type == SheetType.ScriptableObject)
                {
                    // For ScriptableObjects, use directly if the type matches
                    if (!string.IsNullOrEmpty(columnDef.TargetTypeName))
                    {
                        var targetType = Type.GetType(columnDef.TargetTypeName);
                        if (targetType != null && targetType.IsInstanceOfType(targetObject))
                        {
                            targetForProperty = targetObject;
                        }
                    }
                    else
                    {
                        // Fallback to direct use if no specific target type is specified
                        targetForProperty = targetObject;
                    }
                }

                if (targetForProperty != null)
                {
                    return propertyHandler.GetValue(targetForProperty);
                }
            }
            else if (columnDef.Type == ColumnType.Formula)
            {
                // For formula columns, evaluate the formula (but avoid infinite recursion)
                return this.EvaluateFormulaColumn(columnDef, targetObject);
            }
            else if (columnDef.Type == ColumnType.Data)
            {
                // For Data columns, get the value from the handler
                if (this.dataHandlers.TryGetValue(columnDef.ColumnId, out var dataHandler))
                {
                    if (this.currentSheetDefinition == null)
                    {
                        return null;
                    }

                    var dataValue = dataHandler.GetValue(targetObject, this.currentSheetDefinition);

                    // Try to convert to numeric value for formula calculations
                    if (!string.IsNullOrEmpty(dataValue))
                    {
                        if (float.TryParse(dataValue, out var floatValue))
                        {
                            return floatValue;
                        }

                        if (int.TryParse(dataValue, out var intValue))
                        {
                            return intValue;
                        }

                        // Return the string value for non-numeric data
                        return dataValue;
                    }

                    return 0f; // Default value for empty Data
                }
            }

            return null;
        }

        /// <summary>
        /// Called when a property changes to trigger formula recalculation.
        /// </summary>
        /// <param name="propertyPath">The path of the property that changed.</param>
        private void OnPropertyChanged(string propertyPath)
        {
            if (this.isRecalculatingFormulas)
            {
                return; // Prevent recursive recalculation
            }

            // Find the column that changed
            var changedColumn = this.currentSheetDefinition?.Columns.FirstOrDefault(c => c.Type == ColumnType.Property && c.PropertyPath == propertyPath);

            if (changedColumn != null)
            {
                // Get formulas that need recalculation due to this change
                var formulasToRecalculate = this.dependencyGraph.GetRecalculationOrder(changedColumn.ColumnId);

                if (formulasToRecalculate.Count > 0)
                {
                    this.RecalculateSpecificFormulas(formulasToRecalculate);
                }
            }
        }

        /// <summary>
        /// Called when a Data column value changes to trigger formula recalculation.
        /// </summary>
        /// <param name="dataColumnId">The ID of the Data column that changed.</param>
        private void OnDataChanged(int dataColumnId)
        {
            if (this.isRecalculatingFormulas)
            {
                return; // Prevent recursive recalculation
            }

            // Get formulas that need recalculation due to this Data change
            var formulasToRecalculate = this.dependencyGraph.GetRecalculationOrder(dataColumnId);

            if (formulasToRecalculate.Count > 0)
            {
                this.RecalculateSpecificFormulas(formulasToRecalculate);
            }
        }

        /// <summary>
        /// Recalculates specific formulas in dependency order.
        /// </summary>
        /// <param name="formulaColumns">The formula columns to recalculate.</param>
        private void RecalculateSpecificFormulas(List<int> formulaColumns)
        {
            if (this.isRecalculatingFormulas || formulaColumns.Count == 0)
            {
                return;
            }

            this.isRecalculatingFormulas = true;

            try
            {
                // Get the proper calculation order
                var calculationOrder = this.dependencyGraph.GetRecalculationOrder();

                // Recalculate each formula in order
                foreach (var formulaColumnId in calculationOrder)
                {
                    this.RecalculateFormulaColumn(formulaColumnId);
                }

                // Update just the formula display labels instead of rebuilding entire UI
                this.UpdateFormulaDisplays(calculationOrder);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error during formula recalculation: {ex.Message}");
            }
            finally
            {
                this.isRecalculatingFormulas = false;
            }
        }

        /// <summary>
        /// Updates the display labels for the specified formula columns.
        /// </summary>
        /// <param name="formulaColumnIds">The formula column IDs to update display for.</param>
        private void UpdateFormulaDisplays(List<int> formulaColumnIds)
        {
            foreach (var formulaColumnId in formulaColumnIds)
            {
                var column = this.currentSheetDefinition?.Columns?.FirstOrDefault(c => c.ColumnId == formulaColumnId && c.Type == ColumnType.Formula);

                if (column == null)
                {
                    continue;
                }

                // Update the display label for each row
                for (int i = 0; i < this.rowData.Count; i++)
                {
                    var key = (column.ColumnId, i);
                    if (this.formulaLabels.TryGetValue(key, out var label))
                    {
                        try
                        {
                            var formulaResult = this.EvaluateFormulaColumn(column, this.rowData[i].TargetObject);
                            label.value = formulaResult != null ? formulaResult.ToString() : "[Error]";
                        }
                        catch (Exception ex)
                        {
                            Debug.LogWarning($"Failed to update formula display for '{formulaColumnId}' at row {i}: {ex.Message}");
                            label.value = "[Error]";
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Recalculates a specific formula column for all objects.
        /// </summary>
        /// <param name="formulaColumnId">The column ID of the formula column to recalculate.</param>
        private void RecalculateFormulaColumn(int formulaColumnId)
        {
            var column = this.currentSheetDefinition?.Columns.FirstOrDefault(c => c.ColumnId == formulaColumnId && c.Type == ColumnType.Formula);

            if (column == null || !this.formulaHandlers.TryGetValue(column.ColumnId, out var handler))
            {
                return;
            }

            // Recalculate for each object
            foreach (var row in this.rowData)
            {
                try
                {
                    var result = handler.EvaluateFormula(row.TargetObject, this.currentSheetDefinition?.Columns ?? Array.Empty<ColumnDefinition>(),
                        this.GetColumnValue);

                    if (result != null)
                    {
                        // Write the result to the target property if configured
                        handler.WriteFormulaResult(row.TargetObject, result);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Failed to recalculate formula '{formulaColumnId}' for object '{row.ObjectName}': {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Gets the target object for a column (component for GameObjects, direct object for ScriptableObjects).
        /// </summary>
        /// <param name="sourceObject">The source object from the row.</param>
        /// <param name="column">The column definition.</param>
        /// <returns>The target object for the column.</returns>
        private Object? GetTargetObjectForColumn(Object sourceObject, ColumnDefinition column)
        {
            if (this.currentSheetDefinition?.Type == SheetType.GameObject && sourceObject is GameObject gameObject)
            {
                return this.GetTargetComponent(gameObject, column.TargetTypeName);
            }
            else if (this.currentSheetDefinition?.Type == SheetType.ScriptableObject)
            {
                if (!string.IsNullOrEmpty(column.TargetTypeName))
                {
                    var targetType = Type.GetType(column.TargetTypeName);
                    if (targetType != null && targetType.IsInstanceOfType(sourceObject))
                    {
                        return sourceObject;
                    }
                }
                else
                {
                    return sourceObject;
                }
            }

            return null;
        }

        /// <summary>
        /// Clears and properly disposes all property handlers.
        /// </summary>
        private void ClearPropertyHandlers()
        {
            // Properly clean up existing handlers before clearing
            foreach (var handler in this.propertyHandlers.Values)
            {
                handler.PropertyChanged -= this.OnPropertyChanged;
                handler.ClearTrackedObjects();
            }

            this.propertyHandlers.Clear();
        }

        /// <summary>
        /// Gets or creates a PropertyColumnHandler for the specified column, ensuring it uses the shared cache.
        /// </summary>
        /// <param name="column">The column definition.</param>
        /// <returns>A PropertyColumnHandler instance that's cached and reused.</returns>
        private PropertyColumnHandler GetOrCreatePropertyHandler(ColumnDefinition column)
        {
            if (this.propertyHandlers.TryGetValue(column.ColumnId, out var handler))
            {
                return handler;
            }

            // Create new handler with shared cache
            handler = new PropertyColumnHandler(column, this.serializedObjectCache);
            this.propertyHandlers[column.ColumnId] = handler;

            // Subscribe to property changes for real-time formula updates
            handler.PropertyChanged += this.OnPropertyChanged;

            // Register all existing objects for change tracking
            foreach (var row in this.rowData)
            {
                Object? targetForProperty = this.GetTargetObjectForColumn(row.TargetObject, column);
                if (targetForProperty != null)
                {
                    handler.RegisterObject(targetForProperty);
                }
            }

            return handler;
        }

        /// <summary>
        /// Cleans up the real-time formula system.
        /// </summary>
        private void CleanupRealTimeSystem()
        {
            // Unregister formula handlers from dependency graph
            foreach (var handler in this.formulaHandlers.Values)
            {
                handler.UnregisterFromDependencyGraph();
            }

            this.formulaHandlers.Clear();

            // Clean up property handlers
            this.ClearPropertyHandlers();

            // Clear dependency graph and formula label tracking
            this.dependencyGraph.Clear();
            this.formulaLabels.Clear();
        }

        /// <summary>
        /// Evaluates all variables for the current row context.
        /// </summary>
        /// <param name="targetObject">The target object for context.</param>
        private void EvaluateVariablesForRow(Object targetObject)
        {
            if (this.currentSheetDefinition?.Variables == null || this.currentSheetDefinition.Variables.Length == 0)
            {
                return;
            }

            this.variableEvaluator.SetDependencyGraph(this.variableDependencyGraph);
            this.variableEvaluator.EvaluateAllVariables(this.currentSheetDefinition.Variables, this.currentSheetDefinition.Columns, this.GetColumnValue,
                targetObject);
        }

        /// <summary>
        /// Initializes the variable dependency graph based on current sheet variables.
        /// </summary>
        private void InitializeVariableDependencies()
        {
            this.variableDependencyGraph.Clear();

            if (this.currentSheetDefinition?.Variables == null)
            {
                return;
            }

            foreach (var variable in this.currentSheetDefinition.Variables)
            {
                if (variable.Type == VariableType.Formula && !string.IsNullOrEmpty(variable.Formula))
                {
                    try
                    {
                        var formulaEngine = new FormulaEngine();

                        // Extract variable references
                        var referencedVariables = formulaEngine.GetVariableReferences(variable.Formula);
                        foreach (var varRef in referencedVariables)
                        {
                            if (varRef.StartsWith("V") && int.TryParse(varRef.Substring(1), out var referencedVarId))
                            {
                                this.variableDependencyGraph.AddVariableDependency(variable.VariableId, referencedVarId);
                            }
                        }

                        // Extract column references
                        var referencedColumns = formulaEngine.GetColumnReferences(variable.Formula);
                        foreach (var colRef in referencedColumns)
                        {
                            if (colRef.StartsWith("C") && int.TryParse(colRef.Substring(1), out var referencedColId))
                            {
                                this.variableDependencyGraph.AddColumnDependency(variable.VariableId, referencedColId);
                            }
                        }
                    }
                    catch (FormulaException ex)
                    {
                        Debug.LogWarning($"Failed to parse dependencies for variable '{variable.EffectiveDisplayName}': {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// Called when the window is closed to clean up resources.
        /// </summary>
        private void OnDestroy()
        {
            // Sync column layout before window closes
            this.SyncColumnLayoutToAsset();

            // Save any cached data changes back to the serialized list
            if (this.currentSheetDefinition != null)
            {
                this.currentSheetDefinition.SaveCachedData();
                EditorUtility.SetDirty(this);
            }

            AssetDatabase.SaveAssets();

            this.variablesSidebarController.VariableValueChanged -= this.OnVariableValueChanged;

            this.CleanupRealTimeSystem();
        }
    }
}