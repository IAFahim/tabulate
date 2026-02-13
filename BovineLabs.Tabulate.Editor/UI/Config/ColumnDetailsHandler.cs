// <copyright file="ColumnDetailsHandler.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Tabulate.Editor.UI.Config
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using BovineLabs.Tabulate.Editor.Core;
    using BovineLabs.Tabulate.Editor.Formula;
    using BovineLabs.Tabulate.Editor.Services;
    using BovineLabs.Tabulate.Editor.Utilities;
    using UnityEngine;
    using UnityEngine.UIElements;

    public class ColumnDetailsHandler : BaseDetailsHandler
    {
        private readonly List<ColumnDefinition> workingColumns;
        private readonly Action refreshColumnListCallback;
        private readonly Action onChangeCallback;
        private readonly ColumnConfigurationValidator validator;

        private readonly TextField displayNameField;
        private readonly EnumField columnTypeField;
        private readonly TextField targetTypeField;
        private readonly TextField propertyPathField;
        private readonly TextField formulaField;
        private readonly Toggle disableInspectorToggle;
        private readonly Toggle isReadOnlyToggle;
        private readonly DropdownField propertyDropdown;
        private readonly EnumField dataFieldTypeField;
        private readonly Toggle useSliderToggle;
        private readonly FloatField minValueField;
        private readonly FloatField maxValueField;
        private readonly ListView columnListView;
        private readonly Label noSelectionLabel;
        private readonly VisualElement detailsForm;
        private readonly Label validationSummaryLabel;

        private VisualElement? dataValueElement;

        // Store circular dependency validation result
        private ValidationResult? circularDependencyValidation;

        public ColumnDetailsHandler(
            VisualElement rootElement, SheetDefinition sheetDefinition, List<ColumnDefinition> workingColumns, Action refreshColumnListCallback, Action onChangeCallback)
            : base(rootElement, sheetDefinition)
        {
            this.workingColumns = workingColumns;
            this.refreshColumnListCallback = refreshColumnListCallback;
            this.onChangeCallback = onChangeCallback;

            // Initialize validator
            var tokenizer = new FormulaTokenizer();
            var formulaValidator = new FormulaValidator(tokenizer);
            this.validator = new ColumnConfigurationValidator(sheetDefinition, formulaValidator);

            this.displayNameField = this.FindElement<TextField>("display-name-field");
            this.columnTypeField = this.FindElement<EnumField>("column-type-field");
            this.targetTypeField = this.FindElement<TextField>("target-type-field");
            this.propertyPathField = this.FindElement<TextField>("property-path-field");
            this.formulaField = this.FindElement<TextField>("formula-field");
            this.disableInspectorToggle = this.FindElement<Toggle>("disable-inspector-toggle");
            this.isReadOnlyToggle = this.FindElement<Toggle>("readonly-toggle");
            this.propertyDropdown = this.FindElement<DropdownField>("property-dropdown");

            this.dataFieldTypeField = this.FindElement<EnumField>("data-field-type-field");

            // dataValueElement will be created dynamically
            this.useSliderToggle = this.FindElement<Toggle>("use-slider-toggle");
            this.minValueField = this.FindElement<FloatField>("min-value-field");
            this.maxValueField = this.FindElement<FloatField>("max-value-field");
            this.columnListView = this.FindElement<ListView>("column-list-view");

            this.noSelectionLabel = this.FindElement<Label>("no-selection-label");
            this.detailsForm = this.FindElement<VisualElement>("details-form");

            this.validationSummaryLabel = this.FindElement<Label>("validation-summary-label");

            this.SetupUIElements();
            this.RegisterEventHandlers();
        }

        public void ShowDetails(ColumnDefinition column)
        {
            this.ShowDetailsForm(this.noSelectionLabel, this.detailsForm, "column-config-details-form--visible");
            this.PopulateFields(column);
            this.RefreshPropertyDropdown();
            this.UpdateFieldVisibility(column.Type);
            this.ValidateCurrentColumn();
        }

        public void ShowNoSelectionState()
        {
            this.ShowNoSelectionState(this.noSelectionLabel, this.detailsForm, "column-config-details-form--visible");
            this.ClearValidationDisplay();
        }

        private void SetupUIElements()
        {
            this.targetTypeField.isReadOnly = true;
            this.targetTypeField.RegisterCallback<ClickEvent>(this.OnTargetTypeFieldClicked);

            // Initialize enum fields with their respective enum types
            this.columnTypeField.Init(ColumnType.Property);
            this.dataFieldTypeField.Init(DataFieldType.Integer);
        }

        private void RegisterEventHandlers()
        {
            this.RegisterValueChangedCallback<string>(this.displayNameField, this.OnDisplayNameChanged);
            this.RegisterValueChangedCallback<Enum>(this.columnTypeField, this.OnColumnTypeChanged);
            this.RegisterValueChangedCallback<string>(this.targetTypeField, this.OnTargetTypeChanged);
            this.RegisterValueChangedCallback<string>(this.propertyDropdown, this.OnPropertyPathChanged);
            this.RegisterValueChangedCallback<string>(this.propertyPathField, this.OnCustomPropertyPathChanged);
            this.RegisterValueChangedCallback<string>(this.formulaField, this.OnFormulaChanged);
            this.RegisterValueChangedCallback<bool>(this.disableInspectorToggle, this.OnDisableInspectorChanged);
            this.RegisterValueChangedCallback<bool>(this.isReadOnlyToggle, this.OnReadOnlyChanged);
            this.RegisterValueChangedCallback<Enum>(this.dataFieldTypeField, this.OnDataFieldTypeChanged);
            this.RegisterValueChangedCallback<bool>(this.useSliderToggle, this.OnUseSliderChanged);
            this.RegisterValueChangedCallback<float>(this.minValueField, this.OnMinValueChanged);
            this.RegisterValueChangedCallback<float>(this.maxValueField, this.OnMaxValueChanged);
        }

        private void PopulateFields(ColumnDefinition column)
        {
            this.displayNameField.value = column.UserDisplayName;
            this.columnTypeField.value = column.Type;
            this.disableInspectorToggle.value = column.DisableInspector;
            this.isReadOnlyToggle.value = column.IsReadOnly;
            this.targetTypeField.value = column.TargetTypeName;
            this.propertyDropdown.value = column.PropertyPath;
            this.propertyPathField.value = column.PropertyPath;
            this.formulaField.value = column.Formula;
            this.dataFieldTypeField.value = column.DataFieldType;
            this.RecreateDataValueField(column.DataFieldType, column.DataValue, column.UseSlider, column.MinValue, column.MaxValue);
            this.useSliderToggle.value = column.UseSlider;
            this.minValueField.value = column.MinValue;
            this.maxValueField.value = column.MaxValue;
        }

        private void RefreshPropertyDropdown()
        {
            var selectedColumn = this.GetSelectedColumn();
            this.PopulatePropertyDropdown(this.propertyDropdown, selectedColumn?.TargetTypeName);
        }

        private void UpdateFieldVisibility(ColumnType columnType)
        {
            // Show property fields for both Property and Formula columns
            var showPropertyFields = columnType is ColumnType.Property or ColumnType.Formula;

            // For property fields, also check if target type is valid
            var selectedColumn = this.GetSelectedColumn();
            var hasValidTargetType = selectedColumn != null && !string.IsNullOrEmpty(selectedColumn.TargetTypeName);
            var showPropertyPathFields = showPropertyFields && hasValidTargetType;

            // Use the shared utility for field visibility
            this.SetFieldVisibility(this.propertyDropdown, "config-data-field--hidden", showPropertyPathFields);
            this.SetFieldVisibility(this.propertyPathField, "config-property-path-field--hidden", false); // Always manage via dropdown change
            this.SetFieldVisibility(this.disableInspectorToggle, "config-data-field--hidden", columnType == ColumnType.Property);
            this.SetFieldVisibility(this.isReadOnlyToggle, "config-data-field--hidden", columnType == ColumnType.Property);

            // Hide target type field for Data columns (data is always stored locally)
            this.SetFieldVisibility(this.targetTypeField, "config-data-field--hidden", columnType != ColumnType.Data);

            this.SetFieldVisibility(this.formulaField, "config-formula-field--hidden", columnType == ColumnType.Formula);

            var isDataColumn = columnType == ColumnType.Data;
            this.SetFieldVisibility(this.dataFieldTypeField, "config-data-field--hidden", isDataColumn);
            this.SetFieldVisibility(this.dataValueElement, "config-data-field--hidden", isDataColumn);

            this.UpdateDataFieldVisibility();
        }

        private ColumnDefinition? GetSelectedColumn()
        {
            if (this.columnListView.selectedIndex >= 0 && this.columnListView.selectedIndex < this.workingColumns.Count)
            {
                return this.workingColumns[this.columnListView.selectedIndex];
            }

            return null;
        }

        private void UpdateDataFieldVisibility()
        {
            var selectedColumn = this.GetSelectedColumn();
            if (selectedColumn == null)
            {
                return;
            }

            var isDataColumn = selectedColumn.Type == ColumnType.Data;
            var canUseSlider = isDataColumn && selectedColumn.DataFieldType is DataFieldType.Integer or DataFieldType.Float;
            var showRangeFields = isDataColumn && selectedColumn.UseSlider && canUseSlider;

            // Use shared utility for field visibility
            this.SetFieldVisibility(this.useSliderToggle, "config-data-field--hidden", canUseSlider);
            this.SetFieldVisibility(this.minValueField, "config-data-field--hidden", showRangeFields);
            this.SetFieldVisibility(this.maxValueField, "config-data-field--hidden", showRangeFields);
        }

        private void OnTargetTypeFieldClicked(ClickEvent evt)
        {
            var selectedColumn = this.GetSelectedColumn();
            if (selectedColumn == null)
            {
                return;
            }

            this.ShowTypeSelector(typeName =>
            {
                selectedColumn.TargetTypeName = typeName;
                selectedColumn.PropertyPath = string.Empty;
                this.targetTypeField.value = typeName;
                this.propertyPathField.value = string.Empty;
                this.propertyDropdown.value = string.Empty;
                this.RefreshPropertyDropdown();
                this.UpdateFieldVisibility(selectedColumn.Type); // Refresh field visibility after target type selection
                this.refreshColumnListCallback();
            });
        }

        private void OnDisplayNameChanged(ChangeEvent<string> evt)
        {
            var selectedColumn = this.GetSelectedColumn();
            if (selectedColumn != null)
            {
                selectedColumn.UserDisplayName = evt.newValue;
                this.refreshColumnListCallback();
                this.NotifyChange();
            }
        }

        private void NotifyChange()
        {
            this.onChangeCallback();
        }

        private void OnColumnTypeChanged(ChangeEvent<Enum> evt)
        {
            var selectedColumn = this.GetSelectedColumn();
            if (selectedColumn != null && evt.newValue is ColumnType columnType)
            {
                selectedColumn.Type = columnType;
                this.UpdateFieldVisibility(columnType);
                this.refreshColumnListCallback();
                this.ValidateCurrentColumn();
                this.NotifyChange();
            }
        }

        private void OnTargetTypeChanged(ChangeEvent<string> evt)
        {
            var selectedColumn = this.GetSelectedColumn();
            if (selectedColumn != null)
            {
                // Only clear property path if the target type actually changed
                if (evt.previousValue != evt.newValue && !string.IsNullOrEmpty(evt.previousValue))
                {
                    selectedColumn.PropertyPath = string.Empty;
                    this.propertyPathField.value = string.Empty;
                    this.propertyDropdown.value = string.Empty;
                }

                selectedColumn.TargetTypeName = evt.newValue;
                this.RefreshPropertyDropdown();
                this.UpdateFieldVisibility(selectedColumn.Type); // Refresh field visibility after target type change
                this.refreshColumnListCallback();
                this.ValidateCurrentColumn();
                this.NotifyChange();
            }
        }

        private void OnPropertyPathChanged(ChangeEvent<string> evt)
        {
            var selectedColumn = this.GetSelectedColumn();
            this.HandlePropertyDropdownChange(evt, selectedColumn, this.propertyPathField, "config-property-path-field--hidden", this.refreshColumnListCallback,
                (column, path) => column.PropertyPath = path);

            this.ValidateCurrentColumn();
            this.NotifyChange();
        }

        private void OnCustomPropertyPathChanged(ChangeEvent<string> evt)
        {
            var selectedColumn = this.GetSelectedColumn();
            if (selectedColumn != null)
            {
                selectedColumn.PropertyPath = evt.newValue;
                this.refreshColumnListCallback();
                this.ValidateCurrentColumn();
                this.NotifyChange();
            }
        }

        private void OnFormulaChanged(ChangeEvent<string> evt)
        {
            var selectedColumn = this.GetSelectedColumn();
            if (selectedColumn != null)
            {
                selectedColumn.Formula = evt.newValue;

                // Perform real-time cycle detection for formula columns
                this.circularDependencyValidation = selectedColumn.Type == ColumnType.Formula
                    ? this.CheckForCircularDependency(selectedColumn, evt.newValue)
                    : null;

                this.ValidateCurrentColumn();
                this.NotifyChange();
            }
        }

        private ValidationResult? CheckForCircularDependency(ColumnDefinition column, string formula)
        {
            if (string.IsNullOrEmpty(formula))
            {
                return null; // No validation needed for empty formulas
            }

            try
            {
                // Build a complete temporary dependency graph for validation
                // This ensures we have all formula-to-formula dependencies, not just the incomplete graph from sheet view
                var tempDependencyGraph = new FormulaDependencyGraph();

                // Add only formula-to-formula dependencies to the temporary graph
                // Data and Property columns cannot create circular dependencies since they don't reference other columns
                foreach (var existingColumn in this.workingColumns.Where(c => c.Type == ColumnType.Formula && !string.IsNullOrEmpty(c.Formula)))
                {
                    var tokenizer = new FormulaTokenizer();
                    var tokens = tokenizer.Tokenize(existingColumn.Formula);

                    foreach (var token in tokens)
                    {
                        if (token.Type == TokenType.Identifier && token.Value.StartsWith("C"))
                        {
                            if (int.TryParse(token.Value.Substring(1), out var depColumnId))
                            {
                                // Only add dependency if the referenced column is also a formula column
                                var referencedColumn = this.workingColumns.FirstOrDefault(c => c.ColumnId == depColumnId);
                                if (referencedColumn?.Type == ColumnType.Formula)
                                {
                                    tempDependencyGraph.AddDependency(existingColumn.ColumnId, depColumnId);
                                }
                            }
                        }
                    }
                }

                // Parse the current formula to find all column references
                var currentTokenizer = new FormulaTokenizer();
                var currentTokens = currentTokenizer.Tokenize(formula);

                // Collect all formula dependencies from the current formula
                foreach (var token in currentTokens)
                {
                    if (token.Type == TokenType.Identifier && token.Value.StartsWith("C"))
                    {
                        if (int.TryParse(token.Value.Substring(1), out var referencedColumnId))
                        {
                            var referencedColumn = this.workingColumns.FirstOrDefault(c => c.ColumnId == referencedColumnId);

                            // Only add dependencies to formula columns for circular dependency detection
                            if (referencedColumn?.Type == ColumnType.Formula)
                            {
                                tempDependencyGraph.AddDependency(column.ColumnId, referencedColumnId);
                            }
                        }
                    }
                }

                // Now perform a single cycle check on the complete graph
                if (tempDependencyGraph.TryFindCycle(out var cyclePath) && cyclePath.Contains(column.ColumnId))
                {
                    // Find the first referenced column that creates the cycle for error message
                    var firstReferencedInCycle = cyclePath.FirstOrDefault(id => id != column.ColumnId);
                    var referencedColumn = this.workingColumns.FirstOrDefault(c => c.ColumnId == firstReferencedInCycle);
                    var referencedColumnName = referencedColumn?.EffectiveDisplayName ?? $"C{firstReferencedInCycle}";

                    // Create a comprehensive validation result
                    return ValidationResult.Failure($"Circular dependency detected",
                        $"This formula creates a dependency loop involving '{referencedColumnName}' and other columns.",
                        "Remove the reference that creates the circular dependency or reorganize your formulas to avoid the cycle.");
                }

                // If we get here, no cycles were detected
                return null;
            }
            catch (FormulaException ex)
            {
                // Formula parsing error - this will be handled by normal validation
                Debug.LogWarning($"Failed to parse formula for cycle detection: {ex.Message}");
                return null;
            }
        }

        private void OnDisableInspectorChanged(ChangeEvent<bool> evt)
        {
            var selectedColumn = this.GetSelectedColumn();
            if (selectedColumn != null)
            {
                selectedColumn.DisableInspector = evt.newValue;
                this.NotifyChange();
            }
        }

        private void OnReadOnlyChanged(ChangeEvent<bool> evt)
        {
            var selectedColumn = this.GetSelectedColumn();
            if (selectedColumn != null)
            {
                selectedColumn.IsReadOnly = evt.newValue;
                this.NotifyChange();
            }
        }

        private void OnDataFieldTypeChanged(ChangeEvent<Enum> evt)
        {
            var selectedColumn = this.GetSelectedColumn();
            if (selectedColumn != null && evt.newValue is DataFieldType dataFieldType)
            {
                selectedColumn.DataFieldType = dataFieldType;
                this.RecreateDataValueField(dataFieldType, selectedColumn.DataValue, selectedColumn.UseSlider, selectedColumn.MinValue,
                    selectedColumn.MaxValue);

                this.UpdateDataFieldVisibility();
                this.ValidateCurrentColumn();
                this.NotifyChange();
            }
        }

        private void OnDataValueChanged(string newValue)
        {
            var selectedColumn = this.GetSelectedColumn();
            if (selectedColumn != null)
            {
                selectedColumn.DataValue = newValue;
                this.ValidateCurrentColumn();
                this.NotifyChange();
            }
        }

        private void OnUseSliderChanged(ChangeEvent<bool> evt)
        {
            var selectedColumn = this.GetSelectedColumn();
            if (selectedColumn != null)
            {
                selectedColumn.UseSlider = evt.newValue;
                this.RecreateDataValueField(selectedColumn.DataFieldType, selectedColumn.DataValue, evt.newValue, selectedColumn.MinValue,
                    selectedColumn.MaxValue);

                this.UpdateDataFieldVisibility();
                this.ValidateCurrentColumn();
                this.NotifyChange();
            }
        }

        private void OnMinValueChanged(ChangeEvent<float> evt)
        {
            var selectedColumn = this.GetSelectedColumn();
            if (selectedColumn != null)
            {
                selectedColumn.MinValue = evt.newValue;

                // Recreate the data value field to apply new min value if it's a slider
                if (selectedColumn.UseSlider)
                {
                    this.RecreateDataValueField(selectedColumn.DataFieldType, selectedColumn.DataValue, selectedColumn.UseSlider, evt.newValue,
                        selectedColumn.MaxValue);
                }

                this.ValidateCurrentColumn();
                this.NotifyChange();
            }
        }

        private void OnMaxValueChanged(ChangeEvent<float> evt)
        {
            var selectedColumn = this.GetSelectedColumn();
            if (selectedColumn != null)
            {
                selectedColumn.MaxValue = evt.newValue;

                // Recreate the data value field to apply new max value if it's a slider
                if (selectedColumn.UseSlider)
                {
                    this.RecreateDataValueField(selectedColumn.DataFieldType, selectedColumn.DataValue, selectedColumn.UseSlider, selectedColumn.MinValue,
                        evt.newValue);
                }

                this.ValidateCurrentColumn();
                this.NotifyChange();
            }
        }

        /// <summary>
        /// Recreates the data value field with the appropriate UI element for the data field type.
        /// </summary>
        /// <param name="dataFieldType">The data field type to create a field for.</param>
        /// <param name="currentValue">The current value to set.</param>
        /// <param name="useSlider">Whether to use slider controls for numeric types.</param>
        /// <param name="minValue">The minimum value for sliders.</param>
        /// <param name="maxValue">The maximum value for sliders.</param>
        private void RecreateDataValueField(DataFieldType dataFieldType, string currentValue, bool useSlider, float minValue, float maxValue)
        {
            // Use shared utility to recreate the field
            this.dataValueElement = this.RecreateDataValueField(this.detailsForm, this.dataValueElement, this.dataFieldTypeField, dataFieldType,
                "Default Value", "data-value-field", "config-data-field--hidden", useSlider, minValue, maxValue);

            // Bind the value and set up change callback directly on the field
            DataFieldValueBinder.BindElement(this.dataValueElement, dataFieldType, currentValue, this.OnDataValueChanged);

            // Apply visibility based on current column type
            var selectedColumn = this.GetSelectedColumn();
            if (selectedColumn != null)
            {
                this.SetFieldVisibility(this.dataValueElement, "config-data-field--hidden", selectedColumn.Type == ColumnType.Data);
            }
        }

        /// <summary>
        /// Validates the currently selected column and updates the UI with validation results.
        /// </summary>
        private void ValidateCurrentColumn()
        {
            var selectedColumn = this.GetSelectedColumn();
            if (selectedColumn == null)
            {
                this.ClearValidationDisplay();
                return;
            }

            // Check for circular dependencies first (highest priority)
            if (this.circularDependencyValidation is { IsValid: false })
            {
                this.DisplayValidationResult(this.circularDependencyValidation);
                this.UpdateFieldErrorStyling(selectedColumn, this.circularDependencyValidation);
                return;
            }

            // Try to get a sample object for more detailed property validation
            UnityEngine.Object? sampleObject = null;
            if (selectedColumn.Type == ColumnType.Property && !string.IsNullOrEmpty(selectedColumn.TargetTypeName))
            {
                sampleObject = this.GetSampleObjectForValidation(selectedColumn.TargetTypeName);
            }

            // Validate the column using unified validation services
            ValidationResult result;

            if (selectedColumn.Type == ColumnType.Property)
            {
                // Validate property path first
                result = PropertyValidationService.ValidatePropertyPath(selectedColumn.TargetTypeName, selectedColumn.PropertyPath, sampleObject);

                // Then check for duplicates if property path is valid
                if (result.IsValid)
                {
                    result = PropertyValidationService.ValidateDuplicateColumns(selectedColumn, this.workingColumns);
                }
            }
            else if (selectedColumn.Type == ColumnType.Formula)
            {
                // Validate formula syntax and dependencies
                result = this.validator.ValidateColumn(selectedColumn, sampleObject);

                // Also check for duplicates if formula writes to a property
                if (result.IsValid && !string.IsNullOrEmpty(selectedColumn.TargetTypeName) && !string.IsNullOrEmpty(selectedColumn.PropertyPath))
                {
                    result = PropertyValidationService.ValidateDuplicateColumns(selectedColumn, this.workingColumns);
                }
            }
            else
            {
                // Use existing validator for other types
                result = this.validator.ValidateColumn(selectedColumn, sampleObject);
            }

            this.DisplayValidationResult(result);

            // Update field-specific error styling
            this.UpdateFieldErrorStyling(selectedColumn, result);
        }

        /// <summary>
        /// Gets a sample object for property path validation.
        /// </summary>
        /// <param name="targetTypeName">The target type name to find a sample for.</param>
        /// <returns>A sample object of the target type, or null if none found.</returns>
        private UnityEngine.Object? GetSampleObjectForValidation(string targetTypeName)
        {
            // Try to find an existing object in the scene that matches the target type
            try
            {
                var targetType = Type.GetType(targetTypeName);
                if (targetType != null)
                {
                    return ValidationHelper.GetValidationObject(targetType);
                }
            }
            catch (Exception)
            {
                // Ignore errors in sample object creation
            }

            return null;
        }

        /// <summary>
        /// Updates field-specific error styling based on validation results.
        /// </summary>
        /// <param name="column">The column being validated.</param>
        /// <param name="result">The validation result.</param>
        private void UpdateFieldErrorStyling(ColumnDefinition column, ValidationResult result)
        {
            // Clear all field error styling first
            this.ClearFieldErrorStyling(this.targetTypeField);
            this.ClearFieldErrorStyling(this.propertyPathField);
            this.ClearFieldErrorStyling(this.formulaField);

            if (this.dataValueElement != null)
            {
                this.ClearFieldErrorStyling(this.dataValueElement);
            }

            if (!result.IsValid)
            {
                // Add error styling to the specific field that has the issue
                var errorMessage = result.ErrorMessage.ToLower();

                // Handle target type errors (for Property and Formula columns that need types)
                if (errorMessage.Contains("target type") || errorMessage.Contains("could not be resolved") ||
                    (column.Type == ColumnType.Property && errorMessage.Contains("must be specified")))
                {
                    this.ApplyFieldErrorStyling(this.targetTypeField);
                }

                // Handle property path errors (for Property columns)
                else if (errorMessage.Contains("property path") || errorMessage.Contains("not found on type") || errorMessage.Contains("does not exist") ||
                    (errorMessage.Contains("property") && errorMessage.Contains("not")) || (column.Type == ColumnType.Property &&
                        (errorMessage.Contains("path") || errorMessage.Contains("specified"))))
                {
                    this.ApplyFieldErrorStyling(this.propertyPathField);
                }

                // Handle formula errors (for Formula columns)
                else if (errorMessage.Contains("formula") || errorMessage.Contains("syntax error") || errorMessage.Contains("circular dependency") ||
                    errorMessage.Contains("undefined column") || (column.Type == ColumnType.Formula && errorMessage.Contains("must be specified")))
                {
                    this.ApplyFieldErrorStyling(this.formulaField);
                }

                // Handle data value errors (for Data columns)
                else if (errorMessage.Contains("data value") || errorMessage.Contains("boolean") || errorMessage.Contains("integer") ||
                    errorMessage.Contains("number") || errorMessage.Contains("valid") || errorMessage.Contains("slider range") ||
                    (column.Type == ColumnType.Data && (errorMessage.Contains("minimum") || errorMessage.Contains("maximum"))))
                {
                    if (this.dataValueElement != null)
                    {
                        this.ApplyFieldErrorStyling(this.dataValueElement);
                    }
                }
            }
        }

        /// <summary>
        /// Displays validation results in the UI.
        /// </summary>
        /// <param name="result">The validation result to display.</param>
        private void DisplayValidationResult(ValidationResult result)
        {
            if (result.IsValid)
            {
                this.ClearValidationDisplay();
            }
            else
            {
                this.ShowValidationError(result);
            }
        }

        /// <summary>
        /// Shows validation error in the UI.
        /// </summary>
        /// <param name="result">The validation result containing the error.</param>
        private void ShowValidationError(ValidationResult result)
        {
            // Clear existing content
            this.validationSummaryLabel.Clear();

            // Create styled error display similar to main spreadsheet tooltip
            this.CreateStyledErrorDisplay(this.validationSummaryLabel, result);

            this.validationSummaryLabel.style.display = DisplayStyle.Flex;
        }

        /// <summary>
        /// Clears validation error display.
        /// </summary>
        private void ClearValidationDisplay()
        {
            this.validationSummaryLabel.text = string.Empty;
            this.validationSummaryLabel.Clear();
            this.validationSummaryLabel.style.display = DisplayStyle.None;
        }

        /// <summary>
        /// Creates a styled error display matching the main spreadsheet's error tooltip style.
        /// </summary>
        /// <param name="container">The container to add the styled error display to.</param>
        /// <param name="result">The validation result to display.</param>
        private void CreateStyledErrorDisplay(VisualElement container, ValidationResult result)
        {
            var errorDisplayAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(EditorConstants.AssetPath + "ErrorDisplay.uxml");
            var styleSheet = UnityEditor.AssetDatabase.LoadAssetAtPath<StyleSheet>(EditorConstants.AssetPath + "ErrorDisplay.uss");
            var errorElement = errorDisplayAsset.Instantiate();
            errorElement.styleSheets.Add(styleSheet);

            // Populate the template with actual data
            var errorIcon = errorElement.Q<VisualElement>("error-icon");
            var errorTitle = errorElement.Q<Label>("error-title");
            var detailsSection = errorElement.Q<VisualElement>("details-section");
            var detailsText = errorElement.Q<Label>("details-text");
            var suggestionSection = errorElement.Q<VisualElement>("suggestion-section");
            var suggestionText = errorElement.Q<Label>("suggestion-text");

            // Set icon
            var iconTexture = UnityEditor.EditorGUIUtility.IconContent("console.erroricon").image as Texture2D;
            errorIcon.style.backgroundImage = new StyleBackground(iconTexture);

            // Set title
            errorTitle.text = $"Error: {result.ErrorMessage}";

            // Show/hide details section
            if (!string.IsNullOrEmpty(result.DetailedDescription))
            {
                detailsSection.style.display = DisplayStyle.Flex;
                detailsText.text = result.DetailedDescription;
            }
            else
            {
                detailsSection.style.display = DisplayStyle.None;
            }

            // Show/hide suggestion section
            if (!string.IsNullOrEmpty(result.Suggestion))
            {
                suggestionSection.style.display = DisplayStyle.Flex;
                suggestionText.text = result.Suggestion;
            }
            else
            {
                suggestionSection.style.display = DisplayStyle.None;
            }

            container.Add(errorElement);
        }

        /// <summary>
        /// Applies error styling to a form field.
        /// </summary>
        /// <param name="field">The field to apply error styling to.</param>
        private void ApplyFieldErrorStyling(VisualElement field)
        {
            field.AddToClassList("input-field--error");
        }

        /// <summary>
        /// Clears error styling from a form field.
        /// </summary>
        /// <param name="field">The field to clear error styling from.</param>
        private void ClearFieldErrorStyling(VisualElement field)
        {
            field.RemoveFromClassList("input-field--error");
            field.RemoveFromClassList("input-field--warning");
        }
    }
}