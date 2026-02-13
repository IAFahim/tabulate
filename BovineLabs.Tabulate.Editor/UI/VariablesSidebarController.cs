// <copyright file="VariablesSidebarController.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Tabulate.Editor.UI
{
    using System;
    using System.Linq;
    using BovineLabs.Tabulate.Editor.Core;
    using BovineLabs.Tabulate.Editor.Utilities;
    using BovineLabs.Tabulate.Editor.Variables;
    using UnityEditor;
    using UnityEngine.UIElements;

    /// <summary>
    /// Controls the variables sidebar UI and state.
    /// </summary>
    public class VariablesSidebarController
    {
        private readonly VariableEvaluator variableEvaluator;
        private readonly VisualElement variablesSidebar;
        private readonly Button variablesToggleButton;
        private readonly VisualElement variablesList;

        private SheetDefinition? currentSheetDefinition;

        private bool isVariablesSidebarExpanded;

        public VariablesSidebarController(VariableEvaluator variableEvaluator, VisualElement root)
        {
            this.variableEvaluator = variableEvaluator;

            // Query for UI elements
            this.variablesSidebar = root.Q<VisualElement>("variables-sidebar");
            this.variablesToggleButton = root.Q<Button>("variables-toggle-button");
            this.variablesList = root.Q<VisualElement>("variables-list");

            // Register callback for toggle button
            this.variablesToggleButton.clicked += this.ToggleVariablesSidebar;
        }

        public event Action<VariableDefinition>? VariableValueChanged;

        public void SetSheetDefinition(SheetDefinition? sheetDefinition)
        {
            this.currentSheetDefinition = sheetDefinition;
        }

        /// <summary>
        /// Toggles the variables sidebar expanded/collapsed state.
        /// </summary>
        public void ToggleVariablesSidebar()
        {
            this.isVariablesSidebarExpanded = !this.isVariablesSidebarExpanded;
            this.UpdateVariablesSidebarState();
        }

        /// <summary>
        /// Updates the variables sidebar visual state and content.
        /// </summary>
        public void UpdateVariablesSidebarState()
        {
            if (this.isVariablesSidebarExpanded)
            {
                this.variablesSidebar.RemoveFromClassList("variables-sidebar--collapsed");
                this.variablesSidebar.AddToClassList("variables-sidebar--expanded");
                this.variablesToggleButton.text = "▶";
                this.PopulateVariablesList();
            }
            else
            {
                this.variablesSidebar.RemoveFromClassList("variables-sidebar--expanded");
                this.variablesSidebar.AddToClassList("variables-sidebar--collapsed");
                this.variablesToggleButton.text = "◀";
            }
        }

        /// <summary>
        /// Populates the variables list with current sheet variables and their values.
        /// </summary>
        public void PopulateVariablesList()
        {
            if (this.currentSheetDefinition == null)
            {
                return;
            }

            this.variablesList.Clear();

            if (this.currentSheetDefinition.Variables.Length == 0)
            {
                var noVariablesLabel = new Label("No variables defined")
                {
                    name = "no-variables-label",
                };

                noVariablesLabel.AddToClassList("variable-item__empty-message");
                this.variablesList.Add(noVariablesLabel);
                return;
            }

            // Add each variable as a list item
            foreach (var variable in this.currentSheetDefinition.Variables)
            {
                var variableItem = this.CreateVariableItem(variable);
                this.variablesList.Add(variableItem);
            }
        }

        /// <summary>
        /// Creates a visual element for displaying a variable.
        /// </summary>
        /// <param name="variable">The variable definition.</param>
        /// <returns>A visual element representing the variable.</returns>
        public VisualElement CreateVariableItem(VariableDefinition variable)
        {
            var variableItemAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(EditorConstants.AssetPath + "VariableItem.uxml");
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(EditorConstants.AssetPath + "VariableItem.uss");
            var item = variableItemAsset.Instantiate();
            item.name = $"variable-{variable.VariableId}";
            item.styleSheets.Add(styleSheet);

            // Populate the template with actual data
            var idLabel = item.Q<Label>("variable-id");
            var nameLabel = item.Q<Label>("variable-name");
            var typeLabel = item.Q<Label>("variable-type");
            var valueContainer = item.Q<VisualElement>("variable-value-container");
            var valueFieldContainer = item.Q<VisualElement>("variable-value-field");
            var displayContainer = item.Q<VisualElement>("variable-display-container");
            var valueLabel = item.Q<Label>("variable-value-label");

            // Set header data
            idLabel.text = $"V{variable.VariableId}";
            nameLabel.text = variable.EffectiveDisplayName;
            typeLabel.text = variable.Type.ToString();

            // Value display/edit based on variable type
            if (variable.Type == VariableType.Data)
            {
                // Show editable field for Data variables
                valueContainer.style.display = DisplayStyle.Flex;
                displayContainer.style.display = DisplayStyle.None;

                var valueField = DataFieldElementFactory.CreateElement(variable.DataType);
                valueField.AddToClassList("variable-value-field");

                // Bind the current value and set up change callback
                DataFieldValueBinder.BindElement(valueField, variable.DataType, variable.DataValue, newValue =>
                {
                    variable.DataValue = newValue;

                    // Mark the sheet definition as dirty
                    if (this.currentSheetDefinition != null)
                    {
                        EditorUtility.SetDirty(this.currentSheetDefinition);
                    }

                    // Note: Targeted update will be called from the event handler in UnitySheetEditor

                    // Replace direct call with event
                    this.VariableValueChanged?.Invoke(variable);
                });

                valueFieldContainer.Clear();
                valueFieldContainer.Add(valueField);
            }
            else
            {
                // Show read-only display for Property and Formula variables
                valueContainer.style.display = DisplayStyle.None;
                displayContainer.style.display = DisplayStyle.Flex;

                var currentValue = this.variableEvaluator.GetVariableValue(variable.VariableId);
                var valueText = currentValue?.ToString() ?? "null";

                // Truncate long values
                if (valueText.Length > 30)
                {
                    valueText = valueText.Substring(0, 27) + "...";
                }

                valueLabel.text = $"= {valueText}";
            }

            return item;
        }

        /// <summary>
        /// Updates only the specific variable displays that depend on the changed variable.
        /// </summary>
        /// <param name="changedVariable">The variable that changed.</param>
        /// <param name="variableDependencyGraph">The dependency graph to query for dependents.</param>
        public void UpdateVariableDisplays(VariableDefinition changedVariable, VariableDependencyGraph variableDependencyGraph)
        {
            // Get all variables that depend on the one that just changed
            var dependentVariableIds = variableDependencyGraph.GetDependentVariables(changedVariable.VariableId);

            foreach (var variableId in dependentVariableIds)
            {
                var variable = this.currentSheetDefinition?.Variables.FirstOrDefault(v => v.VariableId == variableId);
                if (variable == null)
                {
                    continue;
                }

                // Find the specific UI element for the dependent variable
                var variableItem = this.variablesList.Q<VisualElement>($"variable-{variableId}");

                // Find the label that displays the value
                var valueLabel = variableItem?.Q<Label>("variable-value-label");
                if (valueLabel == null)
                {
                    continue;
                }

                // Get the new, re-evaluated value from the evaluator
                var currentValue = this.variableEvaluator.GetVariableValue(variable.VariableId);
                var valueText = currentValue?.ToString() ?? "null";

                // Truncate and update the text
                if (valueText.Length > 30)
                {
                    valueText = valueText.Substring(0, 27) + "...";
                }

                valueLabel.text = $"= {valueText}";
            }
        }

        /// <summary>
        /// Refreshes the variables sidebar content when data changes.
        /// </summary>
        public void RefreshVariablesSidebar()
        {
            if (this.isVariablesSidebarExpanded)
            {
                this.PopulateVariablesList();
            }
        }
    }
}