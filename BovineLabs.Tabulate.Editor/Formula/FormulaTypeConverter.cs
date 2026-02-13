// <copyright file="FormulaTypeConverter.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Tabulate.Editor.Formula
{
    using System;
    using UnityEngine;

    /// <summary>
    /// Provides type conversion methods for the formula evaluation system.
    ///
    /// This class handles the complex type conversion requirements of the formula engine:
    /// - Preserving boolean semantics for logical operations
    /// - Converting values for arithmetic operations
    /// - Handling null values and string parsing
    /// - Providing consistent behavior across the formula system
    ///
    /// The three conversion methods serve different purposes in the evaluation pipeline:
    /// 1. ConvertForValueProvider: Used by value providers to preserve semantic types (booleans stay booleans)
    /// 2. ConvertToFloat: Used for arithmetic operations (booleans become 1f/0f)
    /// 3. ConvertToBool: Used for logical operations (numbers become truthy/falsy)
    /// </summary>
    public static class FormulaTypeConverter
    {
        /// <summary>
        /// Converts a value to a boolean for logical operations (&&, ||, !).
        ///
        /// Used by the expression evaluator when performing logical operations.
        /// Numbers are converted using "truthy" logic (non-zero = true).
        /// Strings are parsed as booleans first, then as numbers if boolean parsing fails.
        ///
        /// Examples:
        /// - true -> true
        /// - 1.5f -> true (non-zero)
        /// - 0f -> false (zero)
        /// - "true" -> true (boolean string)
        /// - "1.5" -> true (parsed as number, non-zero)
        /// - null -> false
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <returns>The boolean representation of the value.</returns>
        public static bool ConvertToBool(object? value)
        {
            if (value == null)
            {
                return false;
            }

            return value switch
            {
                bool b => b,
                float f => Mathf.Abs(f) > float.Epsilon,
                int i => i != 0,
                double d => Math.Abs(d) > double.Epsilon,
                decimal dec => dec != 0,
                string s when bool.TryParse(s, out var boolValue) => boolValue,
                string s when float.TryParse(s, out var floatValue) => Mathf.Abs(floatValue) > float.Epsilon,
                _ => false, // Default for unhandled types
            };
        }

        /// <summary>
        /// Converts a value to a float for arithmetic operations (+, -, *, /, comparisons).
        ///
        /// Used by the expression evaluator when performing arithmetic operations.
        /// Booleans are converted to numbers (true = 1f, false = 0f).
        /// This allows mathematical operations on boolean results.
        ///
        /// Examples:
        /// - 42 -> 42f
        /// - true -> 1f
        /// - false -> 0f
        /// - "3.14" -> 3.14f
        /// - null -> 0f
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <returns>The float representation of the value.</returns>
        public static float ConvertToFloat(object? value)
        {
            if (value == null)
            {
                return 0f;
            }

            return value switch
            {
                float f => f,
                int i => i,
                double d => (float)d,
                decimal dec => (float)dec,
                bool b => b ? 1f : 0f,
                string s when float.TryParse(s, out var parsed) => parsed,
                _ => 0f, // Default for unhandled types
            };
        }

        /// <summary>
        /// Converts a value for use in formula value providers while preserving semantic types.
        ///
        /// Used by FormulaColumnHandler and VariableEvaluator when setting up value providers.
        /// CRITICAL: Preserves boolean values as booleans so they work correctly in logical operations.
        /// If booleans were converted to floats here, expressions like "C0 && C1" would fail
        /// because the ExpressionEvaluator would receive 1f/0f instead of true/false.
        ///
        /// Type preservation:
        /// - bool -> bool (PRESERVED - this is the key difference from ConvertToFloat)
        /// - int -> float (normalized to float for consistency)
        /// - string "true"/"false" -> bool (semantic parsing)
        /// - string numbers -> float
        ///
        /// Examples:
        /// - true -> true (stays boolean for logical ops)
        /// - 42 -> 42f (normalized to float)
        /// - "false" -> false (parsed as boolean)
        /// - "3.14" -> 3.14f (parsed as number)
        /// - null -> 0f
        /// </summary>
        /// <param name="value">The value to convert for the value provider.</param>
        /// <returns>The converted value with appropriate semantic type preservation.</returns>
        public static object ConvertForValueProvider(object? value)
        {
            if (value == null)
            {
                return 0f;
            }

            return value switch
            {
                float f => f,
                int i => (float)i,
                double d => (float)d,
                decimal dec => (float)dec,
                bool b => b,  // Return boolean as-is for proper conditional evaluation
                string s when bool.TryParse(s, out var boolValue) => boolValue,  // Handle boolean strings first
                string s when float.TryParse(s, out var parsed) => parsed,
                _ => 0f,
            };
        }
    }
}