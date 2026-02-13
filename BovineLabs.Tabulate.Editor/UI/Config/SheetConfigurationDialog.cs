// <copyright file="SheetConfigurationDialog.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Tabulate.Editor.UI.Config
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using BovineLabs.Tabulate.Editor.Core;
    using BovineLabs.Tabulate.Editor.Variables;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.UIElements;

    public class SheetConfigurationDialog : EditorWindow
    {
        private readonly List<ColumnDefinition> workingColumns = new();
        private readonly List<VariableDefinition> workingVariables = new();
        private readonly List<ColumnDefinition> originalColumns = new();
        private readonly List<VariableDefinition> originalVariables = new();

        private ListView columnListView = null!;
        private ListView variableListView = null!;
        private Button saveButton = null!;
        private Button columnsTabButton = null!;
        private Button variablesTabButton = null!;
        private Button assetsTabButton = null!;
        private VisualElement columnsTabContent = null!;
        private VisualElement variablesTabContent = null!;
        private VisualElement assetsTabContent = null!;
        private ColumnDetailsHandler columnDetailsHandler = null!;
        private VariableDetailsHandler variableDetailsHandler = null!;
        private AssetManagementHandler assetManagementHandler = null!;

        private SheetDefinition sheetDefinition = null!;
        private TabType requestedInitialTab;
        private Action<SheetDefinition> onSaved = null!;

        public enum TabType
        {
            Columns,
            Variables,
            Assets,
        }

        public static void ShowDialog(SheetDefinition sheetDefinition, TabType initialTab, Action<SheetDefinition> onSaved)
        {
            var window = CreateInstance<SheetConfigurationDialog>();
            window.titleContent = new GUIContent("Sheet Configuration");
            window.minSize = new Vector2(400, 300);
            window.sheetDefinition = sheetDefinition;
            window.onSaved = onSaved;
            window.requestedInitialTab = initialTab;

            window.Show();
        }

        public void CreateGUI()
        {
            this.LoadUXML();
            this.FindUIElements();
            this.InitializeHandlers();
            this.SetupEventHandlers();
            this.LoadWorkingData();
            this.RefreshLists();

            this.SwitchToTab(this.requestedInitialTab);

            // Set up change tracking - start with no unsaved changes
            this.hasUnsavedChanges = false;
            this.saveChangesMessage = "Save changes to sheet configuration?";
        }

        public override void SaveChanges()
        {
            this.sheetDefinition.Columns = this.workingColumns.ToArray();
            this.sheetDefinition.Variables = this.workingVariables.ToArray();

            // Apply asset management changes
            this.assetManagementHandler.ApplyChanges();

            EditorUtility.SetDirty(this.sheetDefinition);
            AssetDatabase.SaveAssets();

            this.onSaved.Invoke(this.sheetDefinition);

            // Reset change tracking after successful save
            this.LoadWorkingData();
            this.hasUnsavedChanges = false;

            base.SaveChanges();
        }

        private void Save()
        {
            this.SaveChanges();
            this.Close();
        }

        private void LoadUXML()
        {
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(EditorConstants.AssetPath + "SheetConfigurationDialog.uxml");
            visualTree.CloneTree(this.rootVisualElement);
        }

        private void FindUIElements()
        {
            this.columnListView = this.rootVisualElement.Q<ListView>("column-list-view");
            this.variableListView = this.rootVisualElement.Q<ListView>("variable-list-view");
            this.saveButton = this.rootVisualElement.Q<Button>("save-button");

            this.columnsTabButton = this.rootVisualElement.Q<Button>("columns-tab-button");
            this.variablesTabButton = this.rootVisualElement.Q<Button>("variables-tab-button");
            this.assetsTabButton = this.rootVisualElement.Q<Button>("assets-tab-button");
            this.columnsTabContent = this.rootVisualElement.Q<VisualElement>("columns-tab-content");
            this.variablesTabContent = this.rootVisualElement.Q<VisualElement>("variables-tab-content");
            this.assetsTabContent = this.rootVisualElement.Q<VisualElement>("assets-tab-content");

            this.SetupListViewProperties();
            this.SetupButtonHandlers();
        }

        private void SetupListViewProperties()
        {
            this.columnListView.reorderable = true;
            this.columnListView.reorderMode = ListViewReorderMode.Animated;
            this.columnListView.showAddRemoveFooter = true;
            this.columnListView.itemIndexChanged += (_, _) => this.MarkAsChanged();

            this.variableListView.reorderable = true;
            this.variableListView.reorderMode = ListViewReorderMode.Animated;
            this.variableListView.showAddRemoveFooter = true;
            this.variableListView.itemIndexChanged += (_, _) => this.MarkAsChanged();
        }

        private void SetupButtonHandlers()
        {
            this.saveButton.clicked += this.Save;
            this.columnsTabButton.clicked += () => this.SwitchToTab(TabType.Columns);
            this.variablesTabButton.clicked += () => this.SwitchToTab(TabType.Variables);
            this.assetsTabButton.clicked += () => this.SwitchToTab(TabType.Assets);
        }

        private void InitializeHandlers()
        {
            this.columnDetailsHandler = new ColumnDetailsHandler(this.rootVisualElement, this.sheetDefinition, this.workingColumns, this.RefreshColumnList, this.MarkAsChanged);
            this.variableDetailsHandler =
                new VariableDetailsHandler(this.rootVisualElement, this.sheetDefinition, this.workingVariables, this.RefreshVariableList, this.MarkAsChanged);

            this.assetManagementHandler = new AssetManagementHandler(this.sheetDefinition, () =>
            {
                // Apply changes immediately when assets are modified
                EditorUtility.SetDirty(this.sheetDefinition);
                AssetDatabase.SaveAssets();
                // this.MarkAsChanged(); // This doesn't actually do stuff for asset management atm
            });

            this.assetsTabContent.Add(this.assetManagementHandler);
        }

        private void SetupEventHandlers()
        {
            this.SetupColumnListView();
            this.SetupVariableListView();
        }

        private void SetupColumnListView()
        {
            this.columnListView.makeItem = () => new Label();
            this.columnListView.bindItem = (element, index) =>
            {
                if (element is Label label && index < this.workingColumns.Count)
                {
                    var column = this.workingColumns[index];
                    label.text = $"{column.EffectiveDisplayName} ({column.Type})";
                }
            };

            this.columnListView.selectionChanged += this.OnColumnSelectionChanged;
            this.columnListView.itemsAdded += this.OnColumnItemsAdded;

            // Annoyingly this executes before they are actually removed and itemsSourceSizeChanged is internal
            this.columnListView.itemsRemoved += _ => this.columnListView.schedule.Execute(this.MarkAsChanged).ExecuteLater(1);
        }

        private void SetupVariableListView()
        {
            this.variableListView.makeItem = () => new Label();
            this.variableListView.bindItem = (element, index) =>
            {
                if (element is Label label && index < this.workingVariables.Count)
                {
                    var variable = this.workingVariables[index];
                    label.text = $"[V{variable.VariableId}] {variable.EffectiveDisplayName} ({variable.Type})";
                }
            };

            this.variableListView.selectionChanged += this.OnVariableSelectionChanged;
            this.variableListView.itemsAdded += this.OnVariableItemsAdded;

            // Annoyingly this executes before they are actually removed and itemsSourceSizeChanged is internal
            this.variableListView.itemsRemoved += _ => this.variableListView.schedule.Execute(this.MarkAsChanged).ExecuteLater(1);
        }

        private void LoadWorkingData()
        {
            this.LoadWorkingColumns();
            this.LoadWorkingVariables();
        }

        private void LoadWorkingColumns()
        {
            this.workingColumns.Clear();
            this.originalColumns.Clear();

            var needsIdAssignment = this.sheetDefinition.Columns.GroupBy(c => c.ColumnId).Any(g => g.Count() > 1);

            var nextId = 0;
            foreach (var column in this.sheetDefinition.Columns)
            {
                var workingColumn = new ColumnDefinition
                {
                    ColumnId = needsIdAssignment ? nextId++ : column.ColumnId,
                    UserDisplayName = column.UserDisplayName,
                    Type = column.Type,
                    TargetTypeName = column.TargetTypeName,
                    PropertyPath = column.PropertyPath,
                    Formula = column.Formula,
                    DisableInspector = column.DisableInspector,
                    IsReadOnly = column.IsReadOnly,
                    DataFieldType = column.DataFieldType,
                    DataValue = column.DataValue,
                    MinValue = column.MinValue,
                    MaxValue = column.MaxValue,
                    UseSlider = column.UseSlider,
                };

                this.workingColumns.Add(workingColumn);

                // Create a copy for original data tracking
                var originalColumn = new ColumnDefinition
                {
                    ColumnId = workingColumn.ColumnId,
                    UserDisplayName = workingColumn.UserDisplayName,
                    Type = workingColumn.Type,
                    TargetTypeName = workingColumn.TargetTypeName,
                    PropertyPath = workingColumn.PropertyPath,
                    Formula = workingColumn.Formula,
                    DisableInspector = workingColumn.DisableInspector,
                    IsReadOnly = workingColumn.IsReadOnly,
                    DataFieldType = workingColumn.DataFieldType,
                    DataValue = workingColumn.DataValue,
                    MinValue = workingColumn.MinValue,
                    MaxValue = workingColumn.MaxValue,
                    UseSlider = workingColumn.UseSlider,
                };

                this.originalColumns.Add(originalColumn);
            }
        }

        private void LoadWorkingVariables()
        {
            this.workingVariables.Clear();
            this.originalVariables.Clear();

            foreach (var variable in this.sheetDefinition.Variables)
            {
                var workingVariable = new VariableDefinition
                {
                    VariableId = variable.VariableId,
                    DisplayName = variable.DisplayName,
                    Type = variable.Type,
                    DataValue = variable.DataValue,
                    DataType = variable.DataType,
                    TargetTypeName = variable.TargetTypeName,
                    PropertyPath = variable.PropertyPath,
                    TargetObject = variable.TargetObject,
                    Formula = variable.Formula,
                };

                this.workingVariables.Add(workingVariable);

                // Create a copy for original data tracking
                var originalVariable = new VariableDefinition
                {
                    VariableId = workingVariable.VariableId,
                    DisplayName = workingVariable.DisplayName,
                    Type = workingVariable.Type,
                    DataValue = workingVariable.DataValue,
                    DataType = workingVariable.DataType,
                    TargetTypeName = workingVariable.TargetTypeName,
                    PropertyPath = workingVariable.PropertyPath,
                    TargetObject = workingVariable.TargetObject,
                    Formula = workingVariable.Formula,
                };

                this.originalVariables.Add(originalVariable);
            }
        }

        private void RefreshLists()
        {
            this.RefreshColumnList();
            this.RefreshVariableList();
        }

        private void RefreshColumnList()
        {
            this.columnListView.itemsSource = this.workingColumns;
            this.columnListView.Rebuild();
        }

        private void RefreshVariableList()
        {
            this.variableListView.itemsSource = this.workingVariables;
            this.variableListView.Rebuild();
        }

        private void OnColumnItemsAdded(IEnumerable<int> indices)
        {
            foreach (var index in indices)
            {
                this.workingColumns[index] = new ColumnDefinition
                {
                    ColumnId = this.GetNextAvailableColumnId(),
                    Type = ColumnType.Property,
                    TargetTypeName = string.Empty,
                    PropertyPath = string.Empty,
                    Formula = string.Empty,
                    DisableInspector = false,
                    IsReadOnly = false,
                    DataValue = "0",
                };
            }

            this.MarkAsChanged();
        }

        private void OnVariableItemsAdded(IEnumerable<int> indices)
        {
            foreach (var index in indices)
            {
                this.workingVariables[index] = new VariableDefinition
                {
                    VariableId = this.GetNextAvailableVariableId(),
                    Type = VariableType.Data,
                    DataType = DataFieldType.Float,
                    DataValue = "0",
                };
            }

            this.MarkAsChanged();
        }

        private int GetNextAvailableColumnId()
        {
            if (this.workingColumns.Count == 0)
            {
                return 0;
            }

            var usedIds = this.workingColumns.Where(c => c != null).Select(c => c.ColumnId).ToHashSet();
            var nextId = 0;
            while (usedIds.Contains(nextId))
            {
                nextId++;
            }

            return nextId;
        }

        private int GetNextAvailableVariableId()
        {
            if (this.workingVariables.Count == 0)
            {
                return 0;
            }

            var usedIds = this.workingVariables.Where(v => v != null).Select(v => v.VariableId).ToHashSet();
            var nextId = 0;
            while (usedIds.Contains(nextId))
            {
                nextId++;
            }

            return nextId;
        }

        private void OnColumnSelectionChanged(IEnumerable<object> selectedItems)
        {
            var selectedItem = selectedItems.FirstOrDefault();
            if (selectedItem is ColumnDefinition column)
            {
                this.columnDetailsHandler.ShowDetails(column);
            }
            else
            {
                this.columnDetailsHandler.ShowNoSelectionState();
            }
        }

        private void OnVariableSelectionChanged(IEnumerable<object> selectedItems)
        {
            var selectedItem = selectedItems.FirstOrDefault();
            if (selectedItem is VariableDefinition variable)
            {
                this.variableDetailsHandler.ShowDetails(variable);
            }
            else
            {
                this.variableDetailsHandler.ShowNoSelectionState();
            }
        }

        private void SwitchToTab(TabType tabType)
        {
            // Remove active class from all tab buttons
            this.columnsTabButton.RemoveFromClassList("column-config-tab-button--active");
            this.variablesTabButton.RemoveFromClassList("column-config-tab-button--active");
            this.assetsTabButton.RemoveFromClassList("column-config-tab-button--active");

            // Hide all tab content
            this.columnsTabContent.AddToClassList("column-config-tab-content--hidden");
            this.variablesTabContent.AddToClassList("column-config-tab-content--hidden");
            this.assetsTabContent.AddToClassList("column-config-tab-content--hidden");

            // Show selected tab and activate button
            switch (tabType)
            {
                case TabType.Columns:
                    this.columnsTabButton.AddToClassList("column-config-tab-button--active");
                    this.columnsTabContent.RemoveFromClassList("column-config-tab-content--hidden");
                    break;
                case TabType.Variables:
                    this.variablesTabButton.AddToClassList("column-config-tab-button--active");
                    this.variablesTabContent.RemoveFromClassList("column-config-tab-content--hidden");
                    break;
                case TabType.Assets:
                    this.assetsTabButton.AddToClassList("column-config-tab-button--active");
                    this.assetsTabContent.RemoveFromClassList("column-config-tab-content--hidden");
                    break;
            }
        }

        private bool HasActualChanges()
        {
            return this.HasColumnChanges() || this.HasVariableChanges();
        }

        private bool HasColumnChanges()
        {
            if (this.workingColumns.Count != this.originalColumns.Count)
            {
                return true;
            }

            for (int i = 0; i < this.workingColumns.Count; i++)
            {
                if (!this.workingColumns[i].Equals(this.originalColumns[i]))
                {
                    return true;
                }
            }

            return false;
        }

        private bool HasVariableChanges()
        {
            if (this.workingVariables.Count != this.originalVariables.Count)
            {
                return true;
            }

            for (int i = 0; i < this.workingVariables.Count; i++)
            {
                if (!this.workingVariables[i].Equals(this.originalVariables[i]))
                {
                    return true;
                }
            }

            return false;
        }


        private void MarkAsChanged()
        {
            this.hasUnsavedChanges = this.HasActualChanges();
        }
    }
}