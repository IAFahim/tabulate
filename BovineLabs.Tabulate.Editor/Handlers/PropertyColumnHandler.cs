// <copyright file="PropertyColumnHandler.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Tabulate.Editor.Handlers
{
    using System;
    using System.Collections.Generic;
    using BovineLabs.Tabulate.Editor.Core;
    using BovineLabs.Tabulate.Editor.Services;
    using UnityEditor;
    using UnityEngine;

    public class PropertyColumnHandler
    {
        private readonly ColumnDefinition columnDefinition;
        private readonly Dictionary<UnityEngine.Object, SerializedObject> trackedObjects = new();
        private readonly Dictionary<UnityEngine.Object, SerializedObject>? objectCache;

        public PropertyColumnHandler(ColumnDefinition columnDefinition, Dictionary<UnityEngine.Object, SerializedObject>? cache = null)
        {
            this.columnDefinition = columnDefinition;
            this.objectCache = cache;

            // Subscribe to Unity's undo/redo system for property change detection
            Undo.undoRedoPerformed += this.OnUndoRedoPerformed;
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="PropertyColumnHandler"/> class.
        /// </summary>
        ~PropertyColumnHandler()
        {
            Undo.undoRedoPerformed -= this.OnUndoRedoPerformed;
            this.ClearTrackedObjects();
        }

        /// <summary>
        /// Event fired when a property changes that affects formula calculations.
        /// </summary>
        public event Action<string>? PropertyChanged;

        /// <summary>
        /// Validates the column configuration without requiring a specific target object.
        /// </summary>
        /// <param name="sampleObject">Optional sample object to validate against. If null, performs basic validation.</param>
        /// <returns>A validation result indicating whether the column is properly configured.</returns>
        public ValidationResult ValidateColumn(UnityEngine.Object? sampleObject = null)
        {
            // Check if property path is specified
            if (string.IsNullOrEmpty(this.columnDefinition.PropertyPath))
            {
                return ValidationResult.Failure("Missing property path",
                    "The property path is not specified. A property path is required to identify the target property.",
                    "Select a property from the dropdown or enter a valid property path.");
            }

            // Check if target type is specified and can be resolved
            if (!string.IsNullOrEmpty(this.columnDefinition.TargetTypeName))
            {
                var targetType = this.ResolveTargetType();
                if (targetType == null)
                {
                    return ValidationResult.Failure("Unresolved target type",
                        $"The target type '{this.columnDefinition.TargetTypeName}' could not be resolved. This may be due to a missing script or assembly.",
                        "Ensure the type name is correct and that the corresponding script is present in the project and has compiled successfully.");
                }

                // If we have a sample object, perform more detailed validation
                if (sampleObject != null)
                {
                    return this.ValidateColumnWithSample(sampleObject, targetType);
                }
            }

            return ValidationResult.Success();
        }

        /// <summary>
        /// Validates the column configuration using a sample object.
        /// </summary>
        /// <param name="sampleObject">The sample object to validate against.</param>
        /// <param name="targetType">The resolved target type.</param>
        /// <returns>A validation result indicating whether the column works with the sample object.</returns>
        private ValidationResult ValidateColumnWithSample(UnityEngine.Object sampleObject, Type targetType)
        {
            // Try to get the actual target object
            var actualTarget = this.GetActualTargetObject(sampleObject);
            if (actualTarget == null)
            {
                if (sampleObject is GameObject && typeof(MonoBehaviour).IsAssignableFrom(targetType))
                {
                    return ValidationResult.Failure("Missing component on sample object",
                        $"The provided sample GameObject does not have the required component of type '{targetType.Name}'.",
                        "Add the required component to the sample GameObject, or select a different sample object that has the component.");
                }

                return ValidationResult.Failure("Inaccessible target type on sample",
                    $"The target type '{targetType.Name}' could not be accessed on the provided sample object. This can happen if the object is not of the expected type.",
                    "Ensure the sample object is of the correct type or that it has the necessary components.");
            }

            // Check if property path exists on the target object
            var serializedObject = this.GetOrCreateSerializedObject(actualTarget);
            var property = serializedObject.FindProperty(this.columnDefinition.PropertyPath);

            if (property == null)
            {
                return ValidationResult.Failure("Property not found",
                    $"The property path '{this.columnDefinition.PropertyPath}' was not found on the target type '{targetType.Name}'.",
                    "Verify the property path is spelled correctly and that it exists on the target type. Use the property dropdown for a list of available properties.");
            }

            return ValidationResult.Success();
        }

        public object? GetValue(UnityEngine.Object targetObject)
        {
            if (string.IsNullOrEmpty(this.columnDefinition.PropertyPath))
            {
                return null;
            }

            // Get the actual target object (component if needed)
            var actualTarget = this.GetActualTargetObject(targetObject);
            if (actualTarget == null)
            {
                return null;
            }

            var serializedObject = this.GetOrCreateSerializedObject(actualTarget);
            var property = serializedObject.FindProperty(this.columnDefinition.PropertyPath);

            if (property == null)
            {
                return null;
            }

            return this.GetPropertyValue(property);
        }

        public bool SetValue(UnityEngine.Object targetObject, object? value)
        {
            if (this.columnDefinition.IsReadOnly || string.IsNullOrEmpty(this.columnDefinition.PropertyPath))
            {
                return false;
            }

            // Get the actual target object (component if needed)
            var actualTarget = this.GetActualTargetObject(targetObject);
            if (actualTarget == null)
            {
                return false;
            }

            var serializedObject = this.GetOrCreateSerializedObject(actualTarget);
            var property = serializedObject.FindProperty(this.columnDefinition.PropertyPath);

            if (property == null)
            {
                return false;
            }

            var success = this.SetPropertyValue(property, value);
            if (success)
            {
                serializedObject.ApplyModifiedProperties();

                // Trigger property change event for real-time formula updates
                this.OnPropertyChanged(this.columnDefinition.PropertyPath);
            }

            return success;
        }

        public Type GetPropertyType(UnityEngine.Object sampleObject)
        {
            if (string.IsNullOrEmpty(this.columnDefinition.PropertyPath))
            {
                return typeof(object);
            }

            // Get the actual target object (component if needed)
            var actualTarget = this.GetActualTargetObject(sampleObject);
            if (actualTarget == null)
            {
                return typeof(object);
            }

            var serializedObject = this.GetOrCreateSerializedObject(actualTarget);
            var property = serializedObject.FindProperty(this.columnDefinition.PropertyPath);

            if (property == null)
            {
                return typeof(object);
            }

            return property.propertyType switch
            {
                SerializedPropertyType.Integer => typeof(int),
                SerializedPropertyType.Float => typeof(float),
                SerializedPropertyType.String => typeof(string),
                SerializedPropertyType.Boolean => typeof(bool),
                SerializedPropertyType.Vector2 => typeof(Vector2),
                SerializedPropertyType.Vector3 => typeof(Vector3),
                SerializedPropertyType.Vector4 => typeof(Vector4),
                SerializedPropertyType.Color => typeof(Color),
                SerializedPropertyType.Enum => property.enumNames.Length > 0 ? typeof(Enum) : typeof(int),
                SerializedPropertyType.ObjectReference => typeof(UnityEngine.Object),
                SerializedPropertyType.LayerMask => typeof(int),
                SerializedPropertyType.Bounds => typeof(Bounds),
                SerializedPropertyType.Rect => typeof(Rect),
                SerializedPropertyType.AnimationCurve => typeof(AnimationCurve),
                _ => typeof(object),
            };
        }

        private object? GetPropertyValue(SerializedProperty property)
        {
            return property.propertyType switch
            {
                SerializedPropertyType.Integer => property.intValue,
                SerializedPropertyType.Float => property.floatValue,
                SerializedPropertyType.String => property.stringValue,
                SerializedPropertyType.Boolean => property.boolValue,
                SerializedPropertyType.Vector2 => property.vector2Value,
                SerializedPropertyType.Vector3 => property.vector3Value,
                SerializedPropertyType.Vector4 => property.vector4Value,
                SerializedPropertyType.Color => property.colorValue,
                SerializedPropertyType.Enum => property.enumValueIndex,
                SerializedPropertyType.ObjectReference => property.objectReferenceValue,
                SerializedPropertyType.LayerMask => property.intValue,
                SerializedPropertyType.Bounds => property.boundsValue,
                SerializedPropertyType.Rect => property.rectValue,
                SerializedPropertyType.AnimationCurve => property.animationCurveValue,
                _ => $"Unsupported type: {property.propertyType}",
            };
        }

        private bool SetPropertyValue(SerializedProperty property, object? value)
        {
            if (value == null)
            {
                return false;
            }

            try
            {
                switch (property.propertyType)
                {
                    case SerializedPropertyType.Integer:
                        property.intValue = Convert.ToInt32(value);
                        return true;
                    case SerializedPropertyType.Float:
                        property.floatValue = Convert.ToSingle(value);
                        return true;
                    case SerializedPropertyType.String:
                        property.stringValue = value.ToString();
                        return true;
                    case SerializedPropertyType.Boolean:
                        property.boolValue = Convert.ToBoolean(value);
                        return true;
                    case SerializedPropertyType.Vector2:
                        if (value is Vector2 v2)
                        {
                            property.vector2Value = v2;
                            return true;
                        }

                        break;
                    case SerializedPropertyType.Vector3:
                        if (value is Vector3 v3)
                        {
                            property.vector3Value = v3;
                            return true;
                        }

                        break;
                    case SerializedPropertyType.Vector4:
                        if (value is Vector4 v4)
                        {
                            property.vector4Value = v4;
                            return true;
                        }

                        break;
                    case SerializedPropertyType.Color:
                        if (value is Color color)
                        {
                            property.colorValue = color;
                            return true;
                        }

                        break;
                    case SerializedPropertyType.Enum:
                        property.enumValueIndex = Convert.ToInt32(value);
                        return true;
                    case SerializedPropertyType.ObjectReference:
                        if (value is UnityEngine.Object objRef)
                        {
                            property.objectReferenceValue = objRef;
                            return true;
                        }

                        break;
                    case SerializedPropertyType.LayerMask:
                        property.intValue = Convert.ToInt32(value);
                        return true;
                    case SerializedPropertyType.Bounds:
                        if (value is Bounds bounds)
                        {
                            property.boundsValue = bounds;
                            return true;
                        }

                        break;
                    case SerializedPropertyType.Rect:
                        if (value is Rect rect)
                        {
                            property.rectValue = rect;
                            return true;
                        }

                        break;
                    case SerializedPropertyType.AnimationCurve:
                        if (value is AnimationCurve curve)
                        {
                            property.animationCurveValue = curve;
                            return true;
                        }

                        break;
                }

                Debug.LogWarning($"Cannot set property {property.propertyPath} to value {value} (type: {value.GetType()})");
                return false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error setting property {property.propertyPath}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Called when a property value has changed.
        /// </summary>
        /// <param name="propertyPath">The path of the property that changed.</param>
        public void OnPropertyChanged(string propertyPath)
        {
            if (!string.IsNullOrEmpty(propertyPath))
            {
                this.PropertyChanged?.Invoke(propertyPath);
            }
        }

        /// <summary>
        /// Registers an object for property change tracking.
        /// </summary>
        /// <param name="targetObject">The object to track.</param>
        public void RegisterObject(UnityEngine.Object targetObject)
        {
            if (targetObject != null && !this.trackedObjects.ContainsKey(targetObject))
            {
                this.trackedObjects[targetObject] = new SerializedObject(targetObject);
            }
        }

        /// <summary>
        /// Clears all tracked objects.
        /// </summary>
        public void ClearTrackedObjects()
        {
            foreach (var serializedObject in this.trackedObjects.Values)
            {
                serializedObject?.Dispose();
            }

            this.trackedObjects.Clear();
        }

        /// <summary>
        /// Called when undo/redo operations are performed.
        /// </summary>
        private void OnUndoRedoPerformed()
        {
            // Check all tracked objects for changes
            foreach (var kvp in this.trackedObjects)
            {
                var obj = kvp.Key;
                var serializedObject = kvp.Value;

                if (obj != null && serializedObject != null)
                {
                    serializedObject.UpdateIfRequiredOrScript();

                    // If this property path exists on this object, notify of potential change
                    if (!string.IsNullOrEmpty(this.columnDefinition.PropertyPath))
                    {
                        var property = serializedObject.FindProperty(this.columnDefinition.PropertyPath);
                        if (property != null)
                        {
                            this.OnPropertyChanged(this.columnDefinition.PropertyPath);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets the actual target object for property access. If targetObject is a GameObject
        /// and we have a TargetTypeName, tries to get the component of that type.
        /// </summary>
        private UnityEngine.Object GetActualTargetObject(UnityEngine.Object targetObject)
        {
            // If no target type specified, use the object as-is
            if (string.IsNullOrEmpty(this.columnDefinition.TargetTypeName))
            {
                return targetObject;
            }

            // If the target object is already the correct type, use it as-is
            var targetObjectType = targetObject.GetType();
            if (targetObjectType.Name == this.columnDefinition.TargetTypeName ||
                targetObjectType.AssemblyQualifiedName == this.columnDefinition.TargetTypeName)
            {
                return targetObject;
            }

            // If target object is a GameObject and we need a component, try to get it
            if (targetObject is GameObject gameObject)
            {
                // Try to resolve the target type
                var targetType = this.ResolveTargetType();
                if (targetType != null)
                {
                    var component = gameObject.GetComponent(targetType);
                    if (component != null)
                    {
                        return component;
                    }
                }
            }

            // Fallback: return the original object
            return targetObject;
        }

        /// <summary>
        /// Resolves the target type from the TargetTypeName string.
        /// </summary>
        private Type? ResolveTargetType()
        {
            return TypeResolutionService.ResolveType(this.columnDefinition.TargetTypeName);
        }

        /// <summary>
        /// Gets or creates a SerializedObject from the cache.
        /// </summary>
        private SerializedObject GetOrCreateSerializedObject(UnityEngine.Object targetObject)
        {
            if (this.objectCache != null)
            {
                if (!this.objectCache.TryGetValue(targetObject, out var serializedObject))
                {
                    serializedObject = new SerializedObject(targetObject);
                    this.objectCache[targetObject] = serializedObject;
                }

                return serializedObject;
            }

            // Fallback to direct creation when no cache is available
            return new SerializedObject(targetObject);
        }
    }
}