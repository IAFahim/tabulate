// <copyright file="ColumnErrorState.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Tabulate.Editor.Core
{
    /// <summary>
    /// Types of column-level errors that can occur.
    /// </summary>
    public enum ColumnErrorType
    {
        /// <summary>
        /// Formula syntax or evaluation error.
        /// </summary>
        FormulaSystemError,

        /// <summary>
        /// Invalid property path specified.
        /// </summary>
        PropertyPathInvalid,

        /// <summary>
        /// Target type cannot be found or resolved.
        /// </summary>
        TargetTypeInvalid,

        /// <summary>
        /// Circular dependency detected in formula references.
        /// </summary>
        CircularDependency,

        /// <summary>
        /// Type mismatch between formula result and target property.
        /// </summary>
        TypeMismatch,

        /// <summary>
        /// Missing dependency (referenced column or variable).
        /// </summary>
        MissingDependency,
    }

    /// <summary>
    /// Represents the error state of an entire column in the sheet editor.
    /// Column errors suppress cell-level errors within that column.
    /// </summary>
    public class ColumnErrorState
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ColumnErrorState"/> class.
        /// </summary>
        /// <param name="columnId">The column ID.</param>
        /// <param name="columnName">The column name for display.</param>
        /// <param name="errorType">The type of column error.</param>
        /// <param name="severity">The error severity.</param>
        /// <param name="message">The error message.</param>
        /// <param name="affectedRowCount">The number of rows affected by this error.</param>
        public ColumnErrorState(int columnId, string columnName, ColumnErrorType errorType, ErrorSeverity severity, string message, int affectedRowCount)
        {
            this.ColumnId = columnId;
            this.ColumnName = columnName;
            this.ErrorType = errorType;
            this.Severity = severity;
            this.Message = message;
            this.AffectedRowCount = affectedRowCount;
        }

        /// <summary>
        /// Gets the column ID where the error occurred.
        /// </summary>
        public int ColumnId { get; }

        /// <summary>
        /// Gets the column name where the error occurred.
        /// </summary>
        public string ColumnName { get; }

        /// <summary>
        /// Gets the type of column error.
        /// </summary>
        public ColumnErrorType ErrorType { get; }

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
        /// Gets the number of rows affected by this column error.
        /// </summary>
        public int AffectedRowCount { get; }

        /// <summary>
        /// Gets the placeholder text to show in cells when this column has errors.
        /// </summary>
        public string CellPlaceholder => this.ErrorType switch
        {
            ColumnErrorType.FormulaSystemError => "[Formula Syntax Error]",
            ColumnErrorType.PropertyPathInvalid => "[Property Not Found]",
            ColumnErrorType.TargetTypeInvalid => "[Target Type Missing]",
            ColumnErrorType.CircularDependency => "[Circular Reference]",
            ColumnErrorType.TypeMismatch => "[Type Mismatch]",
            ColumnErrorType.MissingDependency => "[Missing Reference]",
            _ => "[Column Error]",
        };
    }
}