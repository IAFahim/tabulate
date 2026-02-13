// <copyright file="ErrorSummaryPanel.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Tabulate.Editor.UI.Components
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using BovineLabs.Tabulate.Editor.Core;
    using UnityEngine;
    using UnityEngine.UIElements;

    /// <summary>
    /// UI component for displaying a summary of errors in the sheet editor.
    /// </summary>
    public class ErrorSummaryPanel : VisualElement
    {
        private readonly ScrollView errorList;

        private List<CellErrorState> currentErrors = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorSummaryPanel"/> class.
        /// </summary>
        public ErrorSummaryPanel()
        {
            this.LoadUXML();

            this.errorList = this.Q<ScrollView>("error-list");

            // Ensure the base class is applied (should be from UXML but let's be explicit)
            this.AddToClassList("error-summary-panel");
        }

        /// <summary>
        /// Event triggered when an error item is clicked.
        /// </summary>
        public event Action<CellErrorState>? ErrorItemClicked;

        /// <summary>
        /// Updates the panel with a new list of errors.
        /// </summary>
        /// <param name="errors">The list of errors to display.</param>
        public void UpdateErrors(List<CellErrorState> errors)
        {
            this.currentErrors = errors;
            this.RefreshErrorList();
        }

        private void LoadUXML()
        {
            var visualTree = UnityEditor.AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(EditorConstants.AssetPath + "ErrorSummaryPanel.uxml");
            visualTree.CloneTree(this);
        }

        private void RefreshErrorList()
        {
            this.errorList.Clear();

            // Group errors by severity for better organization
            var groupedErrors = this.currentErrors
                .GroupBy(e => e.Severity)
                .OrderBy(g => g.Key); // Critical, Error, Warning

            foreach (var group in groupedErrors)
            {
                foreach (var error in group.OrderBy(e => e.ColumnId).ThenBy(e => e.RowIndex))
                {
                    var errorItem = this.CreateErrorItem(error);
                    this.errorList.Add(errorItem);
                }
            }
        }

        private VisualElement CreateErrorItem(CellErrorState error)
        {
            var errorItemAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(EditorConstants.AssetPath + "ErrorSummaryItem.uxml");
            var styleSheet = UnityEditor.AssetDatabase.LoadAssetAtPath<StyleSheet>(EditorConstants.AssetPath + "ErrorSummaryItem.uss");
            var item = errorItemAsset.Instantiate();
            item.styleSheets.Add(styleSheet);

            // Add click handler to main item
            item.RegisterCallback<ClickEvent>(_ => this.ErrorItemClicked?.Invoke(error));

            // Get template elements
            var icon = item.Q<VisualElement>("error-summary-item-icon");
            var text = item.Q<Label>("error-summary-item-text");

            // Configure icon
            icon.AddToClassList($"cell-error-icon--{error.Severity.ToString().ToLower()}");

            // Add hover behavior to icon to match individual element behavior (no old tooltip)
            icon.RegisterCallback<MouseEnterEvent>(_ => this.ShowErrorTooltip(icon, error));
            icon.RegisterCallback<ClickEvent>(_ => this.ShowErrorTooltip(icon, error));

            // Configure text
            text.text = $"{error.ColumnName} | {error.Message} ({error.ObjectName})";

            // Add hover behavior to text to match individual element behavior (no old tooltip)
            text.RegisterCallback<MouseEnterEvent>(_ => this.ShowErrorTooltip(text, error));

            return item;
        }

        private void ShowErrorTooltip(VisualElement target, CellErrorState error)
        {
            var globalPosition = target.LocalToWorld(Vector2.zero);
            ErrorTooltipDrawer.ShowTooltip(globalPosition, error);
        }

    }
}