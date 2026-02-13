// <copyright file="FormulaTokenizer.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Tabulate.Editor.Formula
{
    using System.Collections.Generic;
    using System.Text;

    public class FormulaTokenizer
    {
        public List<Token> Tokenize(string formula)
        {
            var tokens = new List<Token>();
            var i = 0;

            while (i < formula.Length)
            {
                var c = formula[i];

                if (char.IsWhiteSpace(c))
                {
                    i++;
                    continue;
                }

                if (char.IsDigit(c) || c == '.')
                {
                    var numberStr = new StringBuilder();
                    while (i < formula.Length && (char.IsDigit(formula[i]) || formula[i] == '.'))
                    {
                        numberStr.Append(formula[i]);
                        i++;
                    }

                    tokens.Add(new Token(TokenType.Number, numberStr.ToString()));
                    continue;
                }

                if (c is 'C' or 'V' && i + 1 < formula.Length && char.IsDigit(formula[i + 1]))
                {
                    // Handle column references (C0, C1, C10, etc.) and variable references (V0, V1, V10, etc.)
                    var refStr = new StringBuilder();
                    refStr.Append(c); // Add 'C' or 'V'
                    i++;

                    // Parse the number part
                    while (i < formula.Length && char.IsDigit(formula[i]))
                    {
                        refStr.Append(formula[i]);
                        i++;
                    }

                    tokens.Add(new Token(TokenType.Identifier, refStr.ToString()));
                    continue;
                }

                if (char.IsLetter(c) || c == '_')
                {
                    var identifierStr = new StringBuilder();
                    while (i < formula.Length && (char.IsLetterOrDigit(formula[i]) || formula[i] == '_' || formula[i] == '.'))
                    {
                        identifierStr.Append(formula[i]);
                        i++;
                    }

                    var identifier = identifierStr.ToString();

                    // Check if this is followed by a '(' to determine if it's a function
                    if (i < formula.Length && formula[i] == '(')
                    {
                        tokens.Add(new Token(TokenType.Function, identifier));
                    }
                    else if (identifier is "true" or "false")
                    {
                        tokens.Add(new Token(TokenType.Boolean, identifier));
                    }
                    else
                    {
                        tokens.Add(new Token(TokenType.Identifier, identifier));
                    }

                    continue;
                }

                // Handle unary minus (negative numbers)
                if (c == '-')
                {
                    var prevToken = tokens.Count > 0 ? (Token?)tokens[tokens.Count - 1] : null;
                    // Check if '-' is at the start, or after an operator, opening paren, or comma.
                    if (prevToken == null ||
                        prevToken.Value.Type == TokenType.Operator ||
                        prevToken.Value.Type == TokenType.LeftParen ||
                        prevToken.Value.Type == TokenType.Comma)
                    {
                        var numberStr = new StringBuilder();
                        numberStr.Append(c);
                        i++;
                        while (i < formula.Length && (char.IsDigit(formula[i]) || formula[i] == '.'))
                        {
                            numberStr.Append(formula[i]);
                            i++;
                        }
                        tokens.Add(new Token(TokenType.Number, numberStr.ToString()));
                        continue; // Continue to next token
                    }
                }

                // Handle multi-character operators first
                if (c == '>' && i + 1 < formula.Length && formula[i + 1] == '=')
                {
                    tokens.Add(new Token(TokenType.Operator, ">="));
                    i++; // Skip the next character
                }
                else if (c == '<' && i + 1 < formula.Length && formula[i + 1] == '=')
                {
                    tokens.Add(new Token(TokenType.Operator, "<="));
                    i++; // Skip the next character
                }
                else if (c == '=' && i + 1 < formula.Length && formula[i + 1] == '=')
                {
                    tokens.Add(new Token(TokenType.Operator, "=="));
                    i++; // Skip the next character
                }
                else if (c == '!' && i + 1 < formula.Length && formula[i + 1] == '=')
                {
                    tokens.Add(new Token(TokenType.Operator, "!="));
                    i++; // Skip the next character
                }
                else if (c == '&' && i + 1 < formula.Length && formula[i + 1] == '&')
                {
                    tokens.Add(new Token(TokenType.Operator, "&&"));
                    i++; // Skip the next character
                }
                else if (c == '|' && i + 1 < formula.Length && formula[i + 1] == '|')
                {
                    tokens.Add(new Token(TokenType.Operator, "||"));
                    i++; // Skip the next character
                }
                else
                {
                    switch (c)
                    {
                        case '+':
                            tokens.Add(new Token(TokenType.Operator, "+"));
                            break;
                        case '-':
                            tokens.Add(new Token(TokenType.Operator, "-"));
                            break;
                        case '*':
                            tokens.Add(new Token(TokenType.Operator, "*"));
                            break;
                        case '/':
                            tokens.Add(new Token(TokenType.Operator, "/"));
                            break;
                        case '>':
                            tokens.Add(new Token(TokenType.Operator, ">"));
                            break;
                        case '<':
                            tokens.Add(new Token(TokenType.Operator, "<"));
                            break;
                        case '(':
                            tokens.Add(new Token(TokenType.LeftParen, "("));
                            break;
                        case ')':
                            tokens.Add(new Token(TokenType.RightParen, ")"));
                            break;
                        case ',':
                            tokens.Add(new Token(TokenType.Comma, ","));
                            break;
                        case '?':
                            tokens.Add(new Token(TokenType.Question, "?"));
                            break;
                        case ':':
                            tokens.Add(new Token(TokenType.Colon, ":"));
                            break;
                        default:
                            throw new FormulaException($"Unexpected character '{c}' at position {i}");
                    }
                }

                i++;
            }

            return tokens;
        }
    }

    public enum TokenType
    {
        Number,
        Identifier,
        Operator,
        LeftParen,
        RightParen,
        Function,
        Comma,
        Question,
        Colon,
        Boolean,
    }

    public readonly struct Token
    {
        public Token(TokenType type, string value)
        {
            this.Type = type;
            this.Value = value;
        }

        public TokenType Type { get; }

        public string Value { get; }
    }
}