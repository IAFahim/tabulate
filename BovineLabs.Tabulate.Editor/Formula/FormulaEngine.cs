// <copyright file="FormulaEngine.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Tabulate.Editor.Formula
{
    using System;
    using System.Collections.Generic;
    using BovineLabs.Tabulate.Editor.Core;

    public class FormulaEngine
    {
        private readonly Dictionary<string, Func<object>> columnValueProviders = new();
        private readonly Dictionary<string, Func<object>> variableValueProviders = new();
        private readonly FormulaTokenizer tokenizer;
        private readonly FormulaValidator validator;
        private FormulaDependencyGraph? dependencyGraph;

        public FormulaEngine()
        {
            this.tokenizer = new FormulaTokenizer();
            this.validator = new FormulaValidator(this.tokenizer);
        }

        public void SetColumnValueProvider(string columnName, Func<object> valueProvider)
        {
            this.columnValueProviders[columnName] = valueProvider;
        }

        public void ClearColumnValueProviders()
        {
            this.columnValueProviders.Clear();
        }

        public void SetVariableValueProvider(string variableName, Func<object> valueProvider)
        {
            this.variableValueProviders[variableName] = valueProvider;
        }

        public void ClearVariableValueProviders()
        {
            this.variableValueProviders.Clear();
        }

        public object Evaluate(string formula)
        {
            if (string.IsNullOrWhiteSpace(formula))
            {
                return 0f;
            }

            try
            {
                // Validate syntax before evaluation
                var syntaxValidation = this.ValidateSyntax(formula);
                if (!syntaxValidation.IsValid)
                {
                    throw new FormulaException($"Syntax error in formula '{formula}': {syntaxValidation.ErrorMessage}");
                }

                var tokens = this.tokenizer.Tokenize(formula);
                var evaluator = new ExpressionEvaluator(this.columnValueProviders, this.variableValueProviders);
                return evaluator.Evaluate(tokens);
            }
            catch (FormulaException)
            {
                throw; // Re-throw FormulaExceptions as-is
            }
            catch (Exception ex)
            {
                throw new FormulaException($"Error evaluating formula '{formula}': {ex.Message}", ex);
            }
        }

        public ValidationResult ValidateSyntax(string formula)
        {
            return this.validator.ValidateSyntax(formula);
        }

        public HashSet<string> GetColumnReferences(string formula)
        {
            var references = new HashSet<string>();
            if (string.IsNullOrWhiteSpace(formula))
            {
                return references;
            }

            var tokens = this.tokenizer.Tokenize(formula);
            foreach (var token in tokens)
            {
                if (token.Type == TokenType.Identifier && token.Value.StartsWith("C"))
                {
                    references.Add(token.Value);
                }
            }

            return references;
        }

        public HashSet<string> GetVariableReferences(string formula)
        {
            var references = new HashSet<string>();
            if (string.IsNullOrWhiteSpace(formula))
            {
                return references;
            }

            var tokens = this.tokenizer.Tokenize(formula);
            foreach (var token in tokens)
            {
                if (token.Type == TokenType.Identifier && token.Value.StartsWith("V"))
                {
                    references.Add(token.Value);
                }
            }

            return references;
        }

        /// <summary>
        /// Performs complete validation including syntax, references, and circular dependencies.
        /// </summary>
        /// <param name="formula">The formula to validate.</param>
        /// <param name="availableColumns">The available columns for dependency validation.</param>
        /// <param name="currentColumnId">The ID of the current column being validated.</param>
        /// <returns>A validation result with comprehensive error information.</returns>
        public ValidationResult ValidateComplete(string formula, IReadOnlyList<ColumnDefinition> availableColumns, int? currentColumnId = null)
        {
            return this.validator.ValidateComplete(formula, availableColumns, currentColumnId ?? -1);
        }
    }
}