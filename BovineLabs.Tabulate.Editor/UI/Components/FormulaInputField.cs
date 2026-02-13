// <copyright file="FormulaInputField.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Tabulate.Editor.UI.Components
{
    using System;
    using System.Collections.Generic;
    using BovineLabs.Tabulate.Editor.Core;
    using BovineLabs.Tabulate.Editor.Formula;
    using UnityEngine.UIElements;

    /// <summary>
    /// Enhanced TextField for formula input with syntax highlighting and validation feedback.
    /// </summary>
    public class FormulaInputField : VisualElement
    {
        private readonly TextField textField;
        private readonly VisualElement validationIndicator;
        private readonly Label validationLabel;
        private readonly VisualElement highlightContainer;

        private FormulaEngine? formulaEngine;
        private string[]? availableColumns;
        private ValidationResult? lastValidation;

        /// <summary>
        /// Initializes a new instance of the <see cref="FormulaInputField"/> class.
        /// </summary>
        public FormulaInputField()
        {
            this.LoadUXML();

            this.textField = this.Q<TextField>("formula-text-field");
            this.highlightContainer = this.Q<VisualElement>("highlight-container");
            this.validationIndicator = this.Q<VisualElement>("validation-indicator");
            this.validationLabel = this.Q<Label>("validation-label");

            this.SetupUIElements();
            this.SetupEventHandlers();
        }

        /// <summary>
        /// Event triggered when the formula value changes.
        /// </summary>
        public event Action<string>? ValueChanged;

        /// <summary>
        /// Event triggered when validation state changes.
        /// </summary>
        public event Action<ValidationResult>? ValidationChanged;

        private void LoadUXML()
        {
            var visualTree = UnityEditor.AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(EditorConstants.AssetPath + "FormulaInputField.uxml");
            visualTree.CloneTree(this);
        }

        private void SetupUIElements()
        {
            // Apply CSS classes for initial state management
            this.AddToClassList("formula-input-field");
            this.highlightContainer.pickingMode = PickingMode.Ignore;
        }

        private void SetupEventHandlers()
        {
            this.textField.RegisterValueChangedCallback(this.OnValueChanged);
            this.textField.RegisterCallback<FocusInEvent>(this.OnFocusIn);
            this.textField.RegisterCallback<FocusOutEvent>(this.OnFocusOut);
        }

        /// <summary>
        /// Gets the formula value.
        /// </summary>
        public string Value => this.textField.value;

        /// <summary>
        /// Sets the formula engine for validation.
        /// </summary>
        /// <param name="engine">The formula engine to use.</param>
        public void SetFormulaEngine(FormulaEngine engine)
        {
            this.formulaEngine = engine;
            this.ValidateFormula();
        }

        /// <summary>
        /// Sets the available column names for reference validation.
        /// </summary>
        /// <param name="columns">Array of available column names.</param>
        public void SetAvailableColumns(string[] columns)
        {
            this.availableColumns = columns;
            this.UpdateSyntaxHighlighting();
            this.ValidateFormula();
        }

        /// <summary>
        /// Forces validation of the current formula.
        /// </summary>
        public void ValidateFormula()
        {
            if (this.formulaEngine == null || string.IsNullOrWhiteSpace(this.Value))
            {
                this.ClearValidationState();
                return;
            }

            var validation = this.formulaEngine.ValidateSyntax(this.Value);
            this.SetValidationState(validation);
        }

        /// <summary>
        /// Updates the visual styling to reflect the validation state.
        /// </summary>
        public void UpdateVisualState()
        {
            this.RemoveFromClassList("formula-input--valid");
            this.RemoveFromClassList("formula-input--invalid");
            this.RemoveFromClassList("formula-input--warning");

            if (this.lastValidation is { IsValid: false })
            {
                this.AddToClassList("formula-input--invalid");
            }

            // For valid formulas, we don't apply any special styling - neutral is good enough
        }

        /// <summary>
        /// Clears the validation state for empty/neutral formulas.
        /// </summary>
        private void ClearValidationState()
        {
            this.lastValidation = null;
            this.validationIndicator.RemoveFromClassList("formula-validation-indicator--visible");
            this.UpdateVisualState();
        }

        private void OnValueChanged(ChangeEvent<string> evt)
        {
            this.ValidateFormula();
            this.UpdateSyntaxHighlighting();
            this.ValueChanged?.Invoke(evt.newValue);
        }

        private void OnFocusIn(FocusInEvent evt)
        {
            this.UpdateSyntaxHighlighting();
        }

        private void OnFocusOut(FocusOutEvent evt)
        {
            this.ValidateFormula();
        }

        private void SetValidationState(ValidationResult validation)
        {
            this.lastValidation = validation;

            if (validation.IsValid)
            {
                this.validationIndicator.RemoveFromClassList("formula-validation-indicator--visible");
            }
            else
            {
                this.validationIndicator.AddToClassList("formula-validation-indicator--visible");
                this.validationLabel.text = validation.ErrorMessage;
                this.validationLabel.AddToClassList("formula-validation-label--error");

                var icon = this.validationIndicator.Q("validation-icon");
                icon?.AddToClassList("formula-validation-icon--error");
            }

            this.UpdateVisualState();
            this.ValidationChanged?.Invoke(validation);
        }

        private void UpdateSyntaxHighlighting()
        {
            if (string.IsNullOrWhiteSpace(this.Value) || this.availableColumns == null)
            {
                return;
            }

            // Clear existing highlights
            this.highlightContainer.Clear();

            // Simple syntax highlighting - highlight column references
            var formula = this.Value;
            var highlightedRanges = this.FindColumnReferences(formula);

            foreach (var range in highlightedRanges)
            {
                this.CreateHighlightElement(range);
            }
        }

        private List<(int Start, int Length, bool IsValid)> FindColumnReferences(string formula)
        {
            var ranges = new List<(int, int, bool)>();

            if (this.availableColumns == null)
            {
                return ranges;
            }

            // Find all identifiers that could be column references
            var tokens = this.TokenizeForHighlighting(formula);

            foreach (var token in tokens)
            {
                if (token.Type == TokenType.Identifier)
                {
                    var isValidColumn = Array.Exists(this.availableColumns, col => col.Equals(token.Value, StringComparison.OrdinalIgnoreCase));

                    ranges.Add((token.Start, token.Length, isValidColumn));
                }
            }

            return ranges;
        }

        private List<FormulaToken> TokenizeForHighlighting(string formula)
        {
            var tokens = new List<FormulaToken>();
            var i = 0;

            while (i < formula.Length)
            {
                var c = formula[i];

                if (char.IsWhiteSpace(c))
                {
                    i++;
                    continue;
                }

                if (char.IsLetter(c) || c == '_')
                {
                    var start = i;
                    var identifierStr = string.Empty;

                    while (i < formula.Length && (char.IsLetterOrDigit(formula[i]) || formula[i] == '_'))
                    {
                        identifierStr += formula[i];
                        i++;
                    }

                    tokens.Add(new FormulaToken(TokenType.Identifier, identifierStr, start, identifierStr.Length));
                    continue;
                }

                i++;
            }

            return tokens;
        }

        private void CreateHighlightElement((int Start, int Length, bool IsValid) range)
        {
            var highlight = new VisualElement();
            highlight.AddToClassList("formula-highlight-element");
            highlight.AddToClassList(range.IsValid ? "formula-highlight--valid" : "formula-highlight--invalid");

            // Note: Positioning highlights over text is complex in UI Toolkit
            // This is a simplified version - full implementation would need
            // to measure text positions accurately
            this.highlightContainer.Add(highlight);
        }

        private struct FormulaToken
        {
            public TokenType Type { get; }

            public string Value { get; }

            public int Start { get; }

            public int Length { get; }

            public FormulaToken(TokenType type, string value, int start, int length)
            {
                this.Type = type;
                this.Value = value;
                this.Start = start;
                this.Length = length;
            }
        }

        private enum TokenType
        {
            Identifier,
            Number,
            Operator,
        }
    }
}