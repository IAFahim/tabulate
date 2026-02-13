// <copyright file="ExpressionEvaluator.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Tabulate.Editor.Formula
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;

    public class ExpressionEvaluator
    {
        private readonly Dictionary<string, Func<object>> columnValueProviders;
        private readonly Dictionary<string, Func<object>> variableValueProviders;

        public ExpressionEvaluator(Dictionary<string, Func<object>> columnValueProviders, Dictionary<string, Func<object>> variableValueProviders)
        {
            this.columnValueProviders = columnValueProviders;
            this.variableValueProviders = variableValueProviders;
        }

        public object Evaluate(List<Token> tokens)
        {
            return this.EvaluateTernary(tokens, 0).Value;
        }

        private (object Value, int NextIndex) EvaluateTernary(List<Token> tokens, int startIndex)
        {
            var (conditionValue, nextIndex) = this.EvaluateLogical(tokens, startIndex);

            if (nextIndex < tokens.Count && tokens[nextIndex].Type == TokenType.Question)
            {
                var (trueValue, afterTrue) = this.EvaluateLogical(tokens, nextIndex + 1);

                if (afterTrue >= tokens.Count || tokens[afterTrue].Type != TokenType.Colon)
                {
                    throw new FormulaException("Expected ':' in ternary operator");
                }

                var (falseValue, afterFalse) = this.EvaluateLogical(tokens, afterTrue + 1);

                var condition = FormulaTypeConverter.ConvertToFloat(conditionValue);
                return (Math.Abs(condition) > float.Epsilon ? trueValue : falseValue, afterFalse);
            }

            return (conditionValue, nextIndex);
        }

        private (object Value, int NextIndex) EvaluateLogical(List<Token> tokens, int startIndex)
        {
            var (left, nextIndex) = this.EvaluateLogicalAnd(tokens, startIndex);

            while (nextIndex < tokens.Count && tokens[nextIndex].Type == TokenType.Operator)
            {
                var op = tokens[nextIndex].Value;
                if (op != "||")
                {
                    break;
                }

                var (right, afterRight) = this.EvaluateLogicalAnd(tokens, nextIndex + 1);
                left = this.ApplyLogicalOperator(op, left, right);
                nextIndex = afterRight;
            }

            return (left, nextIndex);
        }

        private (object Value, int NextIndex) EvaluateLogicalAnd(List<Token> tokens, int startIndex)
        {
            var (left, nextIndex) = this.EvaluateComparison(tokens, startIndex);

            while (nextIndex < tokens.Count && tokens[nextIndex].Type == TokenType.Operator)
            {
                var op = tokens[nextIndex].Value;
                if (op != "&&")
                {
                    break;
                }

                var (right, afterRight) = this.EvaluateComparison(tokens, nextIndex + 1);
                left = this.ApplyLogicalOperator(op, left, right);
                nextIndex = afterRight;
            }

            return (left, nextIndex);
        }

        private (object Value, int NextIndex) EvaluateComparison(List<Token> tokens, int startIndex)
        {
            var (left, nextIndex) = this.EvaluateArithmetic(tokens, startIndex);

            while (nextIndex < tokens.Count && tokens[nextIndex].Type == TokenType.Operator)
            {
                var op = tokens[nextIndex].Value;
                if (op != ">" && op != "<" && op != ">=" && op != "<=" && op != "==" && op != "!=")
                {
                    break;
                }

                var (right, afterRight) = this.EvaluateArithmetic(tokens, nextIndex + 1);
                left = this.ApplyOperator(op, left, right);
                nextIndex = afterRight;
            }

            return (left, nextIndex);
        }

        private (object Value, int NextIndex) EvaluateArithmetic(List<Token> tokens, int startIndex)
        {
            var (left, nextIndex) = this.EvaluateTerm(tokens, startIndex);

            while (nextIndex < tokens.Count && tokens[nextIndex].Type == TokenType.Operator)
            {
                var op = tokens[nextIndex].Value;
                if (op != "+" && op != "-")
                {
                    break;
                }

                var (right, afterRight) = this.EvaluateTerm(tokens, nextIndex + 1);
                left = this.ApplyOperator(op, left, right);
                nextIndex = afterRight;
            }

            return (left, nextIndex);
        }

        private (object Value, int NextIndex) EvaluateTerm(List<Token> tokens, int startIndex)
        {
            var (left, nextIndex) = this.EvaluateFactor(tokens, startIndex);

            while (nextIndex < tokens.Count && tokens[nextIndex].Type == TokenType.Operator)
            {
                var op = tokens[nextIndex].Value;
                if (op != "*" && op != "/")
                {
                    break;
                }

                var (right, afterRight) = this.EvaluateFactor(tokens, nextIndex + 1);
                left = this.ApplyOperator(op, left, right);
                nextIndex = afterRight;
            }

            return (left, nextIndex);
        }

        private (object Value, int NextIndex) EvaluateFactor(List<Token> tokens, int startIndex)
        {
            if (startIndex >= tokens.Count)
            {
                throw new FormulaException("Unexpected end of expression");
            }

            var token = tokens[startIndex];

            // Handle unary minus
            if (token is { Type: TokenType.Operator, Value: "-" })
            {
                var (value, nextIndex) = this.EvaluateFactor(tokens, startIndex + 1);
                var floatValue = FormulaTypeConverter.ConvertToFloat(value);
                return (-floatValue, nextIndex);
            }

            switch (token.Type)
            {
                case TokenType.Number:
                    if (float.TryParse(token.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var floatValue))
                    {
                        return (floatValue, startIndex + 1);
                    }

                    if (int.TryParse(token.Value, out var intValue))
                    {
                        return (intValue, startIndex + 1);
                    }

                    throw new FormulaException($"Invalid number: {token.Value}");

                case TokenType.Boolean:
                    return (token.Value == "true", startIndex + 1);

                case TokenType.Identifier:
                    if (this.columnValueProviders.TryGetValue(token.Value, out var columnProvider))
                    {
                        var value = columnProvider();
                        return (value, startIndex + 1);
                    }

                    if (this.variableValueProviders.TryGetValue(token.Value, out var variableProvider))
                    {
                        var value = variableProvider();
                        return (value, startIndex + 1);
                    }

                    throw new FormulaException($"Unknown reference: {token.Value}");

                case TokenType.Function:
                    return this.EvaluateFunction(tokens, startIndex);

                case TokenType.LeftParen:
                    var (parenValue, parenNextIndex) = this.EvaluateTernary(tokens, startIndex + 1);

                    if (parenNextIndex >= tokens.Count || tokens[parenNextIndex].Type != TokenType.RightParen)
                    {
                        throw new FormulaException("Expected closing parenthesis");
                    }

                    return (parenValue, parenNextIndex + 1);

                default:
                    throw new FormulaException($"Unexpected token: {token.Value}");
            }
        }

        private (object Value, int NextIndex) EvaluateFunction(List<Token> tokens, int startIndex)
        {
            var functionName = tokens[startIndex].Value;
            var nextIndex = startIndex + 1;

            if (nextIndex >= tokens.Count || tokens[nextIndex].Type != TokenType.LeftParen)
            {
                throw new FormulaException($"Expected '(' after function '{functionName}'");
            }

            nextIndex++; // Skip the '('

            var arguments = new List<object>();

            // Handle empty function calls
            if (nextIndex < tokens.Count && tokens[nextIndex].Type == TokenType.RightParen)
            {
                return (FunctionLibrary.CallFunction(functionName, arguments), nextIndex + 1);
            }

            // Parse arguments
            while (nextIndex < tokens.Count)
            {
                var (argValue, afterArg) = this.EvaluateTernary(tokens, nextIndex);
                arguments.Add(argValue);
                nextIndex = afterArg;

                if (nextIndex >= tokens.Count)
                {
                    throw new FormulaException("Expected ')' or ',' in function call");
                }

                if (tokens[nextIndex].Type == TokenType.RightParen)
                {
                    return (FunctionLibrary.CallFunction(functionName, arguments), nextIndex + 1);
                }

                if (tokens[nextIndex].Type == TokenType.Comma)
                {
                    nextIndex++; // Skip the comma
                }
                else
                {
                    throw new FormulaException("Expected ')' or ',' in function call");
                }
            }

            throw new FormulaException("Unexpected end of function call");
        }

        private object ApplyOperator(string op, object left, object right)
        {
            var leftFloat = FormulaTypeConverter.ConvertToFloat(left);
            var rightFloat = FormulaTypeConverter.ConvertToFloat(right);

            switch (op)
            {
                case "+":
                    return leftFloat + rightFloat;
                case "-":
                    return leftFloat - rightFloat;
                case "*":
                    return leftFloat * rightFloat;

                case "/":
                    if (Math.Abs(rightFloat) < float.Epsilon)
                    {
                        throw new FormulaException("Division by zero");
                    }

                    return leftFloat / rightFloat;
                case ">":
                    return leftFloat > rightFloat ? 1f : 0f;
                case "<":
                    return leftFloat < rightFloat ? 1f : 0f;
                case ">=":
                    return leftFloat >= rightFloat ? 1f : 0f;
                case "<=":
                    return leftFloat <= rightFloat ? 1f : 0f;
                case "==":
                    return Math.Abs(leftFloat - rightFloat) < float.Epsilon ? 1f : 0f;
                case "!=":
                    return Math.Abs(leftFloat - rightFloat) >= float.Epsilon ? 1f : 0f;
                default:
                    throw new FormulaException($"Unknown operator: {op}");
            }
        }

        private object ApplyLogicalOperator(string op, object left, object right)
        {
            var leftBool = FormulaTypeConverter.ConvertToBool(left);
            var rightBool = FormulaTypeConverter.ConvertToBool(right);

            return op switch
            {
                "&&" => leftBool && rightBool,
                "||" => leftBool || rightBool,
                _ => throw new FormulaException($"Unknown logical operator: {op}"),
            };
        }
    }
}