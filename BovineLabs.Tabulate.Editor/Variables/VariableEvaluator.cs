// <copyright file="VariableEvaluator.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Tabulate.Editor.Variables
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using BovineLabs.Tabulate.Editor.Core;
    using BovineLabs.Tabulate.Editor.Formula;
    using BovineLabs.Tabulate.Editor.Handlers;
    using UnityEngine;

    /// <summary>
    /// Evaluates variable values based on their type and configuration.
    /// </summary>
    public class VariableEvaluator
    {
        private readonly Dictionary<int, object?> variableCache = new();
        private readonly FormulaEngine formulaEngine = new();
        private VariableDependencyGraph? dependencyGraph;
        private readonly Func<ColumnDefinition, PropertyColumnHandler>? propertyHandlerProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="VariableEvaluator"/> class.
        /// </summary>
        /// <param name="propertyHandlerProvider">Optional provider for cached PropertyColumnHandler instances.</param>
        public VariableEvaluator(Func<ColumnDefinition, PropertyColumnHandler>? propertyHandlerProvider = null)
        {
            this.propertyHandlerProvider = propertyHandlerProvider;
        }

        public void SetDependencyGraph(VariableDependencyGraph graph)
        {
            this.dependencyGraph = graph;
        }

        /// <summary>
        /// Evaluates all variables in the proper dependency order.
        /// </summary>
        /// <param name="variables">The variables to evaluate.</param>
        /// <param name="columns">All column definitions for column reference variables.</param>
        /// <param name="getColumnValue">Function to get column values for column reference variables.</param>
        /// <param name="targetObject">The target object for property variables.</param>
        public void EvaluateAllVariables(IReadOnlyList<VariableDefinition> variables, IReadOnlyList<ColumnDefinition> columns, Func<ColumnDefinition, UnityEngine.Object, object?>? getColumnValue = null, UnityEngine.Object? targetObject = null)
        {
            this.variableCache.Clear();

            if (this.dependencyGraph == null)
            {
                // Fallback: evaluate in definition order if no dependency graph
                foreach (var variable in variables)
                {
                    this.EvaluateVariable(variable, columns, getColumnValue, targetObject);
                }

                return;
            }

            // Get evaluation order from dependency graph
            var evaluationOrder = this.dependencyGraph.GetVariableEvaluationOrder();

            foreach (var variableId in evaluationOrder)
            {
                var variable = variables.FirstOrDefault(v => v.VariableId == variableId);
                if (variable != null)
                {
                    this.EvaluateVariable(variable, columns, getColumnValue, targetObject);
                }
            }

            // Evaluate any remaining variables not in the dependency graph
            foreach (var variable in variables)
            {
                if (!this.variableCache.ContainsKey(variable.VariableId))
                {
                    this.EvaluateVariable(variable, columns, getColumnValue, targetObject);
                }
            }
        }

        /// <summary>
        /// Gets the cached value of a variable.
        /// </summary>
        /// <param name="variableId">The variable ID.</param>
        /// <returns>The cached variable value or null if not evaluated.</returns>
        public object? GetVariableValue(int variableId)
        {
            return this.variableCache.TryGetValue(variableId, out var value) ? value : null;
        }

        /// <summary>
        /// Gets the cached value of a variable by definition.
        /// </summary>
        /// <param name="variable">The variable definition.</param>
        /// <returns>The cached variable value or null if not evaluated.</returns>
        public object? GetVariableValue(VariableDefinition variable)
        {
            return this.GetVariableValue(variable.VariableId);
        }

        private void EvaluateVariable(VariableDefinition variable, IReadOnlyList<ColumnDefinition> allColumns, Func<ColumnDefinition, UnityEngine.Object, object?>? getColumnValue, UnityEngine.Object? targetObject)
        {
            try
            {
                object? value = variable.Type switch
                {
                    VariableType.Data => this.EvaluateDataVariable(variable),
                    VariableType.Property => this.EvaluatePropertyVariable(variable),
                    VariableType.Formula => this.EvaluateFormulaVariable(variable, allColumns, getColumnValue, targetObject),
                    _ => null,
                };

                this.variableCache[variable.VariableId] = value;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to evaluate variable '{variable.EffectiveDisplayName}' (V{variable.VariableId}): {ex.Message}");
                this.variableCache[variable.VariableId] = null;
            }
        }

        private object EvaluateDataVariable(VariableDefinition variable)
        {
            return variable.GetParsedDataValue();
        }

        private object? EvaluatePropertyVariable(VariableDefinition variable)
        {
            if (variable.TargetObject == null)
            {
                Debug.LogWarning($"Cannot evaluate property variable '{variable.EffectiveDisplayName}': no target object specified");
                return null;
            }

            if (string.IsNullOrEmpty(variable.TargetTypeName) || string.IsNullOrEmpty(variable.PropertyPath))
            {
                Debug.LogWarning($"Property variable '{variable.EffectiveDisplayName}' has incomplete configuration");
                return null;
            }

            try
            {
                var columnDefinition = new ColumnDefinition
                {
                    TargetTypeName = variable.TargetTypeName,
                    PropertyPath = variable.PropertyPath,
                };

                var handler = this.propertyHandlerProvider?.Invoke(columnDefinition) ?? new PropertyColumnHandler(columnDefinition);
                return handler.GetValue(variable.TargetObject);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to read property for variable '{variable.EffectiveDisplayName}': {ex.Message}");
                return null;
            }
        }

        private object? EvaluateFormulaVariable(VariableDefinition variable, IReadOnlyList<ColumnDefinition> allColumns, Func<ColumnDefinition, UnityEngine.Object, object?>? getColumnValue, UnityEngine.Object? targetObject)
        {
            if (string.IsNullOrEmpty(variable.Formula))
            {
                return null;
            }

            try
            {
                // Setup variable value providers
                this.SetupVariableValueProviders(variable.Formula);

                // Setup column value providers if needed
                if (getColumnValue != null && targetObject != null)
                {
                    this.SetupColumnValueProviders(variable.Formula, allColumns, getColumnValue, targetObject);
                }

                return this.formulaEngine.Evaluate(variable.Formula);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to evaluate formula variable '{variable.EffectiveDisplayName}': {ex.Message}");
                return null;
            }
        }

        private void SetupVariableValueProviders(string formula)
        {
            this.formulaEngine.ClearVariableValueProviders();

            var referencedVariables = this.formulaEngine.GetVariableReferences(formula);

            foreach (var variableRef in referencedVariables)
            {
                if (!this.TryParseVariableReference(variableRef, out var variableId))
                {
                    continue;
                }

                // Use cached value if available
                if (this.variableCache.TryGetValue(variableId, out var cachedValue))
                {
                    this.formulaEngine.SetVariableValueProvider(variableRef, () => FormulaTypeConverter.ConvertToFloat(cachedValue));
                }
            }
        }

        private void SetupColumnValueProviders(string formula, IReadOnlyList<ColumnDefinition> allColumns, Func<ColumnDefinition, UnityEngine.Object, object?> getColumnValue, UnityEngine.Object targetObject)
        {
            this.formulaEngine.ClearColumnValueProviders();

            var referencedColumns = this.formulaEngine.GetColumnReferences(formula);

            foreach (var columnRef in referencedColumns)
            {
                if (!this.TryParseColumnReference(columnRef, out var columnId))
                {
                    continue;
                }

                var referencedColumn = allColumns.FirstOrDefault(c => c.ColumnId == columnId);
                if (referencedColumn != null)
                {
                    this.formulaEngine.SetColumnValueProvider(columnRef, () =>
                    {
                        var value = getColumnValue(referencedColumn, targetObject);
                        return FormulaTypeConverter.ConvertToFloat(value);
                    });
                }
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
    }
}