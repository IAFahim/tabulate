// <copyright file="PropertyValidationService.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Tabulate.Editor.Services
{
    using System;
    using System.Collections.Generic;
    using BovineLabs.Tabulate.Editor.Core;
    using UnityEditor;
    using UnityEngine;

    /// <summary>
    /// Centralized service for property path validation.
    /// Eliminates the need to create temporary PropertyColumnHandler objects for validation.
    /// </summary>
    public static class PropertyValidationService
    {
        /// <summary>
        /// Validates a property path against a target type.
        /// </summary>
        /// <param name="targetTypeName">The target type name.</param>
        /// <param name="propertyPath">The property path to validate.</param>
        /// <param name="sampleObject">Optional sample object for validation.</param>
        /// <returns>A validation result indicating if the property path is valid.</returns>
        public static ValidationResult ValidatePropertyPath(string targetTypeName, string propertyPath, UnityEngine.Object? sampleObject = null)
        {
            if (string.IsNullOrEmpty(propertyPath))
            {
                return ValidationResult.Failure("Property path is empty", "A property path is required to identify which property to access on the target type.", "Enter a valid property path using the property dropdown or by typing the path manually.");
            }

            if (string.IsNullOrEmpty(targetTypeName))
            {
                return ValidationResult.Failure("Target type name is empty", "A target type must be specified to determine which object or component contains the property.", "Use the target type dropdown to select an appropriate Unity component or ScriptableObject type.");
            }

            // First validate the target type can be resolved
            var targetType = TypeResolutionService.ResolveType(targetTypeName);
            if (targetType == null)
            {
                return ValidationResult.Failure($"Cannot resolve target type '{targetTypeName}'",
                    "The specified target type cannot be found in the currently loaded assemblies.",
                    "Ensure the type name is correct and the assembly containing the type is loaded. Try using the type selector to browse available types.");
            }

            // If we have a sample object, perform detailed validation
            if (sampleObject != null)
            {
                return ValidatePropertyPathWithSample(targetType, propertyPath, sampleObject);
            }

            // Basic validation - just check that the type exists
            return ValidationResult.Success();
        }

        /// <summary>
        /// Validates a property path using a sample object for detailed checking.
        /// </summary>
        private static ValidationResult ValidatePropertyPathWithSample(Type targetType, string propertyPath, UnityEngine.Object sampleObject)
        {
            // Get the actual target object
            var actualTarget = GetActualTargetObject(sampleObject, targetType);
            if (actualTarget == null)
            {
                if (sampleObject is GameObject && typeof(MonoBehaviour).IsAssignableFrom(targetType))
                {
                    return ValidationResult.Failure($"Sample object does not have required component of type '{targetType.Name}'",
                        "The sample GameObject does not contain the required component.",
                        "Ensure the sample object has the required component, or select a different target type.");
                }

                return ValidationResult.Failure($"Cannot access target type '{targetType.Name}' on sample object",
                    "The sample object is not compatible with the specified target type.", "Verify the target type matches the sample object type.");
            }

            // Check if property path exists on the target object
            using var serializedObject = new SerializedObject(actualTarget);
            var property = serializedObject.FindProperty(propertyPath);

            if (property == null)
            {
                return ValidationResult.Failure($"Property path '{propertyPath}' not found on type '{targetType.Name}'",
                    "The property path does not exist on the specified target type.",
                    "Use the property dropdown to select from available properties, or verify the property path is correct.");
            }

            return ValidationResult.Success();
        }

        /// <summary>
        /// Validates that a column doesn't duplicate the (target type, property path) combination of other columns.
        /// </summary>
        /// <param name="currentColumn">The column being validated.</param>
        /// <param name="allColumns">All columns in the sheet.</param>
        /// <returns>A validation result indicating if duplicates were found.</returns>
        public static ValidationResult ValidateDuplicateColumns(ColumnDefinition currentColumn, IReadOnlyList<ColumnDefinition> allColumns)
        {
            // Only check columns that can write to properties (Property and Formula columns)
            if (currentColumn.Type != ColumnType.Property && currentColumn.Type != ColumnType.Formula)
            {
                return ValidationResult.Success();
            }

            if (string.IsNullOrEmpty(currentColumn.TargetTypeName) || string.IsNullOrEmpty(currentColumn.PropertyPath))
            {
                return ValidationResult.Success(); // Other validation will catch missing values
            }

            // Check all other columns that can write to properties for the same (target type, property path) combination
            foreach (var otherColumn in allColumns)
            {
                if (otherColumn.ColumnId == currentColumn.ColumnId)
                {
                    continue; // Skip self
                }

                // Only check Property and Formula columns as they can write to properties
                if (otherColumn.Type != ColumnType.Property && otherColumn.Type != ColumnType.Formula)
                {
                    continue;
                }

                if (string.Equals(otherColumn.TargetTypeName, currentColumn.TargetTypeName, StringComparison.Ordinal) &&
                    string.Equals(otherColumn.PropertyPath, currentColumn.PropertyPath, StringComparison.Ordinal))
                {
                    var columnTypeText = currentColumn.Type == ColumnType.Property ? "property" : "formula";
                    var otherColumnTypeText = otherColumn.Type == ColumnType.Property ? "property" : "formula";
                    return ValidationResult.Failure(
                        $"Duplicate {columnTypeText} column detected: Another {otherColumnTypeText} column already writes to '{currentColumn.TargetTypeName}.{currentColumn.PropertyPath}'",
                        $"Column C{otherColumn.ColumnId} ({otherColumn.Type}) already uses the same target type and property path combination.",
                        "Choose a different property path or target type to avoid conflicts and potential loops.");
                }
            }

            return ValidationResult.Success();
        }

        /// <summary>
        /// Gets the actual target object for property access.
        /// If targetObject is a GameObject and targetType is a component, gets the component.
        /// </summary>
        private static UnityEngine.Object? GetActualTargetObject(UnityEngine.Object targetObject, Type targetType)
        {
            // If the target object is already the correct type, use it as-is
            if (targetType.IsAssignableFrom(targetObject.GetType()))
            {
                return targetObject;
            }

            // If target object is a GameObject and we need a component, try to get it
            if (targetObject is GameObject gameObject && typeof(MonoBehaviour).IsAssignableFrom(targetType))
            {
                var component = gameObject.GetComponent(targetType);
                if (component != null)
                {
                    return component;
                }
            }

            // No suitable target object found
            return null;
        }
    }
}