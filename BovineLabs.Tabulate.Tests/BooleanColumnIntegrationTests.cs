// <copyright file="BooleanColumnIntegrationTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Tabulate.Tests
{
    using BovineLabs.Tabulate.Editor.Core;
    using BovineLabs.Tabulate.Editor.Formula;
    using BovineLabs.Tabulate.Editor.Handlers;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;

    public class BooleanColumnIntegrationTests
    {
        private ColumnFormulaProcessor processor = null!;
        private SheetDefinition sheetDefinition = null!;
        private GameObject testObject = null!;

        [SetUp]
        public void SetUp()
        {
            this.processor = new ColumnFormulaProcessor(column => new PropertyColumnHandler(column));
            this.sheetDefinition = ScriptableObject.CreateInstance<SheetDefinition>();
            this.testObject = new GameObject("TestObject");

            // Set up columns: C0 (Boolean true), C1 (Boolean false), C2 (Float 10), C3 (Float 20)
            this.sheetDefinition.Columns = new[]
            {
                new ColumnDefinition
                {
                    ColumnId = 0,
                    Type = ColumnType.Data,
                    DataFieldType = DataFieldType.Boolean,
                    UserDisplayName = "BooleanTrue",
                },
                new ColumnDefinition
                {
                    ColumnId = 1,
                    Type = ColumnType.Data,
                    DataFieldType = DataFieldType.Boolean,
                    UserDisplayName = "BooleanFalse",
                },
                new ColumnDefinition
                {
                    ColumnId = 2,
                    Type = ColumnType.Data,
                    DataFieldType = DataFieldType.Float,
                    UserDisplayName = "ValueA",
                },
                new ColumnDefinition
                {
                    ColumnId = 3,
                    Type = ColumnType.Data,
                    DataFieldType = DataFieldType.Float,
                    UserDisplayName = "ValueB",
                },
            };

            // Set up test data - use the actual object ID that will be generated
            this.sheetDefinition.SetDataValue(this.testObject, 0, "True");   // C0 = true
            this.sheetDefinition.SetDataValue(this.testObject, 1, "False");  // C1 = false
            this.sheetDefinition.SetDataValue(this.testObject, 2, "10");     // C2 = 10
            this.sheetDefinition.SetDataValue(this.testObject, 3, "20");     // C3 = 20
        }

        [TearDown]
        public void TearDown()
        {
            if (this.testObject != null)
            {
                Object.DestroyImmediate(this.testObject);
            }

            if (this.sheetDefinition != null)
            {
                Object.DestroyImmediate(this.sheetDefinition);
            }
        }

        [Test]
        public void BooleanColumnConditional_TrueCondition_ReturnsFirstValue()
        {
            // Test: C0 ? C2 : C3 where C0=true, C2=10, C3=20
            // Expected: 10 (first value because condition is true)
            var formula = "C0 ? C2 : C3";

            var result = this.processor.PreviewFormulaResults(// target column doesn't matter for preview
                formula,
                this.sheetDefinition,
                new Object[] { this.testObject });

            Assert.IsFalse(result.HasErrors, $"Formula should not have errors: {result.ErrorMessage}");
            Assert.AreEqual(1, result.PreviewValues.Count);
            Assert.AreEqual("10", result.PreviewValues[0]);
        }

        [Test]
        public void BooleanColumnConditional_FalseCondition_ReturnsSecondValue()
        {
            // Test: C1 ? C2 : C3 where C1=false, C2=10, C3=20
            // Expected: 20 (second value because condition is false)
            var formula = "C1 ? C2 : C3";

            var result = this.processor.PreviewFormulaResults(// target column doesn't matter for preview
                formula,
                this.sheetDefinition,
                new Object[] { this.testObject });

            Assert.IsFalse(result.HasErrors, $"Formula should not have errors: {result.ErrorMessage}");
            Assert.AreEqual(1, result.PreviewValues.Count);
            Assert.AreEqual("20", result.PreviewValues[0]);
        }

        [Test]
        public void BooleanColumnLogical_TrueAndFalse_ReturnsFalse()
        {
            // Test: C0 && C1 where C0=true, C1=false
            // Expected: false (true AND false = false)
            var formula = "C0 && C1";

            var result = this.processor.PreviewFormulaResults(// target column doesn't matter for preview
                formula,
                this.sheetDefinition,
                new Object[] { this.testObject });

            Assert.IsFalse(result.HasErrors, $"Formula should not have errors: {result.ErrorMessage}");
            Assert.AreEqual(1, result.PreviewValues.Count);
            Assert.AreEqual("false", result.PreviewValues[0]);
        }

        [Test]
        public void BooleanColumnLogical_TrueOrFalse_ReturnsTrue()
        {
            // Test: C0 || C1 where C0=true, C1=false
            // Expected: true (true OR false = true)
            var formula = "C0 || C1";

            var result = this.processor.PreviewFormulaResults(// target column doesn't matter for preview
                formula,
                this.sheetDefinition,
                new Object[] { this.testObject });

            Assert.IsFalse(result.HasErrors, $"Formula should not have errors: {result.ErrorMessage}");
            Assert.AreEqual(1, result.PreviewValues.Count);
            Assert.AreEqual("true", result.PreviewValues[0]);
        }

        [Test]
        public void BooleanColumnComparison_BooleanEquality_ReturnsCorrectResult()
        {
            // Test: C0 == C1 where C0=true, C1=false
            // Expected: false (true != false)
            var formula = "C0 == C1";

            var result = this.processor.PreviewFormulaResults(// target column doesn't matter for preview
                formula,
                this.sheetDefinition,
                new Object[] { this.testObject });

            Assert.IsFalse(result.HasErrors, $"Formula should not have errors: {result.ErrorMessage}");
            Assert.AreEqual(1, result.PreviewValues.Count);
            Assert.AreEqual("0", result.PreviewValues[0]); // Comparison returns 0 for false, 1 for true
        }

        [Test]
        public void BooleanColumnMixed_BooleanWithArithmetic_ReturnsCorrectResult()
        {
            // Test: C0 ? C2 + 5 : C3 * 2 where C0=true, C2=10, C3=20
            // Expected: 15 (10 + 5 because condition is true)
            var formula = "C0 ? C2 + 5 : C3 * 2";

            var result = this.processor.PreviewFormulaResults(// target column doesn't matter for preview
                formula,
                this.sheetDefinition,
                new Object[] { this.testObject });

            Assert.IsFalse(result.HasErrors, $"Formula should not have errors: {result.ErrorMessage}");
            Assert.AreEqual(1, result.PreviewValues.Count);
            Assert.AreEqual("15", result.PreviewValues[0]);
        }

        [Test]
        public void BooleanColumnCaseInsensitive_TrueVariations_ParseCorrectly()
        {
            // Test with "true" (lowercase)
            this.sheetDefinition.SetDataValue(this.testObject, 0, "true");
            var formula = "C0 ? 1 : 0";

            var result = this.processor.PreviewFormulaResults(formula,
                this.sheetDefinition,
                new Object[] { this.testObject });

            Assert.IsFalse(result.HasErrors, $"Formula should not have errors: {result.ErrorMessage}");
            Assert.AreEqual(1, result.PreviewValues.Count);
            Assert.AreEqual("1", result.PreviewValues[0]); // Should return 1 because "true" should parse as true
        }

        [Test]
        public void BooleanColumnInvalidValues_DefaultToFalse()
        {
            // Test with invalid boolean string
            this.sheetDefinition.SetDataValue(this.testObject, 0, "invalid_boolean");

            var formula = "C0 ? 1 : 0";

            var result = this.processor.PreviewFormulaResults(formula,
                this.sheetDefinition,
                new Object[] { this.testObject });

            Assert.IsFalse(result.HasErrors, $"Formula should not have errors: {result.ErrorMessage}");
            Assert.AreEqual(1, result.PreviewValues.Count);
            Assert.AreEqual("0", result.PreviewValues[0]); // Should return 0 because invalid boolean defaults to false
        }

        [Test]
        public void FormulaColumnHandler_BooleanColumnReference_WorksInConditional()
        {
            // Create boolean data column (C0)
            var booleanColumn = new ColumnDefinition
            {
                ColumnId = 0,
                Type = ColumnType.Data,
                DataFieldType = DataFieldType.Boolean,
                UserDisplayName = "BooleanCondition",
            };

            // Create data columns for values (C1, C2)
            var valueColumn1 = new ColumnDefinition
            {
                ColumnId = 1,
                Type = ColumnType.Data,
                DataFieldType = DataFieldType.Float,
                UserDisplayName = "Value1",
            };

            var valueColumn2 = new ColumnDefinition
            {
                ColumnId = 2,
                Type = ColumnType.Data,
                DataFieldType = DataFieldType.Float,
                UserDisplayName = "Value2",
            };

            // Create formula column that uses C0 ? C1 : C2
            var formulaColumn = new ColumnDefinition
            {
                ColumnId = 3,
                Type = ColumnType.Formula,
                Formula = "C0 ? C1 : C2", // Boolean column reference in conditional
                UserDisplayName = "ConditionalResult",
            };

            var allColumns = new[] { booleanColumn, valueColumn1, valueColumn2, formulaColumn };

            // Set up test data for true condition
            this.sheetDefinition.SetDataValue(this.testObject, 0, "True");  // C0 = true
            this.sheetDefinition.SetDataValue(this.testObject, 1, "10");    // C1 = 10
            this.sheetDefinition.SetDataValue(this.testObject, 2, "20");    // C2 = 20

            // Create formula handler and test
            var formulaHandler = new FormulaColumnHandler(formulaColumn, column => new PropertyColumnHandler(column));

            var result = formulaHandler.EvaluateFormula(
                this.testObject,
                allColumns,
                this.GetColumnValueWithDataSupport);

            Assert.AreEqual(10f, result); // Should return C1 (10) because C0 is true

            // Test false condition
            this.sheetDefinition.SetDataValue(this.testObject, 0, "False"); // C0 = false

            result = formulaHandler.EvaluateFormula(
                this.testObject,
                allColumns,
                this.GetColumnValueWithDataSupport);

            Assert.AreEqual(20f, result); // Should return C2 (20) because C0 is false
        }

        private object? GetColumnValueWithDataSupport(ColumnDefinition columnDef, Object targetObject)
        {
            switch (columnDef.Type)
            {
                case ColumnType.Data:
                    var dataHandler = new DataColumnHandler(columnDef);
                    var dataValue = dataHandler.GetValue(this.testObject, this.sheetDefinition);

                    // Use the same type-aware parsing as ColumnFormulaProcessor
                    return this.ParseStringValueWithType(dataValue, columnDef.DataFieldType);

                case ColumnType.Property:
                    var propertyHandler = new PropertyColumnHandler(columnDef);
                    return propertyHandler.GetValue(targetObject);

                default:
                    return null;
            }
        }

        private object ParseStringValueWithType(string? value, DataFieldType dataFieldType)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return dataFieldType switch
                {
                    DataFieldType.Integer => 0,
                    DataFieldType.Float => 0f,
                    DataFieldType.Boolean => false,
                    _ => 0f,
                };
            }

            // Parse based on the specified data field type
            switch (dataFieldType)
            {
                case DataFieldType.Boolean:
                    if (bool.TryParse(value, out var boolValue))
                    {
                        return boolValue;
                    }

                    // Fallback to false for invalid boolean strings
                    return false;

                case DataFieldType.Integer:
                    if (int.TryParse(value, out var intValue))
                    {
                        return intValue;
                    }

                    // Try float parsing as fallback for integers
                    if (float.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var floatAsInt))
                    {
                        return (int)floatAsInt;
                    }

                    return 0;

                case DataFieldType.Float:
                default:
                    // Try to parse as number first
                    if (float.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var floatValue))
                    {
                        return floatValue;
                    }

                    // Try to parse as boolean as fallback
                    if (bool.TryParse(value, out var boolFallback))
                    {
                        return boolFallback;
                    }

                    // Return as string for unrecognized values
                    return value ?? string.Empty;
            }
        }
    }
}