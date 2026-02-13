// <copyright file="VariableDefinition.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Tabulate.Editor.Variables
{
    using System;
    using BovineLabs.Tabulate.Editor.Core;
    using UnityEngine;

    [Serializable]
    public class VariableDefinition : IEquatable<VariableDefinition>
    {
        [SerializeField]
        private int variableId;

        [SerializeField]
        private string displayName = string.Empty;

        [SerializeField]
        private VariableType type;

        // Data Variable fields
        [SerializeField]
        private string dataValue = string.Empty;

        [SerializeField]
        private DataFieldType dataType = DataFieldType.Float;

        // Property Variable fields
        [SerializeField]
        private string targetTypeName = string.Empty;

        [SerializeField]
        private string propertyPath = string.Empty;

        [SerializeField]
        private UnityEngine.Object? targetObject;

        // Formula Variable fields
        [SerializeField]
        private string formula = string.Empty;

        public int VariableId
        {
            get => this.variableId;
            set => this.variableId = value;
        }

        public string VariableReference => $"V{this.variableId}";

        public string DisplayName
        {
            get => this.displayName;
            set => this.displayName = value;
        }

        public string EffectiveDisplayName
        {
            get
            {
                if (!string.IsNullOrEmpty(this.displayName))
                {
                    return this.displayName;
                }

                // Fallback defaults for empty display names based on variable type
                return this.type switch
                {
                    VariableType.Data => $"Data ({this.dataType})",
                    VariableType.Property when !string.IsNullOrEmpty(this.targetTypeName) && !string.IsNullOrEmpty(this.propertyPath) =>
                        this.GetPropertyDisplayName(),
                    VariableType.Formula => "Formula Variable",
                    _ => $"Variable {this.VariableReference}",
                };
            }
        }

        public VariableType Type
        {
            get => this.type;
            set => this.type = value;
        }

        public string DataValue
        {
            get => this.dataValue;
            set => this.dataValue = value;
        }

        public DataFieldType DataType
        {
            get => this.dataType;
            set => this.dataType = value;
        }

        public string TargetTypeName
        {
            get => this.targetTypeName;
            set => this.targetTypeName = value;
        }

        public string PropertyPath
        {
            get => this.propertyPath;
            set => this.propertyPath = value;
        }

        public UnityEngine.Object? TargetObject
        {
            get => this.targetObject;
            set => this.targetObject = value;
        }

        public string Formula
        {
            get => this.formula;
            set => this.formula = value;
        }

        /// <summary>
        /// Gets the parsed data value as the appropriate type.
        /// </summary>
        /// <returns>The parsed value or default if parsing fails.</returns>
        public object GetParsedDataValue()
        {
            if (string.IsNullOrEmpty(this.dataValue))
            {
                return this.dataType switch
                {
                    DataFieldType.Integer => 0,
                    DataFieldType.Float => 0f,
                    DataFieldType.Boolean => false,
                    _ => 0f,
                };
            }

            return this.dataType switch
            {
                DataFieldType.Integer => int.TryParse(this.dataValue, out var intValue) ? intValue : 0,
                DataFieldType.Float => float.TryParse(this.dataValue, out var floatValue) ? floatValue : 0f,
                DataFieldType.Boolean => bool.TryParse(this.dataValue, out var boolValue) && boolValue,
                _ => this.dataValue,
            };
        }

        /// <summary>
        /// Sets the data value from an object, converting to string representation.
        /// </summary>
        /// <param name="value">The value to set.</param>
        public void SetDataValue(object value)
        {
            this.dataValue = value.ToString();
        }

        /// <summary>
        /// Validates the variable definition for the current type.
        /// </summary>
        /// <returns>True if valid, false otherwise.</returns>
        public bool IsValid()
        {
            return this.type switch
            {
                VariableType.Data => !string.IsNullOrEmpty(this.dataValue),
                VariableType.Property => !string.IsNullOrEmpty(this.targetTypeName) && !string.IsNullOrEmpty(this.propertyPath),
                VariableType.Formula => !string.IsNullOrEmpty(this.formula),
                _ => false,
            };
        }

        private string GetPropertyDisplayName()
        {
            var targetType = System.Type.GetType(this.targetTypeName);
            var typeName = targetType?.Name ?? "Unknown";

            if (this.targetObject != null)
            {
                return $"{this.targetObject.name}.{this.propertyPath}";
            }

            return $"{typeName}.{this.propertyPath}";
        }

        public bool Equals(VariableDefinition? other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return this.variableId == other.variableId && this.displayName == other.displayName && this.type == other.type && this.dataValue == other.dataValue && this.dataType == other.dataType && this.targetTypeName == other.targetTypeName && this.propertyPath == other.propertyPath && Equals(this.targetObject, other.targetObject) && this.formula == other.formula;
        }
    }

    public enum VariableType
    {
        Data,        // Static values (numbers, strings, booleans)
        Property,    // References to GameObject/ScriptableObject properties
        Formula,     // Calculated expressions (can reference other variables and columns)
    }
}