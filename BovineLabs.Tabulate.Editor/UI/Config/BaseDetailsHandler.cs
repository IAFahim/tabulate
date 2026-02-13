// <copyright file="BaseDetailsHandler.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Tabulate.Editor.UI.Config
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using BovineLabs.Tabulate.Editor.Core;
    using BovineLabs.Tabulate.Editor.Search;
    using BovineLabs.Tabulate.Editor.Services;
    using BovineLabs.Tabulate.Editor.Utilities;
    using UnityEditor.Search;
    using UnityEngine;
    using UnityEngine.Search;
    using UnityEngine.UIElements;

    public abstract class BaseDetailsHandler
    {
        private readonly VisualElement rootElement;
        private readonly SheetDefinition sheetDefinition;

        protected BaseDetailsHandler(VisualElement rootElement, SheetDefinition sheetDefinition)
        {
            this.rootElement = rootElement;
            this.sheetDefinition = sheetDefinition;
        }

        protected void ShowTypeSelector(Action<string> onTypeSelected)
        {
            if (this.sheetDefinition != null)
            {
                var baseType = this.sheetDefinition.Type == SheetType.GameObject
                    ? typeof(MonoBehaviour)
                    : typeof(ScriptableObject);
                var simplifiedQualifiedName = $"{baseType.FullName}, {baseType.Assembly.GetName().Name}";

                var context = SearchService.CreateContext(TypeSearchProvider.SearchProviderType, $"inherit=\"{simplifiedQualifiedName}\"");

                var viewState = new SearchViewState(context, SearchViewFlags.ListView | SearchViewFlags.OpenInBuilderMode | SearchViewFlags.DisableSavedSearchQuery | SearchViewFlags.CompactView)
                {
                    windowTitle = new GUIContent("Type Selector"),
                    title = "Select Type",
                    position = SearchUtils.GetMainWindowCenteredPosition(new Vector2(600, 400)),
                    selectHandler = (item, canceled) =>
                    {
                        if (canceled || item == null)
                        {
                            return;
                        }

                        var typeName = item.data as string ?? string.Empty;
                        onTypeSelected(typeName);
                    },
                };
                SearchService.ShowPicker(viewState);
            }
        }

        protected T FindElement<T>(string name)
            where T : VisualElement
        {
            return this.rootElement.Q<T>(name);
        }

        protected void RegisterValueChangedCallback<T>(VisualElement? element, EventCallback<ChangeEvent<T>> callback)
        {
            if (element is INotifyValueChanged<T> notifyElement)
            {
                notifyElement.RegisterValueChangedCallback(callback);
            }
        }

        protected void ShowNoSelectionState(Label? noSelectionLabel, VisualElement? detailsForm, string detailsFormVisibleClass)
        {
            if (noSelectionLabel != null)
            {
                noSelectionLabel.style.display = DisplayStyle.Flex;
            }

            if (detailsForm != null)
            {
                detailsForm.RemoveFromClassList(detailsFormVisibleClass);
            }
        }

        protected void ShowDetailsForm(Label? noSelectionLabel, VisualElement? detailsForm, string detailsFormVisibleClass)
        {
            if (noSelectionLabel != null)
            {
                noSelectionLabel.style.display = DisplayStyle.None;
            }

            if (detailsForm != null)
            {
                detailsForm.AddToClassList(detailsFormVisibleClass);
            }
        }

        protected void PopulatePropertyDropdown(DropdownField? dropdown, string? targetTypeName)
        {
            if (dropdown == null)
            {
                return;
            }

            var properties = this.GetAvailableProperties(targetTypeName);
            dropdown.choices = properties.ToList();
            dropdown.choices.Add("Custom...");
        }

        protected void HandlePropertyDropdownChange<T>(ChangeEvent<string> evt, T? selectedItem, TextField customPathField, string customPathFieldHiddenClass, Action refreshCallback, Action<T, string> setPropertyPath)
            where T : class
        {
            if (selectedItem == null)
            {
                return;
            }

            if (evt.newValue == "Custom...")
            {
                customPathField.RemoveFromClassList(customPathFieldHiddenClass);
                customPathField.Focus();
            }
            else
            {
                setPropertyPath(selectedItem, evt.newValue);
                customPathField.AddToClassList(customPathFieldHiddenClass);
                refreshCallback();
            }
        }

        /// <summary>
        /// Sets the visibility of a field based on a condition using CSS class manipulation.
        /// </summary>
        /// <param name="element">The element to show/hide.</param>
        /// <param name="hiddenClass">The CSS class that hides the element.</param>
        /// <param name="shouldShow">Whether the element should be shown.</param>
        protected void SetFieldVisibility(VisualElement? element, string hiddenClass, bool shouldShow)
        {
            if (element == null)
            {
                return;
            }

            if (shouldShow)
            {
                element.RemoveFromClassList(hiddenClass);
            }
            else
            {
                element.AddToClassList(hiddenClass);
            }
        }

        /// <summary>
        /// Recreates a dynamic data value field and inserts it at the specified position.
        /// </summary>
        /// <param name="parent">The parent container.</param>
        /// <param name="oldElement">The old element to remove (can be null).</param>
        /// <param name="insertAfter">The element to insert after.</param>
        /// <param name="dataFieldType">The data field type.</param>
        /// <param name="label">The label for the field.</param>
        /// <param name="fieldName">The name to assign to the field.</param>
        /// <param name="hiddenClass">The CSS class to apply for hiding.</param>
        /// <param name="useSlider">Whether to use slider controls.</param>
        /// <param name="minValue">The minimum value for sliders.</param>
        /// <param name="maxValue">The maximum value for sliders.</param>
        /// <returns>The newly created element.</returns>
        protected VisualElement RecreateDataValueField(
            VisualElement parent,
            VisualElement? oldElement,
            VisualElement insertAfter,
            DataFieldType dataFieldType,
            string label,
            string fieldName,
            string hiddenClass,
            bool useSlider = false,
            float minValue = 0f,
            float maxValue = 100f)
        {
            // Remove existing element if it exists
            if (oldElement != null)
            {
                parent.Remove(oldElement);
            }

            // Create new field
            var newField = this.CreateDataValueField(dataFieldType, label, useSlider, minValue, maxValue);
            newField.name = fieldName;
            newField.AddToClassList(hiddenClass);

            // Find insertion position and insert
            var insertIndex = parent.IndexOf(insertAfter) + 1;
            parent.Insert(insertIndex, newField);

            return newField;
        }

        private IEnumerable<string> GetAvailableProperties(string? targetTypeName = null)
        {
            if (string.IsNullOrEmpty(targetTypeName))
            {
                return new List<string>();
            }

            var targetType = Type.GetType(targetTypeName!);
            if (targetType == null)
            {
                return new List<string>();
            }

            return ValidationHelper.GetAvailableSerializedProperties(targetType).OrderBy(p => p);
        }

        /// <summary>
        /// Creates a dynamic data value field element for a given data type.
        /// </summary>
        /// <param name="dataFieldType">The data field type.</param>
        /// <param name="label">The label for the field.</param>
        /// <param name="useSlider">Whether to use slider controls for numeric types.</param>
        /// <param name="minValue">The minimum value for sliders.</param>
        /// <param name="maxValue">The maximum value for sliders.</param>
        /// <returns>The created visual element.</returns>
        private VisualElement CreateDataValueField(DataFieldType dataFieldType, string label, bool useSlider = false, float minValue = 0f, float maxValue = 100f)
        {
            return DataFieldElementFactory.CreateElement(dataFieldType, label, useSlider, minValue, maxValue);
        }
    }
}