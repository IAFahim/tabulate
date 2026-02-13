// <copyright file="CellErrorState.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Tabulate.Editor.Core
{
    /// <summary>
    /// Represents the severity level of an error in a cell.
    /// </summary>
    public enum ErrorSeverity
    {
        /// <summary>
        /// Warning level - non-critical issues that don't prevent functionality.
        /// </summary>
        Warning,

        /// <summary>
        /// Error level - issues that prevent proper functionality.
        /// </summary>
        Error,

        /// <summary>
        /// Critical level - severe issues that could cause system instability.
        /// </summary>
        Critical,
    }

    /// <summary>
    /// Represents the error state of a cell in the sheet editor.
    /// </summary>
    public class CellErrorState
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CellErrorState"/> class.
        /// </summary>
        /// <param name="severity">The error severity.</param>
        /// <param name="message">The error message.</param>
        /// <param name="columnId">The column ID.</param>
        /// <param name="columnName">The column name for display.</param>
        /// <param name="rowIndex">The row index.</param>
        /// <param name="objectName">The object name.</param>
        public CellErrorState(ErrorSeverity severity, string message, int columnId, string columnName, int rowIndex, string objectName)
        {
            this.Severity = severity;
            this.Message = message;
            this.ColumnId = columnId;
            this.ColumnName = columnName;
            this.RowIndex = rowIndex;
            this.ObjectName = objectName;
        }

        /// <summary>
        /// Gets or sets the severity of the error.
        /// </summary>
        public ErrorSeverity Severity { get; set; }

        /// <summary>
        /// Gets the primary error message.
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Gets or sets the detailed description of the error.
        /// </summary>
        public string DetailedDescription { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a suggestion for fixing the error.
        /// </summary>
        public string Suggestion { get; set; } = string.Empty;

        /// <summary>
        /// Gets the column ID where the error occurred.
        /// </summary>
        public int ColumnId { get; }

        /// <summary>
        /// Gets the column name where the error occurred.
        /// </summary>
        public string ColumnName { get; }

        /// <summary>
        /// Gets the row index where the error occurred.
        /// </summary>
        public int RowIndex { get; }

        /// <summary>
        /// Gets the object name for easier identification.
        /// </summary>
        public string ObjectName { get; }

        /// <summary>
        /// Gets the CSS class name for the error severity.
        /// </summary>
        /// <returns>The CSS class name.</returns>
        public string GetSeverityCssClass()
        {
            return this.Severity switch
            {
                ErrorSeverity.Warning => "cell-error--warning",
                ErrorSeverity.Error => "cell-error--error",
                ErrorSeverity.Critical => "cell-error--critical",
                _ => "cell-error--error",
            };
        }

        public override string ToString()
        {
            return $"{this.Severity}: {this.Message} (ColumnName: {this.ColumnName}, Column: {this.ColumnId}, Row: {this.RowIndex}, Object: {this.ObjectName})";
        }
    }
}