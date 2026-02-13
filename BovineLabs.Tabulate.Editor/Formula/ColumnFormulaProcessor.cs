// <copyright file="ColumnFormulaProcessor.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Tabulate.Editor.Formula
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using BovineLabs.Tabulate.Editor.Core;
    using BovineLabs.Tabulate.Editor.Handlers;
    using UnityEditor;
    using UnityEngine;
    using Object = UnityEngine.Object;

    /// <summary>
    /// Handles the application of one-shot formulas to entire columns.
    /// </summary>
    public class ColumnFormulaProcessor
    {
        private readonly Func<ColumnDefinition, PropertyColumnHandler> propertyHandlerProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="ColumnFormulaProcessor"/> class.
        /// </summary>
        /// <param name="propertyHandlerProvider">Optional provider for cached PropertyColumnHandler instances.</param>
        public ColumnFormulaProcessor(Func<ColumnDefinition, PropertyColumnHandler> propertyHandlerProvider)
        {
            this.propertyHandlerProvider = propertyHandlerProvider;
        }

        /// <summary>
        /// Previews the results of applying a formula to a subset of objects.
        /// </summary>
        /// <param name="formula">The formula to apply.</param>
        /// <param name="sheetDefinition">The sheet definition.</param>
        /// <param name="objects">The objects to preview against.</param>
        /// <returns>The preview result.</returns>
        public PreviewResult PreviewFormulaResults(string formula, SheetDefinition sheetDefinition, Object[] objects)
        {
            var result = new PreviewResult();

            try
            {
                var formulaEngine = new FormulaEngine();

                for (int objectIndex = 0; objectIndex < objects.Length; objectIndex++)
                {
                    var currentObject = objects[objectIndex];
                    if (currentObject == null)
                    {
                        result.PreviewValues.Add("null");
                        continue;
                    }

                    // Setup column value providers for this object
                    this.SetupColumnValueProviders(formulaEngine, sheetDefinition, currentObject);

                    try
                    {
                        var evaluatedValue = formulaEngine.Evaluate(formula);
                        var displayValue = this.ConvertValueToString(evaluatedValue);
                        result.PreviewValues.Add(displayValue);
                    }
                    catch (Exception ex)
                    {
                        result.HasErrors = true;
                        result.ErrorMessage = ex.Message;
                        return result;
                    }
                }
            }
            catch (Exception ex)
            {
                result.HasErrors = true;
                result.ErrorMessage = ex.Message;
            }

            return result;
        }

        /// <summary>
        /// Applies a formula to all objects in the specified column.
        /// </summary>
        /// <param name="targetColumn">The target column.</param>
        /// <param name="formula">The formula to apply.</param>
        /// <param name="sheetDefinition">The sheet definition.</param>
        /// <param name="objects">The objects to apply the formula to.</param>
        /// <returns>True if successful, false otherwise.</returns>
        public bool ApplyFormulaToColumn(ColumnDefinition targetColumn, string formula, SheetDefinition sheetDefinition, Object[] objects)
        {
            try
            {
                var formulaEngine = new FormulaEngine();
                var processedCount = 0;
                var errorCount = 0;

                // Record undo operation
                Undo.RecordObject(sheetDefinition, $"Apply Formula to {targetColumn.EffectiveDisplayName}");

                foreach (var currentObject in objects)
                {
                    if (currentObject == null)
                    {
                        continue;
                    }

                    try
                    {
                        // Setup column value providers for this object
                        this.SetupColumnValueProviders(formulaEngine, sheetDefinition, currentObject);

                        // Evaluate formula
                        var evaluatedValue = formulaEngine.Evaluate(formula);

                        // Apply result to target column
                        var success = this.ApplyValueToColumn(targetColumn, sheetDefinition, currentObject, evaluatedValue);
                        if (success)
                        {
                            processedCount++;
                        }
                        else
                        {
                            errorCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Error applying formula to object '{currentObject.name}': {ex.Message}");
                        errorCount++;
                    }
                }

                // Mark sheet definition as dirty
                EditorUtility.SetDirty(sheetDefinition);

                return errorCount == 0;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error applying formula to column: {ex.Message}");
                return false;
            }
        }

        private void SetupColumnValueProviders(FormulaEngine formulaEngine, SheetDefinition sheetDefinition, Object currentObject)
        {
            formulaEngine.ClearColumnValueProviders();

            // Setup column value providers (C0, C1, etc.)
            for (int columnIndex = 0; columnIndex < sheetDefinition.Columns.Length; columnIndex++)
            {
                var column = sheetDefinition.Columns[columnIndex];
                var columnName = $"C{columnIndex}";

                formulaEngine.SetColumnValueProvider(columnName, () => this.GetColumnValue(column, sheetDefinition, currentObject));
            }
        }

        private object GetColumnValue(ColumnDefinition column, SheetDefinition sheetDefinition, Object targetObject)
        {
            switch (column.Type)
            {
                case ColumnType.Data:
                    var dataHandler = new DataColumnHandler(column);
                    var dataValue = dataHandler.GetValue(targetObject, sheetDefinition);
                    return this.ParseStringValue(dataValue, column.DataFieldType);

                case ColumnType.Property:
                    var propertyHandler = this.propertyHandlerProvider(column);
                    var propertyTargetObject = this.GetPropertyTargetObject(column, targetObject);
                    return propertyHandler.GetValue(propertyTargetObject) ?? 0f;

                case ColumnType.Formula:
                    // For formula columns, we would need to evaluate them, but for now return 0
                    // This could be enhanced to support formula dependencies
                    return 0f;

                default:
                    return 0f;
            }
        }

        private Object GetPropertyTargetObject(ColumnDefinition column, Object targetObject)
        {
            // If no specific target type is specified, use the object as-is
            if (string.IsNullOrEmpty(column.TargetTypeName))
            {
                return targetObject;
            }

            // If the target object is a GameObject and we have a TargetTypeName, get the component
            if (targetObject is GameObject gameObject && !string.IsNullOrEmpty(column.TargetTypeName))
            {
                var type = Type.GetType(column.TargetTypeName);
                if (type != null)
                {
                    // Try to find the component by type name
                    var component = gameObject.GetComponent(type);
                    if (component != null)
                    {
                        return component;
                    }
                }

                Debug.LogWarning($"Component '{column.TargetTypeName}' not found on GameObject '{gameObject.name}'");
            }

            // Fallback to the original object
            return targetObject;
        }

        private object ParseStringValue(string? value, DataFieldType dataFieldType = DataFieldType.Float)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return dataFieldType switch
                {
                    DataFieldType.Integer => 0,
                    DataFieldType.Float => 0f,
                    DataFieldType.Boolean => false,
                    _ => 0f,
                };
            }

            // Parse based on the specified data field type
            switch (dataFieldType)
            {
                case DataFieldType.Boolean:
                    if (bool.TryParse(value, out var boolValue))
                    {
                        return boolValue;
                    }

                    // Fallback to false for invalid boolean strings
                    return false;

                case DataFieldType.Integer:
                    if (int.TryParse(value, out var intValue))
                    {
                        return intValue;
                    }

                    // Try float parsing as fallback for integers
                    if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var floatAsInt))
                    {
                        return (int)floatAsInt;
                    }

                    return 0;

                case DataFieldType.Float:
                default:
                    // Try to parse as number first
                    if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var floatValue))
                    {
                        return floatValue;
                    }

                    // Try to parse as boolean as fallback
                    if (bool.TryParse(value, out var boolFallback))
                    {
                        return boolFallback;
                    }

                    // Return as string for unrecognized values
                    return value ?? string.Empty;
            }
        }

        private bool ApplyValueToColumn(ColumnDefinition targetColumn, SheetDefinition sheetDefinition, Object targetObject, object evaluatedValue)
        {
            switch (targetColumn.Type)
            {
                case ColumnType.Data:
                    var dataHandler = new DataColumnHandler(targetColumn);
                    var stringValue = this.ConvertValueToString(evaluatedValue);
                    return dataHandler.SetValue(targetObject, sheetDefinition, stringValue);

                case ColumnType.Property:
                    var propertyHandler = this.propertyHandlerProvider(targetColumn);
                    var propertyTargetObject = this.GetPropertyTargetObject(targetColumn, targetObject);

                    // Record undo for the object being modified
                    Undo.RecordObject(propertyTargetObject, $"Apply Formula to {targetColumn.EffectiveDisplayName}");
                    return propertyHandler.SetValue(propertyTargetObject, evaluatedValue);

                default:
                    Debug.LogWarning($"Cannot apply formula results to column type: {targetColumn.Type}");
                    return false;
            }
        }

        private string ConvertValueToString(object value)
        {
            if (value is float floatVal)
            {
                return floatVal.ToString("G", CultureInfo.InvariantCulture);
            }

            if (value is double doubleVal)
            {
                return doubleVal.ToString("G", CultureInfo.InvariantCulture);
            }

            if (value is bool boolVal)
            {
                return boolVal.ToString().ToLower();
            }

            return value.ToString();
        }

        /// <summary>
        /// Represents the result of a formula preview operation.
        /// </summary>
        public class PreviewResult
        {
            /// <summary>
            /// Gets or sets a value indicating whether the preview has errors.
            /// </summary>
            public bool HasErrors { get; set; }

            /// <summary>
            /// Gets or sets the error message if there are errors.
            /// </summary>
            public string? ErrorMessage { get; set; }

            /// <summary>
            /// Gets or sets the preview values.
            /// </summary>
            public List<string> PreviewValues { get; set; } = new();
        }
    }
}