// <copyright file="DataFieldValueBinder.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Tabulate.Editor.Utilities
{
    using System;
    using System.Globalization;
    using BovineLabs.Tabulate.Editor.Core;
    using UnityEngine.UIElements;

    /// <summary>
    /// Handles binding values and change callbacks for data field elements.
    /// Provides unified data field binding logic across the sheet editor system.
    /// </summary>
    public static class DataFieldValueBinder
    {
        /// <summary>
        /// Binds a value to a data field element and sets up change callbacks.
        /// </summary>
        /// <param name="element">The UI element to bind to.</param>
        /// <param name="dataFieldType">The type of data field.</param>
        /// <param name="currentValue">The current string value to set.</param>
        /// <param name="onValueChanged">Callback when the value changes, receives the new value as a string.</param>
        public static void BindElement(VisualElement element, DataFieldType dataFieldType, string currentValue, Action<string> onValueChanged)
        {
            switch (dataFieldType)
            {
                case DataFieldType.Integer:
                    BindIntegerElement(element, currentValue, onValueChanged);
                    break;

                case DataFieldType.Float:
                    BindFloatElement(element, currentValue, onValueChanged);
                    break;

                case DataFieldType.Boolean:
                    BindBooleanElement(element, currentValue, onValueChanged);
                    break;

                default:
                    // Fallback to integer binding
                    BindIntegerElement(element, currentValue, onValueChanged);
                    break;
            }
        }

        private static void BindIntegerElement(VisualElement element, string currentValue, Action<string> onValueChanged)
        {
            if (element is SliderInt sliderInt)
            {
                sliderInt.value = int.TryParse(currentValue, out var sliderIntVal) ? sliderIntVal : 0;
                sliderInt.RegisterValueChangedCallback(evt => onValueChanged(evt.newValue.ToString()));
            }
            else if (element is IntegerField intField)
            {
                intField.value = int.TryParse(currentValue, out var intVal) ? intVal : 0;
                intField.RegisterValueChangedCallback(evt => onValueChanged(evt.newValue.ToString()));
            }
        }

        private static void BindFloatElement(VisualElement element, string currentValue, Action<string> onValueChanged)
        {
            if (element is Slider floatSlider)
            {
                floatSlider.value = float.TryParse(currentValue, out var sliderFloatVal) ? sliderFloatVal : 0f;
                floatSlider.RegisterValueChangedCallback(evt => onValueChanged(evt.newValue.ToString(CultureInfo.InvariantCulture)));
            }
            else if (element is FloatField floatField)
            {
                floatField.value = float.TryParse(currentValue, out var floatVal) ? floatVal : 0f;
                floatField.RegisterValueChangedCallback(evt => onValueChanged(evt.newValue.ToString(CultureInfo.InvariantCulture)));
            }
        }

        private static void BindBooleanElement(VisualElement element, string currentValue, Action<string> onValueChanged)
        {
            if (element is Toggle toggle)
            {
                toggle.value = bool.TryParse(currentValue, out var boolVal) ? boolVal : false;
                toggle.RegisterValueChangedCallback(evt => onValueChanged(evt.newValue.ToString()));
            }
        }
    }
}