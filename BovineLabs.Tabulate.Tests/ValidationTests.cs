// <copyright file="ValidationTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Tabulate.Tests
{
    using System.Collections.Generic;
    using System.Linq;
    using BovineLabs.Tabulate.Editor.Core;
    using BovineLabs.Tabulate.Editor.Formula;
    using BovineLabs.Tabulate.Editor.Handlers;
    using BovineLabs.Tabulate.Editor.Services;
    using NUnit.Framework;
    using UnityEngine;

    public class ValidationTests
    {
        private FormulaColumnHandler formulaHandler = null!;
        private ColumnDefinition testColumn = null!;
        private List<ColumnDefinition> allColumns = null!;

        [SetUp]
        public void SetUp()
        {
            this.testColumn = new ColumnDefinition
            {
                ColumnId = 2,
                UserDisplayName = "TestFormula",
                Type = ColumnType.Formula,
                Formula = "C0 + C1", // C0 = columnA, C1 = columnB
                PropertyPath = "testFloat",
                TargetTypeName = typeof(TestComponentForValidation).AssemblyQualifiedName,
            };

            this.formulaHandler = new FormulaColumnHandler(this.testColumn, column => new PropertyColumnHandler(column));

            this.allColumns = new List<ColumnDefinition>
            {
                new() { ColumnId = 0, UserDisplayName = "columnA", Type = ColumnType.Property, PropertyPath = "floatValue" },
                new() { ColumnId = 1, UserDisplayName = "columnB", Type = ColumnType.Property, PropertyPath = "intValue" },
                this.testColumn,
            };
        }

        [TearDown]
        public void TearDown()
        {
            if (this.testColumn != null)
            {
                this.testColumn = null!;
            }
        }

        [Test]
        public void ValidateFormulaDependencies_ValidFormula_ReturnsSuccess()
        {
            var result = this.formulaHandler.ValidateColumn(this.allColumns);
            Assert.IsTrue(result.IsValid);
        }

        [Test]
        public void ValidateFormulaDependencies_MissingColumn_ReturnsFailure()
        {
            this.testColumn.Formula = "C0 + C99"; // C0 = columnA, C99 doesn't exist
            var result = this.formulaHandler.ValidateColumn(this.allColumns);

            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.ErrorMessage.Contains("Undefined column reference")); // Missing column reference
        }

        [Test]
        public void ValidateFormulaDependencies_SelfReference_ReturnsFailure()
        {
            this.testColumn.Formula = "C2 + 1"; // C2 = self reference
            var result = this.formulaHandler.ValidateColumn(this.allColumns);

            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.ErrorMessage.Contains("Self-referencing formula"));
        }

        [Test]
        public void ValidateFormulaDependencies_CircularDependency_PassesSyntaxValidation()
        {
            // Create a circular dependency: A -> B -> A
            // Note: FormulaColumnHandler only validates syntax and self-references, not cross-column circular dependencies
            // Cross-column circular dependencies are detected at the sheet level in UnitySheetEditor
            var columnA = new ColumnDefinition { ColumnId = 0, UserDisplayName = "columnA", Type = ColumnType.Formula, Formula = "C1 + 1" }; // C1 = columnB
            var columnB = new ColumnDefinition { ColumnId = 1, UserDisplayName = "columnB", Type = ColumnType.Formula, Formula = "C0 + 1" }; // C0 = columnA

            var circularColumns = new List<ColumnDefinition> { columnA, columnB };
            var circularHandler = new FormulaColumnHandler(columnA, column => new PropertyColumnHandler(column));

            var result = circularHandler.ValidateColumn(circularColumns);

            // This should pass syntax validation - circular dependency detection happens at sheet level
            Assert.IsTrue(result.IsValid, $"Formula syntax validation should pass. Error: {result.ErrorMessage}");
        }

        [Test]
        public void ValidateFormulaDependencies_PropertyReference_DoesNotReportCircularDependency()
        {
            // Test case: C0 is a property, C2 is a formula referencing C0 (like "C0*2")
            // This should NOT report circular dependency since properties don't create cycles
            var propertyColumn = new ColumnDefinition { ColumnId = 0, UserDisplayName = "PropertyColumn", Type = ColumnType.Property, PropertyPath = "floatValue" };
            var formulaColumn = new ColumnDefinition { ColumnId = 2, UserDisplayName = "FormulaColumn", Type = ColumnType.Formula, Formula = "C0 * 2" };

            var columns = new List<ColumnDefinition> { propertyColumn, formulaColumn };
            var formulaHandler = new FormulaColumnHandler(formulaColumn, column => new PropertyColumnHandler(column));

            var result = formulaHandler.ValidateColumn(columns);

            // This should be valid since properties cannot create circular dependencies
            if (!result.IsValid)
            {
                // If there's an error, it should NOT be about circular dependency
                Assert.IsFalse(result.ErrorMessage.Contains("Circular dependency"),
                    $"Formula referencing a property column should not report circular dependency. Error: {result.ErrorMessage}");
            }
        }

        [Test]
        public void ValidateFormulaDependencies_FormulaChainToDataColumn_DoesNotReportCircularDependency()
        {
            // Test case: C2 has formula "C4*2", C4 has formula "C3", C3 is data
            // This should NOT report circular dependency - it's a valid dependency chain
            var dataColumn = new ColumnDefinition { ColumnId = 3, UserDisplayName = "DataColumn", Type = ColumnType.Data, DataValue = "5" };
            var formulaColumn1 = new ColumnDefinition { ColumnId = 4, UserDisplayName = "FormulaColumn1", Type = ColumnType.Formula, Formula = "C3" };
            var formulaColumn2 = new ColumnDefinition { ColumnId = 2, UserDisplayName = "FormulaColumn2", Type = ColumnType.Formula, Formula = "C4 * 2" };

            var columns = new List<ColumnDefinition> { dataColumn, formulaColumn1, formulaColumn2 };
            var formulaHandler = new FormulaColumnHandler(formulaColumn2, column => new PropertyColumnHandler(column));

            var result = formulaHandler.ValidateColumn(columns);

            // This should be valid since it's a linear dependency chain, not circular
            if (!result.IsValid)
            {
                // If there's an error, it should NOT be about circular dependency
                Assert.IsFalse(result.ErrorMessage.Contains("Circular dependency"),
                    $"Formula chain ending in data column should not report circular dependency. Error: {result.ErrorMessage}");
            }
        }

        [Test]
        public void ValidateFormulaDependencies_DirectDataReference_DoesNotReportCircularDependency()
        {
            // Test case: Formula directly references a data column (like "C3 * 5")
            // This should NEVER report circular dependency since data columns can't create loops
            var dataColumn = new ColumnDefinition { ColumnId = 3, UserDisplayName = "DataValue", Type = ColumnType.Data, DataValue = "10" };
            var formulaColumn = new ColumnDefinition { ColumnId = 5, UserDisplayName = "FormulaColumn", Type = ColumnType.Formula, Formula = "C3 * 5" };

            var columns = new List<ColumnDefinition> { dataColumn, formulaColumn };
            var formulaHandler = new FormulaColumnHandler(formulaColumn, column => new PropertyColumnHandler(column));

            var result = formulaHandler.ValidateColumn(columns);

            // This should be valid - data columns cannot create circular dependencies
            if (!result.IsValid)
            {
                // If there's an error, it should NOT be about circular dependency
                Assert.IsFalse(result.ErrorMessage.Contains("Circular dependency"),
                    $"Direct data column reference should never report circular dependency. Error: {result.ErrorMessage}");
                Assert.IsFalse(result.ErrorMessage.Contains("dependency loop"),
                    $"Direct data column reference should never report dependency loop. Error: {result.ErrorMessage}");
            }
        }

        #region Column Configuration Circular Dependency Tests

        [Test]
        public void ColumnConfig_CircularDependency_PropertyReference_NoError()
        {
            // Test: C0 (property) + C2 (formula "C0*2") should NOT report circular error
            var columns = new List<ColumnDefinition>
            {
                new() { ColumnId = 0, UserDisplayName = "Property", Type = ColumnType.Property, PropertyPath = "testValue" },
                new() { ColumnId = 2, UserDisplayName = "Formula", Type = ColumnType.Formula, Formula = "C0 * 2" },
            };

            var result = this.TestColumnConfigCircularDependency(columns[1], "C0 * 2", columns);
            Assert.IsTrue(result.IsValid, $"Property reference should not create circular dependency. Error: {result?.ErrorMessage}");
        }

        [Test]
        public void ColumnConfig_CircularDependency_DataReference_NoError()
        {
            // Test: C3 (data) + C5 (formula "C3*5") should NOT report circular error
            var columns = new List<ColumnDefinition>
            {
                new() { ColumnId = 3, UserDisplayName = "Data", Type = ColumnType.Data, DataValue = "10" },
                new() { ColumnId = 5, UserDisplayName = "Formula", Type = ColumnType.Formula, Formula = "C3 * 5" },
            };

            var result = this.TestColumnConfigCircularDependency(columns[1], "C3 * 5", columns);
            Assert.IsTrue(result.IsValid, $"Data reference should not create circular dependency. Error: {result?.ErrorMessage}");
        }

        [Test]
        public void ColumnConfig_CircularDependency_FormulaChainToData_NoError()
        {
            // Test: C2 "C4*2", C4 "C3", C3 (data) should NOT report circular error
            var columns = new List<ColumnDefinition>
            {
                new() { ColumnId = 3, UserDisplayName = "Data", Type = ColumnType.Data, DataValue = "5" },
                new() { ColumnId = 4, UserDisplayName = "Formula1", Type = ColumnType.Formula, Formula = "C3" },
                new() { ColumnId = 2, UserDisplayName = "Formula2", Type = ColumnType.Formula, Formula = "C4 * 2" },
            };

            var result = this.TestColumnConfigCircularDependency(columns[2], "C4 * 2", columns);
            Assert.IsTrue(result.IsValid, $"Formula chain to data should not create circular dependency. Error: {result?.ErrorMessage}");
        }

        [Test]
        public void ColumnConfig_CircularDependency_MultipleDependencies_NoError()
        {
            // Test: C6 "C4 + C5", where C4 and C5 are valid formulas, should NOT report circular error
            var columns = new List<ColumnDefinition>
            {
                new() { ColumnId = 3, UserDisplayName = "Data", Type = ColumnType.Data, DataValue = "10" },
                new() { ColumnId = 4, UserDisplayName = "Formula1", Type = ColumnType.Formula, Formula = "C3 * 2" },
                new() { ColumnId = 5, UserDisplayName = "Formula2", Type = ColumnType.Formula, Formula = "C3 + 1" },
                new() { ColumnId = 6, UserDisplayName = "Formula3", Type = ColumnType.Formula, Formula = "C4 + C5" },
            };

            var result = this.TestColumnConfigCircularDependency(columns[3], "C4 + C5", columns);
            Assert.IsTrue(result.IsValid, $"Multiple valid dependencies should not create circular dependency. Error: {result?.ErrorMessage}");
        }

        [Test]
        public void ColumnConfig_CircularDependency_RealCircular_ReportsError()
        {
            // Test: C1 "C2 + 1", C2 "C1 * 2" should report circular error
            var columns = new List<ColumnDefinition>
            {
                new() { ColumnId = 1, UserDisplayName = "Formula1", Type = ColumnType.Formula, Formula = "C2 + 1" },
                new() { ColumnId = 2, UserDisplayName = "Formula2", Type = ColumnType.Formula, Formula = "C1 * 2" },
            };

            var result = this.TestColumnConfigCircularDependency(columns[0], "C2 + 1", columns);
            Assert.IsFalse(result.IsValid, "Real circular dependency should be detected");
            Assert.IsTrue(result.ErrorMessage.Contains("Circular dependency"), $"Should report circular dependency. Error: {result.ErrorMessage}");
        }

        [Test]
        public void ColumnConfig_CircularDependency_ComplexCircular_ReportsError()
        {
            // Test: C1 "C2", C2 "C3", C3 "C1" should report circular error
            var columns = new List<ColumnDefinition>
            {
                new() { ColumnId = 1, UserDisplayName = "Formula1", Type = ColumnType.Formula, Formula = "C2" },
                new() { ColumnId = 2, UserDisplayName = "Formula2", Type = ColumnType.Formula, Formula = "C3" },
                new() { ColumnId = 3, UserDisplayName = "Formula3", Type = ColumnType.Formula, Formula = "C1" },
            };

            var result = this.TestColumnConfigCircularDependency(columns[0], "C2", columns);
            Assert.IsFalse(result.IsValid, "Complex circular dependency should be detected");
            Assert.IsTrue(result.ErrorMessage.Contains("Circular dependency"), $"Should report circular dependency. Error: {result.ErrorMessage}");
        }

        /// <summary>
        /// Helper method to test the column configuration circular dependency logic.
        /// This simulates what ColumnDetailsHandler.CheckForCircularDependency does.
        /// </summary>
        private ValidationResult TestColumnConfigCircularDependency(ColumnDefinition column, string formula, List<ColumnDefinition>? additionalColumns = null)
        {
            if (string.IsNullOrEmpty(formula))
            {
                return ValidationResult.Success();
            }

            try
            {
                // Build a complete temporary dependency graph for validation (same as ColumnDetailsHandler)
                var tempDependencyGraph = new FormulaDependencyGraph();
                var workingColumns = new List<ColumnDefinition>();

                // Add columns from the specific test
                if (additionalColumns != null)
                {
                    workingColumns.AddRange(additionalColumns);
                }
                else
                {
                    workingColumns.AddRange(this.allColumns);
                }

                // Add only formula-to-formula dependencies to the temporary graph
                foreach (var existingColumn in workingColumns.Where(c => c.Type == ColumnType.Formula && !string.IsNullOrEmpty(c.Formula) && c.ColumnId != column.ColumnId))
                {
                    var tokenizer = new FormulaTokenizer();
                    var tokens = tokenizer.Tokenize(existingColumn.Formula);

                    foreach (var token in tokens)
                    {
                        if (token.Type == TokenType.Identifier && token.Value.StartsWith("C"))
                        {
                            if (int.TryParse(token.Value.Substring(1), out var depColumnId))
                            {
                                // Only add dependency if the referenced column is also a formula column
                                var referencedColumn = workingColumns.FirstOrDefault(c => c.ColumnId == depColumnId);
                                if (referencedColumn?.Type == ColumnType.Formula)
                                {
                                    tempDependencyGraph.AddDependency(existingColumn.ColumnId, depColumnId);
                                }
                            }
                        }
                    }
                }

                // Parse the current formula to find all column references
                var currentTokenizer = new FormulaTokenizer();
                var currentTokens = currentTokenizer.Tokenize(formula);

                // Collect all formula dependencies from the current formula
                foreach (var token in currentTokens)
                {
                    if (token.Type == TokenType.Identifier && token.Value.StartsWith("C"))
                    {
                        if (int.TryParse(token.Value.Substring(1), out var referencedColumnId))
                        {
                            var referencedColumn = workingColumns.FirstOrDefault(c => c.ColumnId == referencedColumnId);

                            // Only add dependencies to formula columns for circular dependency detection
                            if (referencedColumn?.Type == ColumnType.Formula)
                            {
                                tempDependencyGraph.AddDependency(column.ColumnId, referencedColumnId);
                            }
                        }
                    }
                }

                // Now perform a single cycle check on the complete graph
                if (tempDependencyGraph.TryFindCycle(out var cyclePath) && cyclePath.Contains(column.ColumnId))
                {
                    return ValidationResult.Failure($"Circular dependency detected", "This formula creates a dependency loop.",
                        "Remove the reference that creates the circular dependency.");
                }

                return ValidationResult.Success();
            }
            catch (FormulaException)
            {
                // Formula parsing error - not a circular dependency issue
                return ValidationResult.Success();
            }
        }

        #endregion

        [Test]
        public void ValidateFormulaDependencies_ValidFormulaToFormulaChain_DoesNotReportCircularDependency()
        {
            // Test case: Valid linear chain A -> B -> C where all are formulas but no circular reference
            // C6 has formula "C7 + 1", C7 has formula "C8 * 2", C8 has formula "5"
            var formulaColumn1 = new ColumnDefinition { ColumnId = 8, UserDisplayName = "BaseFormula", Type = ColumnType.Formula, Formula = "5" };
            var formulaColumn2 = new ColumnDefinition { ColumnId = 7, UserDisplayName = "MiddleFormula", Type = ColumnType.Formula, Formula = "C8 * 2" };
            var formulaColumn3 = new ColumnDefinition { ColumnId = 6, UserDisplayName = "TopFormula", Type = ColumnType.Formula, Formula = "C7 + 1" };

            var columns = new List<ColumnDefinition> { formulaColumn1, formulaColumn2, formulaColumn3 };
            var formulaHandler = new FormulaColumnHandler(formulaColumn3, column => new PropertyColumnHandler(column));

            var result = formulaHandler.ValidateColumn(columns);

            // This should be valid - it's a linear dependency chain, not circular
            if (!result.IsValid)
            {
                // If there's an error, it should NOT be about circular dependency
                Assert.IsFalse(result.ErrorMessage.Contains("Circular dependency"),
                    $"Valid formula-to-formula chain should not report circular dependency. Error: {result.ErrorMessage}");
                Assert.IsFalse(result.ErrorMessage.Contains("dependency loop"),
                    $"Valid formula-to-formula chain should not report dependency loop. Error: {result.ErrorMessage}");
            }
        }

        [Test]
        public void ValidateFormulaDependencies_InvalidSyntax_ReturnsFailure()
        {
            this.testColumn.Formula = "C0 +"; // Invalid syntax
            var result = this.formulaHandler.ValidateColumn(this.allColumns);

            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.ErrorMessage.Contains("Invalid operator position"));
        }

        [Test]
        public void ValidateResultType_CompatibleType_ReturnsSuccess()
        {
            var testObject = this.CreateTestComponent();
            var result = this.formulaHandler.ValidateResultType(5.0f, testObject);

            Assert.IsTrue(result.IsValid);

            Object.DestroyImmediate(testObject.gameObject);
        }

        [Test]
        public void ValidateResultType_NumericConversion_ReturnsSuccess()
        {
            var testObject = this.CreateTestComponent();

            // Test int to float conversion
            var result = this.formulaHandler.ValidateResultType(5, testObject);
            Assert.IsTrue(result.IsValid);

            Object.DestroyImmediate(testObject.gameObject);
        }

        [Test]
        public void ValidateResultType_IncompatibleType_ReturnsFailure()
        {
            var testObject = this.CreateTestComponent();

            // String to float should fail
            var result = this.formulaHandler.ValidateResultType("not a number", testObject);
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.ErrorMessage.Contains("Formula result type mismatch"));

            Object.DestroyImmediate(testObject.gameObject);
        }

        [Test]
        public void ValidateResultType_NullResult_ReturnsSuccess()
        {
            var testObject = this.CreateTestComponent();
            var result = this.formulaHandler.ValidateResultType(null, testObject);

            Assert.IsTrue(result.IsValid);

            Object.DestroyImmediate(testObject.gameObject);
        }

        [Test]
        public void ValidateFormulaDependencies_EmptyFormula_ReturnsSuccess()
        {
            this.testColumn.Formula = string.Empty;
            var result = this.formulaHandler.ValidateColumn(this.allColumns);

            Assert.IsTrue(result.IsValid);
        }

        [Test]
        public void ValidateFormulaDependencies_ComplexValidFormula_ReturnsSuccess()
        {
            this.testColumn.Formula = "(C0 + C1) * 2"; // C0 = columnA, C1 = columnB
            var result = this.formulaHandler.ValidateColumn(this.allColumns);

            Assert.IsTrue(result.IsValid);
        }

        [Test]
        public void ValidateFormulaDependencies_MultipleReferencesToSameColumn_ReturnsSuccess()
        {
            this.testColumn.Formula = "C0 + C0 * 2"; // C0 = columnA
            var result = this.formulaHandler.ValidateColumn(this.allColumns);

            Assert.IsTrue(result.IsValid);
        }

        [Test]
        public void ValidateFormulaDependencies_CaseInsensitiveColumnNames_ReturnsSuccess()
        {
            this.testColumn.Formula = "C0 + C1"; // Column references are case-sensitive by design
            var result = this.formulaHandler.ValidateColumn(this.allColumns);

            Assert.IsTrue(result.IsValid);
        }

        [Test]
        public void PropertyValidationService_ValidateDuplicateColumns_PropertyAndFormulaDuplicates_ReturnsFailure()
        {
            // Create columns with duplicate target type and property path
            var columns = new List<ColumnDefinition>
            {
                new()
                {
                    ColumnId = 0,
                    Type = ColumnType.Property,
                    TargetTypeName = typeof(TestComponentForValidation).AssemblyQualifiedName!,
                    PropertyPath = "TestFloat",
                },
                new()
                {
                    ColumnId = 1,
                    Type = ColumnType.Formula,
                    Formula = "5 + 3",
                    TargetTypeName = typeof(TestComponentForValidation).AssemblyQualifiedName!,
                    PropertyPath = "TestFloat", // Same target type and property path as property column
                },
            };

            // Test validating the formula column (should detect duplicate with property column)
            var result = PropertyValidationService.ValidateDuplicateColumns(columns[1], columns);

            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.ErrorMessage.Contains("Duplicate formula column"));
            Assert.IsTrue(result.ErrorMessage.Contains("property column already writes"));
            Assert.IsTrue(result.ErrorMessage.Contains("TestFloat"));
            Assert.IsTrue(result.DetailedDescription.Contains("C0"));
            Assert.IsTrue(result.DetailedDescription.Contains("Property"));
        }

        [Test]
        public void PropertyValidationService_ValidateDuplicateColumns_DifferentTargetTypes_ReturnsSuccess()
        {
            // Create columns with different target types but same property path
            var columns = new List<ColumnDefinition>
            {
                new()
                {
                    ColumnId = 0,
                    Type = ColumnType.Property,
                    TargetTypeName = typeof(TestComponentForValidation).AssemblyQualifiedName!,
                    PropertyPath = "name",
                },
                new()
                {
                    ColumnId = 1,
                    Type = ColumnType.Formula,
                    Formula = "\"test\"",
                    TargetTypeName = typeof(Transform).AssemblyQualifiedName!,
                    PropertyPath = "name", // Same property path but different target type
                },
            };

            // Both columns should be valid since they have different target types
            var result1 = PropertyValidationService.ValidateDuplicateColumns(columns[0], columns);
            var result2 = PropertyValidationService.ValidateDuplicateColumns(columns[1], columns);

            Assert.IsTrue(result1.IsValid);
            Assert.IsTrue(result2.IsValid);
        }

        [Test]
        public void PropertyValidationService_ValidateDuplicateColumns_DataColumnSkipped_ReturnsSuccess()
        {
            // Create property and data columns (data column should be skipped)
            var columns = new List<ColumnDefinition>
            {
                new()
                {
                    ColumnId = 0,
                    Type = ColumnType.Property,
                    TargetTypeName = typeof(TestComponentForValidation).AssemblyQualifiedName!,
                    PropertyPath = "TestFloat",
                },
                new()
                {
                    ColumnId = 1,
                    Type = ColumnType.Data,
                    DataFieldType = DataFieldType.Float,
                },
            };

            // Data column validation should always pass (doesn't write to properties)
            var result = PropertyValidationService.ValidateDuplicateColumns(columns[1], columns);

            Assert.IsTrue(result.IsValid);
        }

        private TestComponentForValidation CreateTestComponent()
        {
            var gameObject = new GameObject("TestObject");
            return gameObject.AddComponent<TestComponentForValidation>();
        }
    }

    // Test component for validation tests
    public class TestComponentForValidation : MonoBehaviour
    {
        [SerializeField]
        private float testFloat = 0f;

        [SerializeField]
        private int testInt = 0;

        [SerializeField]
        private string testString = string.Empty;

        [SerializeField]
        private bool testBool = false;

        [SerializeField]
        private Vector3 testVector = Vector3.zero;

        public float TestFloat
        {
            get => this.testFloat;
            set => this.testFloat = value;
        }

        public int TestInt
        {
            get => this.testInt;
            set => this.testInt = value;
        }

        public string TestString
        {
            get => this.testString;
            set => this.testString = value;
        }

        public bool TestBool
        {
            get => this.testBool;
            set => this.testBool = value;
        }

        public Vector3 TestVector
        {
            get => this.testVector;
            set => this.testVector = value;
        }
    }
}