// <copyright file="FormulaDependencyGraph.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Tabulate.Editor.Formula
{
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Manages dependencies between formula columns to ensure proper calculation order
    /// and detect circular dependencies.
    /// </summary>
    public class FormulaDependencyGraph
    {
        private readonly Dictionary<int, HashSet<int>> dependencies = new();
        private readonly Dictionary<int, HashSet<int>> dependents = new();
        private readonly HashSet<int> formulaColumns = new();

        /// <summary>
        /// Adds a dependency relationship between a formula column and a referenced column.
        /// </summary>
        /// <param name="formulaColumn">The column that contains the formula.</param>
        /// <param name="referencedColumn">The column that the formula references.</param>
        public void AddDependency(int formulaColumn, int referencedColumn)
        {
            if (formulaColumn < 0 || referencedColumn < 0)
            {
                return;
            }

            // Track this as a formula column
            this.formulaColumns.Add(formulaColumn);

            // Add to dependencies (formula -> referenced columns)
            if (!this.dependencies.ContainsKey(formulaColumn))
            {
                this.dependencies[formulaColumn] = new HashSet<int>();
            }

            this.dependencies[formulaColumn].Add(referencedColumn);

            // Add to dependents (referenced column -> formulas that depend on it)
            if (!this.dependents.ContainsKey(referencedColumn))
            {
                this.dependents[referencedColumn] = new HashSet<int>();
            }

            this.dependents[referencedColumn].Add(formulaColumn);
        }

        /// <summary>
        /// Removes all dependencies for a formula column.
        /// </summary>
        /// <param name="formulaColumn">The formula column to remove.</param>
        public void RemoveDependencies(int formulaColumn)
        {
            if (formulaColumn < 0)
            {
                return;
            }

            // Remove from formula columns
            this.formulaColumns.Remove(formulaColumn);

            // Remove from dependencies
            if (this.dependencies.TryGetValue(formulaColumn, out var referencedColumns))
            {
                // Remove this formula from all its referenced columns' dependents
                foreach (var referencedColumn in referencedColumns)
                {
                    if (this.dependents.TryGetValue(referencedColumn, out var dependentFormulas))
                    {
                        dependentFormulas.Remove(formulaColumn);
                        if (dependentFormulas.Count == 0)
                        {
                            this.dependents.Remove(referencedColumn);
                        }
                    }
                }

                this.dependencies.Remove(formulaColumn);
            }

            // Remove from dependents if this was referenced by other formulas
            if (this.dependents.TryGetValue(formulaColumn, out var dependentsList))
            {
                foreach (var dependent in dependentsList.ToList())
                {
                    if (this.dependencies.TryGetValue(dependent, out var deps))
                    {
                        deps.Remove(formulaColumn);
                    }
                }

                this.dependents.Remove(formulaColumn);
            }
        }

        /// <summary>
        /// Gets the columns that need to be recalculated when the specified column changes,
        /// ordered by dependency (columns with no dependencies first).
        /// </summary>
        /// <param name="changedColumn">The column that changed.</param>
        /// <returns>List of columns to recalculate in proper order.</returns>
        public List<int> GetRecalculationOrder(int changedColumn)
        {
            var toRecalculate = new HashSet<int>();
            var visited = new HashSet<int>();

            // Recursively find all columns that depend on the changed column
            this.CollectDependents(changedColumn, toRecalculate, visited);

            // Return topologically sorted order
            return this.TopologicalSort(toRecalculate.ToList());
        }

        /// <summary>
        /// Gets the recalculation order for all formula columns.
        /// </summary>
        /// <returns>List of all formula columns in topological order.</returns>
        public List<int> GetRecalculationOrder()
        {
            return this.TopologicalSort(this.formulaColumns.ToList());
        }

        /// <summary>
        /// Checks if adding a dependency from sourceColumnId to targetColumnId would create a cycle.
        /// </summary>
        /// <param name="sourceColumnId">The column that would depend on the target.</param>
        /// <param name="targetColumnId">The column that would be referenced.</param>
        /// <returns>True if adding the dependency would create a cycle, false otherwise.</returns>
        public bool WillCreateCycle(int sourceColumnId, int targetColumnId)
        {
            if (sourceColumnId == targetColumnId)
            {
                return true;
            }

            var visited = new HashSet<int>();
            var queue = new Queue<int>();
            queue.Enqueue(targetColumnId);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (current == sourceColumnId)
                {
                    return true; // Cycle detected
                }

                if (!visited.Add(current))
                {
                    continue;
                }

                // Check which other columns depend on the current one
                if (this.dependents.TryGetValue(current, out var dependentNodes))
                {
                    foreach (var dependentNode in dependentNodes)
                    {
                        queue.Enqueue(dependentNode);
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Attempts to find a cycle in the dependency graph.
        /// </summary>
        /// <param name="cyclePath">If a cycle is found, contains the column IDs that form the loop.</param>
        /// <returns>True if a cycle is found, false otherwise.</returns>
        public bool TryFindCycle(out List<int> cyclePath)
        {
            cyclePath = new List<int>();
            var whiteSet = new HashSet<int>(this.formulaColumns);
            var graySet = new HashSet<int>();
            var blackSet = new HashSet<int>();
            var parent = new Dictionary<int, int>();

            foreach (var column in this.formulaColumns)
            {
                if (whiteSet.Contains(column))
                {
                    if (this.DfsVisit(column, whiteSet, graySet, blackSet, parent, cyclePath))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Clears all dependencies.
        /// </summary>
        public void Clear()
        {
            this.dependencies.Clear();
            this.dependents.Clear();
            this.formulaColumns.Clear();
        }

        private bool DfsVisit(int column, HashSet<int> whiteSet, HashSet<int> graySet, HashSet<int> blackSet, Dictionary<int, int> parent, List<int> cyclePath)
        {
            whiteSet.Remove(column);
            graySet.Add(column);

            if (this.dependencies.TryGetValue(column, out var dependencies))
            {
                foreach (var dependency in dependencies)
                {
                    if (!this.formulaColumns.Contains(dependency))
                    {
                        continue;
                    }

                    if (graySet.Contains(dependency))
                    {
                        this.BuildCyclePath(dependency, column, parent, cyclePath);
                        return true;
                    }

                    if (whiteSet.Contains(dependency))
                    {
                        parent[dependency] = column;
                        if (this.DfsVisit(dependency, whiteSet, graySet, blackSet, parent, cyclePath))
                        {
                            return true;
                        }
                    }
                }
            }

            graySet.Remove(column);
            blackSet.Add(column);
            return false;
        }

        private void BuildCyclePath(int cycleStart, int current, Dictionary<int, int> parent, List<int> cyclePath)
        {
            cyclePath.Clear();
            cyclePath.Add(cycleStart);

            var node = current;
            while (node != cycleStart)
            {
                cyclePath.Add(node);
                if (parent.TryGetValue(node, out var parentNode))
                {
                    node = parentNode;
                }
                else
                {
                    break;
                }
            }

            cyclePath.Reverse();
        }

        private void CollectDependents(int column, HashSet<int> toRecalculate, HashSet<int> visited)
        {
            if (!visited.Add(column))
            {
                return;
            }

            if (this.dependents.TryGetValue(column, out var dependentFormulas))
            {
                foreach (var dependent in dependentFormulas)
                {
                    toRecalculate.Add(dependent);
                    this.CollectDependents(dependent, toRecalculate, visited);
                }
            }
        }

        private List<int> TopologicalSort(List<int> columns)
        {
            var inDegree = new Dictionary<int, int>();
            var result = new List<int>();
            var queue = new Queue<int>();

            // Initialize in-degree count
            foreach (var column in columns)
            {
                inDegree[column] = 0;
            }

            // Calculate in-degrees
            foreach (var column in columns)
            {
                if (this.dependencies.TryGetValue(column, out var deps))
                {
                    foreach (var dep in deps)
                    {
                        if (columns.Contains(dep))
                        {
                            inDegree[column]++;
                        }
                    }
                }
            }

            // Add columns with no dependencies to queue
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
                if (this.dependents.TryGetValue(current, out var dependentFormulas))
                {
                    foreach (var dependent in dependentFormulas)
                    {
                        if (columns.Contains(dependent))
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