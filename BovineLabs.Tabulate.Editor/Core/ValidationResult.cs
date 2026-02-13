// <copyright file="ValidationResult.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Tabulate.Editor.Core
{
    using System;

    public class ValidationResult
    {
        private ValidationResult(bool isValid, string errorMessage, string detailedDescription, string suggestion)
        {
            this.IsValid = isValid;
            this.ErrorMessage = errorMessage;
            this.DetailedDescription = detailedDescription;
            this.Suggestion = suggestion;
        }

        public bool IsValid { get; }

        public string ErrorMessage { get; }

        public string DetailedDescription { get; }

        public string Suggestion { get; }

        public static ValidationResult Success()
        {
            return new ValidationResult(true, string.Empty, string.Empty, string.Empty);
        }

        public static ValidationResult Failure(string errorMessage, string detailedDescription, string suggestion)
        {
            if (string.IsNullOrEmpty(errorMessage))
            {
                throw new ArgumentException("Error message cannot be null or empty", nameof(errorMessage));
            }

            return new ValidationResult(false, errorMessage, detailedDescription, suggestion);
        }

        public override string ToString()
        {
            return this.IsValid ? "Valid" : $"Invalid: {this.ErrorMessage}";
        }
    }
}