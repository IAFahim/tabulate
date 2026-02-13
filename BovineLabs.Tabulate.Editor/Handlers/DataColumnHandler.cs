// <copyright file="DataColumnHandler.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Tabulate.Editor.Handlers
{
    using System;
    using BovineLabs.Tabulate.Editor.Core;
    using UnityEditor;
    using UnityEngine;
    using Object = UnityEngine.Object;

    public class DataColumnHandler
    {
        private readonly ColumnDefinition columnDefinition;

        public DataColumnHandler(ColumnDefinition columnDefinition)
        {
            this.columnDefinition = columnDefinition ?? throw new ArgumentNullException(nameof(columnDefinition));
        }

        public string? GetValue(Object obj, SheetDefinition? sheetDefinition)
        {
            if (sheetDefinition == null)
            {
                return null;
            }

            return sheetDefinition.GetDataValue(obj, this.columnDefinition.ColumnId);
        }

        public bool SetValue(Object obj, SheetDefinition? sheetDefinition, string? value)
        {
            if (sheetDefinition == null)
            {
                return false;
            }

            try
            {
                sheetDefinition.SetDataValue(obj, this.columnDefinition.ColumnId, value);

                // Mark the SheetDefinition as dirty so Unity saves it
                EditorUtility.SetDirty(sheetDefinition);

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error setting data value for column '{this.columnDefinition.EffectiveDisplayName}': {ex.Message}");
                return false;
            }
        }
    }
}