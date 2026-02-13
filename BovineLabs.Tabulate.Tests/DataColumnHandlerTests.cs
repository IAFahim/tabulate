// <copyright file="DataColumnHandlerTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Tabulate.Tests
{
    using BovineLabs.Tabulate.Editor.Core;
    using BovineLabs.Tabulate.Editor.Handlers;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;

    [TestFixture]
    public class DataColumnHandlerTests
    {
        private DataColumnHandler handler = null!;
        private ColumnDefinition dataColumn = null!;
        private SheetDefinition sheetDefinition = null!;
        private TestObject testObject = null!;

        [SetUp]
        public void SetUp()
        {
            // Create test objects
            this.testObject = ScriptableObject.CreateInstance<TestObject>();
            this.sheetDefinition = ScriptableObject.CreateInstance<SheetDefinition>();

            // Create pure data column definition
            this.dataColumn = new ColumnDefinition
            {
                UserDisplayName = "Notes",
                Type = ColumnType.Data,
            };

            this.handler = new DataColumnHandler(this.dataColumn);
        }

        [TearDown]
        public void TearDown()
        {
            if (this.testObject)
            {
                Object.DestroyImmediate(this.testObject);
            }

            if (this.sheetDefinition)
            {
                Object.DestroyImmediate(this.sheetDefinition);
            }
        }

        [Test]
        public void GetValue_WithNoStoredData_ReturnsNull()
        {
            var result = this.handler.GetValue(this.testObject, this.sheetDefinition);

            Assert.IsNull(result);
        }

        [Test]
        public void SetValue_StoresDataInSheetDefinition()
        {
            var testValue = "Test note content";

            var success = this.handler.SetValue(this.testObject, this.sheetDefinition, testValue);

            Assert.IsTrue(success);

            // Verify value is stored
            var retrievedValue = this.handler.GetValue(this.testObject, this.sheetDefinition);
            Assert.AreEqual(testValue, retrievedValue);
        }

        [Test]
        public void SetValue_OverwritesExistingData()
        {
            var firstValue = "First note";
            var secondValue = "Second note";

            // Set first value
            this.handler.SetValue(this.testObject, this.sheetDefinition, firstValue);

            // Set second value (should overwrite)
            var success = this.handler.SetValue(this.testObject, this.sheetDefinition, secondValue);

            Assert.IsTrue(success);

            // Verify second value is stored
            var retrievedValue = this.handler.GetValue(this.testObject, this.sheetDefinition);
            Assert.AreEqual(secondValue, retrievedValue);
        }

        [Test]
        public void SetValue_WithNullValue_RemovesEntry()
        {
            var testValue = "Test note";

            // Set initial value
            this.handler.SetValue(this.testObject, this.sheetDefinition, testValue);

            // Set to null (should remove)
            var success = this.handler.SetValue(this.testObject, this.sheetDefinition, null);

            Assert.IsTrue(success);

            // Verify value is removed
            var retrievedValue = this.handler.GetValue(this.testObject, this.sheetDefinition);
            Assert.IsNull(retrievedValue);
        }

        [Test]
        public void SetValue_WithEmptyString_RemovesEntry()
        {
            var testValue = "Test note";

            // Set initial value
            this.handler.SetValue(this.testObject, this.sheetDefinition, testValue);

            // Set to empty string (should remove)
            var success = this.handler.SetValue(this.testObject, this.sheetDefinition, string.Empty);

            Assert.IsTrue(success);

            // Verify value is removed
            var retrievedValue = this.handler.GetValue(this.testObject, this.sheetDefinition);
            Assert.IsNull(retrievedValue);
        }

        [Test]
        public void GetValue_WithNullSheetDefinition_ReturnsNull()
        {
            var result = this.handler.GetValue(this.testObject, null);

            Assert.IsNull(result);
        }

        [Test]
        public void SetValue_WithNullSheetDefinition_ReturnsFalse()
        {
            var success = this.handler.SetValue(this.testObject, null, "test");

            Assert.IsFalse(success);
        }

        [Test]
        public void DifferentObjects_HaveSeparateData()
        {
            var secondObject = ScriptableObject.CreateInstance<TestObject>();

            try
            {
                var firstValue = "First object note";
                var secondValue = "Second object note";

                // Set values for different objects
                this.handler.SetValue(this.testObject, this.sheetDefinition, firstValue);
                this.handler.SetValue(secondObject, this.sheetDefinition, secondValue);

                // Verify each object has its own data
                var firstRetrieved = this.handler.GetValue(this.testObject, this.sheetDefinition);
                var secondRetrieved = this.handler.GetValue(secondObject, this.sheetDefinition);

                Assert.AreEqual(firstValue, firstRetrieved);
                Assert.AreEqual(secondValue, secondRetrieved);
            }
            finally
            {
                Object.DestroyImmediate(secondObject);
            }
        }

        // Boolean Data Type Specific Tests
        [Test]
        public void SetValue_BooleanTrueString_StoresCorrectly()
        {
            this.dataColumn.DataFieldType = DataFieldType.Boolean;

            var success = this.handler.SetValue(this.testObject, this.sheetDefinition, "true");

            Assert.IsTrue(success);
            var retrievedValue = this.handler.GetValue(this.testObject, this.sheetDefinition);
            Assert.AreEqual("true", retrievedValue);
        }

        [Test]
        public void SetValue_BooleanFalseString_StoresCorrectly()
        {
            this.dataColumn.DataFieldType = DataFieldType.Boolean;

            var success = this.handler.SetValue(this.testObject, this.sheetDefinition, "false");

            Assert.IsTrue(success);
            var retrievedValue = this.handler.GetValue(this.testObject, this.sheetDefinition);
            Assert.AreEqual("false", retrievedValue);
        }

        [Test]
        public void SetValue_BooleanTrueValue_StoresAsString()
        {
            this.dataColumn.DataFieldType = DataFieldType.Boolean;

            var success = this.handler.SetValue(this.testObject, this.sheetDefinition, "True");

            Assert.IsTrue(success);
            var retrievedValue = this.handler.GetValue(this.testObject, this.sheetDefinition);
            Assert.AreEqual("True", retrievedValue);
        }

        [Test]
        public void SetValue_BooleanFalseValue_StoresAsString()
        {
            this.dataColumn.DataFieldType = DataFieldType.Boolean;

            var success = this.handler.SetValue(this.testObject, this.sheetDefinition, "False");

            Assert.IsTrue(success);
            var retrievedValue = this.handler.GetValue(this.testObject, this.sheetDefinition);
            Assert.AreEqual("False", retrievedValue);
        }

        // Integer and Float Data Type Tests (regression tests)
        [Test]
        public void SetValue_IntegerString_StoresCorrectly()
        {
            this.dataColumn.DataFieldType = DataFieldType.Integer;

            var success = this.handler.SetValue(this.testObject, this.sheetDefinition, "42");

            Assert.IsTrue(success);
            var retrievedValue = this.handler.GetValue(this.testObject, this.sheetDefinition);
            Assert.AreEqual("42", retrievedValue);
        }

        [Test]
        public void SetValue_FloatString_StoresCorrectly()
        {
            this.dataColumn.DataFieldType = DataFieldType.Float;

            var success = this.handler.SetValue(this.testObject, this.sheetDefinition, "3.14");

            Assert.IsTrue(success);
            var retrievedValue = this.handler.GetValue(this.testObject, this.sheetDefinition);
            Assert.AreEqual("3.14", retrievedValue);
        }

        // Comprehensive Data Field Type Tests - String Data (stored as raw values)
        [Test]
        public void SetValue_NoDataFieldType_StoresStringCorrectly()
        {
            // When no DataFieldType is specified, data columns store strings as-is

            var testString = "Test string with spaces and 123 numbers!";
            var success = this.handler.SetValue(this.testObject, this.sheetDefinition, testString);

            Assert.IsTrue(success);
            var retrievedValue = this.handler.GetValue(this.testObject, this.sheetDefinition);
            Assert.AreEqual(testString, retrievedValue);
        }

        [Test]
        public void SetValue_NoDataFieldType_EmptyString_NormalizesToNull()
        {
            var success = this.handler.SetValue(this.testObject, this.sheetDefinition, string.Empty);

            Assert.IsTrue(success);
            var retrievedValue = this.handler.GetValue(this.testObject, this.sheetDefinition);
            Assert.IsNull(retrievedValue); // SheetDefinition normalizes empty strings to null
        }

        [Test]
        public void SetValue_NoDataFieldType_SpecialCharacters_StoresCorrectly()
        {
            var specialString = "Special chars: !@#$%^&*()_+-=[]{}|;:,.<>?/~`";
            var success = this.handler.SetValue(this.testObject, this.sheetDefinition, specialString);

            Assert.IsTrue(success);
            var retrievedValue = this.handler.GetValue(this.testObject, this.sheetDefinition);
            Assert.AreEqual(specialString, retrievedValue);
        }

        [Test]
        public void SetValue_NoDataFieldType_UnicodeCharacters_StoresCorrectly()
        {
            var unicodeString = "Unicode: 你好 🌟 Ωα β γ";
            var success = this.handler.SetValue(this.testObject, this.sheetDefinition, unicodeString);

            Assert.IsTrue(success);
            var retrievedValue = this.handler.GetValue(this.testObject, this.sheetDefinition);
            Assert.AreEqual(unicodeString, retrievedValue);
        }

        // Enhanced Boolean Tests
        [Test]
        public void SetValue_BooleanDataType_CaseInsensitive_True()
        {
            this.dataColumn.DataFieldType = DataFieldType.Boolean;

            var testValues = new[] { "TRUE", "True", "true", "tRuE" };

            foreach (var value in testValues)
            {
                var success = this.handler.SetValue(this.testObject, this.sheetDefinition, value);
                Assert.IsTrue(success, $"Failed to set boolean value: {value}");

                var retrievedValue = this.handler.GetValue(this.testObject, this.sheetDefinition);
                Assert.AreEqual(value, retrievedValue, $"Retrieved value doesn't match set value: {value}");
            }
        }

        [Test]
        public void SetValue_BooleanDataType_CaseInsensitive_False()
        {
            this.dataColumn.DataFieldType = DataFieldType.Boolean;

            var testValues = new[] { "FALSE", "False", "false", "fAlSe" };

            foreach (var value in testValues)
            {
                var success = this.handler.SetValue(this.testObject, this.sheetDefinition, value);
                Assert.IsTrue(success, $"Failed to set boolean value: {value}");

                var retrievedValue = this.handler.GetValue(this.testObject, this.sheetDefinition);
                Assert.AreEqual(value, retrievedValue, $"Retrieved value doesn't match set value: {value}");
            }
        }

        [Test]
        public void SetValue_BooleanDataType_InvalidValues_StillStores()
        {
            this.dataColumn.DataFieldType = DataFieldType.Boolean;

            var invalidValues = new[] { "yes", "no", "1", "0", "maybe", "invalid" };

            foreach (var value in invalidValues)
            {
                var success = this.handler.SetValue(this.testObject, this.sheetDefinition, value);
                Assert.IsTrue(success, $"Failed to set value (data columns store as-is): {value}");

                var retrievedValue = this.handler.GetValue(this.testObject, this.sheetDefinition);
                Assert.AreEqual(value, retrievedValue, $"Retrieved value doesn't match set value: {value}");
            }
        }

        // Enhanced Integer Tests
        [Test]
        public void SetValue_IntegerDataType_PositiveNumbers()
        {
            this.dataColumn.DataFieldType = DataFieldType.Integer;

            var testValues = new[] { "0", "1", "42", "999", "2147483647" }; // int.MaxValue

            foreach (var value in testValues)
            {
                var success = this.handler.SetValue(this.testObject, this.sheetDefinition, value);
                Assert.IsTrue(success, $"Failed to set integer value: {value}");

                var retrievedValue = this.handler.GetValue(this.testObject, this.sheetDefinition);
                Assert.AreEqual(value, retrievedValue, $"Retrieved value doesn't match set value: {value}");
            }
        }

        [Test]
        public void SetValue_IntegerDataType_NegativeNumbers()
        {
            this.dataColumn.DataFieldType = DataFieldType.Integer;

            var testValues = new[] { "-1", "-42", "-999", "-2147483648" }; // int.MinValue

            foreach (var value in testValues)
            {
                var success = this.handler.SetValue(this.testObject, this.sheetDefinition, value);
                Assert.IsTrue(success, $"Failed to set integer value: {value}");

                var retrievedValue = this.handler.GetValue(this.testObject, this.sheetDefinition);
                Assert.AreEqual(value, retrievedValue, $"Retrieved value doesn't match set value: {value}");
            }
        }

        [Test]
        public void SetValue_IntegerDataType_InvalidNumbers_StillStores()
        {
            this.dataColumn.DataFieldType = DataFieldType.Integer;

            var invalidValues = new[] { "not_a_number", "3.14", "1.0", "abc123" };

            foreach (var value in invalidValues)
            {
                var success = this.handler.SetValue(this.testObject, this.sheetDefinition, value);
                Assert.IsTrue(success, $"Failed to set value (data columns store as-is): {value}");

                var retrievedValue = this.handler.GetValue(this.testObject, this.sheetDefinition);
                Assert.AreEqual(value, retrievedValue, $"Retrieved value doesn't match set value: {value}");
            }

            // Test empty string separately since it gets normalized to null
            var emptySuccess = this.handler.SetValue(this.testObject, this.sheetDefinition, string.Empty);
            Assert.IsTrue(emptySuccess, "Failed to set empty string value");

            var emptyRetrievedValue = this.handler.GetValue(this.testObject, this.sheetDefinition);
            Assert.IsNull(emptyRetrievedValue, "Empty string should be normalized to null");
        }

        // Enhanced Float Tests
        [Test]
        public void SetValue_FloatDataType_VariousFormats()
        {
            this.dataColumn.DataFieldType = DataFieldType.Float;

            var testValues = new[] { "0", "0.0", "3.14", "-2.5", "1.0", "99.999", "0.001" };

            foreach (var value in testValues)
            {
                var success = this.handler.SetValue(this.testObject, this.sheetDefinition, value);
                Assert.IsTrue(success, $"Failed to set float value: {value}");

                var retrievedValue = this.handler.GetValue(this.testObject, this.sheetDefinition);
                Assert.AreEqual(value, retrievedValue, $"Retrieved value doesn't match set value: {value}");
            }
        }

        [Test]
        public void SetValue_FloatDataType_ScientificNotation()
        {
            this.dataColumn.DataFieldType = DataFieldType.Float;

            var testValues = new[] { "1e5", "1E5", "1.23e-4", "1.23E+4", "-2.5e10" };

            foreach (var value in testValues)
            {
                var success = this.handler.SetValue(this.testObject, this.sheetDefinition, value);
                Assert.IsTrue(success, $"Failed to set float value: {value}");

                var retrievedValue = this.handler.GetValue(this.testObject, this.sheetDefinition);
                Assert.AreEqual(value, retrievedValue, $"Retrieved value doesn't match set value: {value}");
            }
        }

        [Test]
        public void SetValue_FloatDataType_SpecialValues()
        {
            this.dataColumn.DataFieldType = DataFieldType.Float;

            var testValues = new[] { "Infinity", "-Infinity", "NaN" };

            foreach (var value in testValues)
            {
                var success = this.handler.SetValue(this.testObject, this.sheetDefinition, value);
                Assert.IsTrue(success, $"Failed to set float value: {value}");

                var retrievedValue = this.handler.GetValue(this.testObject, this.sheetDefinition);
                Assert.AreEqual(value, retrievedValue, $"Retrieved value doesn't match set value: {value}");
            }
        }

        // Data Persistence and Concurrency Tests
        [Test]
        public void MultipleDataColumns_SameObject_IndependentStorage()
        {
            var column1 = new ColumnDefinition
            {
                ColumnId = 1,
                UserDisplayName = "Column1",
                Type = ColumnType.Data,
                DataFieldType = DataFieldType.Boolean,
            };

            var column2 = new ColumnDefinition
            {
                ColumnId = 2,
                UserDisplayName = "Column2",
                Type = ColumnType.Data,
                DataFieldType = DataFieldType.Integer,
            };

            var handler1 = new DataColumnHandler(column1);
            var handler2 = new DataColumnHandler(column2);

            // Set values in both columns
            var success1 = handler1.SetValue(this.testObject, this.sheetDefinition, "true");
            var success2 = handler2.SetValue(this.testObject, this.sheetDefinition, "42");

            Assert.IsTrue(success1);
            Assert.IsTrue(success2);

            // Verify values are independent
            var value1 = handler1.GetValue(this.testObject, this.sheetDefinition);
            var value2 = handler2.GetValue(this.testObject, this.sheetDefinition);

            Assert.AreEqual("true", value1);
            Assert.AreEqual("42", value2);
        }

        [Test]
        public void SameDataColumn_MultipleSheets_IndependentStorage()
        {
            var sheet2 = ScriptableObject.CreateInstance<SheetDefinition>();

            try
            {
                var success1 = this.handler.SetValue(this.testObject, this.sheetDefinition, "Sheet1 Value");
                var success2 = this.handler.SetValue(this.testObject, sheet2, "Sheet2 Value");

                Assert.IsTrue(success1);
                Assert.IsTrue(success2);

                // Verify values are independent per sheet
                var value1 = this.handler.GetValue(this.testObject, this.sheetDefinition);
                var value2 = this.handler.GetValue(this.testObject, sheet2);

                Assert.AreEqual("Sheet1 Value", value1);
                Assert.AreEqual("Sheet2 Value", value2);
            }
            finally
            {
                Object.DestroyImmediate(sheet2);
            }
        }

        [Test]
        public void DataColumn_LargeStringValues_HandledCorrectly()
        {
            // Test with no specific DataFieldType - should handle raw string values

            // Create a large string (10KB)
            var largeString = new string('A', 10240);

            var success = this.handler.SetValue(this.testObject, this.sheetDefinition, largeString);
            Assert.IsTrue(success);

            var retrievedValue = this.handler.GetValue(this.testObject, this.sheetDefinition);
            Assert.AreEqual(largeString, retrievedValue);
            Assert.AreEqual(10240, retrievedValue?.Length);
        }

        [Test]
        public void DataColumn_RepeatedSetOperations_PerformanceTest()
        {
            // Test with Integer DataFieldType for performance

            this.dataColumn.DataFieldType = DataFieldType.Integer;

            const int operationCount = 1000;
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            for (int i = 0; i < operationCount; i++)
            {
                var value = i.ToString();
                var success = this.handler.SetValue(this.testObject, this.sheetDefinition, value);
                Assert.IsTrue(success, $"Operation {i} failed");
            }

            stopwatch.Stop();

            // Should complete all operations quickly
            Assert.Less(stopwatch.ElapsedMilliseconds, 1000, $"1000 set operations took too long: {stopwatch.ElapsedMilliseconds}ms");

            // Verify final value
            var finalValue = this.handler.GetValue(this.testObject, this.sheetDefinition);
            Assert.AreEqual("999", finalValue);
        }

        [Test]
        public void DataColumn_SerializationCompatibility_MaintainsData()
        {
            this.dataColumn.DataFieldType = DataFieldType.Boolean;

            var testValue = "true";
            var success = this.handler.SetValue(this.testObject, this.sheetDefinition, testValue);
            Assert.IsTrue(success);

            // Force Unity serialization cycle
            EditorUtility.SetDirty(this.sheetDefinition);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Verify value persists after serialization
            var retrievedValue = this.handler.GetValue(this.testObject, this.sheetDefinition);
            Assert.AreEqual(testValue, retrievedValue, "Value should persist after serialization cycle");
        }

        private class TestObject : ScriptableObject
        {
            // Empty test object - pure data doesn't use object properties
        }
    }
}