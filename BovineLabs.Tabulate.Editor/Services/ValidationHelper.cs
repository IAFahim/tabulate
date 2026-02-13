// <copyright file="ValidationHelper.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Tabulate.Editor.Services
{
    using System;
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEngine;
    using Object = UnityEngine.Object;

    /// <summary>
    /// Helper class for managing persistent validation objects to avoid constantly creating and destroying temporary GameObjects.
    /// </summary>
    internal static class ValidationHelper
    {
        private static readonly Dictionary<Type, ScriptableObject> ScriptableObjectCache = new();
        private static readonly Dictionary<Type, Component> ComponentCache = new();

        private static GameObject? validationGameObject;

        static ValidationHelper()
        {
            AssemblyReloadEvents.beforeAssemblyReload += Cleanup;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        public static Object? GetValidationObject(Type targetType)
        {
            if (typeof(MonoBehaviour).IsAssignableFrom(targetType))
            {
                // Use persistent validation helper to get component
                return GetValidationComponent(targetType);
            }

            // For ScriptableObjects, use validation helper
            if (typeof(ScriptableObject).IsAssignableFrom(targetType))
            {
                return GetValidationScriptableObject(targetType);
            }

            return null;
        }

        public static IEnumerable<string> GetAvailableSerializedProperties(Type type)
        {
            var obj = GetValidationObject(type);
            if (obj == null)
            {
                return Array.Empty<string>();
            }

            var so = new SerializedObject(obj);
            var result = new List<string>();

            var prop = so.GetIterator();
            if (prop.NextVisible(true))
            {
                do
                {
                    if (prop.propertyPath != "m_Script")
                    {
                        result.Add(prop.propertyPath);
                    }
                }
                while (prop.NextVisible(false));
            }

            return result;
        }

        private static GameObject GetValidationGameObject()
        {
            if (!validationGameObject)
            {
                ComponentCache.Clear();
                validationGameObject = new("TabulateValidationHelper") { hideFlags = HideFlags.DontSave };
            }

            return validationGameObject;
        }

        private static Component GetValidationComponent(Type componentType)
        {
            // Need to do this before the cache to make sure it's valid
            var validationObject = GetValidationGameObject();

            if (!ComponentCache.TryGetValue(componentType, out var component) || !component)
            {
                component = validationObject.AddComponent(componentType);
                ComponentCache[componentType] = component;
            }

            return component;
        }

        private static ScriptableObject GetValidationScriptableObject(Type scriptableObjectType)
        {
            if (!ScriptableObjectCache.TryGetValue(scriptableObjectType, out var instance) || !instance)
            {
                instance = ScriptableObject.CreateInstance(scriptableObjectType);
                ScriptableObjectCache[scriptableObjectType] = instance;
                instance.hideFlags = HideFlags.DontSave;
            }

            return instance;
        }

        private static void Cleanup()
        {
            if (validationGameObject)
            {
                Object.DestroyImmediate(validationGameObject);
                foreach (var s in ScriptableObjectCache)
                {
                    Object.DestroyImmediate(s.Value);
                }

                ComponentCache.Clear();
                ScriptableObjectCache.Clear();
            }
        }

        private static void OnPlayModeChanged(PlayModeStateChange change)
        {
            if (change is PlayModeStateChange.ExitingEditMode)
            {
                Cleanup();
            }
        }
    }
}
