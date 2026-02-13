// <copyright file="VariableDetailsHandler.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Tabulate.Editor.UI.Config
{
    using System;
    using System.Collections.Generic;
    using BovineLabs.Tabulate.Editor.Core;
    using BovineLabs.Tabulate.Editor.Utilities;
    using BovineLabs.Tabulate.Editor.Variables;
    using UnityEditor.UIElements;
    using UnityEngine.UIElements;

    public class VariableDetailsHandler : BaseDetailsHandler
    {
        private readonly List<VariableDefinition> workingVariables;
        private readonly Action refreshVariableListCallback;
        private readonly Action onChangeCallback;

        private readonly TextField variableDisplayNameField;
        private readonly EnumField variableTypeField;
        private readonly EnumField variableDataTypeField;
        private readonly TextField variableTargetTypeField;
        private readonly TextField variablePropertyPathField;
        private readonly DropdownField variablePropertyDropdown;
        private readonly ObjectField variableTargetObjectField;
        private readonly TextField variableFormulaField;
        private readonly ListView variableListView;
        private readonly Label variableNoSelectionLabel;
        private readonly VisualElement variableDetailsForm;

        private VisualElement? variableDataValueElement;

        public VariableDetailsHandler(
            VisualElement rootElement, SheetDefinition sheetDefinition, List<VariableDefinition> workingVariables, Action refreshVariableListCallback, Action onChangeCallback)
            : base(rootElement, sheetDefinition)
        {
            this.workingVariables = workingVariables;
            this.refreshVariableListCallback = refreshVariableListCallback;
            this.onChangeCallback = onChangeCallback;

            this.variableDisplayNameField = this.FindElement<TextField>("variable-display-name-field");
            this.variableTypeField = this.FindElement<EnumField>("variable-type-field");
            this.variableDataTypeField = this.FindElement<EnumField>("variable-data-type-field");
            this.variableTargetTypeField = this.FindElement<TextField>("variable-target-type-field");
            this.variablePropertyPathField = this.FindElement<TextField>("variable-property-path-field");
            this.variablePropertyDropdown = this.FindElement<DropdownField>("variable-property-dropdown");
            this.variableTargetObjectField = this.FindElement<ObjectField>("variable-target-object-field");
            this.variableFormulaField = this.FindElement<TextField>("variable-formula-field");
            this.variableListView = this.FindElement<ListView>("variable-list-view");

            this.variableNoSelectionLabel = this.FindElement<Label>("variable-no-selection-label");
            this.variableDetailsForm = this.FindElement<VisualElement>("variable-details-form");

            this.SetupUIElements();
            this.RegisterEventHandlers();
        }

        public void ShowDetails(VariableDefinition variable)
        {
            this.ShowDetailsForm(this.variableNoSelectionLabel, this.variableDetailsForm, "column-config-details-form--visible");
            this.PopulateFields(variable);
            this.RefreshVariablePropertyDropdown();
            this.UpdateFieldVisibility(variable.Type);
        }

        public void ShowNoSelectionState()
        {
            this.ShowNoSelectionState(this.variableNoSelectionLabel, this.variableDetailsForm, "column-config-details-form--visible");
        }

        private void SetupUIElements()
        {
            this.variableTargetTypeField.isReadOnly = true;
            this.variableTargetTypeField.RegisterCallback<ClickEvent>(this.OnTargetTypeFieldClicked);

            // Initialize enum fields with their respective enum types
            this.variableTypeField.Init(VariableType.Data);
            this.variableDataTypeField.Init(DataFieldType.Float);
        }

        private void RegisterEventHandlers()
        {
            this.RegisterValueChangedCallback<string>(this.variableDisplayNameField, this.OnDisplayNameChanged);
            this.RegisterValueChangedCallback<Enum>(this.variableTypeField, this.OnTypeChanged);
            this.RegisterValueChangedCallback<Enum>(this.variableDataTypeField, this.OnDataTypeChanged);
            this.RegisterValueChangedCallback<string>(this.variableTargetTypeField, this.OnTargetTypeChanged);
            this.RegisterValueChangedCallback<string>(this.variablePropertyDropdown, this.OnPropertyPathChanged);
            this.RegisterValueChangedCallback<string>(this.variablePropertyPathField, this.OnCustomPropertyPathChanged);
            this.RegisterValueChangedCallback<UnityEngine.Object>(this.variableTargetObjectField, this.OnTargetObjectChanged);
            this.RegisterValueChangedCallback<string>(this.variableFormulaField, this.OnFormulaChanged);
        }

        private void PopulateFields(VariableDefinition variable)
        {
            this.variableDisplayNameField.value = variable.DisplayName;
            this.variableTypeField.value = variable.Type;
            this.variableDataTypeField.value = variable.DataType;
            this.variableTargetTypeField.value = variable.TargetTypeName;
            this.variablePropertyDropdown.value = variable.PropertyPath;
            this.variablePropertyPathField.value = variable.PropertyPath;
            this.variableTargetObjectField.value = variable.TargetObject;
            this.variableFormulaField.value = variable.Formula;

            this.RecreateVariableDataValueField(variable.DataType, variable.DataValue);
        }

        private void RefreshVariablePropertyDropdown()
        {
            var selectedVariable = this.GetSelectedVariable();
            this.PopulatePropertyDropdown(this.variablePropertyDropdown, selectedVariable?.TargetTypeName);
        }

        private void UpdateFieldVisibility(VariableType variableType)
        {
            // Use shared utility for field visibility
            var isProperty = variableType == VariableType.Property;
            this.SetFieldVisibility(this.variableTargetTypeField, "config-property-field--hidden", isProperty);
            this.SetFieldVisibility(this.variablePropertyDropdown, "config-property-field--hidden", isProperty);
            this.SetFieldVisibility(this.variableTargetObjectField, "config-property-field--hidden", isProperty);

            this.SetFieldVisibility(this.variableFormulaField, "config-formula-field--hidden", variableType == VariableType.Formula);

            var isData = variableType == VariableType.Data;
            this.SetFieldVisibility(this.variableDataTypeField, "config-data-field--hidden", isData);
            this.SetFieldVisibility(this.variableDataValueElement, "config-data-field--hidden", isData);
        }

        private VariableDefinition? GetSelectedVariable()
        {
            if (this.variableListView.selectedIndex >= 0 && this.variableListView.selectedIndex < this.workingVariables.Count)
            {
                return this.workingVariables[this.variableListView.selectedIndex];
            }

            return null;
        }

        private void OnTargetTypeFieldClicked(ClickEvent evt)
        {
            var selectedVariable = this.GetSelectedVariable();
            if (selectedVariable == null)
            {
                return;
            }

            this.ShowTypeSelector(typeName =>
            {
                selectedVariable.TargetTypeName = typeName;
                this.variableTargetTypeField.value = typeName;
                this.RefreshVariablePropertyDropdown();
                this.refreshVariableListCallback();
            });
        }

        private void OnDisplayNameChanged(ChangeEvent<string> evt)
        {
            var selectedVariable = this.GetSelectedVariable();
            if (selectedVariable != null)
            {
                selectedVariable.DisplayName = evt.newValue;
                this.refreshVariableListCallback();
                this.NotifyChange();
            }
        }

        private void NotifyChange()
        {
            this.onChangeCallback();
        }

        private void OnTypeChanged(ChangeEvent<Enum> evt)
        {
            var selectedVariable = this.GetSelectedVariable();
            if (selectedVariable != null && evt.newValue is VariableType variableType)
            {
                selectedVariable.Type = variableType;
                this.UpdateFieldVisibility(variableType);
                this.refreshVariableListCallback();
                this.NotifyChange();
            }
        }

        private void OnDataTypeChanged(ChangeEvent<Enum> evt)
        {
            var selectedVariable = this.GetSelectedVariable();
            if (selectedVariable != null && evt.newValue is DataFieldType dataType)
            {
                selectedVariable.DataType = dataType;
                this.RecreateVariableDataValueField(dataType, selectedVariable.DataValue);
                this.NotifyChange();
            }
        }

        private void OnDataValueChanged(string newValue)
        {
            var selectedVariable = this.GetSelectedVariable();
            if (selectedVariable != null)
            {
                selectedVariable.DataValue = newValue;
                this.NotifyChange();
            }
        }

        private void OnTargetTypeChanged(ChangeEvent<string> evt)
        {
            var selectedVariable = this.GetSelectedVariable();
            if (selectedVariable != null)
            {
                selectedVariable.TargetTypeName = evt.newValue;
                this.RefreshVariablePropertyDropdown();
                this.NotifyChange();
            }
        }

        private void OnPropertyPathChanged(ChangeEvent<string> evt)
        {
            var selectedVariable = this.GetSelectedVariable();
            this.HandlePropertyDropdownChange(evt, selectedVariable, this.variablePropertyPathField, "config-property-path-field--hidden",
                this.refreshVariableListCallback, (variable, path) => variable.PropertyPath = path);
            this.NotifyChange();
        }

        private void OnCustomPropertyPathChanged(ChangeEvent<string> evt)
        {
            var selectedVariable = this.GetSelectedVariable();
            if (selectedVariable != null)
            {
                selectedVariable.PropertyPath = evt.newValue;
                this.refreshVariableListCallback();
                this.NotifyChange();
            }
        }

        private void OnTargetObjectChanged(ChangeEvent<UnityEngine.Object> evt)
        {
            var selectedVariable = this.GetSelectedVariable();
            if (selectedVariable != null)
            {
                selectedVariable.TargetObject = evt.newValue;
                this.NotifyChange();
            }
        }

        private void OnFormulaChanged(ChangeEvent<string> evt)
        {
            var selectedVariable = this.GetSelectedVariable();
            if (selectedVariable != null)
            {
                selectedVariable.Formula = evt.newValue;
                this.NotifyChange();
            }
        }

        /// <summary>
        /// Recreates the variable data value field with the appropriate UI element for the data type.
        /// </summary>
        /// <param name="dataType">The data type to create a field for.</param>
        /// <param name="currentValue">The current value to set.</param>
        private void RecreateVariableDataValueField(DataFieldType dataType, string currentValue)
        {
            // Use shared utility to recreate the field
            this.variableDataValueElement = this.RecreateDataValueField(
                this.variableDetailsForm,
                this.variableDataValueElement,
                this.variableDataTypeField,
                dataType,
                "Value",
                "variable-data-value-field",
                "config-data-field--hidden");

            // Bind the value and set up change callback directly on the field
            DataFieldValueBinder.BindElement(this.variableDataValueElement, dataType, currentValue, this.OnDataValueChanged);

            // Apply visibility based on current variable type
            var selectedVariable = this.GetSelectedVariable();
            if (selectedVariable != null)
            {
                this.SetFieldVisibility(this.variableDataValueElement, "config-data-field--hidden", selectedVariable.Type == VariableType.Data);
            }
        }
    }
}