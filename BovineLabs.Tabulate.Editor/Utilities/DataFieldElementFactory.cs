// <copyright file="DataFieldElementFactory.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Tabulate.Editor.Utilities
{
    using BovineLabs.Tabulate.Editor.Core;
    using UnityEngine.UIElements;

    /// <summary>
    /// Factory for creating appropriate UI elements based on DataFieldType.
    /// Provides unified data field creation logic across the sheet editor system.
    /// </summary>
    public static class DataFieldElementFactory
    {
        /// <summary>
        /// Creates an appropriate UI element for the specified data field type.
        /// </summary>
        /// <param name="dataFieldType">The type of data field to create.</param>
        /// <param name="label">Optional label text for the field. If null, creates field without label.</param>
        /// <param name="useSlider">Whether to use slider controls for numeric types.</param>
        /// <param name="minValue">The minimum value for sliders.</param>
        /// <param name="maxValue">The maximum value for sliders.</param>
        /// <returns>A VisualElement appropriate for the data field type.</returns>
        public static VisualElement CreateElement(DataFieldType dataFieldType, string? label = null, bool useSlider = false, float minValue = 0f, float maxValue = 1f)
        {
            return dataFieldType switch
            {
                DataFieldType.Integer when useSlider => new SliderInt(label, (int)minValue, (int)maxValue) { showInputField = true },
                DataFieldType.Integer => new IntegerField(label) { isDelayed = true },
                DataFieldType.Float when useSlider => new Slider(label, minValue, maxValue) { showInputField = true },
                DataFieldType.Float => new FloatField(label) { isDelayed = true },
                DataFieldType.Boolean => new Toggle(label),
                _ => new IntegerField(label) { isDelayed = true }, // Default fallback
            };
        }
    }
}