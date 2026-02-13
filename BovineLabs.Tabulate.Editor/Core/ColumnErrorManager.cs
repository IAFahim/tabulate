// <copyright file="ColumnErrorManager.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Tabulate.Editor.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Manages column-level errors and their relationship to cell-level errors.
    /// When a column has errors, cell-level errors in that column are suppressed.
    /// </summary>
    public class ColumnErrorManager
    {
        private readonly Dictionary<int, ColumnErrorState> columnErrors = new();
        private readonly Dictionary<(int ColumnId, int RowIndex), CellErrorState> cellErrors = new();

        /// <summary>
        /// Event fired when column error state changes.
        /// </summary>
        public event Action<int>? ColumnErrorChanged;

        /// <summary>
        /// Event fired when cell error state changes.
        /// </summary>
        public event Action<int, int>? CellErrorChanged;

        /// <summary>
        /// Adds or updates a column-level error.
        /// </summary>
        /// <param name="columnId">The column ID.</param>
        /// <param name="error">The column error state.</param>
        public void SetColumnError(int columnId, ColumnErrorState error)
        {
            this.columnErrors[columnId] = error;
            this.ColumnErrorChanged?.Invoke(columnId);

            // Clear any existing cell errors for this column since they're now suppressed
            this.ClearCellErrorsForColumn(columnId);
        }

        /// <summary>
        /// Adds or updates a cell-level error.
        /// Cell errors are only visible if the parent column has no errors.
        /// </summary>
        /// <param name="columnId">The column ID.</param>
        /// <param name="rowIndex">The row index.</param>
        /// <param name="error">The cell error state.</param>
        public void SetCellError(int columnId, int rowIndex, CellErrorState error)
        {
            // Only track cell errors if the column doesn't have errors
            if (!this.HasColumnError(columnId))
            {
                var key = (columnId, rowIndex);
                this.cellErrors[key] = error;
                this.CellErrorChanged?.Invoke(columnId, rowIndex);
            }
        }

        /// <summary>
        /// Removes a cell-level error.
        /// </summary>
        /// <param name="columnId">The column ID.</param>
        /// <param name="rowIndex">The row index.</param>
        /// <returns>True if an error was removed.</returns>
        public bool ClearCellError(int columnId, int rowIndex)
        {
            var key = (columnId, rowIndex);
            if (this.cellErrors.Remove(key))
            {
                this.CellErrorChanged?.Invoke(columnId, rowIndex);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if a column has any errors.
        /// </summary>
        /// <param name="columnId">The column ID.</param>
        /// <returns>True if the column has errors.</returns>
        public bool HasColumnError(int columnId)
        {
            return this.columnErrors.ContainsKey(columnId);
        }

        /// <summary>
        /// Gets the column error for a specific column.
        /// </summary>
        /// <param name="columnId">The column ID.</param>
        /// <returns>The column error state, or null if no error exists.</returns>
        public ColumnErrorState? GetColumnError(int columnId)
        {
            return this.columnErrors.TryGetValue(columnId, out var error) ? error : null;
        }

        /// <summary>
        /// Gets the cell error for a specific cell.
        /// Returns null if the parent column has errors (cell errors are suppressed).
        /// </summary>
        /// <param name="columnId">The column ID.</param>
        /// <param name="rowIndex">The row index.</param>
        /// <returns>The cell error state, or null if no error exists or column has errors.</returns>
        public CellErrorState? GetCellError(int columnId, int rowIndex)
        {
            // Suppress cell errors if column has errors
            if (this.HasColumnError(columnId))
            {
                return null;
            }

            var key = (columnId, rowIndex);
            return this.cellErrors.TryGetValue(key, out var error) ? error : null;
        }

        /// <summary>
        /// Gets all column errors.
        /// </summary>
        /// <returns>A collection of all column errors.</returns>
        public IReadOnlyCollection<ColumnErrorState> GetAllColumnErrors()
        {
            return this.columnErrors.Values.ToList();
        }

        /// <summary>
        /// Gets all visible cell errors (only for columns without column-level errors).
        /// </summary>
        /// <returns>A collection of all visible cell errors.</returns>
        public IReadOnlyCollection<CellErrorState> GetAllVisibleCellErrors()
        {
            return this.cellErrors
                .Where(kvp => !this.HasColumnError(kvp.Key.ColumnId))
                .Select(kvp => kvp.Value)
                .ToList();
        }

        /// <summary>
        /// Clears all errors.
        /// </summary>
        public void ClearAllErrors()
        {
            var affectedColumns = this.columnErrors.Keys.Concat(
                this.cellErrors.Keys.Select(k => k.ColumnId)).Distinct().ToList();

            this.columnErrors.Clear();
            this.cellErrors.Clear();

            foreach (var columnId in affectedColumns)
            {
                this.ColumnErrorChanged?.Invoke(columnId);
            }
        }

        /// <summary>
        /// Removes all cell errors for a specific column.
        /// Used internally when a column error is set.
        /// </summary>
        /// <param name="columnId">The column ID.</param>
        private void ClearCellErrorsForColumn(int columnId)
        {
            var keysToRemove = this.cellErrors.Keys
                .Where(key => key.ColumnId == columnId)
                .ToList();

            foreach (var key in keysToRemove)
            {
                this.cellErrors.Remove(key);
                this.CellErrorChanged?.Invoke(key.ColumnId, key.RowIndex);
            }
        }
    }
}