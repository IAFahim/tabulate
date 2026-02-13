// <copyright file="FunctionLibrary.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Tabulate.Editor.Formula
{
    using System.Collections.Generic;
    using UnityEngine;

    public static class FunctionLibrary
    {
        public static object CallFunction(string functionName, List<object> arguments)
        {
            switch (functionName.ToUpperInvariant())
            {
                case "IF":
                    return CallIf(arguments);

                case "MATHF.ABS":
                case "ABS":
                    return CallAbs(arguments);

                case "MATHF.MIN":
                case "MIN":
                    return CallMin(arguments);

                case "MATHF.MAX":
                case "MAX":
                    return CallMax(arguments);

                case "MATHF.CLAMP":
                case "CLAMP":
                    return CallClamp(arguments);

                case "MATHF.SQRT":
                case "SQRT":
                    return CallSqrt(arguments);

                case "MATHF.POW":
                case "POW":
                    return CallPow(arguments);

                case "MATHF.ROUND":
                case "ROUND":
                    return CallRound(arguments);

                case "MATHF.CEILING":
                case "CEILING":
                case "CEIL":
                    return CallCeiling(arguments);

                case "MATHF.FLOOR":
                case "FLOOR":
                    return CallFloor(arguments);

                case "MATHF.LERP":
                case "LERP":
                    return CallLerp(arguments);

                case "MATHF.INVERSELERP":
                case "INVERSELERP":
                    return CallInverseLerp(arguments);

                case "MATHF.SMOOTHSTEP":
                case "SMOOTHSTEP":
                    return CallSmoothStep(arguments);

                default:
                    throw new FormulaException($"Unknown function: {functionName}");
            }
        }

        private static object CallIf(List<object> arguments)
        {
            if (arguments.Count != 3)
            {
                throw new FormulaException("IF function requires exactly 3 arguments: condition, true_value, false_value");
            }

            var condition = FormulaTypeConverter.ConvertToFloat(arguments[0]);
            return Mathf.Abs(condition) > float.Epsilon ? arguments[1] : arguments[2];
        }

        private static object CallAbs(List<object> arguments)
        {
            if (arguments.Count != 1)
            {
                throw new FormulaException("ABS function requires exactly 1 argument: value");
            }

            return Mathf.Abs(FormulaTypeConverter.ConvertToFloat(arguments[0]));
        }

        private static object CallMin(List<object> arguments)
        {
            if (arguments.Count != 2)
            {
                throw new FormulaException("MIN function requires exactly 2 arguments: value1, value2");
            }

            return Mathf.Min(FormulaTypeConverter.ConvertToFloat(arguments[0]), FormulaTypeConverter.ConvertToFloat(arguments[1]));
        }

        private static object CallMax(List<object> arguments)
        {
            if (arguments.Count != 2)
            {
                throw new FormulaException("MAX function requires exactly 2 arguments: value1, value2");
            }

            return Mathf.Max(FormulaTypeConverter.ConvertToFloat(arguments[0]), FormulaTypeConverter.ConvertToFloat(arguments[1]));
        }

        private static object CallClamp(List<object> arguments)
        {
            if (arguments.Count != 3)
            {
                throw new FormulaException("CLAMP function requires exactly 3 arguments: value, min, max");
            }

            var value = FormulaTypeConverter.ConvertToFloat(arguments[0]);
            var min = FormulaTypeConverter.ConvertToFloat(arguments[1]);
            var max = FormulaTypeConverter.ConvertToFloat(arguments[2]);
            return Mathf.Clamp(value, min, max);
        }

        private static object CallSqrt(List<object> arguments)
        {
            if (arguments.Count != 1)
            {
                throw new FormulaException("SQRT function requires exactly 1 argument: value");
            }

            var sqrtValue = FormulaTypeConverter.ConvertToFloat(arguments[0]);
            if (sqrtValue < 0)
            {
                throw new FormulaException($"SQRT function cannot take square root of negative number: {sqrtValue}");
            }

            return Mathf.Sqrt(sqrtValue);
        }

        private static object CallPow(List<object> arguments)
        {
            if (arguments.Count != 2)
            {
                throw new FormulaException("POW function requires exactly 2 arguments: base, exponent");
            }

            return Mathf.Pow(FormulaTypeConverter.ConvertToFloat(arguments[0]), FormulaTypeConverter.ConvertToFloat(arguments[1]));
        }

        private static object CallRound(List<object> arguments)
        {
            if (arguments.Count == 1)
            {
                // Round to nearest integer
                return Mathf.Round(FormulaTypeConverter.ConvertToFloat(arguments[0]));
            }

            if (arguments.Count == 2)
            {
                // Round to specified number of decimal places
                var value = FormulaTypeConverter.ConvertToFloat(arguments[0]);
                var decimals = Mathf.RoundToInt(FormulaTypeConverter.ConvertToFloat(arguments[1]));

                if (decimals < 0)
                {
                    throw new FormulaException("ROUND function decimal places must be non-negative");
                }

                var multiplier = Mathf.Pow(10f, decimals);
                return Mathf.Round(value * multiplier) / multiplier;
            }

            throw new FormulaException("ROUND function requires 1 or 2 arguments: value, [decimal_places]");
        }

        private static object CallCeiling(List<object> arguments)
        {
            if (arguments.Count != 1)
            {
                throw new FormulaException("CEILING function requires exactly 1 argument: value");
            }

            return Mathf.Ceil(FormulaTypeConverter.ConvertToFloat(arguments[0]));
        }

        private static object CallFloor(List<object> arguments)
        {
            if (arguments.Count != 1)
            {
                throw new FormulaException("FLOOR function requires exactly 1 argument: value");
            }

            return Mathf.Floor(FormulaTypeConverter.ConvertToFloat(arguments[0]));
        }

        private static object CallLerp(List<object> arguments)
        {
            if (arguments.Count != 3)
            {
                throw new FormulaException("LERP function requires exactly 3 arguments: from, to, t");
            }

            var from = FormulaTypeConverter.ConvertToFloat(arguments[0]);
            var to = FormulaTypeConverter.ConvertToFloat(arguments[1]);
            var t = FormulaTypeConverter.ConvertToFloat(arguments[2]);

            return Mathf.Lerp(from, to, t);
        }

        private static object CallInverseLerp(List<object> arguments)
        {
            if (arguments.Count != 3)
            {
                throw new FormulaException("INVERSELERP function requires exactly 3 arguments: from, to, value");
            }

            var from = FormulaTypeConverter.ConvertToFloat(arguments[0]);
            var to = FormulaTypeConverter.ConvertToFloat(arguments[1]);
            var value = FormulaTypeConverter.ConvertToFloat(arguments[2]);

            return Mathf.InverseLerp(from, to, value);
        }

        private static object CallSmoothStep(List<object> arguments)
        {
            if (arguments.Count != 3)
            {
                throw new FormulaException("SMOOTHSTEP function requires exactly 3 arguments: min, max, t");
            }

            var min = FormulaTypeConverter.ConvertToFloat(arguments[0]);
            var max = FormulaTypeConverter.ConvertToFloat(arguments[1]);
            var t = FormulaTypeConverter.ConvertToFloat(arguments[2]);

            return Mathf.SmoothStep(min, max, t);
        }
    }
}