// <copyright file="AssetPopulationHelper.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Tabulate.Editor.Utilities
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using BovineLabs.Tabulate.Editor.Core;
    using UnityEditor.Search;
    using UnityEngine;
    using Object = UnityEngine.Object;

    /// <summary>
    /// Helper utilities for populating managed asset lists in sheet definitions.
    /// </summary>
    public static class AssetPopulationHelper
    {
        /// <summary>
        /// Populates assets from current discovery rules based on sheet configuration.
        /// </summary>
        /// <param name="sheetDefinition">The sheet definition to populate.</param>
        /// <returns>Array of assets found using current rules.</returns>
        public static Object[] PopulateFromCurrentRules(SheetDefinition sheetDefinition)
        {
            if (sheetDefinition == null)
            {
                return Array.Empty<Object>();
            }

            return sheetDefinition.AssetManagementMode switch
            {
                AssetManagementMode.Automatic => PopulateFromAutomatic(sheetDefinition),
                AssetManagementMode.Custom => PopulateFromCustomSearch(sheetDefinition),
                AssetManagementMode.Manual => Array.Empty<Object>(), // Manual mode doesn't auto-populate
                _ => Array.Empty<Object>(),
            };
        }

        private static Object[] PopulateFromAutomatic(SheetDefinition sheetDefinition)
        {
            if (sheetDefinition == null)
            {
                return Array.Empty<Object>();
            }

            // Safety check: Don't auto-discover if no columns are configured
            if (sheetDefinition.Columns.Length == 0)
            {
                return Array.Empty<Object>();
            }

            var uniqueTargetTypes = sheetDefinition.GetUniqueTargetTypes();
            if (uniqueTargetTypes.Length == 0)
            {
                return Array.Empty<Object>();
            }

            return sheetDefinition.Type switch
            {
                SheetType.GameObject => PopulateGameObjectsFromRules(sheetDefinition).Cast<Object>().ToArray(),
                SheetType.ScriptableObject => PopulateScriptableObjectsFromRules(sheetDefinition).Cast<Object>().ToArray(),
                _ => Array.Empty<Object>(),
            };
        }

        private static Object[] PopulateFromCustomSearch(SheetDefinition sheetDefinition)
        {
            if (sheetDefinition == null || string.IsNullOrWhiteSpace(sheetDefinition.CustomSearchString))
            {
                return Array.Empty<Object>();
            }

            return sheetDefinition.Type switch
            {
                SheetType.GameObject => FindGameObjectsWithCustomSearch(sheetDefinition.CustomSearchString).Cast<Object>().ToArray(),
                SheetType.ScriptableObject => FindScriptableObjectsWithCustomSearch(sheetDefinition.CustomSearchString).Cast<Object>().ToArray(),
                _ => Array.Empty<Object>(),
            };
        }

        /// <summary>
        /// Populates GameObjects using the sheet's current discovery rules.
        /// </summary>
        /// <param name="sheetDefinition">The sheet definition to use for discovery rules.</param>
        /// <returns>Array of GameObjects found.</returns>
        private static GameObject[] PopulateGameObjectsFromRules(SheetDefinition sheetDefinition)
        {
            if (sheetDefinition == null || sheetDefinition.Type != SheetType.GameObject)
            {
                return Array.Empty<GameObject>();
            }

            var uniqueTargetTypes = sheetDefinition.GetUniqueTargetTypes();
            if (uniqueTargetTypes.Length == 0)
            {
                return Array.Empty<GameObject>();
            }

            var validTypes = uniqueTargetTypes.Select(Type.GetType).Where(t => t != null && typeof(MonoBehaviour).IsAssignableFrom(t));
            var gameObjectsWithComponents = FindGameObjectsWithComponent(validTypes);
            return gameObjectsWithComponents.ToArray();
        }

        /// <summary>
        /// Populates ScriptableObjects using the sheet's current discovery rules.
        /// </summary>
        /// <param name="sheetDefinition">The sheet definition to use for discovery rules.</param>
        /// <returns>Array of ScriptableObjects found.</returns>
        private static ScriptableObject[] PopulateScriptableObjectsFromRules(SheetDefinition sheetDefinition)
        {
            if (sheetDefinition == null || sheetDefinition.Type != SheetType.ScriptableObject)
            {
                return Array.Empty<ScriptableObject>();
            }

            var uniqueTargetTypes = sheetDefinition.GetUniqueTargetTypes();
            if (uniqueTargetTypes.Length == 0)
            {
                return Array.Empty<ScriptableObject>();
            }

            var allScriptableObjects = new HashSet<ScriptableObject>();

            foreach (var targetTypeName in uniqueTargetTypes)
            {
                var targetType = Type.GetType(targetTypeName);
                if (targetType != null && typeof(ScriptableObject).IsAssignableFrom(targetType))
                {
                    var scriptableObjectsOfType = FindScriptableObjectsOfType(targetType);
                    foreach (var scriptableObject in scriptableObjectsOfType)
                    {
                        allScriptableObjects.Add(scriptableObject);
                    }
                }
            }

            return allScriptableObjects.ToArray();
        }

        private static GameObject[] FindGameObjectsWithComponent(IEnumerable<Type> componentTypes)
        {
            var searchString = string.Join(' ', componentTypes.Select(c => $"t:{c.Name}"));
            return FindGameObjectsWithCustomSearch(searchString);
        }

        private static ScriptableObject[] FindScriptableObjectsOfType(Type scriptableObjectType)
        {
            var searchQuery = $"t:{scriptableObjectType.Name}";
            return FindScriptableObjectsWithCustomSearch(searchQuery);
        }

        /// <summary>
        /// Finds GameObjects using a custom search string with the Unity Search Service.
        /// </summary>
        /// <param name="customSearchString">The custom search string to use.</param>
        /// <returns>Array of GameObjects found using the custom search.</returns>
        private static GameObject[] FindGameObjectsWithCustomSearch(string customSearchString)
        {
            if (string.IsNullOrWhiteSpace(customSearchString))
            {
                return Array.Empty<GameObject>();
            }

            var gameObjects = new List<GameObject>();
            var searchContext = SearchService.CreateContext("asset", "+noResultsLimit " + customSearchString, SearchFlags.Synchronous);
            var searchResults = SearchService.Request(searchContext).ToList();

            foreach (var result in searchResults)
            {
                if (result.ToObject() is GameObject gameObject)
                {
                    gameObjects.Add(gameObject);
                }
            }

            return gameObjects.ToArray();
        }

        /// <summary>
        /// Finds ScriptableObjects using a custom search string with the Unity Search Service.
        /// </summary>
        /// <param name="customSearchString">The custom search string to use.</param>
        /// <returns>Array of ScriptableObjects found using the custom search.</returns>
        private static ScriptableObject[] FindScriptableObjectsWithCustomSearch(string customSearchString)
        {
            if (string.IsNullOrWhiteSpace(customSearchString))
            {
                return Array.Empty<ScriptableObject>();
            }

            var scriptableObjects = new List<ScriptableObject>();
            var searchContext = SearchService.CreateContext("asset", "+noResultsLimit " + customSearchString, SearchFlags.Synchronous);
            var searchResults = SearchService.Request(searchContext).ToList();

            foreach (var result in searchResults)
            {
                if (result.ToObject() is ScriptableObject scriptableObject)
                {
                    scriptableObjects.Add(scriptableObject);
                }
            }

            return scriptableObjects.ToArray();
        }
    }
}