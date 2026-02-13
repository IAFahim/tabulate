// <copyright file="QuickFormulaDialog.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Tabulate.Editor.UI.Dialogs
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using BovineLabs.Tabulate.Editor.Core;
    using BovineLabs.Tabulate.Editor.Formula;
    using BovineLabs.Tabulate.Editor.UI.Components;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.UIElements;
    using Object = UnityEngine.Object;

    /// <summary>
    /// Modal dialog for applying one-shot formulas to entire columns.
    /// </summary>
    public class QuickFormulaDialog : EditorWindow
    {
        private const int PreviewRowCount = 5;

        private ColumnDefinition targetColumn = null!;
        private SheetDefinition sheetDefinition = null!;
        private Object[] objects = Array.Empty<Object>();

        private FormulaInputField formulaInputField = null!;
        private VisualElement previewContainer = null!;
        private Button applyButton = null!;
        private Button cancelButton = null!;
        private Label titleLabel = null!;
        private VisualElement formulaInputContainer = null!;

        private ColumnFormulaProcessor formulaProcessor = null!;

        /// <summary>
        /// Shows the quick formula dialog for the specified column.
        /// </summary>
        /// <param name="sheetDef">The sheet definition.</param>
        /// <param name="processor">The formula processor.</param>
        /// <param name="column">The target column.</param>
        /// <param name="objects">The objects to apply the formula to.</param>
        public static void ShowDialog(
            SheetDefinition sheetDef,
            ColumnFormulaProcessor processor,
            ColumnDefinition column,
            Object[] objects)
        {
            var window = CreateInstance<QuickFormulaDialog>();
            window.titleContent = new GUIContent($"Apply Formula to {column.EffectiveDisplayName}");
            window.targetColumn = column;
            window.sheetDefinition = sheetDef;
            window.objects = objects;
            window.formulaProcessor = processor;

            window.minSize = new Vector2(400, 300);
            window.maxSize = new Vector2(600, 500);

            window.ShowModal();
        }

        private void CreateGUI()
        {
            this.LoadUXML();
            this.FindUIElements();
            this.SetupUIElements();
            this.SetupEventHandlers();
        }

        private void LoadUXML()
        {
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(EditorConstants.AssetPath + "QuickFormulaDialog.uxml");
            visualTree.CloneTree(this.rootVisualElement);
        }

        private void FindUIElements()
        {
            this.titleLabel = this.rootVisualElement.Q<Label>("title-label");
            this.formulaInputContainer = this.rootVisualElement.Q<VisualElement>("formula-input-container");
            this.previewContainer = this.rootVisualElement.Q<VisualElement>("preview-container");
            this.cancelButton = this.rootVisualElement.Q<Button>("cancel-button");
            this.applyButton = this.rootVisualElement.Q<Button>("apply-button");
        }

        private void SetupUIElements()
        {
            // Set the column-specific title
            this.titleLabel.text = $"Apply Formula to Column: {this.targetColumn.EffectiveDisplayName}";

            // Create and add the formula input field
            this.formulaInputField = new FormulaInputField();
            this.formulaInputField.SetFormulaEngine(new FormulaEngine());
            this.formulaInputField.SetAvailableColumns(this.GetColumnNames());
            this.formulaInputContainer.Add(this.formulaInputField);

            // Set initial button states
            this.applyButton.SetEnabled(false);
        }

        private void SetupEventHandlers()
        {
            this.formulaInputField.ValueChanged += this.OnFormulaChanged;
            this.cancelButton.clicked += this.Close;
            this.applyButton.clicked += this.ApplyFormula;

            // Focus the input field
            this.formulaInputField.Focus();
        }

        private string[] GetColumnNames()
        {
            var columnNames = new List<string>();
            for (int i = 0; i < this.sheetDefinition.Columns.Length; i++)
            {
                var column = this.sheetDefinition.Columns[i];
                columnNames.Add($"C{i} ({column.EffectiveDisplayName})");
            }

            return columnNames.ToArray();
        }

        private void OnFormulaChanged(string formula)
        {
            this.UpdatePreview(formula);
        }

        private void UpdatePreview(string formula)
        {
            this.previewContainer.Clear();

            if (string.IsNullOrWhiteSpace(formula))
            {
                var emptyLabel = new Label("Enter a formula to see preview...");
                emptyLabel.AddToClassList("quick-formula-preview-empty");
                this.previewContainer.Add(emptyLabel);
                this.applyButton.SetEnabled(false);
                return;
            }

            try
            {
                var previewObjects = this.objects.Take(PreviewRowCount).ToArray();
                var results = this.formulaProcessor.PreviewFormulaResults(formula, this.sheetDefinition, previewObjects);

                if (results.HasErrors)
                {
                    var errorLabel = new Label($"Formula Error: {results.ErrorMessage}");
                    errorLabel.AddToClassList("quick-formula-preview-error");
                    this.previewContainer.Add(errorLabel);
                    this.applyButton.SetEnabled(false);
                    return;
                }

                // Show preview results
                for (int i = 0; i < results.PreviewValues.Count; i++)
                {
                    var obj = previewObjects[i];
                    var value = results.PreviewValues[i];
                    var objectName = obj != null ? obj.name : $"Object {i}";

                    var previewRow = new Label($"{objectName}: {value}");
                    previewRow.AddToClassList("quick-formula-preview-row");
                    this.previewContainer.Add(previewRow);
                }

                if (this.objects.Length > PreviewRowCount)
                {
                    var moreLabel = new Label($"... and {this.objects.Length - PreviewRowCount} more objects");
                    moreLabel.AddToClassList("quick-formula-preview-more");
                    this.previewContainer.Add(moreLabel);
                }

                this.applyButton.SetEnabled(true);
            }
            catch (Exception ex)
            {
                var errorLabel = new Label($"Preview Error: {ex.Message}");
                errorLabel.AddToClassList("quick-formula-preview-error");
                this.previewContainer.Add(errorLabel);
                this.applyButton.SetEnabled(false);
            }
        }

        private void ApplyFormula()
        {
            var formula = this.formulaInputField.Value;
            if (string.IsNullOrWhiteSpace(formula))
            {
                return;
            }

            this.formulaProcessor.ApplyFormulaToColumn(this.targetColumn, formula, this.sheetDefinition, this.objects);
            this.Close();
        }
    }
}