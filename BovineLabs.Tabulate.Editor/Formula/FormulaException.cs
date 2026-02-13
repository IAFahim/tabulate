// <copyright file="FormulaException.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Tabulate.Editor.Formula
{
    using System;

    public class FormulaException : Exception
    {
        public FormulaException(string message)
            : base(message)
        {
        }

        public FormulaException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
