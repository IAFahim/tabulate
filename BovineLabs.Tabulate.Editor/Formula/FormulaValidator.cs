// <copyright file="FormulaValidator.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Tabulate.Editor.Formula
{
    using System.Collections.Generic;
    using System.Linq;
    using BovineLabs.Tabulate.Editor.Core;

    public class FormulaValidator
    {
        private readonly FormulaTokenizer tokenizer;

        public FormulaValidator(FormulaTokenizer tokenizer)
        {
            this.tokenizer = tokenizer;
        }

        public ValidationResult ValidateSyntax(string formula)
        {
            if (string.IsNullOrWhiteSpace(formula))
            {
                return ValidationResult.Success();
            }

            try
            {
                var tokens = this.tokenizer.Tokenize(formula);

                // Check for empty formula
                if (tokens.Count == 0)
                {
                    return ValidationResult.Failure("Empty formula", "The formula field is empty. A formula is required to calculate the column's value.",
                        "Enter a valid formula, for example: C1 + 5 * C2.");
                }

                // Check for valid token sequence
                var sequenceValidation = this.ValidateTokenSequence(tokens);
                if (!sequenceValidation.IsValid)
                {
                    return sequenceValidation;
                }

                // Check for balanced parentheses
                var parenthesesValidation = this.ValidateParentheses(tokens);
                if (!parenthesesValidation.IsValid)
                {
                    return parenthesesValidation;
                }

                // Try to convert to postfix to catch any structural issues
                this.ConvertToPostfix(tokens);

                return ValidationResult.Success();
            }
            catch (FormulaException ex)
            {
                return ValidationResult.Failure("Invalid formula syntax", $"The formula could not be parsed: {ex.Message}.",
                    "Check the formula for syntax errors, such as mismatched parentheses or invalid operators.");
            }
            catch (System.Exception ex)
            {
                return ValidationResult.Failure("Unexpected validation error", $"An unexpected error occurred during syntax validation: {ex.Message}.",
                    "This may indicate a bug in the validation logic. Please report this issue.");
            }
        }

        public ValidationResult ValidateTokenSequence(List<Token> tokens)
        {
            if (tokens.Count == 0)
            {
                return ValidationResult.Failure("Empty token sequence",
                    "The formula appears to be empty or contains only whitespace, resulting in no tokens to process.", "Enter a valid formula expression, such as 'C1 + C2' or '42 * 1.5'.");
            }

            for (int i = 0; i < tokens.Count; i++)
            {
                var token = tokens[i];
                var prevToken = i > 0 ? (Token?)tokens[i - 1] : null;
                var nextToken = i < tokens.Count - 1 ? (Token?)tokens[i + 1] : null;

                switch (token.Type)
                {
                    case TokenType.Operator:
                        // Operators cannot be at the beginning or end (except unary minus, which we don't support yet)
                        if (i == 0 || i == tokens.Count - 1)
                        {
                            return ValidationResult.Failure("Invalid operator position",
                                $"The operator '{token.Value}' cannot be at the beginning or end of an expression.",
                                "Operators must be placed between values or expressions. For example: C1 + C2.");
                        }

                        // Operators cannot follow other operators
                        if (prevToken?.Type == TokenType.Operator)
                        {
                            return ValidationResult.Failure("Consecutive operators",
                                $"The operators '{prevToken.Value.Value}' and '{token.Value}' cannot be used consecutively.",
                                "Separate operators with values, column references, or parenthesized expressions.");
                        }

                        // Operators cannot precede closing parentheses
                        if (nextToken?.Type == TokenType.RightParen)
                        {
                            return ValidationResult.Failure("Invalid operator placement",
                                $"The operator '{token.Value}' cannot be immediately followed by a closing parenthesis.",
                                "Ensure expressions within parentheses are complete, like (C1 + 5).");
                        }

                        // Allow unary minus after opening parentheses or commas (for negative numbers)
                        if (prevToken?.Type is TokenType.LeftParen or TokenType.Comma)
                        {
                            if (token.Value != "-")
                            {
                                return ValidationResult.Failure("Invalid operator after parenthesis",
                                    $"Only unary minus (-) is allowed immediately after opening parentheses or commas to represent negative values.",
                                    $"Replace '{token.Value}' with a value, column reference, or use unary minus for negative numbers: (-5) or MAX(-C1, 0).");
                            }

                            // Unary minus is allowed, continue validation
                        }

                        break;

                    case TokenType.Number:
                    case TokenType.Identifier:
                        // Numbers and identifiers cannot follow other numbers/identifiers without an operator
                        if (prevToken?.Type is TokenType.Number or TokenType.Identifier)
                        {
                            return ValidationResult.Failure("Missing operator",
                                $"A mathematical or logical operator is missing between the values '{prevToken.Value.Value}' and '{token.Value}'.",
                                "Insert an operator like +, -, *, /, or == between the values.");
                        }

                        break;

                    case TokenType.LeftParen:
                        // Left parentheses cannot follow numbers/identifiers without an operator
                        if (prevToken?.Type is TokenType.Number or TokenType.Identifier)
                        {
                            return ValidationResult.Failure("Missing operator before parenthesis",
                                $"An operator is missing before an opening parenthesis, following the value '{prevToken.Value.Value}'.",
                                "If you intend to multiply, insert a * operator. For function calls, ensure the function name is correctly spelled.");
                        }

                        break;

                    case TokenType.RightParen:
                        // Right parentheses cannot follow operators or other right parentheses (empty parentheses)
                        if (prevToken?.Type == TokenType.Operator)
                        {
                            return ValidationResult.Failure("Invalid closing parenthesis",
                                $"A closing parenthesis cannot immediately follow the operator '{prevToken.Value.Value}'.",
                                "Complete the expression after the operator before closing the parenthesis.");
                        }

                        if (prevToken?.Type == TokenType.LeftParen)
                        {
                            return ValidationResult.Failure("Empty parentheses", "Parentheses must contain an expression - they cannot be empty as this has no mathematical meaning.",
                                "Either remove the parentheses entirely, or add a value, column reference, or sub-expression inside them.");
                        }

                        break;
                }
            }

            return ValidationResult.Success();
        }

        public ValidationResult ValidateParentheses(List<Token> tokens)
        {
            var stack = new Stack<int>();

            for (int i = 0; i < tokens.Count; i++)
            {
                var token = tokens[i];

                if (token.Type == TokenType.LeftParen)
                {
                    stack.Push(i);
                }
                else if (token.Type == TokenType.RightParen)
                {
                    if (stack.Count == 0)
                    {
                        return ValidationResult.Failure("Extra closing parenthesis",
                            $"A closing parenthesis at position {i} has no matching opening parenthesis preceding it in the formula.",
                            "Either remove this closing parenthesis if it's not needed, or add an opening parenthesis earlier in the formula where the grouping should begin.");
                    }

                    stack.Pop();
                }
            }

            if (stack.Count > 0)
            {
                var position = stack.Pop();
                return ValidationResult.Failure("Unclosed opening parenthesis", $"An opening parenthesis at position {position} was never closed before the end of the formula.",
                    "Add a closing parenthesis ')' after the expression that should be grouped, or remove the opening parenthesis if grouping is not needed.");
            }

            return ValidationResult.Success();
        }

        /// <summary>
        /// Validates formula references against available columns.
        /// </summary>
        /// <param name="formula">The formula to validate.</param>
        /// <param name="availableColumns">The available columns.</param>
        /// <param name="currentColumnId">The ID of the current column being validated.</param>
        /// <returns>A validation result indicating if all references are valid.</returns>
        public ValidationResult ValidateReferences(string formula, IReadOnlyList<ColumnDefinition> availableColumns, int currentColumnId)
        {
            if (string.IsNullOrEmpty(formula))
            {
                return ValidationResult.Success();
            }

            try
            {
                var tokens = this.tokenizer.Tokenize(formula);
                var references = ExtractColumnReferences(tokens);

                foreach (var reference in references)
                {
                    if (!TryParseColumnReference(reference, out var columnId))
                    {
                        return ValidationResult.Failure("Invalid column reference format",
                            $"The reference '{reference}' is not a valid column reference. Column references must start with 'C' followed by a number (e.g., C1, C42).",
                            "Correct the reference format or replace it with a valid column identifier.");
                    }

                    var referencedColumn = availableColumns.FirstOrDefault(c => c.ColumnId == columnId);
                    if (referencedColumn == null)
                    {
                        return ValidationResult.Failure("Undefined column reference",
                            $"The formula references column '{reference}', which does not exist in the current sheet definition.",
                            "Verify the column reference is correct. Available columns can be seen in the column list.");
                    }

                    // Check for self-reference
                    if (referencedColumn.ColumnId == currentColumnId)
                    {
                        return ValidationResult.Failure("Self-referencing formula",
                            "A formula cannot reference its own column, as this would create an unresolvable circular dependency.",
                            $"Remove the reference to '{reference}' from the formula and use other columns or constant values instead.");
                    }
                }

                return ValidationResult.Success();
            }
            catch (FormulaException ex)
            {
                return ValidationResult.Failure("Error validating references", $"An error occurred while validating column references: {ex.Message}.",
                    "Ensure all column references are correctly formatted (e.g., C1, C2). If the issue persists, it may be a bug.");
            }
        }

        /// <summary>
        /// Performs complete validation including syntax and references.
        /// </summary>
        /// <param name="formula">The formula to validate.</param>
        /// <param name="availableColumns">The available columns.</param>
        /// <param name="currentColumnId">The ID of the current column being validated.</param>
        /// <returns>A validation result with comprehensive error information.</returns>
        public ValidationResult ValidateComplete(string formula, IReadOnlyList<ColumnDefinition> availableColumns, int currentColumnId)
        {
            // First validate syntax
            var syntaxResult = this.ValidateSyntax(formula);
            if (!syntaxResult.IsValid)
            {
                return syntaxResult;
            }

            // Then validate references
            var referencesResult = this.ValidateReferences(formula, availableColumns, currentColumnId);
            if (!referencesResult.IsValid)
            {
                return referencesResult;
            }

            return ValidationResult.Success();
        }

        private void ConvertToPostfix(List<Token> tokens)
        {
            var operators = new Stack<Token>();

            foreach (var token in tokens)
            {
                switch (token.Type)
                {
                    case TokenType.Number:
                    case TokenType.Identifier:
                        break;

                    case TokenType.Operator:
                        while (operators.Count > 0 && operators.Peek().Type == TokenType.Operator &&
                            this.GetPrecedence(operators.Peek().Value) >= this.GetPrecedence(token.Value))
                        {
                            operators.Pop();
                        }

                        operators.Push(token);
                        break;

                    case TokenType.LeftParen:
                        operators.Push(token);
                        break;

                    case TokenType.RightParen:
                        while (operators.Count > 0 && operators.Peek().Type != TokenType.LeftParen)
                        {
                            operators.Pop();
                        }

                        if (operators.Count == 0)
                        {
                            throw new FormulaException("Mismatched parentheses");
                        }

                        operators.Pop(); // Remove the left paren
                        break;
                }
            }

            while (operators.Count > 0)
            {
                var op = operators.Pop();
                if (op.Type is TokenType.LeftParen or TokenType.RightParen)
                {
                    throw new FormulaException("Mismatched parentheses");
                }
            }
        }

        /// <summary>
        /// Extracts column references from tokenized formula.
        /// </summary>
        private static HashSet<string> ExtractColumnReferences(List<Token> tokens)
        {
            var references = new HashSet<string>();

            foreach (var token in tokens)
            {
                if (token.Type == TokenType.Identifier && token.Value.StartsWith("C"))
                {
                    references.Add(token.Value);
                }
            }

            return references;
        }

        /// <summary>
        /// Parses a column reference to extract the column ID.
        /// </summary>
        private static bool TryParseColumnReference(string columnReference, out int columnId)
        {
            columnId = 0;

            if (string.IsNullOrEmpty(columnReference) || !columnReference.StartsWith("C"))
            {
                return false;
            }

            var numberPart = columnReference.Substring(1);
            return int.TryParse(numberPart, out columnId);
        }

        private int GetPrecedence(string op)
        {
            return op switch
            {
                "?" or ":" => 0, // Lowest precedence for ternary
                "||" => 1, // Logical OR
                "&&" => 2, // Logical AND
                "==" or "!=" or ">" or "<" or ">=" or "<=" => 3, // Comparison
                "+" or "-" => 4, // Arithmetic
                "*" or "/" => 5, // Multiplicative
                _ => 0,
            };
        }
    }
}