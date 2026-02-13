// <copyright file="PropertyColumnIntegrationTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Tabulate.Tests
{
    using BovineLabs.Tabulate.Editor.Core;
    using BovineLabs.Tabulate.Editor.Handlers;
    using NUnit.Framework;
    using UnityEngine;

    [TestFixture]
    public class PropertyColumnIntegrationTests
    {
        private SheetDefinition gameObjectSheetDefinition = null!;
        private SheetDefinition scriptableObjectSheetDefinition = null!;
        private GameObject testGameObject = null!;
        private TestComponent testComponent = null!;
        private TestScriptableObject testScriptableObject = null!;

        [SetUp]
        public void SetUp()
        {
            // Create test objects
            this.testGameObject = new GameObject("TestGameObject");
            this.testComponent = this.testGameObject.AddComponent<TestComponent>();
            this.testScriptableObject = ScriptableObject.CreateInstance<TestScriptableObject>();

            // Initialize test values
            this.testComponent.FloatValue = 5.5f;
            this.testComponent.IntValue = 42;
            this.testComponent.BoolValue = true;
            this.testComponent.StringValue = "Test String";

            this.testScriptableObject.FloatValue = 10.5f;
            this.testScriptableObject.IntValue = 84;
            this.testScriptableObject.BoolValue = false;
            this.testScriptableObject.StringValue = "SO Test String";

            // Create GameObject sheet definition
            this.gameObjectSheetDefinition = ScriptableObject.CreateInstance<SheetDefinition>();
            this.gameObjectSheetDefinition.Type = SheetType.GameObject;

            // Create ScriptableObject sheet definition
            this.scriptableObjectSheetDefinition = ScriptableObject.CreateInstance<SheetDefinition>();
            this.scriptableObjectSheetDefinition.Type = SheetType.ScriptableObject;
        }

        [TearDown]
        public void TearDown()
        {
            if (this.testGameObject != null)
            {
                Object.DestroyImmediate(this.testGameObject);
            }

            if (this.testScriptableObject != null)
            {
                Object.DestroyImmediate(this.testScriptableObject);
            }

            if (this.gameObjectSheetDefinition != null)
            {
                Object.DestroyImmediate(this.gameObjectSheetDefinition);
            }

            if (this.scriptableObjectSheetDefinition != null)
            {
                Object.DestroyImmediate(this.scriptableObjectSheetDefinition);
            }
        }

        #region Float Property Tests

        [Test]
        public void PropertyColumn_GameObject_FloatProperty_GetValue_ReturnsCorrectValue()
        {
            var columnDef = this.CreateFloatPropertyColumn();
            var handler = new PropertyColumnHandler(columnDef);

            var result = handler.GetValue(this.testGameObject);

            Assert.AreEqual(5.5f, result);
        }

        [Test]
        public void PropertyColumn_GameObject_FloatProperty_SetValue_UpdatesProperty()
        {
            var columnDef = this.CreateFloatPropertyColumn();
            var handler = new PropertyColumnHandler(columnDef);

            var success = handler.SetValue(this.testGameObject, 15.75f);

            Assert.IsTrue(success);
            Assert.AreEqual(15.75f, this.testComponent.FloatValue);
        }

        [Test]
        public void PropertyColumn_ScriptableObject_FloatProperty_GetValue_ReturnsCorrectValue()
        {
            var columnDef = this.CreateScriptableObjectFloatColumn();
            var handler = new PropertyColumnHandler(columnDef);

            var result = handler.GetValue(this.testScriptableObject);

            Assert.AreEqual(10.5f, result);
        }

        [Test]
        public void PropertyColumn_ScriptableObject_FloatProperty_SetValue_UpdatesProperty()
        {
            var columnDef = this.CreateScriptableObjectFloatColumn();
            var handler = new PropertyColumnHandler(columnDef);

            var success = handler.SetValue(this.testScriptableObject, 25.25f);

            Assert.IsTrue(success);
            Assert.AreEqual(25.25f, this.testScriptableObject.FloatValue);
        }

        #endregion

        #region Integer Property Tests

        [Test]
        public void PropertyColumn_GameObject_IntProperty_GetValue_ReturnsCorrectValue()
        {
            var columnDef = this.CreateIntPropertyColumn();
            var handler = new PropertyColumnHandler(columnDef);

            var result = handler.GetValue(this.testGameObject);

            Assert.AreEqual(42, result);
        }

        [Test]
        public void PropertyColumn_GameObject_IntProperty_SetValue_UpdatesProperty()
        {
            var columnDef = this.CreateIntPropertyColumn();
            var handler = new PropertyColumnHandler(columnDef);

            var success = handler.SetValue(this.testGameObject, 100);

            Assert.IsTrue(success);
            Assert.AreEqual(100, this.testComponent.IntValue);
        }

        [Test]
        public void PropertyColumn_ScriptableObject_IntProperty_GetValue_ReturnsCorrectValue()
        {
            var columnDef = this.CreateScriptableObjectIntColumn();
            var handler = new PropertyColumnHandler(columnDef);

            var result = handler.GetValue(this.testScriptableObject);

            Assert.AreEqual(84, result);
        }

        [Test]
        public void PropertyColumn_ScriptableObject_IntProperty_SetValue_UpdatesProperty()
        {
            var columnDef = this.CreateScriptableObjectIntColumn();
            var handler = new PropertyColumnHandler(columnDef);

            var success = handler.SetValue(this.testScriptableObject, 200);

            Assert.IsTrue(success);
            Assert.AreEqual(200, this.testScriptableObject.IntValue);
        }

        #endregion

        #region Boolean Property Tests

        [Test]
        public void PropertyColumn_GameObject_BoolProperty_GetValue_ReturnsCorrectValue()
        {
            var columnDef = this.CreateBoolPropertyColumn();
            var handler = new PropertyColumnHandler(columnDef);

            var result = handler.GetValue(this.testGameObject);

            Assert.AreEqual(true, result);
        }

        [Test]
        public void PropertyColumn_GameObject_BoolProperty_SetValue_UpdatesProperty()
        {
            var columnDef = this.CreateBoolPropertyColumn();
            var handler = new PropertyColumnHandler(columnDef);

            var success = handler.SetValue(this.testGameObject, false);

            Assert.IsTrue(success);
            Assert.AreEqual(false, this.testComponent.BoolValue);
        }

        [Test]
        public void PropertyColumn_ScriptableObject_BoolProperty_GetValue_ReturnsCorrectValue()
        {
            var columnDef = this.CreateScriptableObjectBoolColumn();
            var handler = new PropertyColumnHandler(columnDef);

            var result = handler.GetValue(this.testScriptableObject);

            Assert.AreEqual(false, result);
        }

        [Test]
        public void PropertyColumn_ScriptableObject_BoolProperty_SetValue_UpdatesProperty()
        {
            var columnDef = this.CreateScriptableObjectBoolColumn();
            var handler = new PropertyColumnHandler(columnDef);

            var success = handler.SetValue(this.testScriptableObject, true);

            Assert.IsTrue(success);
            Assert.AreEqual(true, this.testScriptableObject.BoolValue);
        }

        #endregion

        #region String Property Tests

        [Test]
        public void PropertyColumn_GameObject_StringProperty_GetValue_ReturnsCorrectValue()
        {
            var columnDef = this.CreateStringPropertyColumn();
            var handler = new PropertyColumnHandler(columnDef);

            var result = handler.GetValue(this.testGameObject);

            Assert.AreEqual("Test String", result);
        }

        [Test]
        public void PropertyColumn_GameObject_StringProperty_SetValue_UpdatesProperty()
        {
            var columnDef = this.CreateStringPropertyColumn();
            var handler = new PropertyColumnHandler(columnDef);

            var success = handler.SetValue(this.testGameObject, "Updated String");

            Assert.IsTrue(success);
            Assert.AreEqual("Updated String", this.testComponent.StringValue);
        }

        [Test]
        public void PropertyColumn_ScriptableObject_StringProperty_GetValue_ReturnsCorrectValue()
        {
            var columnDef = this.CreateScriptableObjectStringColumn();
            var handler = new PropertyColumnHandler(columnDef);

            var result = handler.GetValue(this.testScriptableObject);

            Assert.AreEqual("SO Test String", result);
        }

        [Test]
        public void PropertyColumn_ScriptableObject_StringProperty_SetValue_UpdatesProperty()
        {
            var columnDef = this.CreateScriptableObjectStringColumn();
            var handler = new PropertyColumnHandler(columnDef);

            var success = handler.SetValue(this.testScriptableObject, "Updated SO String");

            Assert.IsTrue(success);
            Assert.AreEqual("Updated SO String", this.testScriptableObject.StringValue);
        }

        #endregion

        #region Error Handling Tests

        [Test]
        public void PropertyColumn_GetValue_WithNullObject_ThrowsException()
        {
            var columnDef = this.CreateFloatPropertyColumn();
            var handler = new PropertyColumnHandler(columnDef);

            // PropertyColumnHandler doesn't handle null objects gracefully - it throws NullReferenceException
            Assert.Throws<System.NullReferenceException>(() => handler.GetValue(null!));
        }

        [Test]
        public void PropertyColumn_SetValue_WithNullObject_ThrowsException()
        {
            var columnDef = this.CreateFloatPropertyColumn();
            var handler = new PropertyColumnHandler(columnDef);

            // PropertyColumnHandler doesn't handle null objects gracefully - it throws NullReferenceException  
            Assert.Throws<System.NullReferenceException>(() => handler.SetValue(null!, 5.0f));
        }

        [Test]
        public void PropertyColumn_GetValue_WithWrongObjectType_ReturnsNull()
        {
            var columnDef = this.CreateComponentOnlyPropertyColumn();
            var handler = new PropertyColumnHandler(columnDef);

            // Try to get value from ScriptableObject using GameObject column definition
            // ScriptableObject doesn't have ComponentOnlyProperty
            var result = handler.GetValue(this.testScriptableObject);

            Assert.IsNull(result);
        }

        [Test]
        public void PropertyColumn_SetValue_WithWrongObjectType_ReturnsFalse()
        {
            var columnDef = this.CreateComponentOnlyPropertyColumn();
            var handler = new PropertyColumnHandler(columnDef);

            // Try to set value on ScriptableObject using GameObject column definition  
            // ScriptableObject doesn't have ComponentOnlyProperty
            var success = handler.SetValue(this.testScriptableObject, "test");

            Assert.IsFalse(success);
        }

        [Test]
        public void PropertyColumn_GetValue_WithInvalidPropertyPath_ReturnsNull()
        {
            var columnDef = new ColumnDefinition
            {
                UserDisplayName = "Invalid Property",
                Type = ColumnType.Property,
                PropertyPath = "NonExistentProperty",
                TargetTypeName = typeof(TestComponent).AssemblyQualifiedName!,
            };

            var handler = new PropertyColumnHandler(columnDef);
            var result = handler.GetValue(this.testGameObject);

            Assert.IsNull(result);
        }

        [Test]
        public void PropertyColumn_SetValue_WithInvalidPropertyPath_ReturnsFalse()
        {
            var columnDef = new ColumnDefinition
            {
                UserDisplayName = "Invalid Property",
                Type = ColumnType.Property,
                PropertyPath = "NonExistentProperty",
                TargetTypeName = typeof(TestComponent).AssemblyQualifiedName!,
            };

            var handler = new PropertyColumnHandler(columnDef);
            var success = handler.SetValue(this.testGameObject, 5.0f);

            Assert.IsFalse(success);
        }

        #endregion

        #region Type Conversion Tests

        [Test]
        public void PropertyColumn_SetValue_IntToFloat_PerformsConversion()
        {
            var columnDef = this.CreateFloatPropertyColumn();
            var handler = new PropertyColumnHandler(columnDef);

            var success = handler.SetValue(this.testGameObject, 10); // Int value

            Assert.IsTrue(success);
            Assert.AreEqual(10.0f, this.testComponent.FloatValue);
        }

        [Test]
        public void PropertyColumn_SetValue_FloatToInt_PerformsConversion()
        {
            var columnDef = this.CreateIntPropertyColumn();
            var handler = new PropertyColumnHandler(columnDef);

            var success = handler.SetValue(this.testGameObject, 15.75f); // Float value

            Assert.IsTrue(success);
            Assert.AreEqual(16, this.testComponent.IntValue); // Unity rounds instead of truncating
        }

        [Test]
        public void PropertyColumn_SetValue_StringToBool_PerformsConversion()
        {
            var columnDef = this.CreateBoolPropertyColumn();
            var handler = new PropertyColumnHandler(columnDef);

            var success1 = handler.SetValue(this.testGameObject, "true");
            Assert.IsTrue(success1);
            Assert.IsTrue(this.testComponent.BoolValue);

            var success2 = handler.SetValue(this.testGameObject, "false");
            Assert.IsTrue(success2);
            Assert.IsFalse(this.testComponent.BoolValue);
        }

        [Test]
        public void PropertyColumn_SetValue_IncompatibleType_LogsErrorAndReturnsFalse()
        {
            var columnDef = this.CreateIntPropertyColumn();
            var handler = new PropertyColumnHandler(columnDef);

            // PropertyColumnHandler logs an error and returns false for invalid conversions
            UnityEngine.TestTools.LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("Error setting property IntValue"));
            
            var success = handler.SetValue(this.testGameObject, "not a number");

            Assert.IsFalse(success); // PropertyColumnHandler returns false on conversion errors
        }

        #endregion

        #region Property Change Event Tests

        [Test]
        public void PropertyColumn_PropertyChangeEvent_TriggersWhenPropertyChanges()
        {
            var columnDef = this.CreateFloatPropertyColumn();
            var handler = new PropertyColumnHandler(columnDef);

            bool eventTriggered = false;
            string? changedPropertyPath = null;

            handler.PropertyChanged += (propertyPath) =>
            {
                eventTriggered = true;
                changedPropertyPath = propertyPath;
            };

            handler.RegisterObject(this.testGameObject);
            handler.SetValue(this.testGameObject, 99.9f);

            Assert.IsTrue(eventTriggered);
            Assert.AreEqual("FloatValue", changedPropertyPath);
        }

        [Test]
        public void PropertyColumn_MultipleObjects_EventTriggersForCorrectProperty()
        {
            var columnDef = this.CreateFloatPropertyColumn();
            var handler = new PropertyColumnHandler(columnDef);

            var secondGameObject = new GameObject("SecondGameObject");
            var secondComponent = secondGameObject.AddComponent<TestComponent>();

            try
            {
                int eventCount = 0;
                handler.PropertyChanged += (_) => eventCount++;

                handler.RegisterObject(this.testGameObject);
                handler.RegisterObject(secondGameObject);

                // Change both objects
                handler.SetValue(this.testGameObject, 100.0f);
                handler.SetValue(secondGameObject, 200.0f);

                Assert.AreEqual(2, eventCount);
            }
            finally
            {
                Object.DestroyImmediate(secondGameObject);
            }
        }

        #endregion

        #region Helper Methods

        private ColumnDefinition CreateFloatPropertyColumn()
        {
            return new ColumnDefinition
            {
                UserDisplayName = "Float Value",
                Type = ColumnType.Property,
                PropertyPath = "FloatValue",
                TargetTypeName = typeof(TestComponent).AssemblyQualifiedName!,
            };
        }

        private ColumnDefinition CreateIntPropertyColumn()
        {
            return new ColumnDefinition
            {
                UserDisplayName = "Int Value",
                Type = ColumnType.Property,
                PropertyPath = "IntValue",
                TargetTypeName = typeof(TestComponent).AssemblyQualifiedName!,
            };
        }

        private ColumnDefinition CreateBoolPropertyColumn()
        {
            return new ColumnDefinition
            {
                UserDisplayName = "Bool Value",
                Type = ColumnType.Property,
                PropertyPath = "BoolValue",
                TargetTypeName = typeof(TestComponent).AssemblyQualifiedName!,
            };
        }

        private ColumnDefinition CreateStringPropertyColumn()
        {
            return new ColumnDefinition
            {
                UserDisplayName = "String Value",
                Type = ColumnType.Property,
                PropertyPath = "StringValue",
                TargetTypeName = typeof(TestComponent).AssemblyQualifiedName!,
            };
        }

        private ColumnDefinition CreateScriptableObjectFloatColumn()
        {
            return new ColumnDefinition
            {
                UserDisplayName = "SO Float Value",
                Type = ColumnType.Property,
                PropertyPath = "FloatValue",
                TargetTypeName = typeof(TestScriptableObject).AssemblyQualifiedName!,
            };
        }

        private ColumnDefinition CreateScriptableObjectIntColumn()
        {
            return new ColumnDefinition
            {
                UserDisplayName = "SO Int Value",
                Type = ColumnType.Property,
                PropertyPath = "IntValue",
                TargetTypeName = typeof(TestScriptableObject).AssemblyQualifiedName!,
            };
        }

        private ColumnDefinition CreateScriptableObjectBoolColumn()
        {
            return new ColumnDefinition
            {
                UserDisplayName = "SO Bool Value",
                Type = ColumnType.Property,
                PropertyPath = "BoolValue",
                TargetTypeName = typeof(TestScriptableObject).AssemblyQualifiedName!,
            };
        }

        private ColumnDefinition CreateScriptableObjectStringColumn()
        {
            return new ColumnDefinition
            {
                UserDisplayName = "SO String Value",
                Type = ColumnType.Property,
                PropertyPath = "StringValue",
                TargetTypeName = typeof(TestScriptableObject).AssemblyQualifiedName!,
            };
        }

        private ColumnDefinition CreateComponentOnlyPropertyColumn()
        {
            return new ColumnDefinition
            {
                UserDisplayName = "Component Only Property",
                Type = ColumnType.Property,
                PropertyPath = "ComponentOnlyProperty",
                TargetTypeName = typeof(TestComponent).AssemblyQualifiedName!,
            };
        }

        #endregion

        #region Test Classes

        public class TestComponent : MonoBehaviour
        {
            public float FloatValue = 0f;
            public int IntValue = 0;
            public bool BoolValue = false;
            public string StringValue = string.Empty;
            public string ComponentOnlyProperty = "Component";
        }

        public class TestScriptableObject : ScriptableObject
        {
            public float FloatValue = 0f;
            public int IntValue = 0;
            public bool BoolValue = false;
            public string StringValue = string.Empty;
            public string ScriptableObjectOnlyProperty = "SO";
        }

        #endregion
    }
}