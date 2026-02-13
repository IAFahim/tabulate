// <copyright file="ErrorController.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Tabulate.Editor.UI
{
    using System.Collections.Generic;
    using System.Linq;
    using BovineLabs.Tabulate.Editor.Core;
    using BovineLabs.Tabulate.Editor.UI.Components;
    using UnityEngine;
    using UnityEngine.UIElements;

    /// <summary>
    /// Handles all error management functionality for the UnitySheetEditor.
    /// </summary>
    public class ErrorController
    {
        // Error handling system
        private readonly ColumnErrorManager columnErrorManager = new();
        private readonly Dictionary<(int ColumnId, int RowIndex), ErrorAwareVisualElement> errorAwareCells = new();

        // UI elements
        private readonly VisualElement errorSummaryContainer;
        private readonly VisualElement errorStatusContainer;
        private readonly VisualElement errorStatusIcon;
        private readonly Label errorStatusLabel;
        private readonly ErrorSummaryPanel errorSummaryPanel;

        private bool isErrorPanelExpanded;

        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorController"/> class.
        /// </summary>
        /// <param name="rootVisualElement">The root visual element to find UI components.</param>
        public ErrorController(VisualElement rootVisualElement)
        {
            // Find UI elements
            this.errorStatusContainer = rootVisualElement.Q<VisualElement>("error-status-container");
            this.errorStatusIcon = rootVisualElement.Q<VisualElement>("error-status-icon");
            this.errorStatusLabel = rootVisualElement.Q<Label>("error-status-label");

            // Initialize error summary panel
            this.errorSummaryContainer = rootVisualElement.Q<VisualElement>("error-summary-panel");

            this.errorSummaryContainer.style.display = DisplayStyle.None;
            this.errorSummaryPanel = new ErrorSummaryPanel();
            this.errorSummaryPanel.ErrorItemClicked += this.OnErrorItemClicked;
            this.errorSummaryContainer.Add(this.errorSummaryPanel);

            // Set up error status click handler
            this.errorStatusContainer.RegisterCallback<ClickEvent>(_ => this.ToggleErrorPanel());
        }

        /// <summary>
        /// Gets the column error manager.
        /// </summary>
        public ColumnErrorManager ColumnErrorManager => this.columnErrorManager;

        /// <summary>
        /// Registers an error-aware cell for tracking.
        /// </summary>
        /// <param name="columnId">The column ID.</param>
        /// <param name="rowIndex">The row index.</param>
        /// <param name="errorAwareElement">The error-aware visual element.</param>
        public void RegisterErrorAwareCell(int columnId, int rowIndex, ErrorAwareVisualElement errorAwareElement)
        {
            var key = (columnId, rowIndex);
            this.errorAwareCells[key] = errorAwareElement;
            errorAwareElement.ShowErrorTooltip += this.ShowErrorTooltip;
        }

        /// <summary>
        /// Unregisters an error-aware cell.
        /// </summary>
        /// <param name="columnId">The column ID.</param>
        /// <param name="rowIndex">The row index.</param>
        public void UnregisterErrorAwareCell(int columnId, int rowIndex)
        {
            var key = (columnId, rowIndex);
            if (this.errorAwareCells.TryGetValue(key, out var element))
            {
                element.ShowErrorTooltip -= this.ShowErrorTooltip;
                this.errorAwareCells.Remove(key);
            }
        }

        /// <summary>
        /// Adds an error to the error tracking system.
        /// </summary>
        /// <param name="columnName">The column where the error occurred.</param>
        /// <param name="columnId">The column index where the error occurred.</param>
        /// <param name="rowIndex">The row index where the error occurred.</param>
        /// <param name="severity">The severity of the error.</param>
        /// <param name="message">The error message.</param>
        /// <param name="detailedDescription">Detailed description of the error (optional).</param>
        /// <param name="suggestion">Optional suggestion for fixing the error.</param>
        public void AddError(
            string columnName, int columnId, int rowIndex, ErrorSeverity severity, string message, string detailedDescription = "", string suggestion = "")
        {
            this.AddCellError(columnName, columnId, rowIndex, severity, message, detailedDescription, suggestion);
        }

        /// <summary>
        /// Adds a column-level error that affects all cells in the column.
        /// </summary>
        /// <param name="columnId">The column ID.</param>
        /// <param name="columnName">The column display name.</param>
        /// <param name="errorType">The type of column error.</param>
        /// <param name="severity">The error severity.</param>
        /// <param name="message">The error message.</param>
        /// <param name="detailedDescription">Detailed description of the error (optional).</param>
        /// <param name="suggestion">Optional suggestion for fixing the error.</param>
        /// <param name="affectedRowCount">Number of rows affected by this error (optional).</param>
        public void AddColumnError(
            int columnId, string columnName, ColumnErrorType errorType, ErrorSeverity severity, string message, string detailedDescription = "",
            string suggestion = "", int affectedRowCount = 0)
        {
            var columnError = new ColumnErrorState(columnId, columnName, errorType, severity, message, affectedRowCount)
            {
                DetailedDescription = detailedDescription,
                Suggestion = suggestion,
            };

            this.columnErrorManager.SetColumnError(columnId, columnError);

            // Update the error summary panel
            this.UpdateErrorSummaryPanel();

            // Update error panel visibility
            this.UpdateErrorPanelVisibility();

            // Update status bar
            this.UpdateErrorStatus();
        }

        /// <summary>
        /// Adds an error for a specific cell (only shown if column has no errors).
        /// </summary>
        /// <param name="columnName">The column name.</param>
        /// <param name="columnId">The column ID.</param>
        /// <param name="rowIndex">The row index.</param>
        /// <param name="severity">The error severity.</param>
        /// <param name="message">The error message.</param>
        /// <param name="detailedDescription">Detailed description of the error (optional).</param>
        /// <param name="suggestion">Optional suggestion for fixing the error.</param>
        public void AddCellError(
            string columnName, int columnId, int rowIndex, ErrorSeverity severity, string message, string detailedDescription = "", string suggestion = "")
        {
            var objectName = string.Empty; // We'll need to get this from the calling context

            var errorState = new CellErrorState(severity, message, columnId, columnName, rowIndex, objectName)
            {
                DetailedDescription = detailedDescription,
                Suggestion = suggestion,
            };

            this.columnErrorManager.SetCellError(columnId, rowIndex, errorState);

            // Update the cell visual if it exists and the column doesn't have errors
            var key = (columnId, rowIndex);
            if (!this.columnErrorManager.HasColumnError(columnId) && this.errorAwareCells.TryGetValue(key, out var cell))
            {
                cell.SetError(errorState);
                cell.ShowErrorTooltip += this.ShowErrorTooltip;
            }

            // Update the error summary panel
            this.UpdateErrorSummaryPanel();

            // Update error panel visibility
            this.UpdateErrorPanelVisibility();

            // Update status bar
            this.UpdateErrorStatus();
        }

        /// <summary>
        /// Removes errors for a specific cell.
        /// </summary>
        /// <param name="columnId">The column ID.</param>
        /// <param name="rowIndex">The row index.</param>
        public void RemoveError(int columnId, int rowIndex)
        {
            this.RemoveCellError(columnId, rowIndex);
        }

        /// <summary>
        /// Removes a cell-level error.
        /// </summary>
        /// <param name="columnId">The column ID.</param>
        /// <param name="rowIndex">The row index.</param>
        public void RemoveCellError(int columnId, int rowIndex)
        {
            if (this.columnErrorManager.ClearCellError(columnId, rowIndex))
            {
                // Clear visual error state
                var key = (columnId, rowIndex);
                if (this.errorAwareCells.TryGetValue(key, out var cell))
                {
                    cell.ClearError();
                }

                // Update the error summary panel
                this.UpdateErrorSummaryPanel();

                // Update error panel visibility
                this.UpdateErrorPanelVisibility();

                // Update status bar
                this.UpdateErrorStatus();
            }
        }

        /// <summary>
        /// Clears all errors.
        /// </summary>
        public void ClearAllErrors()
        {
            this.columnErrorManager.ClearAllErrors();

            // Clear all visual error states
            foreach (var cell in this.errorAwareCells.Values)
            {
                cell.ClearError();
            }

            this.UpdateErrorSummaryPanel();
            this.UpdateErrorPanelVisibility();
            this.UpdateErrorStatus();
        }

        /// <summary>
        /// Updates error state for a cell when column error state changes.
        /// </summary>
        /// <param name="columnId">The column ID.</param>
        /// <param name="rowIndex">The row index.</param>
        /// <param name="columnError">The column error state (null if no column error).</param>
        /// <param name="rowObjectName">The name of the row object for display.</param>
        public void UpdateCellErrorState(int columnId, int rowIndex, ColumnErrorState? columnError, string rowObjectName)
        {
            var key = (columnId, rowIndex);
            if (!this.errorAwareCells.TryGetValue(key, out var errorAwareElement))
            {
                return;
            }

            if (columnError != null)
            {
                // Show column error takes priority
                var columnCellError =
                    new CellErrorState(ErrorSeverity.Error, $"{columnError.Message}", columnError.ColumnId, columnError.ColumnName, rowIndex, rowObjectName)
                    {
                        DetailedDescription = columnError.DetailedDescription,
                        Suggestion = columnError.Suggestion,
                        Severity = columnError.Severity,
                    };

                errorAwareElement.SetError(columnCellError);
            }
            else
            {
                var cellError = this.columnErrorManager.GetCellError(columnId, rowIndex);
                if (cellError != null)
                {
                    errorAwareElement.SetError(cellError);
                }
                else
                {
                    errorAwareElement.ClearError();
                }
            }
        }

        /// <summary>
        /// Updates the error summary panel with current error state.
        /// </summary>
        private void UpdateErrorSummaryPanel()
        {
            // Collect all visible errors (column + visible cell errors)
            var allErrors = new List<CellErrorState>();

            // Add column errors as special cell errors for display
            foreach (var columnError in this.columnErrorManager.GetAllColumnErrors())
            {
                // Create a placeholder cell error to represent the column error
                var columnCellError =
                    new CellErrorState(ErrorSeverity.Error, $"{columnError.Message}", columnError.ColumnId, columnError.ColumnName, 0,
                        $"All {columnError.AffectedRowCount} rows")
                    {
                        DetailedDescription = columnError.DetailedDescription,
                        Suggestion = columnError.Suggestion,
                        Severity = columnError.Severity,
                    };

                allErrors.Add(columnCellError);
            }

            // Add visible cell errors
            allErrors.AddRange(this.columnErrorManager.GetAllVisibleCellErrors());

            this.errorSummaryPanel.UpdateErrors(allErrors);
        }

        /// <summary>
        /// Updates the error status in the status bar.
        /// </summary>
        private void UpdateErrorStatus()
        {
            var columnErrors = this.columnErrorManager.GetAllColumnErrors();
            var cellErrors = this.columnErrorManager.GetAllVisibleCellErrors();
            var totalErrorCount = columnErrors.Count + cellErrors.Count;

            if (totalErrorCount == 0)
            {
                this.errorStatusContainer.RemoveFromClassList("error-status-container--visible");
                return;
            }

            // Show the error status
            this.errorStatusContainer.AddToClassList("error-status-container--visible");

            // Count errors by severity (prioritize column errors)
            var warningCount = columnErrors.Count(e => e.Severity == ErrorSeverity.Warning) + cellErrors.Count(e => e.Severity == ErrorSeverity.Warning);
            var errorSevereCount = columnErrors.Count(e => e.Severity == ErrorSeverity.Error) + cellErrors.Count(e => e.Severity == ErrorSeverity.Error);
            var criticalCount = columnErrors.Count(e => e.Severity == ErrorSeverity.Critical) + cellErrors.Count(e => e.Severity == ErrorSeverity.Critical);

            // Update icon style
            this.errorStatusIcon.RemoveFromClassList("error-status-icon--warning");
            this.errorStatusIcon.RemoveFromClassList("error-status-icon--error");
            this.errorStatusIcon.RemoveFromClassList("error-status-icon--critical");

            if (criticalCount > 0)
            {
                this.errorStatusIcon.AddToClassList("error-status-icon--critical");
            }
            else if (errorSevereCount > 0)
            {
                this.errorStatusIcon.AddToClassList("error-status-icon--error");
            }
            else
            {
                this.errorStatusIcon.AddToClassList("error-status-icon--warning");
            }

            // Set error text
            var errorText = totalErrorCount == 1 ? "1 error" : $"{totalErrorCount} errors";
            if (totalErrorCount > 1)
            {
                var parts = new List<string>();

                if (criticalCount > 0)
                {
                    parts.Add($"{criticalCount} critical");
                }

                if (errorSevereCount > 0)
                {
                    parts.Add($"{errorSevereCount} error");
                }

                if (warningCount > 0)
                {
                    parts.Add($"{warningCount} warning");
                }

                if (parts.Count > 0)
                {
                    errorText = string.Join(", ", parts);
                }
            }

            this.errorStatusLabel.text = errorText;
        }

        /// <summary>
        /// Toggles the visibility of the error summary panel.
        /// </summary>
        private void ToggleErrorPanel()
        {
            var totalErrorCount = this.columnErrorManager.GetAllColumnErrors().Count + this.columnErrorManager.GetAllVisibleCellErrors().Count;

            if (totalErrorCount == 0)
            {
                return; // No errors to show
            }

            this.isErrorPanelExpanded = !this.isErrorPanelExpanded;
            this.UpdateErrorPanelVisibility();
        }

        /// <summary>
        /// Updates the visibility of the error summary panel container.
        /// </summary>
        private void UpdateErrorPanelVisibility()
        {
            var totalErrorCount = this.columnErrorManager.GetAllColumnErrors().Count + this.columnErrorManager.GetAllVisibleCellErrors().Count;

            // Show the panel only if we have errors AND the panel is expanded
            var shouldShow = totalErrorCount > 0 && this.isErrorPanelExpanded;
            this.errorSummaryContainer.style.display = shouldShow ? DisplayStyle.Flex : DisplayStyle.None;
        }

        /// <summary>
        /// Handles clicks on error items in the error summary panel.
        /// </summary>
        /// <param name="errorState">The error that was clicked.</param>
        private void OnErrorItemClicked(CellErrorState errorState)
        {
            // Navigate to the error location by highlighting the relevant cell
            this.FlashCell(errorState.ColumnId, errorState.RowIndex);
        }

        /// <summary>
        /// Flashes a cell to highlight it.
        /// </summary>
        /// <param name="columnId">The column ID.</param>
        /// <param name="rowIndex">The row index.</param>
        private void FlashCell(int columnId, int rowIndex)
        {
            var key = (columnId, rowIndex);
            if (this.errorAwareCells.TryGetValue(key, out var cell))
            {
                this.FlashCell(cell);
            }
        }

        /// <summary>
        /// Flashes a cell to highlight it.
        /// </summary>
        /// <param name="cell">The cell to flash.</param>
        private void FlashCell(ErrorAwareVisualElement cell)
        {
            // Implementation would add a temporary CSS class for animation
            // This is a simplified version - you might want to implement proper animation
            cell.AddToClassList("flash-highlight");

            // Remove the class after a short delay
            // In a real implementation, you'd use proper timing
            cell.schedule.Execute(() => cell.RemoveFromClassList("flash-highlight")).StartingIn(500);
        }

        /// <summary>
        /// Shows an error tooltip at the specified position.
        /// </summary>
        /// <param name="pos">The position to show the tooltip.</param>
        /// <param name="errorState">The error state to display.</param>
        private void ShowErrorTooltip(Vector2 pos, CellErrorState errorState)
        {
            ErrorTooltipDrawer.ShowTooltip(pos, errorState);
        }

        /// <summary>
        /// Determines the column error type based on the column definition and error message.
        /// </summary>
        public static ColumnErrorType DetermineColumnErrorType(ColumnDefinition columnDef, string errorMessage)
        {
            var lowerMessage = errorMessage.ToLowerInvariant();

            if (lowerMessage.Contains("property") && lowerMessage.Contains("not found"))
            {
                return ColumnErrorType.PropertyPathInvalid;
            }

            if (lowerMessage.Contains("target type") && (lowerMessage.Contains("resolved") || lowerMessage.Contains("missing")))
            {
                return ColumnErrorType.TargetTypeInvalid;
            }

            if (lowerMessage.Contains("circular"))
            {
                return ColumnErrorType.CircularDependency;
            }

            if (lowerMessage.Contains("references unknown"))
            {
                return ColumnErrorType.MissingDependency;
            }

            if (lowerMessage.Contains("syntax"))
            {
                return ColumnErrorType.FormulaSystemError;
            }

            // Default based on column type
            return columnDef.Type == ColumnType.Formula ? ColumnErrorType.FormulaSystemError : ColumnErrorType.PropertyPathInvalid;
        }

        /// <summary>
        /// Determines error severity based on the error type.
        /// </summary>
        public static ErrorSeverity DetermineErrorSeverity(ColumnErrorType errorType)
        {
            return errorType switch
            {
                ColumnErrorType.CircularDependency => ErrorSeverity.Critical,
                ColumnErrorType.FormulaSystemError => ErrorSeverity.Error,
                ColumnErrorType.PropertyPathInvalid => ErrorSeverity.Error,
                ColumnErrorType.TargetTypeInvalid => ErrorSeverity.Error,
                ColumnErrorType.MissingDependency => ErrorSeverity.Error,
                ColumnErrorType.TypeMismatch => ErrorSeverity.Warning,
                _ => ErrorSeverity.Error,
            };
        }
    }
}