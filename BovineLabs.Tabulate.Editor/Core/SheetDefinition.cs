// <copyright file="SheetDefinition.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Tabulate.Editor.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using BovineLabs.Tabulate.Editor.Variables;
    using UnityEditor;
    using UnityEngine;
    using Object = UnityEngine.Object;

    public enum SheetType
    {
        GameObject,
        ScriptableObject,
    }

    public enum AssetManagementMode
    {
        Manual,
        Automatic,
        Custom,
    }

    public class SheetDefinition : ScriptableObject
    {
        [HideInInspector]
        [SerializeField]
        private SheetType type;

        [HideInInspector]
        [SerializeField]
        private AssetManagementMode assetManagementMode = AssetManagementMode.Manual;

        [HideInInspector]
        [SerializeField]
        private string customSearchString = string.Empty;

        [HideInInspector]
        [SerializeField]
        private ColumnDefinition[] columns = Array.Empty<ColumnDefinition>();

        [HideInInspector]
        [SerializeField]
        private VariableDefinition[] variables = Array.Empty<VariableDefinition>();

        [HideInInspector]
        [SerializeField]
        private List<DataEntry> dataEntries = new();

        [HideInInspector]
        [SerializeField]
        private List<GameObject> managedGameObjects = new();

        [HideInInspector]
        [SerializeField]
        private List<ScriptableObject> managedScriptableObjects = new();

        // Fast lookup dictionary for runtime access (not serialized)
        private Dictionary<(Object, int), DataEntry>? dataEntriesLookup;

        public SheetType Type
        {
            get => this.type;
            internal set => this.type = value;
        }

        public AssetManagementMode AssetManagementMode
        {
            get => this.assetManagementMode;
            internal set => this.assetManagementMode = value;
        }

        public string CustomSearchString
        {
            get => this.customSearchString;
            internal set => this.customSearchString = value ?? string.Empty;
        }

        public ColumnDefinition[] Columns
        {
            get => this.columns;
            internal set => this.columns = value;
        }

        public VariableDefinition[] Variables
        {
            get => this.variables;
            internal set => this.variables = value;
        }

        public string[] GetUniqueTargetTypes()
        {
            var types = new HashSet<string>();

            // Add all unique column target types
            foreach (var column in this.columns)
            {
                if (!string.IsNullOrEmpty(column.TargetTypeName))
                {
                    types.Add(column.TargetTypeName);
                }
            }

            // Add all unique variable target types
            foreach (var variable in this.variables)
            {
                if (variable.Type == VariableType.Property && !string.IsNullOrEmpty(variable.TargetTypeName))
                {
                    types.Add(variable.TargetTypeName);
                }
            }

            return types.ToArray();
        }

        public string? GetDataValue(Object obj, int columnId)
        {
            this.EnsureLookupInitialized();
            if (this.dataEntriesLookup!.TryGetValue((obj, columnId), out var entry))
            {
                return entry.Value;
            }

            return null;
        }

        public void SetDataValue(Object obj, int columnId, string? value)
        {
            this.EnsureLookupInitialized();
            var key = (objectInstanceId: obj, columnId);
            if (string.IsNullOrEmpty(value))
            {
                this.dataEntriesLookup!.Remove(key);
            }
            else
            {
                if (this.dataEntriesLookup!.TryGetValue(key, out var entry))
                {
                    entry.Value = value!;
                }
                else
                {
                    var newEntry = new DataEntry
                    {
                        ObjectInstanceId = obj.ToString(),
                        ColumnId = columnId,
                        Value = value!,
                    };
                    this.dataEntriesLookup[key] = newEntry;
                }
            }
        }

        /// <summary>
        /// Gets all managed assets based on the sheet type.
        /// </summary>
        /// <returns>List of managed assets appropriate for this sheet type.</returns>
        public List<Object> GetManagedAssets()
        {
            return this.Type switch
            {
                SheetType.GameObject => this.managedGameObjects.Cast<Object>().ToList(),
                SheetType.ScriptableObject => this.managedScriptableObjects.Cast<Object>().ToList(),
                _ => new List<Object>(),
            };
        }

        /// <summary>
        /// Gets the count of managed assets.
        /// </summary>
        /// <returns>Number of managed assets for this sheet type.</returns>
        public int GetManagedAssetCount()
        {
            return this.Type switch
            {
                SheetType.GameObject => this.managedGameObjects.Count,
                SheetType.ScriptableObject => this.managedScriptableObjects.Count,
                _ => 0,
            };
        }

        /// <summary>
        /// Adds a GameObject to the managed assets list.
        /// </summary>
        /// <param name="gameObject">The GameObject to add.</param>
        /// <returns>True if the GameObject was added, false if it already exists or sheet type is incorrect.</returns>
        public bool AddGameObject(GameObject gameObject)
        {
            if (this.Type != SheetType.GameObject || gameObject == null || this.managedGameObjects.Contains(gameObject))
            {
                return false;
            }

            this.managedGameObjects.Add(gameObject);
            return true;
        }

        /// <summary>
        /// Adds a ScriptableObject to the managed assets list.
        /// </summary>
        /// <param name="scriptableObject">The ScriptableObject to add.</param>
        /// <returns>True if the ScriptableObject was added, false if it already exists or sheet type is incorrect.</returns>
        public bool AddScriptableObject(ScriptableObject scriptableObject)
        {
            if (this.Type != SheetType.ScriptableObject || scriptableObject == null || this.managedScriptableObjects.Contains(scriptableObject))
            {
                return false;
            }

            this.managedScriptableObjects.Add(scriptableObject);
            return true;
        }

        /// <summary>
        /// Clears all managed assets.
        /// </summary>
        public void ClearManagedAssets()
        {
            this.managedGameObjects.Clear();
            this.managedScriptableObjects.Clear();
        }

        /// <summary>
        /// Checks if there are any managed assets defined.
        /// </summary>
        /// <returns>True if there are managed assets, false otherwise.</returns>
        public bool HasManagedAssets()
        {
            return this.GetManagedAssetCount() > 0;
        }

        /// <summary>
        /// Syncs the lookup dictionary back to the serialized list.
        /// Called immediately after changes to ensure data consistency.
        /// </summary>
        public void SaveCachedData()
        {
            if (this.dataEntriesLookup != null)
            {
                this.dataEntries.Clear();
                this.dataEntries.AddRange(this.dataEntriesLookup.Values);
            }
        }

        /// <summary>
        /// Ensures the lookup dictionary is initialized from the serialized data.
        /// Uses lazy initialization to avoid performance costs during normal SO lifecycle.
        /// </summary>
        private void EnsureLookupInitialized()
        {
            if (this.dataEntriesLookup == null)
            {
                this.dataEntriesLookup = new Dictionary<(Object, int), DataEntry>();
                foreach (var entry in this.dataEntries)
                {
                    if (!GlobalObjectId.TryParse(entry.ObjectInstanceId, out var id))
                    {
                        var obj = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(id);
                        if (obj)
                        {
                            // Handle potential duplicates in old data, last one wins
                            this.dataEntriesLookup[(obj, entry.ColumnId)] = entry;
                        }
                    }
                }
            }
        }
    }
}