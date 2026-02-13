// <copyright file="PropertyColumnHandlerTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Tabulate.Tests
{
    using BovineLabs.Tabulate.Editor.Core;
    using BovineLabs.Tabulate.Editor.Handlers;
    using NUnit.Framework;
    using UnityEngine;

    [TestFixture]
    public class PropertyColumnHandlerTests
    {
        private GameObject testGameObject = null!;
        private ConcreteTestComponent testComponent = null!;
        private ConcreteTestScriptableObject testScriptableObject = null!;

        [SetUp]
        public void SetUp()
        {
            this.testGameObject = new GameObject("TestGameObject");
            this.testComponent = this.testGameObject.AddComponent<ConcreteTestComponent>();
            this.testScriptableObject = ScriptableObject.CreateInstance<ConcreteTestScriptableObject>();
        }

        [TearDown]
        public void TearDown()
        {
            if (this.testGameObject)
            {
                Object.DestroyImmediate(this.testGameObject);
            }

            if (this.testScriptableObject)
            {
                Object.DestroyImmediate(this.testScriptableObject);
            }
        }

        [Test]
        public void GetValue_FloatProperty_ReturnsCorrectValue()
        {
            // Arrange
            var columnDefinition = new ColumnDefinition
            {
                Type = ColumnType.Property,
                PropertyPath = "testFloat",
            };
            var handler = new PropertyColumnHandler(columnDefinition);
            this.testComponent.TestFloat = 3.14f;

            // Act
            var value = handler.GetValue(this.testComponent);

            // Assert
            Assert.That(value, Is.EqualTo(3.14f).Within(0.001f));
        }

        [Test]
        public void GetValue_IntProperty_ReturnsCorrectValue()
        {
            // Arrange
            var columnDefinition = new ColumnDefinition
            {
                Type = ColumnType.Property,
                PropertyPath = "testInt",
            };
            var handler = new PropertyColumnHandler(columnDefinition);
            this.testComponent.TestInt = 123;

            // Act
            var value = handler.GetValue(this.testComponent);

            // Assert
            Assert.That(value, Is.EqualTo(123));
        }

        [Test]
        public void GetValue_StringProperty_ReturnsCorrectValue()
        {
            // Arrange
            var columnDefinition = new ColumnDefinition
            {
                Type = ColumnType.Property,
                PropertyPath = "testString",
            };
            var handler = new PropertyColumnHandler(columnDefinition);
            this.testComponent.TestString = "Hello World";

            // Act
            var value = handler.GetValue(this.testComponent);

            // Assert
            Assert.That(value, Is.EqualTo("Hello World"));
        }

        [Test]
        public void GetValue_BoolProperty_ReturnsCorrectValue()
        {
            // Arrange
            var columnDefinition = new ColumnDefinition
            {
                Type = ColumnType.Property,
                PropertyPath = "testBool",
            };
            var handler = new PropertyColumnHandler(columnDefinition);
            this.testComponent.TestBool = false;

            // Act
            var value = handler.GetValue(this.testComponent);

            // Assert
            Assert.That(value, Is.EqualTo(false));
        }

        [Test]
        public void SetValue_FloatProperty_SetsCorrectValue()
        {
            // Arrange
            var columnDefinition = new ColumnDefinition
            {
                Type = ColumnType.Property,
                PropertyPath = "testFloat",
                IsReadOnly = false,
            };
            var handler = new PropertyColumnHandler(columnDefinition);

            // Act
            var success = handler.SetValue(this.testComponent, 2.71f);

            // Assert
            Assert.That(success, Is.True);
            Assert.That(this.testComponent.TestFloat, Is.EqualTo(2.71f).Within(0.001f));
        }

        [Test]
        public void SetValue_IntProperty_SetsCorrectValue()
        {
            // Arrange
            var columnDefinition = new ColumnDefinition
            {
                Type = ColumnType.Property,
                PropertyPath = "testInt",
                IsReadOnly = false,
            };
            var handler = new PropertyColumnHandler(columnDefinition);

            // Act
            var success = handler.SetValue(this.testComponent, 999);

            // Assert
            Assert.That(success, Is.True);
            Assert.That(this.testComponent.TestInt, Is.EqualTo(999));
        }

        [Test]
        public void SetValue_StringProperty_SetsCorrectValue()
        {
            // Arrange
            var columnDefinition = new ColumnDefinition
            {
                Type = ColumnType.Property,
                PropertyPath = "testString",
                IsReadOnly = false,
            };
            var handler = new PropertyColumnHandler(columnDefinition);

            // Act
            var success = handler.SetValue(this.testComponent, "New Value");

            // Assert
            Assert.That(success, Is.True);
            Assert.That(this.testComponent.TestString, Is.EqualTo("New Value"));
        }

        [Test]
        public void SetValue_ReadOnlyColumn_ReturnsFalse()
        {
            // Arrange
            var columnDefinition = new ColumnDefinition
            {
                Type = ColumnType.Property,
                PropertyPath = "testFloat",
                IsReadOnly = true,
            };
            var handler = new PropertyColumnHandler(columnDefinition);
            var originalValue = this.testComponent.TestFloat;

            // Act
            var success = handler.SetValue(this.testComponent, 999.0f);

            // Assert
            Assert.That(success, Is.False);
            Assert.That(this.testComponent.TestFloat, Is.EqualTo(originalValue));
        }

        [Test]
        public void SetValue_InvalidPropertyPath_ReturnsFalse()
        {
            // Arrange
            var columnDefinition = new ColumnDefinition
            {
                Type = ColumnType.Property,
                PropertyPath = "nonExistentProperty",
                IsReadOnly = false,
            };
            var handler = new PropertyColumnHandler(columnDefinition);

            // Act
            var success = handler.SetValue(this.testComponent, 123);

            // Assert
            Assert.That(success, Is.False);
        }

        [Test]
        public void ScriptableObject_GetValue_ReturnsCorrectValue()
        {
            // Arrange
            var columnDefinition = new ColumnDefinition
            {
                Type = ColumnType.Property,
                PropertyPath = "health",
            };
            var handler = new PropertyColumnHandler(columnDefinition);
            this.testScriptableObject.Health = 75.5f;

            // Act
            var value = handler.GetValue(this.testScriptableObject);

            // Assert
            Assert.That(value, Is.EqualTo(75.5f).Within(0.001f));
        }

        [Test]
        public void ScriptableObject_SetValue_SetsCorrectValue()
        {
            // Arrange
            var columnDefinition = new ColumnDefinition
            {
                Type = ColumnType.Property,
                PropertyPath = "damage",
                IsReadOnly = false,
            };
            var handler = new PropertyColumnHandler(columnDefinition);

            // Act
            var success = handler.SetValue(this.testScriptableObject, 50);

            // Assert
            Assert.That(success, Is.True);
            Assert.That(this.testScriptableObject.Damage, Is.EqualTo(50));
        }
    }
}