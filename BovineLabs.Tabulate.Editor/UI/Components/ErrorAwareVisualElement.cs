// <copyright file="ErrorAwareVisualElement.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Tabulate.Editor.UI.Components
{
    using System;
    using BovineLabs.Tabulate.Editor.Core;
    using UnityEngine;
    using UnityEngine.UIElements;

    /// <summary>
    /// A visual element wrapper that can display error states with visual indicators.
    /// </summary>
    public class ErrorAwareVisualElement : VisualElement
    {
        private readonly VisualElement contentElement;
        private readonly VisualElement errorIcon;
        private CellErrorState? currentError;

        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorAwareVisualElement"/> class.
        /// </summary>
        /// <param name="content">The actual content element to wrap.</param>
        public ErrorAwareVisualElement(VisualElement content)
        {
            this.contentElement = content ?? throw new ArgumentNullException(nameof(content));

            // Set up container positioning
            this.style.position = Position.Relative;
            this.style.flexGrow = 1;

            // Add the content element
            this.Add(this.contentElement);

            // Create error icon
            this.errorIcon = new VisualElement();
            this.errorIcon.AddToClassList("cell-error-icon");
            this.errorIcon.style.display = DisplayStyle.None;
            this.errorIcon.pickingMode = PickingMode.Position;
            this.Add(this.errorIcon);

            // Set up error icon click handler for tooltip
            this.errorIcon.RegisterCallback<ClickEvent>(this.OnErrorIconClicked);

            // Also show tooltip on hover
            this.errorIcon.RegisterCallback<MouseEnterEvent>(this.OnErrorIconHover);
        }

        /// <summary>
        /// Event triggered when the error tooltip should be shown.
        /// </summary>
        public event Action<Vector2, CellErrorState>? ShowErrorTooltip;

        /// <summary>
        /// Gets the wrapped content element.
        /// </summary>
        public VisualElement Content => this.contentElement;

        /// <summary>
        /// Sets the error state for this element.
        /// </summary>
        /// <param name="error">The error state to set, or null to clear the error.</param>
        public void SetError(CellErrorState? error)
        {
            // Clear previous error styling
            this.ClearErrorStyling();

            this.currentError = error;

            if (error != null)
            {
                this.ApplyErrorStyling(error);
            }
        }

        /// <summary>
        /// Clears the current error state.
        /// </summary>
        public void ClearError()
        {
            this.SetError(null);
        }

        private void ApplyErrorStyling(CellErrorState error)
        {
            // Apply error styling to the content element
            this.contentElement.AddToClassList(error.GetSeverityCssClass());

            // Configure and show error icon
            this.errorIcon.ClearClassList();
            this.errorIcon.AddToClassList("cell-error-icon");
            this.errorIcon.AddToClassList($"cell-error-icon--{error.Severity.ToString().ToLower()}");
            this.errorIcon.style.display = DisplayStyle.Flex;
            this.errorIcon.tooltip = error.Message;
        }

        private void ClearErrorStyling()
        {
            // Remove all error-related CSS classes from content
            this.contentElement.RemoveFromClassList("cell-error--warning");
            this.contentElement.RemoveFromClassList("cell-error--error");
            this.contentElement.RemoveFromClassList("cell-error--critical");

            // Hide error icon
            this.errorIcon.style.display = DisplayStyle.None;
            this.errorIcon.tooltip = string.Empty;
        }

        private void OnErrorIconClicked(ClickEvent evt)
        {
            if (this.currentError != null)
            {
                var globalPosition = this.errorIcon.LocalToWorld(Vector2.zero);
                this.ShowErrorTooltip?.Invoke(globalPosition, this.currentError);
            }

            evt.StopPropagation();
        }

        private void OnErrorIconHover(MouseEnterEvent evt)
        {
            if (this.currentError != null)
            {
                var globalPosition = this.errorIcon.LocalToWorld(Vector2.zero);
                this.ShowErrorTooltip?.Invoke(globalPosition, this.currentError);
            }
        }
    }
}