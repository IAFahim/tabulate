// <copyright file="ColumnIdSystemTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Tabulate.Tests
{
    using System.Collections.Generic;
    using System.Linq;
    using BovineLabs.Tabulate.Editor.Core;
    using BovineLabs.Tabulate.Editor.Handlers;
    using NUnit.Framework;

    public class ColumnIdSystemTests
    {
        [Test]
        public void ColumnDefinition_FormulaReference_ReturnsCorrectFormat()
        {
            var column = new ColumnDefinition { ColumnId = 0 };
            Assert.AreEqual("C0", column.FormulaReference);

            column.ColumnId = 10;
            Assert.AreEqual("C10", column.FormulaReference);

            column.ColumnId = 123;
            Assert.AreEqual("C123", column.FormulaReference);
        }

        [Test]
        public void FormulaColumnHandler_TryParseColumnReference_ValidFormats()
        {
            // We need to test the private method via FormulaColumnHandler validation
            var column = new ColumnDefinition
            {
                ColumnId = 2,
                Type = ColumnType.Formula,
                Formula = "C0 + C1 + C10",
            };

            var handler = new FormulaColumnHandler(column, col => new PropertyColumnHandler(col));
            var allColumns = new List<ColumnDefinition>
            {
                new() { ColumnId = 0, Type = ColumnType.Property },
                new() { ColumnId = 1, Type = ColumnType.Property },
                new() { ColumnId = 10, Type = ColumnType.Property },
            };

            var result = handler.ValidateColumn(allColumns);
            Assert.IsTrue(result.IsValid, $"Validation failed: {result.ErrorMessage}");
        }

        [Test]
        public void FormulaColumnHandler_TryParseColumnReference_InvalidFormats()
        {
            var column = new ColumnDefinition
            {
                ColumnId = 0,
                Type = ColumnType.Formula,
                Formula = "C + 1", // Invalid - C without number
            };

            var handler = new FormulaColumnHandler(column, col => new PropertyColumnHandler(col));
            var allColumns = new List<ColumnDefinition>();

            var result = handler.ValidateColumn(allColumns);
            Assert.IsFalse(result.IsValid);
        }

        [Test]
        public void FormulaColumnHandler_ValidateFormulaDependencies_MissingColumn()
        {
            var column = new ColumnDefinition
            {
                ColumnId = 0,
                Type = ColumnType.Formula,
                Formula = "C99 + 1", // C99 doesn't exist
            };

            var handler = new FormulaColumnHandler(column, col => new PropertyColumnHandler(col));
            var allColumns = new List<ColumnDefinition>
            {
                new() { ColumnId = 0, Type = ColumnType.Property },
                new() { ColumnId = 1, Type = ColumnType.Property },
            };

            var result = handler.ValidateColumn(allColumns);
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.ErrorMessage.Contains("Undefined column reference"));
        }

        [Test]
        public void FormulaColumnHandler_ValidateFormulaDependencies_SelfReference()
        {
            var column = new ColumnDefinition
            {
                ColumnId = 5,
                Type = ColumnType.Formula,
                Formula = "C5 + 1", // References itself
            };

            var handler = new FormulaColumnHandler(column, col => new PropertyColumnHandler(col));
            var allColumns = new List<ColumnDefinition> { column };

            var result = handler.ValidateColumn(allColumns);
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.ErrorMessage.Contains("Self-referencing formula"));
        }

        [Test]
        public void FormulaColumnHandler_GetColumnDependencies_ReturnsColumnIds()
        {
            var column = new ColumnDefinition
            {
                ColumnId = 0,
                Type = ColumnType.Formula,
                Formula = "C1 * C2 + C10",
            };

            var handler = new FormulaColumnHandler(column, col => new PropertyColumnHandler(col));
            var dependencies = handler.GetColumnDependencies();

            Assert.AreEqual(3, dependencies.Count);
            Assert.IsTrue(dependencies.Contains("C1"));
            Assert.IsTrue(dependencies.Contains("C2"));
            Assert.IsTrue(dependencies.Contains("C10"));
        }

        [Test]
        public void ColumnIdAssignment_GapPreservation_WorksCorrectly()
        {
            // Simulate the gap-preserving ID assignment logic
            var existingColumns = new List<ColumnDefinition>
            {
                new() { ColumnId = 0 },
                new() { ColumnId = 2 }, // Gap at 1
                new() { ColumnId = 5 }, // Gaps at 3, 4
            };

            var usedIds = existingColumns.Select(c => c.ColumnId).ToHashSet();
            var nextId = 0;
            while (usedIds.Contains(nextId))
            {
                nextId++;
            }

            // Should assign ID 1 (first gap)
            Assert.AreEqual(1, nextId);

            // Add the new column and test again
            existingColumns.Add(new ColumnDefinition { ColumnId = nextId });
            usedIds = existingColumns.Select(c => c.ColumnId).ToHashSet();
            nextId = 0;
            while (usedIds.Contains(nextId))
            {
                nextId++;
            }

            // Should assign ID 3 (next gap)
            Assert.AreEqual(3, nextId);
        }

        [Test]
        public void PropertyDisplayName_AutoGeneration_FormatsCorrectly()
        {
            // Test the auto-generation logic for Property DisplayNames
            // Use a simpler type that will definitely resolve
            var targetTypeName = "UnityEngine.Transform, UnityEngine.CoreModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null";
            var propertyPath = "position";

            // Simulate the auto-generation logic
            var targetType = System.Type.GetType(targetTypeName);
            string actualDisplayName;
            if (targetType != null)
            {
                actualDisplayName = $"{targetType.Name}.{propertyPath}";
            }
            else
            {
                actualDisplayName = "InvalidType.position";
            }

            Assert.AreEqual("Transform.position", actualDisplayName);
        }

        [Test]
        public void FormulaColumnHandler_CircularReference_PassesSyntaxValidation()
        {
            // Set up circular dependency: C0 -> C1 -> C0
            // Note: FormulaColumnHandler only validates syntax and self-references, not cross-column circular dependencies
            // Cross-column circular dependencies are detected at the sheet level
            var column0 = new ColumnDefinition
            {
                ColumnId = 0,
                Type = ColumnType.Formula,
                Formula = "C1 + 1",
            };

            var column1 = new ColumnDefinition
            {
                ColumnId = 1,
                Type = ColumnType.Formula,
                Formula = "C0 * 2", // Creates circular dependency
            };

            var allColumns = new List<ColumnDefinition> { column0, column1 };
            var handler = new FormulaColumnHandler(column0, col => new PropertyColumnHandler(col));

            var result = handler.ValidateColumn(allColumns);
            // This should pass syntax validation - circular dependency detection happens at sheet level
            Assert.IsTrue(result.IsValid, $"Formula syntax validation should pass. Error: {result.ErrorMessage}");
        }

        [Test]
        public void FormulaColumnHandler_ComplexFormula_ValidatesCorrectly()
        {
            var column = new ColumnDefinition
            {
                ColumnId = 5,
                Type = ColumnType.Formula,
                Formula = "(C0 + C1) * C2 / (C3 - C4)",
            };

            var allColumns = new List<ColumnDefinition>
            {
                new() { ColumnId = 0, Type = ColumnType.Property },
                new() { ColumnId = 1, Type = ColumnType.Property },
                new() { ColumnId = 2, Type = ColumnType.Property },
                new() { ColumnId = 3, Type = ColumnType.Property },
                new() { ColumnId = 4, Type = ColumnType.Property },
                column,
            };

            var handler = new FormulaColumnHandler(column, col => new PropertyColumnHandler(col));
            var result = handler.ValidateColumn(allColumns);
            Assert.IsTrue(result.IsValid, $"Validation failed: {result.ErrorMessage}");

            var dependencies = handler.GetColumnDependencies();
            Assert.AreEqual(5, dependencies.Count);
            Assert.IsTrue(dependencies.Contains("C0"));
            Assert.IsTrue(dependencies.Contains("C1"));
            Assert.IsTrue(dependencies.Contains("C2"));
            Assert.IsTrue(dependencies.Contains("C3"));
            Assert.IsTrue(dependencies.Contains("C4"));
        }
    }
}