// <copyright file="FormulaColumnHandler.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Tabulate.Editor.Handlers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using BovineLabs.Tabulate.Editor.Core;
    using BovineLabs.Tabulate.Editor.Formula;
    using BovineLabs.Tabulate.Editor.Services;
    using BovineLabs.Tabulate.Editor.Variables;
    using UnityEngine;

    public class FormulaColumnHandler
    {
        private readonly ColumnDefinition columnDefinition;
        private readonly FormulaEngine formulaEngine;
        private readonly Func<ColumnDefinition, PropertyColumnHandler> propertyHandlerProvider;
        private PropertyColumnHandler? propertyHandler;
        private FormulaDependencyGraph? dependencyGraph;

        public FormulaColumnHandler(ColumnDefinition columnDefinition, Func<ColumnDefinition, PropertyColumnHandler> propertyHandlerProvider)
        {
            this.columnDefinition = columnDefinition ?? throw new ArgumentNullException(nameof(columnDefinition));
            this.formulaEngine = new FormulaEngine();
            this.propertyHandlerProvider = propertyHandlerProvider;

            // Property handler will be created lazily when needed
            this.UpdatePropertyHandler();
        }

        public object? EvaluateFormula(UnityEngine.Object targetObject, IReadOnlyList<ColumnDefinition> allColumns, Func<ColumnDefinition, UnityEngine.Object, object?> getColumnValue, IReadOnlyList<VariableDefinition>? variables = null, Func<VariableDefinition, object?>? getVariableValue = null)
        {
            if (string.IsNullOrEmpty(this.columnDefinition.Formula))
            {
                return null;
            }

            try
            {
                // Validate formula dependencies before evaluation - use comprehensive validation
                var validationResult = this.formulaEngine.ValidateComplete(this.columnDefinition.Formula, allColumns, this.columnDefinition.ColumnId);
                if (!validationResult.IsValid)
                {
                    return null;
                }

                // Set up column value providers for referenced columns
                this.SetupColumnValueProviders(targetObject, allColumns, getColumnValue);

                // Set up variable value providers for referenced variables
                this.SetupVariableValueProviders(variables, getVariableValue);

                // Evaluate the formula
                var result = this.formulaEngine.Evaluate(this.columnDefinition.Formula);

                // Validate result type if we have a target property
                if (this.propertyHandler != null)
                {
                    var typeValidationResult = this.ValidateResultType(result, targetObject);
                    if (!typeValidationResult.IsValid)
                    {
                        return null;
                    }
                }

                return result;
            }
            catch (FormulaException)
            {
                return null;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Unexpected error evaluating formula for column '{this.columnDefinition.EffectiveDisplayName}': {ex.Message}");
                return null;
            }
        }

        public bool WriteFormulaResult(UnityEngine.Object targetObject, object? formulaResult)
        {
            // Update property handler in case PropertyPath or TargetTypeName changed
            this.UpdatePropertyHandler();

            // Only write if we have a property path, target type, and property handler
            if (this.propertyHandler == null ||
                string.IsNullOrEmpty(this.columnDefinition.PropertyPath) ||
                string.IsNullOrEmpty(this.columnDefinition.TargetTypeName))
            {
                return false;
            }

            try
            {
                return this.propertyHandler.SetValue(targetObject, formulaResult);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to write formula result to property '{this.columnDefinition.PropertyPath}' on object '{targetObject.name}' (type: {targetObject.GetType().Name}): {ex.Message}");
                return false;
            }
        }

        public HashSet<string> GetColumnDependencies()
        {
            if (string.IsNullOrEmpty(this.columnDefinition.Formula))
            {
                return new HashSet<string>();
            }

            return this.formulaEngine.GetColumnReferences(this.columnDefinition.Formula);
        }

        /// <summary>
        /// Registers this formula column with the dependency graph for real-time updates.
        /// </summary>
        /// <param name="graph">The dependency graph to register with.</param>
        public void RegisterWithDependencyGraph(FormulaDependencyGraph graph)
        {
            this.dependencyGraph = graph;

            // Use column ID for consistent dependency tracking
            var dependencies = this.GetColumnDependencies();
            foreach (var dependency in dependencies)
            {
                // Parse dependency as column reference (e.g., "C0", "C1")
                if (dependency.StartsWith("C") && int.TryParse(dependency.Substring(1), out var depColumnId))
                {
                    this.dependencyGraph.AddDependency(this.columnDefinition.ColumnId, depColumnId);
                }
            }
        }

        /// <summary>
        /// Unregisters this formula column from the dependency graph.
        /// </summary>
        public void UnregisterFromDependencyGraph()
        {
            if (this.dependencyGraph != null)
            {
                this.dependencyGraph.RemoveDependencies(this.columnDefinition.ColumnId);
                this.dependencyGraph = null;
            }
        }

        /// <summary>
        /// Validates the column configuration without requiring evaluation context.
        /// </summary>
        /// <param name="allColumns">All column definitions for dependency validation.</param>
        /// <param name="variables">Optional variable definitions for variable reference validation.</param>
        /// <returns>A validation result indicating whether the column is properly configured.</returns>
        public ValidationResult ValidateColumn(IReadOnlyList<ColumnDefinition> allColumns, IReadOnlyList<VariableDefinition>? variables = null)
        {
            // Allow empty formulas (they're valid for incomplete configurations)
            if (string.IsNullOrEmpty(this.columnDefinition.Formula))
            {
                return ValidationResult.Success();
            }

            // Use comprehensive formula validation from FormulaEngine
            var formulaResult = this.formulaEngine.ValidateComplete(this.columnDefinition.Formula, allColumns, this.columnDefinition.ColumnId);
            if (!formulaResult.IsValid)
            {
                return formulaResult;
            }

            // Validate variable references if variables are provided
            if (variables != null)
            {
                var variableResult = this.ValidateVariableReferences(variables);
                if (!variableResult.IsValid)
                {
                    return variableResult;
                }
            }

            // If we have a property path and target type, validate those too
            if (!string.IsNullOrEmpty(this.columnDefinition.PropertyPath) && !string.IsNullOrEmpty(this.columnDefinition.TargetTypeName))
            {
                var propertyResult = TypeResolutionService.ValidateTypeName(this.columnDefinition.TargetTypeName);
                if (!propertyResult.IsValid)
                {
                    return propertyResult;
                }
            }

            return ValidationResult.Success();
        }

        public ValidationResult ValidateResultType(object? result, UnityEngine.Object targetObject)
        {
            // Update property handler in case PropertyPath or TargetTypeName changed
            this.UpdatePropertyHandler();

            if (this.propertyHandler == null || result == null)
            {
                return ValidationResult.Success();
            }

            try
            {
                var expectedType = this.propertyHandler.GetPropertyType(targetObject);
                var resultType = result.GetType();

                // Check if the result type is compatible with the expected property type
                if (this.IsTypeCompatible(resultType, expectedType))
                {
                    return ValidationResult.Success();
                }

                // Special case: allow numeric conversions
                if (this.IsNumericConversionValid(resultType, expectedType))
                {
                    return ValidationResult.Success();
                }

                return ValidationResult.Failure("Formula result type mismatch",
                    $"The formula result of type '{resultType.Name}' is not compatible with the target property's type '{expectedType.Name}'.",
                    "Ensure the formula produces a value that can be assigned to the target property. You may need to convert the result using functions or adjust the formula logic.");
            }
            catch (Exception ex)
            {
                return ValidationResult.Failure("Type validation error",
                    $"An unexpected error occurred while validating the formula result type: {ex.Message}.",
                    "This may indicate a bug in the type validation logic. Please report this issue.");
            }
        }

        /// <summary>
        /// Validates variable references in the formula.
        /// </summary>
        /// <param name="variables">The available variable definitions.</param>
        /// <returns>A validation result indicating whether all variable references are valid.</returns>
        private ValidationResult ValidateVariableReferences(IReadOnlyList<VariableDefinition> variables)
        {
            try
            {
                var referencedVariables = this.GetVariableDependencies();
                var missingVariables = new List<string>();

                foreach (var variableReference in referencedVariables)
                {
                    // Parse variable reference (e.g., "V0", "V1", "V10")
                    if (!this.TryParseVariableReference(variableReference, out var variableId))
                    {
                        missingVariables.Add(variableReference);
                        continue;
                    }

                    var referencedVariable = variables.FirstOrDefault(v => v.VariableId == variableId);
                    if (referencedVariable == null)
                    {
                        missingVariables.Add(variableReference);
                    }
                }

                if (missingVariables.Count > 0)
                {
                    return ValidationResult.Failure("Unknown variable reference",
                        $"The formula references one or more variables that are not defined: {string.Join(", ", missingVariables)}.",
                        "Ensure all referenced variables are defined in the sheet's variable list and that their names are spelled correctly.");
                }

                return ValidationResult.Success();
            }
            catch (FormulaException ex)
            {
                return ValidationResult.Failure("Variable validation error", $"An error occurred while validating variable references: {ex.Message}.",
                    "Ensure variable references are correctly formatted (e.g., V1, V2). If the issue persists, it may be a bug.");
            }
            catch (Exception ex)
            {
                return ValidationResult.Failure("Unexpected variable validation error",
                    $"An unexpected error occurred during variable validation: {ex.Message}.",
                    "This may indicate a bug in the validation logic. Please report this issue.");
            }
        }

        private void UpdatePropertyHandler()
        {
            // Create or recreate property handler if both PropertyPath and TargetTypeName are set
            if (!string.IsNullOrEmpty(this.columnDefinition.PropertyPath) && !string.IsNullOrEmpty(this.columnDefinition.TargetTypeName))
            {
                this.propertyHandler = this.propertyHandlerProvider.Invoke(this.columnDefinition);
            }
            else
            {
                this.propertyHandler = null;
            }
        }

        private bool IsTypeCompatible(Type resultType, Type expectedType)
        {
            // Exact match
            if (resultType == expectedType)
            {
                return true;
            }

            // Check if result type can be assigned to expected type
            if (expectedType.IsAssignableFrom(resultType))
            {
                return true;
            }

            // Handle object type (accepts anything)
            if (expectedType == typeof(object))
            {
                return true;
            }

            return false;
        }

        private bool IsNumericConversionValid(Type resultType, Type expectedType)
        {
            // Define numeric types
            var numericTypes = new HashSet<Type>
            {
                typeof(int), typeof(float), typeof(double), typeof(decimal),
                typeof(byte), typeof(sbyte), typeof(short), typeof(ushort),
                typeof(uint), typeof(long), typeof(ulong),
            };

            // Allow conversion between any numeric types
            return numericTypes.Contains(resultType) && numericTypes.Contains(expectedType);
        }

        private void SetupColumnValueProviders(UnityEngine.Object targetObject, IReadOnlyList<ColumnDefinition> allColumns, Func<ColumnDefinition, UnityEngine.Object, object?> getColumnValue)
        {
            this.formulaEngine.ClearColumnValueProviders();

            var referencedColumns = this.GetColumnDependencies();

            foreach (var columnReference in referencedColumns)
            {
                // Parse column reference (e.g., "C0", "C1", "C10")
                if (!this.TryParseColumnReference(columnReference, out var columnId))
                {
                    Debug.LogWarning($"Invalid column reference '{columnReference}' in formula for '{this.columnDefinition.EffectiveDisplayName}'");
                    continue;
                }

                // Find the column definition by column ID
                var referencedColumn = allColumns.FirstOrDefault(c => c.ColumnId == columnId);

                if (referencedColumn != null)
                {
                    // Create a value provider that gets the current value of the referenced column
                    this.formulaEngine.SetColumnValueProvider(columnReference, () =>
                    {
                        var value = getColumnValue(referencedColumn, targetObject);
                        return FormulaTypeConverter.ConvertForValueProvider(value);
                    });
                }
                else
                {
                    Debug.LogWarning($"Formula references unknown column '{columnReference}' in '{this.columnDefinition.EffectiveDisplayName}'");
                }
            }
        }

        private bool TryParseColumnReference(string columnReference, out int columnId)
        {
            columnId = 0;

            if (string.IsNullOrEmpty(columnReference) || !columnReference.StartsWith("C"))
            {
                return false;
            }

            var numberPart = columnReference.Substring(1);
            return int.TryParse(numberPart, out columnId);
        }

        private void SetupVariableValueProviders(IReadOnlyList<VariableDefinition>? variables, Func<VariableDefinition, object?>? getVariableValue)
        {
            this.formulaEngine.ClearVariableValueProviders();

            if (variables == null || getVariableValue == null)
            {
                return;
            }

            var referencedVariables = this.GetVariableDependencies();

            foreach (var variableReference in referencedVariables)
            {
                // Parse variable reference (e.g., "V0", "V1", "V10")
                if (!this.TryParseVariableReference(variableReference, out var variableId))
                {
                    Debug.LogWarning($"Invalid variable reference '{variableReference}' in formula for '{this.columnDefinition.EffectiveDisplayName}'");
                    continue;
                }

                // Find the variable definition by variable ID
                var referencedVariable = variables.FirstOrDefault(v => v.VariableId == variableId);

                if (referencedVariable != null)
                {
                    // Create a value provider that gets the current value of the referenced variable
                    this.formulaEngine.SetVariableValueProvider(variableReference, () =>
                    {
                        var value = getVariableValue(referencedVariable);
                        return FormulaTypeConverter.ConvertForValueProvider(value);
                    });
                }
                else
                {
                    Debug.LogWarning($"Formula references unknown variable '{variableReference}' in '{this.columnDefinition.EffectiveDisplayName}'");
                }
            }
        }

        private HashSet<string> GetVariableDependencies()
        {
            if (string.IsNullOrEmpty(this.columnDefinition.Formula))
            {
                return new HashSet<string>();
            }

            try
            {
                return this.formulaEngine.GetVariableReferences(this.columnDefinition.Formula);
            }
            catch (FormulaException ex)
            {
                Debug.LogWarning($"Failed to extract variable dependencies from formula in column '{this.columnDefinition.EffectiveDisplayName}': {ex.Message}");
                return new HashSet<string>();
            }
        }

        private bool TryParseVariableReference(string variableReference, out int variableId)
        {
            variableId = 0;

            if (string.IsNullOrEmpty(variableReference) || !variableReference.StartsWith("V"))
            {
                return false;
            }

            var numberPart = variableReference.Substring(1);
            return int.TryParse(numberPart, out variableId);
        }
    }
}