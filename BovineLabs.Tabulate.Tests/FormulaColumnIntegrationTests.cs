// <copyright file="FormulaColumnIntegrationTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Tabulate.Tests
{
    using BovineLabs.Tabulate.Editor.Core;
    using BovineLabs.Tabulate.Editor.Handlers;
    using NUnit.Framework;
    using UnityEngine;
    using UnityEngine.Serialization;

    public class FormulaColumnIntegrationTests
    {
        private FormulaColumnHandler columnHandler = null!;
        private ColumnDefinition formulaColumn = null!;
        private ColumnDefinition radiusColumn = null!;
        private ColumnDefinition heightColumn = null!;
        private TestCapsule testObject = null!;

        [SetUp]
        public void SetUp()
        {
            // Create test object
            this.testObject = ScriptableObject.CreateInstance<TestCapsule>();
            this.testObject.Radius = 2f;
            this.testObject.Height = 10f;

            // Create column definitions with column IDs
            this.radiusColumn = new ColumnDefinition
            {
                ColumnId = 0,
                UserDisplayName = "Radius",
                Type = ColumnType.Property,
                PropertyPath = "Radius",
            };

            this.heightColumn = new ColumnDefinition
            {
                ColumnId = 1,
                UserDisplayName = "Height",
                Type = ColumnType.Property,
                PropertyPath = "Height",
            };

            this.formulaColumn = new ColumnDefinition
            {
                ColumnId = 2,
                UserDisplayName = "Diameter",
                Type = ColumnType.Formula,
                Formula = "C0 * 2", // Use column ID reference (C0 = radius)
                PropertyPath = "Diameter",
                TargetTypeName = typeof(TestCapsule).AssemblyQualifiedName!,
            };

            this.columnHandler = new FormulaColumnHandler(this.formulaColumn, column => new PropertyColumnHandler(column));
        }

        [TearDown]
        public void TearDown()
        {
            if (this.testObject != null)
            {
                Object.DestroyImmediate(this.testObject);
            }
        }

        [Test]
        public void EvaluateFormula_SimpleFormula_ReturnsCorrectResult()
        {
            var allColumns = new[] { this.radiusColumn, this.formulaColumn };

            var result = this.columnHandler.EvaluateFormula(this.testObject, allColumns, this.GetColumnValue);

            Assert.AreEqual(4f, result); // radius (2) * 2 = 4
        }

        [Test]
        public void EvaluateFormula_MultipleReferences_ReturnsCorrectResult()
        {
            this.formulaColumn.Formula = "C0 + C1"; // C0 = radius, C1 = height

            var allColumns = new[] { this.radiusColumn, this.heightColumn, this.formulaColumn };

            var result = this.columnHandler.EvaluateFormula(this.testObject, allColumns, this.GetColumnValue);

            Assert.AreEqual(12f, result); // radius (2) + height (10) = 12
        }

        [Test]
        public void EvaluateFormula_ComplexExpression_ReturnsCorrectResult()
        {
            this.formulaColumn.Formula = "(C0 + C1) / 2"; // C0 = radius, C1 = height

            var allColumns = new[] { this.radiusColumn, this.heightColumn, this.formulaColumn };

            var result = this.columnHandler.EvaluateFormula(this.testObject, allColumns, this.GetColumnValue);

            Assert.AreEqual(6f, result); // (radius (2) + height (10)) / 2 = 6
        }

        [Test]
        public void WriteFormulaResult_WithPropertyPath_WritesToProperty()
        {
            var result = 5f;
            var success = this.columnHandler.WriteFormulaResult(this.testObject, result);

            Assert.IsTrue(success);
            Assert.AreEqual(5f, this.testObject.Diameter);
        }

        [Test]
        public void WriteFormulaResult_WithoutPropertyPath_ReturnsFalse()
        {
            this.formulaColumn.PropertyPath = string.Empty;
            var handler = new FormulaColumnHandler(this.formulaColumn, column => new PropertyColumnHandler(column));

            var result = 5f;
            var success = handler.WriteFormulaResult(this.testObject, result);

            Assert.IsFalse(success);
        }

        [Test]
        public void WriteFormulaResult_WithoutTargetTypeName_ReturnsFalse()
        {
            this.formulaColumn.TargetTypeName = string.Empty;
            var handler = new FormulaColumnHandler(this.formulaColumn, column => new PropertyColumnHandler(column));

            var result = 5f;
            var success = handler.WriteFormulaResult(this.testObject, result);

            Assert.IsFalse(success);
        }

        [Test]
        public void EvaluateFormula_DisplayOnlyFormula_StillEvaluates()
        {
            // Formula with no target type (display-only)
            var displayFormula = new ColumnDefinition
            {
                ColumnId = 3,
                UserDisplayName = "display",
                Type = ColumnType.Formula,
                Formula = "C0 * 3", // C0 = radius
                PropertyPath = string.Empty,
                TargetTypeName = string.Empty,
            };

            var handler = new FormulaColumnHandler(displayFormula, column => new PropertyColumnHandler(column));
            var allColumns = new[] { this.radiusColumn, displayFormula };

            var result = handler.EvaluateFormula(this.testObject, allColumns, this.GetColumnValue);

            Assert.AreEqual(6f, result); // radius (2) * 3 = 6

            // Should not write to any property
            var writeSuccess = handler.WriteFormulaResult(this.testObject, result);
            Assert.IsFalse(writeSuccess);
        }

        [Test]
        public void GetColumnDependencies_SimpleFormula_ReturnsCorrectReferences()
        {
            var dependencies = this.columnHandler.GetColumnDependencies();

            Assert.AreEqual(1, dependencies.Count);
            Assert.IsTrue(dependencies.Contains("C0")); // C0 = radius
        }

        [Test]
        public void GetColumnDependencies_MultipleReferences_ReturnsAllReferences()
        {
            this.formulaColumn.Formula = "C0 + C1 - C2"; // C0 = radius, C1 = height, C2 = diameter
            var handler = new FormulaColumnHandler(this.formulaColumn, column => new PropertyColumnHandler(column));

            var dependencies = handler.GetColumnDependencies();

            Assert.AreEqual(3, dependencies.Count);
            Assert.IsTrue(dependencies.Contains("C0")); // C0 = radius
            Assert.IsTrue(dependencies.Contains("C1")); // C1 = height
            Assert.IsTrue(dependencies.Contains("C2")); // C2 = diameter
        }

        [Test]
        public void EvaluateFormula_InvalidFormula_ReturnsNull()
        {
            this.formulaColumn.Formula = "C99 + 1"; // C99 doesn't exist
            var handler = new FormulaColumnHandler(this.formulaColumn, column => new PropertyColumnHandler(column));

            var allColumns = new[] { this.radiusColumn, this.formulaColumn };

            var result = handler.EvaluateFormula(this.testObject, allColumns, this.GetColumnValue);

            Assert.IsNull(result);
        }

        [Test]
        public void EvaluateFormula_EmptyFormula_ReturnsNull()
        {
            this.formulaColumn.Formula = string.Empty;
            var handler = new FormulaColumnHandler(this.formulaColumn, column => new PropertyColumnHandler(column));

            var allColumns = new[] { this.radiusColumn, this.formulaColumn };

            var result = handler.EvaluateFormula(this.testObject, allColumns, this.GetColumnValue);

            Assert.IsNull(result);
        }

        private object? GetColumnValue(ColumnDefinition columnDef, Object targetObject)
        {
            if (columnDef.Type == ColumnType.Property)
            {
                var propertyHandler = new PropertyColumnHandler(columnDef);
                return propertyHandler.GetValue(targetObject);
            }

            return null;
        }

        // Boolean Integration Tests
        [Test]
        public void EvaluateFormula_BooleanLiteralInFormula_ReturnsCorrectBoolean()
        {
            // Create a boolean formula column targeting the isValid property
            var booleanColumn = new ColumnDefinition
            {
                ColumnId = 3,
                UserDisplayName = "IsValid",
                Type = ColumnType.Formula,
                Formula = "true",
                PropertyPath = "IsValid",
                TargetTypeName = typeof(TestCapsule).AssemblyQualifiedName!,
            };
            var handler = new FormulaColumnHandler(booleanColumn, column => new PropertyColumnHandler(column));

            var allColumns = new[] { this.radiusColumn, booleanColumn };

            var result = handler.EvaluateFormula(this.testObject, allColumns, this.GetColumnValue);

            Assert.AreEqual(true, result);
        }

        [Test]
        public void EvaluateFormula_BooleanLogicalExpression_ReturnsCorrectResult()
        {
            // Create a boolean formula column targeting the isValid property
            var booleanColumn = new ColumnDefinition
            {
                ColumnId = 3,
                UserDisplayName = "IsValid",
                Type = ColumnType.Formula,
                Formula = "C0 > 1 && C1 < 20", // radius > 1 && height < 20
                PropertyPath = "IsValid",
                TargetTypeName = typeof(TestCapsule).AssemblyQualifiedName!,
            };
            var handler = new FormulaColumnHandler(booleanColumn, column => new PropertyColumnHandler(column));

            var allColumns = new[] { this.radiusColumn, this.heightColumn, booleanColumn };

            var result = handler.EvaluateFormula(this.testObject, allColumns, this.GetColumnValue);

            Assert.AreEqual(true, result); // 2 > 1 && 10 < 20 = true && true = true
        }

        [Test]
        public void EvaluateFormula_BooleanLogicalOr_ReturnsCorrectResult()
        {
            // Create a boolean formula column targeting the isValid property
            var booleanColumn = new ColumnDefinition
            {
                ColumnId = 3,
                UserDisplayName = "IsValid",
                Type = ColumnType.Formula,
                Formula = "C0 < 1 || C1 > 5", // radius < 1 || height > 5
                PropertyPath = "IsValid",
                TargetTypeName = typeof(TestCapsule).AssemblyQualifiedName!,
            };
            var handler = new FormulaColumnHandler(booleanColumn, column => new PropertyColumnHandler(column));

            var allColumns = new[] { this.radiusColumn, this.heightColumn, booleanColumn };

            var result = handler.EvaluateFormula(this.testObject, allColumns, this.GetColumnValue);

            Assert.AreEqual(true, result); // 2 < 1 || 10 > 5 = false || true = true
        }

        [Test]
        public void EvaluateFormula_BooleanInTernary_ReturnsCorrectValue()
        {
            // Create a boolean formula column targeting the isValid property
            var booleanColumn = new ColumnDefinition
            {
                ColumnId = 3,
                UserDisplayName = "IsValid",
                Type = ColumnType.Formula,
                Formula = "C0 > 1 ? true : false", // radius > 1 ? true : false
                PropertyPath = "IsValid",
                TargetTypeName = typeof(TestCapsule).AssemblyQualifiedName!,
            };
            var handler = new FormulaColumnHandler(booleanColumn, column => new PropertyColumnHandler(column));

            var allColumns = new[] { this.radiusColumn, booleanColumn };

            var result = handler.EvaluateFormula(this.testObject, allColumns, this.GetColumnValue);

            Assert.AreEqual(true, result);
        }

        [Test]
        public void EvaluateFormula_BooleanMixedWithArithmetic_ReturnsCorrectResult()
        {
            this.formulaColumn.Formula = "(C0 > 1) + (C1 < 20)"; // (radius > 1) + (height < 20)
            var handler = new FormulaColumnHandler(this.formulaColumn, column => new PropertyColumnHandler(column));

            var allColumns = new[] { this.radiusColumn, this.heightColumn, this.formulaColumn };

            var result = handler.EvaluateFormula(this.testObject, allColumns, this.GetColumnValue);

            Assert.AreEqual(2f, result); // true + true = 1 + 1 = 2
        }

        [System.Serializable]
        private class TestCapsule : ScriptableObject
        {
            public float Radius;
            public float Height;
            public float Diameter;
            public bool IsValid;
        }
    }
}