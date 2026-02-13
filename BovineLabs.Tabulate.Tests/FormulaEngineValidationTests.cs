// <copyright file="FormulaEngineValidationTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Tabulate.Tests
{
    using BovineLabs.Tabulate.Editor.Formula;
    using NUnit.Framework;

    public class FormulaEngineValidationTests
    {
        private FormulaEngine engine = null!;

        [SetUp]
        public void SetUp()
        {
            this.engine = new FormulaEngine();
        }

        [Test]
        public void ValidateSyntax_ValidFormula_ReturnsSuccess()
        {
            var result = this.engine.ValidateSyntax("2 + 3 * 4");
            Assert.IsTrue(result.IsValid);
        }

        [Test]
        public void ValidateSyntax_ValidFormulaWithParentheses_ReturnsSuccess()
        {
            var result = this.engine.ValidateSyntax("(2 + 3) * 4");
            Assert.IsTrue(result.IsValid);
        }

        [Test]
        public void ValidateSyntax_ValidFormulaWithIdentifiers_ReturnsSuccess()
        {
            var result = this.engine.ValidateSyntax("radius * 2 + height");
            Assert.IsTrue(result.IsValid);
        }

        [Test]
        public void ValidateSyntax_EmptyFormula_ReturnsSuccess()
        {
            var result = this.engine.ValidateSyntax(string.Empty);
            Assert.IsTrue(result.IsValid);
        }

        [Test]
        public void ValidateSyntax_WhitespaceOnlyFormula_ReturnsSuccess()
        {
            var result = this.engine.ValidateSyntax("   ");
            Assert.IsTrue(result.IsValid);
        }

        [Test]
        public void ValidateSyntax_OperatorAtBeginning_ReturnsFailure()
        {
            var result = this.engine.ValidateSyntax("+ 2 + 3");
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.ErrorMessage.Contains("Invalid operator position"));
        }

        [Test]
        public void ValidateSyntax_OperatorAtEnd_ReturnsFailure()
        {
            var result = this.engine.ValidateSyntax("2 + 3 +");
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.ErrorMessage.Contains("Invalid operator position"));
        }

        [Test]
        public void ValidateSyntax_ConsecutiveOperators_ReturnsFailure()
        {
            var result = this.engine.ValidateSyntax("2 + * 3");
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.ErrorMessage.Contains("Consecutive operators"));
        }

        [Test]
        public void ValidateSyntax_OperatorBeforeClosingParen_ReturnsFailure()
        {
            var result = this.engine.ValidateSyntax("(2 + 3 *)");
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.ErrorMessage.Contains("Invalid operator placement"));
        }

        [Test]
        public void ValidateSyntax_MissingOperatorBetweenNumbers_ReturnsFailure()
        {
            var result = this.engine.ValidateSyntax("2 3");
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.ErrorMessage.Contains("Missing operator"));
        }

        [Test]
        public void ValidateSyntax_MissingOperatorBetweenIdentifiers_ReturnsFailure()
        {
            var result = this.engine.ValidateSyntax("radius height");
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.ErrorMessage.Contains("Missing operator"));
        }

        [Test]
        public void ValidateSyntax_MissingOperatorBeforeParen_ReturnsFailure()
        {
            var result = this.engine.ValidateSyntax("2 (3 + 4)");
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.ErrorMessage.Contains("Missing operator before parenthesis"));
        }

        [Test]
        public void ValidateSyntax_EmptyParentheses_ReturnsFailure()
        {
            var result = this.engine.ValidateSyntax("2 + ()");
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.ErrorMessage.Contains("Empty parentheses"));
        }

        [Test]
        public void ValidateSyntax_UnmatchedOpeningParen_ReturnsFailure()
        {
            var result = this.engine.ValidateSyntax("(2 + 3");
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.ErrorMessage.Contains("Unclosed opening parenthesis"));
        }

        [Test]
        public void ValidateSyntax_UnmatchedClosingParen_ReturnsFailure()
        {
            var result = this.engine.ValidateSyntax("2 + 3)");
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.ErrorMessage.Contains("Extra closing parenthesis"));
        }

        [Test]
        public void ValidateSyntax_UnaryMinusAfterOpeningParen_ReturnsSuccess()
        {
            // Unary minus is now supported after opening parenthesis
            var result = this.engine.ValidateSyntax("(-2 + 3)");
            Assert.IsTrue(result.IsValid); // Should pass because unary minus is supported
        }

        [Test]
        public void ValidateSyntax_UnaryPlusOperator_ReturnsFailure()
        {
            // Unary plus is also not supported
            var result = this.engine.ValidateSyntax("(+5 * 2)");
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.ErrorMessage.Contains("Invalid operator after parenthesis"));
        }

        [Test]
        public void ValidateSyntax_ComplexValidExpression_ReturnsSuccess()
        {
            var result = this.engine.ValidateSyntax("((radius + height) * 2) / (width + depth)");
            Assert.IsTrue(result.IsValid);
        }

        [Test]
        public void ValidateSyntax_InvalidCharacter_ReturnsFailure()
        {
            var result = this.engine.ValidateSyntax("2 + 3 #");
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.ErrorMessage.Contains("Invalid formula syntax"));
        }

        [Test]
        public void Evaluate_WithSyntaxValidation_ValidFormula_ReturnsResult()
        {
            var result = this.engine.Evaluate("2 + 3");
            Assert.AreEqual(5f, result);
        }

        [Test]
        public void Evaluate_WithSyntaxValidation_InvalidFormula_ThrowsException()
        {
            Assert.Throws<FormulaException>(() => this.engine.Evaluate("2 + + 3"));
        }

        [Test]
        public void Evaluate_InvalidSyntax_IncludesOriginalFormula()
        {
            var ex = Assert.Throws<FormulaException>(() => this.engine.Evaluate("2 + + 3"));
            Assert.IsTrue(ex.Message.Contains("2 + + 3"));
        }

        [Test]
        public void ValidateSyntax_NestedParentheses_ReturnsSuccess()
        {
            var result = this.engine.ValidateSyntax("((2 + 3) * (4 + 5))");
            Assert.IsTrue(result.IsValid);
        }

        [Test]
        public void ValidateSyntax_MultipleUnmatchedParens_ReturnsFailure()
        {
            var result = this.engine.ValidateSyntax("((2 + 3)");
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.ErrorMessage.Contains("Unclosed opening parenthesis"));
        }
    }
}