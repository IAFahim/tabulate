// <copyright file="RealTimeFormulaTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Tabulate.Tests
{
    using System.Collections.Generic;
    using BovineLabs.Tabulate.Editor.Core;
    using BovineLabs.Tabulate.Editor.Formula;
    using BovineLabs.Tabulate.Editor.Handlers;
    using NUnit.Framework;
    using UnityEngine;

    [TestFixture]
    public class RealTimeFormulaTests
    {
        private FormulaDependencyGraph dependencyGraph = null!;
        private PropertyColumnHandler propertyHandler = null!;
        private TestScriptableObject testObject = null!;

        [SetUp]
        public void SetUp()
        {
            this.dependencyGraph = new FormulaDependencyGraph();
            this.testObject = ScriptableObject.CreateInstance<TestScriptableObject>();
            this.testObject.BaseValue = 10f;
            this.testObject.Multiplier = 2f;
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
        public void FormulaDependencyGraph_GetRecalculationOrder_ReturnsCorrectOrder()
        {
            this.dependencyGraph.AddDependency(0, 1);
            this.dependencyGraph.AddDependency(2, 0);

            var order = this.dependencyGraph.GetRecalculationOrder(1);

            Assert.AreEqual(2, order.Count);
            Assert.AreEqual(0, order[0]);
            Assert.AreEqual(2, order[1]);
        }

        [Test]
        public void PropertyColumnHandler_PropertyChange_TriggersEvent()
        {
            var columnDef = new ColumnDefinition
            {
                UserDisplayName = "BaseValue",
                Type = ColumnType.Property,
                PropertyPath = "BaseValue",
                TargetTypeName = typeof(TestScriptableObject).AssemblyQualifiedName!,
            };

            this.propertyHandler = new PropertyColumnHandler(columnDef);
            bool eventTriggered = false;
            string? changedPropertyPath = null;

            this.propertyHandler.PropertyChanged += (propertyPath) =>
            {
                eventTriggered = true;
                changedPropertyPath = propertyPath;
            };

            this.propertyHandler.RegisterObject(this.testObject);
            this.propertyHandler.SetValue(this.testObject, 15f);

            Assert.IsTrue(eventTriggered);
            Assert.AreEqual("BaseValue", changedPropertyPath);
        }

        [Test]
        public void ComplexDependencyChain_CalculationOrder_IsCorrect()
        {
            // Create a complex dependency chain: 0 -> 1 -> 2
            this.dependencyGraph.AddDependency(1, 0); // derived depends on base
            this.dependencyGraph.AddDependency(2, 1); // final depends on derived
            this.dependencyGraph.AddDependency(2, 0); // final depends on base
            this.dependencyGraph.AddDependency(3, 0); // another depends on base

            var order = this.dependencyGraph.GetRecalculationOrder(0);

            // Should recalculate in dependency order
            Assert.Contains(1, order);
            Assert.Contains(2, order);
            Assert.Contains(3, order);

            // '1' should come before '2' since '2' depends on '1'
            var derivedIndex = order.IndexOf(1);
            var finalIndex = order.IndexOf(2);
            Assert.Less(derivedIndex, finalIndex);
        }

        [Test]
        public void DeepDependencyChain_MultiLevel_CorrectOrder()
        {
            // Create a deep chain: 0 -> 1 -> 2 -> 3 -> 4 -> 5
            this.dependencyGraph.AddDependency(1, 0);
            this.dependencyGraph.AddDependency(2, 1);
            this.dependencyGraph.AddDependency(3, 2);
            this.dependencyGraph.AddDependency(4, 3);
            this.dependencyGraph.AddDependency(5, 4);

            var order = this.dependencyGraph.GetRecalculationOrder(0);

            Assert.AreEqual(5, order.Count);
            Assert.AreEqual(1, order[0]);
            Assert.AreEqual(2, order[1]);
            Assert.AreEqual(3, order[2]);
            Assert.AreEqual(4, order[3]);
            Assert.AreEqual(5, order[4]);
        }

        [Test]
        public void DiamondDependencyPattern_CorrectOrder()
        {
            // Create diamond pattern: 0 -> (1,2) -> 3
            //   0
            //  / \
            // 1   2
            //  \ /
            //   3
            this.dependencyGraph.AddDependency(1, 0);
            this.dependencyGraph.AddDependency(2, 0);
            this.dependencyGraph.AddDependency(3, 1);
            this.dependencyGraph.AddDependency(3, 2);

            var order = this.dependencyGraph.GetRecalculationOrder(0);

            Assert.AreEqual(3, order.Count);
            Assert.Contains(1, order);
            Assert.Contains(2, order);
            Assert.Contains(3, order);

            // Both 1 and 2 should come before 3
            var index1 = order.IndexOf(1);
            var index2 = order.IndexOf(2);
            var index3 = order.IndexOf(3);

            Assert.Less(index1, index3);
            Assert.Less(index2, index3);
        }

        [Test]
        public void MultipleBranchesDependencyTree_CorrectOrder()
        {
            // Create tree with multiple branches:
            //     0
            //   / | \
            //  1  2  3
            //  |  |  |
            //  4  5  6
            this.dependencyGraph.AddDependency(1, 0);
            this.dependencyGraph.AddDependency(2, 0);
            this.dependencyGraph.AddDependency(3, 0);
            this.dependencyGraph.AddDependency(4, 1);
            this.dependencyGraph.AddDependency(5, 2);
            this.dependencyGraph.AddDependency(6, 3);

            var order = this.dependencyGraph.GetRecalculationOrder(0);

            Assert.AreEqual(6, order.Count);

            // All level 1 dependencies (1,2,3) should come before level 2 (4,5,6)
            var indices = new Dictionary<int, int>();
            for (int i = 0; i < order.Count; i++)
            {
                indices[order[i]] = i;
            }

            Assert.Less(indices[1], indices[4]);
            Assert.Less(indices[2], indices[5]);
            Assert.Less(indices[3], indices[6]);
        }

        [Test]
        public void NoDependencies_EmptyRecalculationOrder()
        {
            var order = this.dependencyGraph.GetRecalculationOrder(0);
            Assert.AreEqual(0, order.Count);
        }

        [Test]
        public void RemoveDependencies_UpdatesRecalculationOrder()
        {
            this.dependencyGraph.AddDependency(1, 0);
            this.dependencyGraph.AddDependency(2, 1);

            // Initial order should include both
            var initialOrder = this.dependencyGraph.GetRecalculationOrder(0);
            Assert.Contains(1, initialOrder);
            Assert.Contains(2, initialOrder);

            // Remove all dependencies for column 2
            this.dependencyGraph.RemoveDependencies(2);
            var updatedOrder = this.dependencyGraph.GetRecalculationOrder(0);
            
            Assert.Contains(1, updatedOrder);
            Assert.IsFalse(updatedOrder.Contains(2));
        }

        [Test]
        public void Clear_EmptyRecalculationOrder()
        {
            this.dependencyGraph.AddDependency(1, 0);
            this.dependencyGraph.AddDependency(2, 0);
            this.dependencyGraph.AddDependency(3, 1);

            // Verify dependencies exist
            var orderBefore = this.dependencyGraph.GetRecalculationOrder(0);
            Assert.Greater(orderBefore.Count, 0);

            // Clear and verify empty
            this.dependencyGraph.Clear();
            var orderAfter = this.dependencyGraph.GetRecalculationOrder(0);
            Assert.AreEqual(0, orderAfter.Count);
        }

        [Test]
        public void SelfDependency_DoesNotCauseInfiniteLoop()
        {
            // This should either be rejected or handled gracefully
            this.dependencyGraph.AddDependency(0, 0);

            // Should not throw and should not include self in recalculation
            Assert.DoesNotThrow(() =>
            {
                var order = this.dependencyGraph.GetRecalculationOrder(0);
                Assert.IsFalse(order.Contains(0), "Self-dependency should not be included in recalculation order");
            });
        }

        [Test]
        public void CircularDependency_TwoNodes_HandledGracefully()
        {
            // Create circular dependency: 0 -> 1 -> 0
            this.dependencyGraph.AddDependency(1, 0);
            this.dependencyGraph.AddDependency(0, 1);

            // Should handle circular dependency gracefully (may return partial order)
            Assert.DoesNotThrow(() =>
            {
                var order = this.dependencyGraph.GetRecalculationOrder(0);
                // Order may be incomplete due to circular dependency, but should not crash
                Assert.IsNotNull(order);
            });
        }

        [Test]
        public void CircularDependency_ThreeNodes_HandledGracefully()
        {
            // Create circular dependency: 0 -> 1 -> 2 -> 0
            this.dependencyGraph.AddDependency(1, 0);
            this.dependencyGraph.AddDependency(2, 1);
            this.dependencyGraph.AddDependency(0, 2);

            // Should handle circular dependency gracefully
            Assert.DoesNotThrow(() =>
            {
                var order = this.dependencyGraph.GetRecalculationOrder(0);
                // Order may be incomplete due to circular dependency, but should not crash
                Assert.IsNotNull(order);
            });
        }

        [Test]
        public void LargeDependencyGraph_Performance()
        {
            // Create a large dependency graph for performance testing
            const int nodeCount = 100;
            
            // Create a chain: 0 -> 1 -> 2 -> ... -> 99
            for (int i = 1; i < nodeCount; i++)
            {
                this.dependencyGraph.AddDependency(i, i - 1);
            }

            // Add some branches to make it more complex
            for (int i = 10; i < nodeCount; i += 10)
            {
                this.dependencyGraph.AddDependency(i + nodeCount, i);
            }

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var order = this.dependencyGraph.GetRecalculationOrder(0);
            stopwatch.Stop();

            // Should complete quickly (within reasonable time)
            Assert.Less(stopwatch.ElapsedMilliseconds, 100, "Dependency graph calculation should be fast for 100+ nodes");
            Assert.Greater(order.Count, nodeCount - 1, "Should return all dependent nodes");
        }
    }

    /// <summary>
    /// Test ScriptableObject for real-time formula testing.
    /// </summary>
    [System.Serializable]
    public class TestScriptableObject : ScriptableObject
    {
        public float BaseValue = 10f;
        public float Multiplier = 2f;
        public float CalculatedValue = 0f;
    }
}