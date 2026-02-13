// <copyright file="FormulaEngineTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Tabulate.Tests
{
    using BovineLabs.Tabulate.Editor.Formula;
    using NUnit.Framework;

    public class FormulaEngineTests
    {
        private FormulaEngine engine = null!;

        [SetUp]
        public void SetUp()
        {
            this.engine = new FormulaEngine();
        }

        [Test]
        public void Evaluate_SimpleNumber_ReturnsNumber()
        {
            var result = this.engine.Evaluate("42");
            Assert.AreEqual(42f, result);
        }

        [Test]
        public void Evaluate_FloatNumber_ReturnsFloat()
        {
            var result = this.engine.Evaluate("3.14");
            Assert.AreEqual(3.14f, result);
        }

        [Test]
        public void Evaluate_SimpleAddition_ReturnsSum()
        {
            var result = this.engine.Evaluate("2 + 3");
            Assert.AreEqual(5f, result);
        }

        [Test]
        public void Evaluate_SimpleSubtraction_ReturnsDifference()
        {
            var result = this.engine.Evaluate("5 - 2");
            Assert.AreEqual(3f, result);
        }

        [Test]
        public void Evaluate_SimpleMultiplication_ReturnsProduct()
        {
            var result = this.engine.Evaluate("4 * 3");
            Assert.AreEqual(12f, result);
        }

        [Test]
        public void Evaluate_SimpleDivision_ReturnsQuotient()
        {
            var result = this.engine.Evaluate("8 / 2");
            Assert.AreEqual(4f, result);
        }

        [Test]
        public void Evaluate_DivisionByZero_ThrowsFormulaException()
        {
            Assert.Throws<FormulaException>(() => this.engine.Evaluate("5 / 0"));
        }

        [Test]
        public void Evaluate_OperatorPrecedence_RespectsOrder()
        {
            var result = this.engine.Evaluate("2 + 3 * 4");
            Assert.AreEqual(14f, result); // Should be 2 + (3 * 4) = 14, not (2 + 3) * 4 = 20
        }

        [Test]
        public void Evaluate_ParenthesesOverridePrecedence_ReturnsCorrectResult()
        {
            var result = this.engine.Evaluate("(2 + 3) * 4");
            Assert.AreEqual(20f, result);
        }

        [Test]
        public void Evaluate_NestedParentheses_ReturnsCorrectResult()
        {
            var result = this.engine.Evaluate("((2 + 3) * 4) - 1");
            Assert.AreEqual(19f, result);
        }

        [Test]
        public void Evaluate_ComplexExpression_ReturnsCorrectResult()
        {
            var result = this.engine.Evaluate("2 * (3 + 4) - 5 / 2");
            Assert.AreEqual(11.5f, result); // 2 * 7 - 2.5 = 14 - 2.5 = 11.5
        }

        [Test]
        public void Evaluate_MismatchedParentheses_ThrowsFormulaException()
        {
            Assert.Throws<FormulaException>(() => this.engine.Evaluate("(2 + 3"));
            Assert.Throws<FormulaException>(() => this.engine.Evaluate("2 + 3)"));
            Assert.Throws<FormulaException>(() => this.engine.Evaluate("((2 + 3)"));
        }

        [Test]
        public void Evaluate_InvalidCharacter_ThrowsFormulaException()
        {
            Assert.Throws<FormulaException>(() => this.engine.Evaluate("2 + 3 & 4"));
        }

        [Test]
        public void Evaluate_EmptyFormula_ReturnsZero()
        {
            var result = this.engine.Evaluate(string.Empty);
            Assert.AreEqual(0f, result);
        }

        [Test]
        public void Evaluate_WhitespaceFormula_ReturnsZero()
        {
            var result = this.engine.Evaluate("   ");
            Assert.AreEqual(0f, result);
        }

        [Test]
        public void Evaluate_WithSpaces_IgnoresSpaces()
        {
            var result = this.engine.Evaluate(" 2 + 3 * 4 ");
            Assert.AreEqual(14f, result);
        }

        [Test]
        public void Evaluate_ColumnReference_UsesProvidedValue()
        {
            this.engine.SetColumnValueProvider("C0", () => 5f);
            var result = this.engine.Evaluate("C0 * 2");
            Assert.AreEqual(10f, result);
        }

        [Test]
        public void Evaluate_MultipleColumnReferences_UsesAllValues()
        {
            this.engine.SetColumnValueProvider("C0", () => 3f);
            this.engine.SetColumnValueProvider("C1", () => 4f);
            var result = this.engine.Evaluate("C0 * C1");
            Assert.AreEqual(12f, result);
        }

        [Test]
        public void Evaluate_ColumnReferenceWithMath_ReturnsCorrectResult()
        {
            this.engine.SetColumnValueProvider("C0", () => 10f);
            var result = this.engine.Evaluate("C0 + 5 * 2");
            Assert.AreEqual(20f, result); // 10 + (5 * 2) = 20
        }

        [Test]
        public void Evaluate_UnknownColumnReference_ThrowsFormulaException()
        {
            Assert.Throws<FormulaException>(() => this.engine.Evaluate("C99 + 1"));
        }

        [Test]
        public void GetColumnReferences_SimpleReference_ReturnsColumnName()
        {
            var references = this.engine.GetColumnReferences("C0 * 2");
            Assert.AreEqual(1, references.Count);
            Assert.IsTrue(references.Contains("C0"));
        }

        [Test]
        public void GetColumnReferences_MultipleReferences_ReturnsAllNames()
        {
            var references = this.engine.GetColumnReferences("C0 * C1 + C2");
            Assert.AreEqual(3, references.Count);
            Assert.IsTrue(references.Contains("C0"));
            Assert.IsTrue(references.Contains("C1"));
            Assert.IsTrue(references.Contains("C2"));
        }

        [Test]
        public void GetColumnReferences_DuplicateReferences_ReturnsUniqueNames()
        {
            var references = this.engine.GetColumnReferences("C0 + C0 * 2");
            Assert.AreEqual(1, references.Count);
            Assert.IsTrue(references.Contains("C0"));
        }

        [Test]
        public void GetColumnReferences_NoReferences_ReturnsEmpty()
        {
            var references = this.engine.GetColumnReferences("2 + 3 * 4");
            Assert.AreEqual(0, references.Count);
        }

        [Test]
        public void GetColumnReferences_EmptyFormula_ReturnsEmpty()
        {
            var references = this.engine.GetColumnReferences(string.Empty);
            Assert.AreEqual(0, references.Count);
        }

        [Test]
        public void SetColumnValueProvider_IntValue_ConvertsToFloat()
        {
            this.engine.SetColumnValueProvider("C0", () => 42);
            var result = this.engine.Evaluate("C0 * 2");
            Assert.AreEqual(84f, result);
        }

        [Test]
        public void ClearColumnValueProviders_RemovesAllProviders()
        {
            this.engine.SetColumnValueProvider("C0", () => 5f);
            this.engine.ClearColumnValueProviders();
            Assert.Throws<FormulaException>(() => this.engine.Evaluate("C0 + 1"));
        }

        [Test]
        public void Evaluate_InvalidExpression_ThrowsFormulaException()
        {
            Assert.Throws<FormulaException>(() => this.engine.Evaluate("2 +"));
            Assert.Throws<FormulaException>(() => this.engine.Evaluate("* 3"));
            Assert.Throws<FormulaException>(() => this.engine.Evaluate("2 + + 3"));
        }

        // Column ID system tests
        [Test]
        public void Evaluate_ColumnIdReference_C0_Works()
        {
            this.engine.SetColumnValueProvider("C0", () => 10f);
            var result = this.engine.Evaluate("C0");
            Assert.AreEqual(10f, result);
        }

        [Test]
        public void Evaluate_ColumnIdReference_C10_Works()
        {
            this.engine.SetColumnValueProvider("C10", () => 25f);
            var result = this.engine.Evaluate("C10 * 2");
            Assert.AreEqual(50f, result);
        }

        [Test]
        public void Evaluate_ColumnIdReference_C123_Works()
        {
            this.engine.SetColumnValueProvider("C123", () => 5f);
            var result = this.engine.Evaluate("C123 + 1");
            Assert.AreEqual(6f, result);
        }

        [Test]
        public void GetColumnReferences_ColumnIds_ReturnsCorrectReferences()
        {
            var references = this.engine.GetColumnReferences("C0 + C1 * C10");
            Assert.AreEqual(3, references.Count);
            Assert.IsTrue(references.Contains("C0"));
            Assert.IsTrue(references.Contains("C1"));
            Assert.IsTrue(references.Contains("C10"));
        }

        [Test]
        public void Evaluate_MixedColumnIds_ReturnsCorrectResult()
        {
            this.engine.SetColumnValueProvider("C0", () => 2f);
            this.engine.SetColumnValueProvider("C5", () => 3f);
            this.engine.SetColumnValueProvider("C10", () => 4f);

            var result = this.engine.Evaluate("C0 + C5 * C10");
            Assert.AreEqual(14f, result); // 2 + (3 * 4) = 14
        }

        [Test]
        public void Evaluate_InvalidColumnFormat_ThrowsFormulaException()
        {
            // Should fail because 'C' without number is not a valid column reference
            Assert.Throws<FormulaException>(() => this.engine.Evaluate("C + 1"));
        }

        [Test]
        public void Evaluate_OldDisplayNameStyle_ThrowsFormulaException()
        {
            // Old style references like 'radius' should no longer work
            Assert.Throws<FormulaException>(() => this.engine.Evaluate("radius * 2"));
        }

        [Test]
        public void GetColumnReferences_EmptyAndValidMixed_ReturnsOnlyValid()
        {
            // This tests that we only get valid C-prefix references
            var references = this.engine.GetColumnReferences("C0 + 5 + C1");
            Assert.AreEqual(2, references.Count);
            Assert.IsTrue(references.Contains("C0"));
            Assert.IsTrue(references.Contains("C1"));
        }

        // Enhanced Formula Tests - Comparison Operators
        [Test]
        public void Evaluate_ComparisonGreaterThan_ReturnsCorrectResult()
        {
            var result = this.engine.Evaluate("5 > 3");
            Assert.AreEqual(1f, result); // true = 1

            result = this.engine.Evaluate("3 > 5");
            Assert.AreEqual(0f, result); // false = 0
        }

        [Test]
        public void Evaluate_ComparisonLessThan_ReturnsCorrectResult()
        {
            var result = this.engine.Evaluate("3 < 5");
            Assert.AreEqual(1f, result); // true = 1

            result = this.engine.Evaluate("5 < 3");
            Assert.AreEqual(0f, result); // false = 0
        }

        [Test]
        public void Evaluate_ComparisonGreaterThanOrEqual_ReturnsCorrectResult()
        {
            var result = this.engine.Evaluate("5 >= 5");
            Assert.AreEqual(1f, result); // true = 1

            result = this.engine.Evaluate("5 >= 3");
            Assert.AreEqual(1f, result); // true = 1

            result = this.engine.Evaluate("3 >= 5");
            Assert.AreEqual(0f, result); // false = 0
        }

        [Test]
        public void Evaluate_ComparisonLessThanOrEqual_ReturnsCorrectResult()
        {
            var result = this.engine.Evaluate("3 <= 5");
            Assert.AreEqual(1f, result); // true = 1

            result = this.engine.Evaluate("5 <= 5");
            Assert.AreEqual(1f, result); // true = 1

            result = this.engine.Evaluate("5 <= 3");
            Assert.AreEqual(0f, result); // false = 0
        }

        [Test]
        public void Evaluate_ComparisonEqual_ReturnsCorrectResult()
        {
            var result = this.engine.Evaluate("5 == 5");
            Assert.AreEqual(1f, result); // true = 1

            result = this.engine.Evaluate("5 == 3");
            Assert.AreEqual(0f, result); // false = 0

            // Test float equality with tolerance
            result = this.engine.Evaluate("5.0 == 5");
            Assert.AreEqual(1f, result); // true = 1
        }

        [Test]
        public void Evaluate_ComparisonNotEqual_ReturnsCorrectResult()
        {
            var result = this.engine.Evaluate("5 != 3");
            Assert.AreEqual(1f, result); // true = 1

            result = this.engine.Evaluate("5 != 5");
            Assert.AreEqual(0f, result); // false = 0
        }

        // Enhanced Formula Tests - Ternary Operator
        [Test]
        public void Evaluate_TernaryOperator_ReturnsCorrectResult()
        {
            var result = this.engine.Evaluate("5 > 3 ? 10 : 20");
            Assert.AreEqual(10f, result); // condition is true, return first value

            result = this.engine.Evaluate("3 > 5 ? 10 : 20");
            Assert.AreEqual(20f, result); // condition is false, return second value
        }

        [Test]
        public void Evaluate_TernaryOperatorWithExpressions_ReturnsCorrectResult()
        {
            var result = this.engine.Evaluate("5 > 3 ? 2 * 5 : 3 + 4");
            Assert.AreEqual(10f, result); // condition is true, return 2 * 5 = 10

            result = this.engine.Evaluate("3 > 5 ? 2 * 5 : 3 + 4");
            Assert.AreEqual(7f, result); // condition is false, return 3 + 4 = 7
        }

        [Test]
        public void Evaluate_TernaryOperatorWithColumns_ReturnsCorrectResult()
        {
            this.engine.SetColumnValueProvider("C0", () => 5f);
            this.engine.SetColumnValueProvider("C1", () => 10f);
            this.engine.SetColumnValueProvider("C2", () => 20f);

            var result = this.engine.Evaluate("C0 > 3 ? C1 : C2");
            Assert.AreEqual(10f, result); // C0 (5) > 3 is true, return C1 (10)
        }

        // Enhanced Formula Tests - Math Functions
        [Test]
        public void Evaluate_AbsFunction_ReturnsCorrectResult()
        {
            var result = this.engine.Evaluate("ABS(-5)");
            Assert.AreEqual(5f, result);

            result = this.engine.Evaluate("ABS(5)");
            Assert.AreEqual(5f, result);

            result = this.engine.Evaluate("MATHF.ABS(-3.5)");
            Assert.AreEqual(3.5f, result);
        }

        [Test]
        public void Evaluate_MinFunction_ReturnsCorrectResult()
        {
            var result = this.engine.Evaluate("MIN(5, 3)");
            Assert.AreEqual(3f, result);

            result = this.engine.Evaluate("MATHF.MIN(10, 20)");
            Assert.AreEqual(10f, result);
        }

        [Test]
        public void Evaluate_MaxFunction_ReturnsCorrectResult()
        {
            var result = this.engine.Evaluate("MAX(5, 3)");
            Assert.AreEqual(5f, result);

            result = this.engine.Evaluate("MATHF.MAX(10, 20)");
            Assert.AreEqual(20f, result);
        }

        [Test]
        public void Evaluate_ClampFunction_ReturnsCorrectResult()
        {
            var result = this.engine.Evaluate("CLAMP(5, 1, 10)");
            Assert.AreEqual(5f, result); // 5 is within range [1, 10]

            result = this.engine.Evaluate("CLAMP(0, 1, 10)");
            Assert.AreEqual(1f, result); // 0 is below min, clamp to 1

            result = this.engine.Evaluate("CLAMP(15, 1, 10)");
            Assert.AreEqual(10f, result); // 15 is above max, clamp to 10
        }

        [Test]
        public void Evaluate_SqrtFunction_ReturnsCorrectResult()
        {
            var result = this.engine.Evaluate("SQRT(9)");
            Assert.AreEqual(3f, result);

            result = this.engine.Evaluate("SQRT(16)");
            Assert.AreEqual(4f, result);
        }

        [Test]
        public void Evaluate_SqrtFunction_NegativeNumber_ThrowsException()
        {
            Assert.Throws<FormulaException>(() => this.engine.Evaluate("SQRT(-1)"));
        }

        [Test]
        public void Evaluate_PowFunction_ReturnsCorrectResult()
        {
            var result = this.engine.Evaluate("POW(2, 3)");
            Assert.AreEqual(8f, result); // 2^3 = 8

            result = this.engine.Evaluate("POW(5, 2)");
            Assert.AreEqual(25f, result); // 5^2 = 25
        }

        // Enhanced Formula Tests - IF Function
        [Test]
        public void Evaluate_IfFunction_ReturnsCorrectResult()
        {
            var result = this.engine.Evaluate("IF(1, 10, 20)");
            Assert.AreEqual(10f, result); // condition is true (1), return first value

            result = this.engine.Evaluate("IF(0, 10, 20)");
            Assert.AreEqual(20f, result); // condition is false (0), return second value
        }

        [Test]
        public void Evaluate_IfFunctionWithComparison_ReturnsCorrectResult()
        {
            var result = this.engine.Evaluate("IF(5 > 3, 100, 200)");
            Assert.AreEqual(100f, result); // 5 > 3 is true, return 100

            result = this.engine.Evaluate("IF(3 > 5, 100, 200)");
            Assert.AreEqual(200f, result); // 3 > 5 is false, return 200
        }

        [Test]
        public void Evaluate_IfFunctionWrongArgumentCount_ThrowsException()
        {
            Assert.Throws<FormulaException>(() => this.engine.Evaluate("IF(1, 2)"));
            Assert.Throws<FormulaException>(() => this.engine.Evaluate("IF(1, 2, 3, 4)"));
        }

        // Enhanced Formula Tests - Complex Expressions
        [Test]
        public void Evaluate_ComplexExpressionWithFunctions_ReturnsCorrectResult()
        {
            this.engine.SetColumnValueProvider("C0", () => -10f);
            this.engine.SetColumnValueProvider("C1", () => 5f);

            var result = this.engine.Evaluate("MAX(ABS(C0), C1) * 2");
            Assert.AreEqual(20f, result); // MAX(ABS(-10), 5) * 2 = MAX(10, 5) * 2 = 10 * 2 = 20
        }

        [Test]
        public void Evaluate_NestedFunctions_ReturnsCorrectResult()
        {
            var result = this.engine.Evaluate("ABS(MIN(-5, -10))");
            Assert.AreEqual(10f, result); // ABS(MIN(-5, -10)) = ABS(-10) = 10
        }

        [Test]
        public void Evaluate_OperatorPrecedenceWithComparisons_ReturnsCorrectResult()
        {
            var result = this.engine.Evaluate("2 + 3 > 4");
            Assert.AreEqual(1f, result); // (2 + 3) > 4 = 5 > 4 = true = 1

            result = this.engine.Evaluate("2 > 3 + 4");
            Assert.AreEqual(0f, result); // 2 > (3 + 4) = 2 > 7 = false = 0
        }

        [Test]
        public void Evaluate_InvalidFunctionName_ThrowsException()
        {
            Assert.Throws<FormulaException>(() => this.engine.Evaluate("UNKNOWN(5)"));
        }

        [Test]
        public void Evaluate_FunctionWrongArgumentCount_ThrowsException()
        {
            Assert.Throws<FormulaException>(() => this.engine.Evaluate("ABS(1, 2)"));
            Assert.Throws<FormulaException>(() => this.engine.Evaluate("MIN(1)"));
            Assert.Throws<FormulaException>(() => this.engine.Evaluate("CLAMP(1, 2)"));
        }

        // Boolean Literal Tests
        [Test]
        public void Evaluate_BooleanLiteralTrue_ReturnsTrue()
        {
            var result = this.engine.Evaluate("true");
            Assert.AreEqual(true, result);
        }

        [Test]
        public void Evaluate_BooleanLiteralFalse_ReturnsFalse()
        {
            var result = this.engine.Evaluate("false");
            Assert.AreEqual(false, result);
        }

        // Logical Operator Tests
        [Test]
        public void Evaluate_LogicalAnd_TrueTrueReturnsTrue()
        {
            var result = this.engine.Evaluate("true && true");
            Assert.AreEqual(true, result);
        }

        [Test]
        public void Evaluate_LogicalAnd_TrueFalseReturnsFalse()
        {
            var result = this.engine.Evaluate("true && false");
            Assert.AreEqual(false, result);
        }

        [Test]
        public void Evaluate_LogicalAnd_FalseTrueReturnsFalse()
        {
            var result = this.engine.Evaluate("false && true");
            Assert.AreEqual(false, result);
        }

        [Test]
        public void Evaluate_LogicalAnd_FalseFalseReturnsFalse()
        {
            var result = this.engine.Evaluate("false && false");
            Assert.AreEqual(false, result);
        }

        [Test]
        public void Evaluate_LogicalOr_TrueTrueReturnsTrue()
        {
            var result = this.engine.Evaluate("true || true");
            Assert.AreEqual(true, result);
        }

        [Test]
        public void Evaluate_LogicalOr_TrueFalseReturnsTrue()
        {
            var result = this.engine.Evaluate("true || false");
            Assert.AreEqual(true, result);
        }

        [Test]
        public void Evaluate_LogicalOr_FalseTrueReturnsTrue()
        {
            var result = this.engine.Evaluate("false || true");
            Assert.AreEqual(true, result);
        }

        [Test]
        public void Evaluate_LogicalOr_FalseFalseReturnsFalse()
        {
            var result = this.engine.Evaluate("false || false");
            Assert.AreEqual(false, result);
        }

        // Mixed Boolean and Numeric Tests
        [Test]
        public void Evaluate_BooleanToFloatConversion_TrueReturnsOne()
        {
            var result = this.engine.Evaluate("true + 0");
            Assert.AreEqual(1f, result);
        }

        [Test]
        public void Evaluate_BooleanToFloatConversion_FalseReturnsZero()
        {
            var result = this.engine.Evaluate("false + 0");
            Assert.AreEqual(0f, result);
        }

        [Test]
        public void Evaluate_NumericToBooleanConversion_NonZeroReturnsTrue()
        {
            var result = this.engine.Evaluate("5 && true");
            Assert.AreEqual(true, result);
        }

        [Test]
        public void Evaluate_NumericToBooleanConversion_ZeroReturnsFalse()
        {
            var result = this.engine.Evaluate("0 && true");
            Assert.AreEqual(false, result);
        }

        // Logical Operator Precedence Tests
        [Test]
        public void Evaluate_LogicalOperatorPrecedence_AndOverOr()
        {
            var result = this.engine.Evaluate("true || false && false");
            Assert.AreEqual(true, result); // true || (false && false) = true || false = true
        }

        [Test]
        public void Evaluate_LogicalOperatorPrecedence_ParenthesesOverride()
        {
            var result = this.engine.Evaluate("(true || false) && false");
            Assert.AreEqual(false, result); // (true || false) && false = true && false = false
        }

        // Comparison with Boolean Results Tests
        [Test]
        public void Evaluate_ComparisonResultInLogicalExpression_ReturnsCorrectResult()
        {
            var result = this.engine.Evaluate("(5 > 3) && (2 < 4)");
            Assert.AreEqual(true, result); // true && true = true
        }

        [Test]
        public void Evaluate_ComparisonResultInLogicalExpression_MixedResults()
        {
            var result = this.engine.Evaluate("(5 > 3) && (2 > 4)");
            Assert.AreEqual(false, result); // true && false = false
        }

        // Ternary Operator with Boolean Tests
        [Test]
        public void Evaluate_TernaryWithBooleanCondition_TrueReturnsFirstValue()
        {
            var result = this.engine.Evaluate("true ? 10 : 20");
            Assert.AreEqual(10f, result);
        }

        [Test]
        public void Evaluate_TernaryWithBooleanCondition_FalseReturnsSecondValue()
        {
            var result = this.engine.Evaluate("false ? 10 : 20");
            Assert.AreEqual(20f, result);
        }

        [Test]
        public void Evaluate_TernaryWithBooleanValues_ReturnsCorrectBoolean()
        {
            var result = this.engine.Evaluate("5 > 3 ? true : false");
            Assert.AreEqual(true, result);
        }

        // Complex Boolean Expression Tests
        [Test]
        public void Evaluate_ComplexBooleanExpression_ReturnsCorrectResult()
        {
            var result = this.engine.Evaluate("(true && false) || (true && true)");
            Assert.AreEqual(true, result); // (false) || (true) = true
        }

        [Test]
        public void Evaluate_ComplexMixedExpression_ReturnsCorrectResult()
        {
            var result = this.engine.Evaluate("(5 > 3 && 2 < 4) || false");
            Assert.AreEqual(true, result); // (true && true) || false = true || false = true
        }

        // Boolean with Column References Tests
        [Test]
        public void Evaluate_BooleanColumnReference_ReturnsCorrectResult()
        {
            this.engine.SetColumnValueProvider("C0", () => true);
            this.engine.SetColumnValueProvider("C1", () => false);

            var result = this.engine.Evaluate("C0 && C1");
            Assert.AreEqual(false, result);
        }

        [Test]
        public void Evaluate_BooleanColumnWithLogicalOperators_ReturnsCorrectResult()
        {
            this.engine.SetColumnValueProvider("C0", () => true);
            this.engine.SetColumnValueProvider("C1", () => false);

            var result = this.engine.Evaluate("C0 || C1");
            Assert.AreEqual(true, result);
        }

        // New Function Tests - Rounding Functions
        [Test]
        public void Evaluate_RoundFunction_SingleArgument_RoundsToInteger()
        {
            var result = this.engine.Evaluate("ROUND(3.7)");
            Assert.AreEqual(4f, result);

            result = this.engine.Evaluate("ROUND(3.2)");
            Assert.AreEqual(3f, result);

            result = this.engine.Evaluate("ROUND(3.5)");
            Assert.AreEqual(4f, result); // Unity rounds .5 up
        }

        [Test]
        public void Evaluate_RoundFunction_TwoArguments_RoundsToDecimalPlaces()
        {
            var result = this.engine.Evaluate("ROUND(3.14159, 2)");
            Assert.AreEqual(3.14f, (float)result, 0.001f);

            result = this.engine.Evaluate("ROUND(3.14159, 0)");
            Assert.AreEqual(3f, result);

            result = this.engine.Evaluate("ROUND(123.456, 1)");
            Assert.AreEqual(123.5f, (float)result, 0.001f);
        }

        [Test]
        public void Evaluate_RoundFunction_NegativeDecimals_ThrowsException()
        {
            Assert.Throws<FormulaException>(() => this.engine.Evaluate("ROUND(3.14, -1)"));
        }

        [Test]
        public void Evaluate_RoundFunction_WrongArgumentCount_ThrowsException()
        {
            Assert.Throws<FormulaException>(() => this.engine.Evaluate("ROUND()"));
            Assert.Throws<FormulaException>(() => this.engine.Evaluate("ROUND(1, 2, 3)"));
        }

        [Test]
        public void Evaluate_CeilingFunction_ReturnsCorrectResult()
        {
            var result = this.engine.Evaluate("CEILING(3.1)");
            Assert.AreEqual(4f, result);

            result = this.engine.Evaluate("CEILING(3.0)");
            Assert.AreEqual(3f, result);

            result = this.engine.Evaluate("CEILING(-2.7)");
            Assert.AreEqual(-2f, result);

            // Test CEIL alias
            result = this.engine.Evaluate("CEIL(2.2)");
            Assert.AreEqual(3f, result);
        }

        [Test]
        public void Evaluate_CeilingFunction_WrongArgumentCount_ThrowsException()
        {
            Assert.Throws<FormulaException>(() => this.engine.Evaluate("CEILING()"));
            Assert.Throws<FormulaException>(() => this.engine.Evaluate("CEILING(1, 2)"));
        }

        [Test]
        public void Evaluate_FloorFunction_ReturnsCorrectResult()
        {
            var result = this.engine.Evaluate("FLOOR(3.9)");
            Assert.AreEqual(3f, result);

            result = this.engine.Evaluate("FLOOR(3.0)");
            Assert.AreEqual(3f, result);

            result = this.engine.Evaluate("FLOOR(-2.1)");
            Assert.AreEqual(-3f, result);
        }

        [Test]
        public void Evaluate_FloorFunction_WrongArgumentCount_ThrowsException()
        {
            Assert.Throws<FormulaException>(() => this.engine.Evaluate("FLOOR()"));
            Assert.Throws<FormulaException>(() => this.engine.Evaluate("FLOOR(1, 2)"));
        }

        // Unity Interpolation Function Tests
        [Test]
        public void Evaluate_LerpFunction_ReturnsCorrectResult()
        {
            var result = this.engine.Evaluate("LERP(0, 10, 0.5)");
            Assert.AreEqual(5f, result);

            result = this.engine.Evaluate("LERP(10, 20, 0)");
            Assert.AreEqual(10f, result);

            result = this.engine.Evaluate("LERP(10, 20, 1)");
            Assert.AreEqual(20f, result);

            result = this.engine.Evaluate("LERP(-5, 5, 0.75)");
            Assert.AreEqual(2.5f, result);
        }

        [Test]
        public void Evaluate_LerpFunction_WrongArgumentCount_ThrowsException()
        {
            Assert.Throws<FormulaException>(() => this.engine.Evaluate("LERP(1, 2)"));
            Assert.Throws<FormulaException>(() => this.engine.Evaluate("LERP(1, 2, 3, 4)"));
        }

        [Test]
        public void Evaluate_InverseLerpFunction_ReturnsCorrectResult()
        {
            var result = this.engine.Evaluate("INVERSELERP(0, 10, 5)");
            Assert.AreEqual(0.5f, result);

            result = this.engine.Evaluate("INVERSELERP(10, 20, 10)");
            Assert.AreEqual(0f, result);

            result = this.engine.Evaluate("INVERSELERP(10, 20, 20)");
            Assert.AreEqual(1f, result);

            result = this.engine.Evaluate("INVERSELERP(-5, 5, 2.5)");
            Assert.AreEqual(0.75f, result);
        }

        [Test]
        public void Evaluate_InverseLerpFunction_WrongArgumentCount_ThrowsException()
        {
            Assert.Throws<FormulaException>(() => this.engine.Evaluate("INVERSELERP(1, 2)"));
            Assert.Throws<FormulaException>(() => this.engine.Evaluate("INVERSELERP(1, 2, 3, 4)"));
        }

        [Test]
        public void Evaluate_SmoothStepFunction_ReturnsCorrectResult()
        {
            var result = this.engine.Evaluate("SMOOTHSTEP(0, 1, 0)");
            Assert.AreEqual(0f, result);

            result = this.engine.Evaluate("SMOOTHSTEP(0, 1, 1)");
            Assert.AreEqual(1f, result);

            result = this.engine.Evaluate("SMOOTHSTEP(0, 1, 0.5)");
            Assert.AreEqual(0.5f, (float)result, 0.001f);

            // Unity's SmoothStep expects t to be between 0 and 1
            // It interpolates smoothly between min and max based on t
            result = this.engine.Evaluate("SMOOTHSTEP(10, 20, 0.5)");
            Assert.AreEqual(15f, (float)result, 0.001f);
        }

        [Test]
        public void Evaluate_SmoothStepFunction_WrongArgumentCount_ThrowsException()
        {
            Assert.Throws<FormulaException>(() => this.engine.Evaluate("SMOOTHSTEP(1, 2)"));
            Assert.Throws<FormulaException>(() => this.engine.Evaluate("SMOOTHSTEP(1, 2, 3, 4)"));
        }

        // Combined Function Tests
        [Test]
        public void Evaluate_CombinedNewFunctions_ReturnsCorrectResult()
        {
            this.engine.SetColumnValueProvider("C0", () => 3.14159f);
            var result = this.engine.Evaluate("CEILING(ROUND(C0, 2))");
            Assert.AreEqual(4f, result); // ROUND(3.14159, 2) = 3.14, CEILING(3.14) = 4

            result = this.engine.Evaluate("FLOOR(LERP(1, 10, 0.7))");
            Assert.AreEqual(7f, result); // LERP(1, 10, 0.7) = 7.3, FLOOR(7.3) = 7
        }

        // MATHF prefix tests
        [Test]
        public void Evaluate_MathfPrefixFunctions_ReturnsCorrectResult()
        {
            var result = this.engine.Evaluate("MATHF.ROUND(3.7)");
            Assert.AreEqual(4f, result);

            result = this.engine.Evaluate("MATHF.CEILING(3.2)");
            Assert.AreEqual(4f, result);

            result = this.engine.Evaluate("MATHF.FLOOR(3.8)");
            Assert.AreEqual(3f, result);

            result = this.engine.Evaluate("MATHF.LERP(0, 10, 0.3)");
            Assert.AreEqual(3f, result);

            result = this.engine.Evaluate("MATHF.INVERSELERP(0, 10, 3)");
            Assert.AreEqual(0.3f, result);

            result = this.engine.Evaluate("MATHF.SMOOTHSTEP(0, 10, 0.5)");
            Assert.AreEqual(5f, (float)result, 0.001f);
        }
    }
}