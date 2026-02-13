// <copyright file="VariableDependencyGraph.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Tabulate.Editor.Variables
{
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Manages dependencies between variables and between variables and columns
    /// to ensure proper evaluation order and detect circular dependencies.
    /// </summary>
    public class VariableDependencyGraph
    {
        // Variable -> Variables it depends on
        private readonly Dictionary<int, HashSet<int>> variableDependencies = new();

        // Variable -> Columns it depends on
        private readonly Dictionary<int, HashSet<int>> variableColumnDependencies = new();

        // Variable -> Variables that depend on it
        private readonly Dictionary<int, HashSet<int>> variableDependents = new();

        // Column -> Variables that depend on it
        private readonly Dictionary<int, HashSet<int>> columnVariableDependents = new();

        // All variables that have formulas
        private readonly HashSet<int> formulaVariables = new();

        /// <summary>
        /// Adds a dependency relationship between a variable and another variable.
        /// </summary>
        /// <param name="variable">The variable that has the dependency.</param>
        /// <param name="referencedVariable">The variable it depends on.</param>
        public void AddVariableDependency(int variable, int referencedVariable)
        {
            if (variable < 0 || referencedVariable < 0)
            {
                return;
            }

            // Track this as a formula variable
            this.formulaVariables.Add(variable);

            // Add to variable -> variable dependencies
            if (!this.variableDependencies.ContainsKey(variable))
            {
                this.variableDependencies[variable] = new HashSet<int>();
            }

            this.variableDependencies[variable].Add(referencedVariable);

            // Add to variable dependents
            if (!this.variableDependents.ContainsKey(referencedVariable))
            {
                this.variableDependents[referencedVariable] = new HashSet<int>();
            }

            this.variableDependents[referencedVariable].Add(variable);
        }

        /// <summary>
        /// Adds a dependency relationship between a variable and a column.
        /// </summary>
        /// <param name="variable">The variable that depends on the column.</param>
        /// <param name="referencedColumn">The column it depends on.</param>
        public void AddColumnDependency(int variable, int referencedColumn)
        {
            if (variable < 0 || referencedColumn < 0)
            {
                return;
            }

            // Track this as a formula variable
            this.formulaVariables.Add(variable);

            // Add to variable -> column dependencies
            if (!this.variableColumnDependencies.ContainsKey(variable))
            {
                this.variableColumnDependencies[variable] = new HashSet<int>();
            }

            this.variableColumnDependencies[variable].Add(referencedColumn);

            // Add to column -> variable dependents
            if (!this.columnVariableDependents.ContainsKey(referencedColumn))
            {
                this.columnVariableDependents[referencedColumn] = new HashSet<int>();
            }

            this.columnVariableDependents[referencedColumn].Add(variable);
        }

        /// <summary>
        /// Gets the evaluation order for all variables.
        /// </summary>
        /// <returns>List of all variables in topological order.</returns>
        public List<int> GetVariableEvaluationOrder()
        {
            return this.TopologicalSortVariables(this.formulaVariables.ToList());
        }

        /// <summary>
        /// Gets all variables that depend on the specified variable.
        /// </summary>
        /// <param name="variableId">The variable ID to get dependents for.</param>
        /// <returns>List of variable IDs that depend on the specified variable.</returns>
        public List<int> GetDependentVariables(int variableId)
        {
            if (this.variableDependents.TryGetValue(variableId, out var dependents))
            {
                return dependents.ToList();
            }

            return new List<int>();
        }

        /// <summary>
        /// Clears all dependencies.
        /// </summary>
        public void Clear()
        {
            this.variableDependencies.Clear();
            this.variableColumnDependencies.Clear();
            this.variableDependents.Clear();
            this.columnVariableDependents.Clear();
            this.formulaVariables.Clear();
        }

        private List<int> TopologicalSortVariables(List<int> variables)
        {
            var inDegree = new Dictionary<int, int>();
            var result = new List<int>();
            var queue = new Queue<int>();

            // Initialize in-degree count
            foreach (var variable in variables)
            {
                inDegree[variable] = 0;
            }

            // Calculate in-degrees from variable dependencies
            foreach (var variable in variables)
            {
                if (this.variableDependencies.TryGetValue(variable, out var deps))
                {
                    foreach (var dep in deps)
                    {
                        if (variables.Contains(dep))
                        {
                            inDegree[variable]++;
                        }
                    }
                }
            }

            // Add variables with no dependencies to queue
            foreach (var kvp in inDegree)
            {
                if (kvp.Value == 0)
                {
                    queue.Enqueue(kvp.Key);
                }
            }

            // Process queue
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                result.Add(current);

                // Reduce in-degree for dependents
                if (this.variableDependents.TryGetValue(current, out var dependentVariables))
                {
                    foreach (var dependent in dependentVariables)
                    {
                        if (variables.Contains(dependent))
                        {
                            inDegree[dependent]--;
                            if (inDegree[dependent] == 0)
                            {
                                queue.Enqueue(dependent);
                            }
                        }
                    }
                }
            }

            return result;
        }
    }
}