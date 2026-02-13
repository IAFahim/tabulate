// <copyright file="ColumnConfigurationValidator.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Tabulate.Editor.Core
{
    using BovineLabs.Tabulate.Editor.Formula;
    using BovineLabs.Tabulate.Editor.Services;

    /// <summary>
    /// Validates column configuration settings and provides detailed error messages with suggestions.
    /// </summary>
    public class ColumnConfigurationValidator
    {
        private readonly FormulaValidator formulaValidator;
        private readonly SheetDefinition sheetDefinition;

        /// <summary>
        /// Initializes a new instance of the <see cref="ColumnConfigurationValidator"/> class.
        /// </summary>
        /// <param name="definition">The sheet definition.</param>
        /// <param name="formulaValidator">The formula validator to use for formula syntax checking.</param>
        public ColumnConfigurationValidator(SheetDefinition definition, FormulaValidator formulaValidator)
        {
            this.sheetDefinition = definition;
            this.formulaValidator = formulaValidator;
        }

        /// <summary>
        /// Validates the complete column configuration.
        /// </summary>
        /// <param name="column">The column definition to validate.</param>
        /// <param name="sampleObject">Optional sample object for property path validation.</param>
        /// <returns>A validation result with detailed error information.</returns>
        public ValidationResult ValidateColumn(ColumnDefinition column, UnityEngine.Object? sampleObject = null)
        {
            // Validate based on column type
            var result = column.Type switch
            {
                ColumnType.Property => this.ValidatePropertyColumn(column, sampleObject),
                ColumnType.Formula => this.ValidateFormulaColumn(column),
                ColumnType.Data => this.ValidateDataColumn(column),
                _ => ValidationResult.Failure($"Unknown column type: {column.Type}",
                    "The column type is not recognized by the system. This may be due to a corrupted or outdated package version.",
                    "Ensure you are using a compatible version of the Tabulate package. If the issue persists, consider recreating the column or reporting a bug."),
            };

            return result;
        }

        /// <summary>
        /// Validates a property column configuration.
        /// </summary>
        /// <param name="column">The column definition.</param>
        /// <param name="sampleObject">Optional sample object for validation.</param>
        /// <returns>Validation result with specific property column errors.</returns>
        private ValidationResult ValidatePropertyColumn(ColumnDefinition column, UnityEngine.Object? sampleObject = null)
        {
            // Check if target type is specified
            if (string.IsNullOrEmpty(column.TargetTypeName))
            {
                return ValidationResult.Failure("Missing target type for property column",
                    "Property columns require a target type to identify which object or component to read the property from.",
                    "Click the target type field and select the appropriate Unity component or ScriptableObject type from the list.");
            }

            // Validate target type can be resolved
            var targetTypeValidation = TypeResolutionService.ValidateTypeName(column.TargetTypeName);
            if (!targetTypeValidation.IsValid)
            {
                return targetTypeValidation;
            }

            // Check if property path is specified
            if (string.IsNullOrEmpty(column.PropertyPath))
            {
                return ValidationResult.Failure("Missing property path for property column",
                    "A property path must be specified to identify which property to read from the selected target type.",
                    "Use the property dropdown to select an available property. If the desired property is not listed, you can enter a custom path manually.");
            }

            // Validate property path with sample object if available
            if (sampleObject != null)
            {
                var propertyValidation = PropertyValidationService.ValidatePropertyPath(column.TargetTypeName, column.PropertyPath, sampleObject);
                if (!propertyValidation.IsValid)
                {
                    return propertyValidation;
                }
            }

            return ValidationResult.Success();
        }

        /// <summary>
        /// Validates a formula column configuration.
        /// </summary>
        /// <param name="column">The column definition.</param>
        /// <returns>Validation result with specific formula column errors.</returns>
        private ValidationResult ValidateFormulaColumn(ColumnDefinition column)
        {
            // Check if formula is specified
            if (string.IsNullOrEmpty(column.Formula))
            {
                return ValidationResult.Failure("Missing formula for formula column",
                    "Formula columns require a mathematical or logical expression to calculate their values. The formula can reference other columns or use constant values.",
                    "Enter a formula using column references (e.g., C1, C2) and supported operators and functions. For example: C1 + C2 * 10.");
            }

            return this.formulaValidator.ValidateComplete(column.Formula, this.sheetDefinition.Columns, column.ColumnId);
        }

        /// <summary>
        /// Validates a data column configuration.
        /// </summary>
        /// <param name="column">The column definition.</param>
        /// <returns>Validation result with specific data column errors.</returns>
        private ValidationResult ValidateDataColumn(ColumnDefinition column)
        {
            // Validate data value against data field type
            if (!string.IsNullOrEmpty(column.DataValue))
            {
                var dataValidation = DataValidationService.ValidateDataValue(column.DataFieldType, column.DataValue);
                if (!dataValidation.IsValid)
                {
                    return dataValidation;
                }
            }

            // Validate slider configuration for numeric types (not duplicated elsewhere)
            return this.ValidateSliderConfiguration(column);
        }

        /// <summary>
        /// Validates slider configuration for data columns.
        /// </summary>
        private ValidationResult ValidateSliderConfiguration(ColumnDefinition column)
        {
            if (column is { UseSlider: true, DataFieldType: DataFieldType.Integer or DataFieldType.Float })
            {
                if (column.MinValue >= column.MaxValue)
                {
                    return ValidationResult.Failure("Invalid slider range",
                        $"The minimum value ({column.MinValue}) of a slider must be strictly less than the maximum value ({column.MaxValue}).",
                        "Adjust the minimum or maximum values to ensure the slider has a valid range.");
                }
            }

            return ValidationResult.Success();
        }
    }
}