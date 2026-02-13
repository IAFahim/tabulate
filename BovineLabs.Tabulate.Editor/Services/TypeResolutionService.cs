// <copyright file="TypeResolutionService.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Tabulate.Editor.Services
{
    using System;
    using System.Collections.Generic;
    using BovineLabs.Tabulate.Editor.Core;

    /// <summary>
    /// Centralized service for resolving type names to Type objects.
    /// Eliminates duplicate type resolution implementations across validation classes.
    /// </summary>
    public static class TypeResolutionService
    {
        private static readonly Dictionary<string, Type> typeCache = new();

        /// <summary>
        /// Resolves a type name to a Type object.
        /// Uses caching for improved performance.
        /// </summary>
        /// <param name="typeName">The type name to resolve.</param>
        /// <returns>The resolved Type object or null if not found.</returns>
        public static Type? ResolveType(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
            {
                return null;
            }

            // Check cached types first
            if (typeCache.TryGetValue(typeName, out var cachedType))
            {
                return cachedType;
            }

            // Try direct Type.GetType first
            var type = Type.GetType(typeName);
            if (type != null)
            {
                typeCache[typeName] = type;
                return type;
            }

            return null;
        }

        /// <summary>
        /// Validates that a type name can be resolved to an actual type.
        /// </summary>
        /// <param name="typeName">The type name to validate.</param>
        /// <returns>A validation result indicating if the type can be resolved.</returns>
        public static ValidationResult ValidateTypeName(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
            {
                return ValidationResult.Failure("Type name is empty", "A type name must be provided to resolve the target type for validation.", "Specify a valid type name, preferably using the type selector to browse available types.");
            }

            var type = ResolveType(typeName);
            if (type == null)
            {
                return ValidationResult.Failure($"Cannot resolve type '{typeName}'", "The specified type cannot be found in the currently loaded assemblies.",
                    "Ensure the type name is correct and the assembly containing the type is loaded. Try using the type selector to browse available types.");
            }

            return ValidationResult.Success();
        }
    }
}