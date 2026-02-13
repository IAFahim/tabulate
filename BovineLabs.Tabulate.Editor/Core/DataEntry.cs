// <copyright file="DataEntry.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Tabulate.Editor.Core
{
    using System;
    using UnityEngine;

    [Serializable]
    public class DataEntry
    {
        [SerializeField]
        private string objectInstanceId = string.Empty;
        [SerializeField]
        private int columnId = -1;
        [SerializeField]
        private string value = string.Empty;

        public string ObjectInstanceId
        {
            get => this.objectInstanceId;
            set => this.objectInstanceId = value;
        }

        public int ColumnId
        {
            get => this.columnId;
            set => this.columnId = value;
        }

        public string Value
        {
            get => this.value;
            set => this.value = value;
        }
    }
}
