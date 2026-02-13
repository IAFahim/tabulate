// <copyright file="ColumnDefinition.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Tabulate.Editor.Core
{
    using System;
    using UnityEngine;

    [Serializable]
    public class ColumnDefinition : IEquatable<ColumnDefinition>
    {
        [SerializeField]
        private int columnId;

        [SerializeField]
        private string userDisplayName = string.Empty;

        [SerializeField]
        private ColumnType type;

        [SerializeField]
        private string propertyPath = string.Empty;

        [SerializeField]
        private string formula = string.Empty;

        [SerializeField]
        private bool disableInspector;

        [SerializeField]
        private bool isReadOnly;

        [SerializeField]
        private string targetTypeName = string.Empty;

        [SerializeField]
        private DataFieldType dataFieldType = DataFieldType.Integer;

        [SerializeField]
        private string dataValue = string.Empty;

        [SerializeField]
        private float minValue;

        [SerializeField]
        private float maxValue = 1f;

        [SerializeField]
        private bool useSlider;

        [SerializeField]
        private float columnWidth = 120f;

        public int ColumnId
        {
            get => this.columnId;
            set => this.columnId = value;
        }

        public string FormulaReference => $"C{this.columnId}";

        public string UserDisplayName
        {
            get => this.userDisplayName;
            set => this.userDisplayName = value;
        }

        public string EffectiveDisplayName
        {
            get
            {
                string baseName;

                if (!string.IsNullOrEmpty(this.userDisplayName))
                {
                    baseName = this.userDisplayName;
                }
                else
                {
                    baseName = this.type switch
                    {
                        ColumnType.Property => this.GetPropertyDisplayName(),
                        ColumnType.Formula => "Formula",
                        ColumnType.Data => "Value",
                        _ => "Unknown Column",
                    };
                }

                return $"[C{this.columnId}] {baseName}";
            }
        }

        public ColumnType Type
        {
            get => this.type;
            set => this.type = value;
        }

        public string PropertyPath
        {
            get => this.propertyPath;
            set => this.propertyPath = value;
        }

        public string Formula
        {
            get => this.formula;
            set => this.formula = value;
        }

        public bool DisableInspector
        {
            get => this.disableInspector;
            set => this.disableInspector = value;
        }

        public bool IsReadOnly
        {
            get => this.isReadOnly;
            set => this.isReadOnly = value;
        }

        public string TargetTypeName
        {
            get => this.targetTypeName;
            set => this.targetTypeName = value;
        }

        public DataFieldType DataFieldType
        {
            get => this.dataFieldType;
            set => this.dataFieldType = value;
        }

        public string DataValue
        {
            get => this.dataValue;
            set => this.dataValue = value;
        }

        public float MinValue
        {
            get => this.minValue;
            set => this.minValue = value;
        }

        public float MaxValue
        {
            get => this.maxValue;
            set => this.maxValue = value;
        }

        public bool UseSlider
        {
            get => this.useSlider;
            set => this.useSlider = value;
        }

        public float ColumnWidth
        {
            get => this.columnWidth;
            set => this.columnWidth = value;
        }

        private string GetPropertyDisplayName()
        {
            return !string.IsNullOrWhiteSpace(this.propertyPath) ? this.propertyPath : "Unknown Property";
        }

        public bool Equals(ColumnDefinition? other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return this.columnId == other.columnId && this.userDisplayName == other.userDisplayName && this.type == other.type &&
                this.propertyPath == other.propertyPath && this.formula == other.formula && this.disableInspector == other.disableInspector &&
                this.isReadOnly == other.isReadOnly && this.targetTypeName == other.targetTypeName && this.dataFieldType == other.dataFieldType &&
                this.dataValue == other.dataValue && this.minValue.Equals(other.minValue) && this.maxValue.Equals(other.maxValue) &&
                this.useSlider == other.useSlider && this.columnWidth.Equals(other.columnWidth);
        }
    }

    public enum ColumnType
    {
        Property,
        Formula,
        Data,
    }

    public enum DataFieldType
    {
        Integer,
        Float,
        Boolean,
    }
}