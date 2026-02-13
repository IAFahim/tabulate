// <copyright file="DataValidationService.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Tabulate.Editor.Services
{
    using System;
    using BovineLabs.Tabulate.Editor.Core;

    /// <summary>
    /// Centralized service for data value validation and parsing.
    /// Eliminates scattered data parsing logic across validation classes.
    /// </summary>
    public static class DataValidationService
    {
        /// <summary>
        /// Validates a data value against its expected data field type.
        /// </summary>
        /// <param name="dataFieldType">The expected data field type.</param>
        /// <param name="dataValue">The data value to validate.</param>
        /// <returns>A validation result indicating if the data value is valid for the type.</returns>
        public static ValidationResult ValidateDataValue(DataFieldType dataFieldType, string dataValue)
        {
            if (string.IsNullOrEmpty(dataValue))
            {
                return ValidationResult.Success(); // Empty values are generally allowed
            }

            try
            {
                switch (dataFieldType)
                {
                    case DataFieldType.Integer:
                        if (!int.TryParse(dataValue, out _))
                        {
                            return ValidationResult.Failure($"'{dataValue}' is not a valid integer", "The data value must be a whole number (integer).",
                                "Enter a valid integer value like 42 or -10.");
                        }

                        break;

                    case DataFieldType.Float:
                        if (!float.TryParse(dataValue, out _))
                        {
                            return ValidationResult.Failure($"'{dataValue}' is not a valid number",
                                "The data value must be a valid floating-point number.", "Enter a valid number like 3.14 or -2.5.");
                        }

                        break;

                    case DataFieldType.Boolean:
                        var lowerValue = dataValue.ToLowerInvariant();
                        if (lowerValue != "true" && lowerValue != "false" && lowerValue != "1" && lowerValue != "0")
                        {
                            return ValidationResult.Failure($"'{dataValue}' is not a valid boolean value",
                                "Boolean values must be true, false, 1, or 0 (case insensitive).", "Enter 'true' or 'false' (or '1'/'0').");
                        }

                        break;

                    default:
                        return ValidationResult.Failure($"Unknown data field type: {dataFieldType}", "The specified data field type is not supported by the system. This may indicate a corrupted configuration or unsupported package version.", "Ensure you are using a compatible version of the Tabulate package. If the issue persists, try recreating the column or report this as a bug.");
                }

                return ValidationResult.Success();
            }
            catch (Exception ex)
            {
                return ValidationResult.Failure($"Error validating data value: {ex.Message}", "An unexpected error occurred while parsing the data value. This may indicate invalid input or a system issue.", "Check the data value format and try again. If the issue persists, please report this as a bug.");
            }
        }
    }
}