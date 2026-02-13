// <copyright file="VariableDefinitionTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Tabulate.Tests
{
    using BovineLabs.Tabulate.Editor.Core;
    using BovineLabs.Tabulate.Editor.Variables;
    using NUnit.Framework;

    public class VariableDefinitionTests
    {
        private VariableDefinition variable = null!;

        [SetUp]
        public void SetUp()
        {
            this.variable = new VariableDefinition();
        }

        // Boolean Data Type Tests
        [Test]
        public void GetParsedDataValue_BooleanTrue_ReturnsTrue()
        {
            this.variable.DataType = DataFieldType.Boolean;
            this.variable.DataValue = "true";

            var result = this.variable.GetParsedDataValue();
            Assert.AreEqual(true, result);
        }

        [Test]
        public void GetParsedDataValue_BooleanFalse_ReturnsFalse()
        {
            this.variable.DataType = DataFieldType.Boolean;
            this.variable.DataValue = "false";

            var result = this.variable.GetParsedDataValue();
            Assert.AreEqual(false, result);
        }

        [Test]
        public void GetParsedDataValue_BooleanTrue_CaseInsensitive()
        {
            this.variable.DataType = DataFieldType.Boolean;
            this.variable.DataValue = "True";

            var result = this.variable.GetParsedDataValue();
            Assert.AreEqual(true, result);
        }

        [Test]
        public void GetParsedDataValue_BooleanFalse_CaseInsensitive()
        {
            this.variable.DataType = DataFieldType.Boolean;
            this.variable.DataValue = "False";

            var result = this.variable.GetParsedDataValue();
            Assert.AreEqual(false, result);
        }

        [Test]
        public void GetParsedDataValue_BooleanEmpty_ReturnsFalse()
        {
            this.variable.DataType = DataFieldType.Boolean;
            this.variable.DataValue = string.Empty;

            var result = this.variable.GetParsedDataValue();
            Assert.AreEqual(false, result);
        }

        [Test]
        public void GetParsedDataValue_BooleanNull_ReturnsFalse()
        {
            this.variable.DataType = DataFieldType.Boolean;
            this.variable.DataValue = null!;

            var result = this.variable.GetParsedDataValue();
            Assert.AreEqual(false, result);
        }

        [Test]
        public void GetParsedDataValue_BooleanInvalidValue_ReturnsFalse()
        {
            this.variable.DataType = DataFieldType.Boolean;
            this.variable.DataValue = "invalid";

            var result = this.variable.GetParsedDataValue();
            Assert.AreEqual(false, result);
        }

        [Test]
        public void SetDataValue_BooleanTrue_StoresCorrectString()
        {
            this.variable.SetDataValue(true);
            Assert.AreEqual("True", this.variable.DataValue);
        }

        [Test]
        public void SetDataValue_BooleanFalse_StoresCorrectString()
        {
            this.variable.SetDataValue(false);
            Assert.AreEqual("False", this.variable.DataValue);
        }

        // Existing Data Type Tests (to ensure we didn't break anything)
        [Test]
        public void GetParsedDataValue_IntegerValue_ReturnsInteger()
        {
            this.variable.DataType = DataFieldType.Integer;
            this.variable.DataValue = "42";

            var result = this.variable.GetParsedDataValue();
            Assert.AreEqual(42, result);
        }

        [Test]
        public void GetParsedDataValue_FloatValue_ReturnsFloat()
        {
            this.variable.DataType = DataFieldType.Float;
            this.variable.DataValue = "3.14";

            var result = this.variable.GetParsedDataValue();
            Assert.AreEqual(3.14f, result);
        }

        [Test]
        public void GetParsedDataValue_IntegerEmpty_ReturnsZero()
        {
            this.variable.DataType = DataFieldType.Integer;
            this.variable.DataValue = string.Empty;

            var result = this.variable.GetParsedDataValue();
            Assert.AreEqual(0, result);
        }

        [Test]
        public void GetParsedDataValue_FloatEmpty_ReturnsZero()
        {
            this.variable.DataType = DataFieldType.Float;
            this.variable.DataValue = string.Empty;

            var result = this.variable.GetParsedDataValue();
            Assert.AreEqual(0f, result);
        }

        // EffectiveDisplayName Tests
        [Test]
        public void EffectiveDisplayName_BooleanDataType_ReturnsCorrectFormat()
        {
            this.variable.DataType = DataFieldType.Boolean;
            this.variable.DisplayName = string.Empty;

            var result = this.variable.EffectiveDisplayName;
            Assert.AreEqual("Data (Boolean)", result);
        }
    }
}