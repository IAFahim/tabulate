// <copyright file="SheetRowData.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Tabulate.Editor.UI
{
    using UnityEngine;

    public class SheetRowData
    {
        public SheetRowData(Object targetObject)
        {
            this.TargetObject = targetObject;
            this.ObjectName = targetObject.name;
        }

        public Object TargetObject { get; }

        public string ObjectName { get; }
    }
}
