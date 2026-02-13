// <copyright file="ErrorTooltipDrawer.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Tabulate.Editor.UI.Components
{
    using BovineLabs.Tabulate.Editor.Core;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.UIElements;
    using PopupWindow = UnityEditor.PopupWindow;

    /// <summary>
    /// Popup window content for displaying detailed error information using UI Toolkit.
    /// </summary>
    public class ErrorTooltipDrawer : PopupWindowContent
    {
        private readonly CellErrorState errorState;
        private readonly Vector2 preferredSize;
        private VisualElement? rootElement;

        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorTooltipDrawer"/> class.
        /// </summary>
        /// <param name="errorState">The error state to display.</param>
        public ErrorTooltipDrawer(CellErrorState errorState)
        {
            this.errorState = errorState;
            this.preferredSize = this.CalculatePreferredSize();
        }

        public override Vector2 GetWindowSize()
        {
            return this.preferredSize;
        }

        public override void OnGUI(Rect rect)
        {
            // UI Toolkit handles the rendering, so we just need to ensure proper sizing
            if (this.rootElement != null)
            {
                this.rootElement.style.width = rect.width;
                this.rootElement.style.height = rect.height;
            }
        }

        public override VisualElement CreateGUI()
        {
            var errorTooltipAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(EditorConstants.AssetPath + "ErrorTooltip.uxml");
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(EditorConstants.AssetPath + "ErrorDisplay.uss");

            // Create root element and clone template into it to avoid TemplateContainer sizing issues
            this.rootElement = new VisualElement();
            this.rootElement.AddToClassList("error-tooltip-container");
            this.rootElement.styleSheets.Add(styleSheet);
            errorTooltipAsset.CloneTree(this.rootElement);

            // Populate the template with actual data
            var errorIcon = this.rootElement.Q<VisualElement>("error-icon");
            var errorTitle = this.rootElement.Q<Label>("error-title");
            var detailsSection = this.rootElement.Q<VisualElement>("details-section");
            var detailsText = this.rootElement.Q<Label>("details-text");
            var suggestionSection = this.rootElement.Q<VisualElement>("suggestion-section");
            var suggestionText = this.rootElement.Q<Label>("suggestion-text");
            var locationSection = this.rootElement.Q<VisualElement>("location-section");
            var locationText = this.rootElement.Q<Label>("location-text");

            // Set icon
            var iconTexture = this.GetSeverityIcon();
            errorIcon.style.backgroundImage = new StyleBackground(iconTexture);

            // Set title with severity-specific color
            errorTitle.text = $"{this.errorState.Severity}: {this.errorState.Message}";
            errorTitle.style.color = this.GetSeverityColor();

            // Show/hide details section
            if (!string.IsNullOrEmpty(this.errorState.DetailedDescription))
            {
                detailsSection.style.display = DisplayStyle.Flex;
                detailsText.text = this.errorState.DetailedDescription;
            }
            else
            {
                detailsSection.style.display = DisplayStyle.None;
            }

            // Show/hide suggestion section
            if (!string.IsNullOrEmpty(this.errorState.Suggestion))
            {
                suggestionSection.style.display = DisplayStyle.Flex;
                suggestionText.text = this.errorState.Suggestion;
            }
            else
            {
                suggestionSection.style.display = DisplayStyle.None;
            }

            // Set location information
            locationText.text = $"Column: {this.errorState.ColumnName} | Row: {this.errorState.RowIndex + 1} | Object: {this.errorState.ObjectName}";

            return this.rootElement;
        }

        /// <summary>
        /// Shows an error tooltip at the specified position.
        /// </summary>
        /// <param name="position">The position to show the tooltip.</param>
        /// <param name="errorState">The error state to display.</param>
        public static void ShowTooltip(Vector2 position, CellErrorState errorState)
        {
            var tooltip = new ErrorTooltipDrawer(errorState);
            var rect = new Rect(position, Vector2.zero);
            PopupWindow.Show(rect, tooltip);
        }

        private Vector2 CalculatePreferredSize()
        {
            const float maxWidth = 400f;
            const float minWidth = 250f;
            const float padding = 16f;
            const float lineHeight = 18f;

            var width = minWidth;
            var height = padding + lineHeight; // Title

            // Calculate width and height for details
            if (!string.IsNullOrEmpty(this.errorState.DetailedDescription))
            {
                height += lineHeight + 4; // "Details:" label
                var detailsLines = Mathf.CeilToInt(this.errorState.DetailedDescription.Length / 60f);
                height += detailsLines * lineHeight + 4;
                width = Mathf.Max(width, Mathf.Min(maxWidth, this.errorState.DetailedDescription.Length * 7f));
            }

            // Calculate for suggestion
            if (!string.IsNullOrEmpty(this.errorState.Suggestion))
            {
                height += lineHeight + 4; // "Suggestion:" label
                var suggestionLines = Mathf.CeilToInt(this.errorState.Suggestion.Length / 60f);
                height += suggestionLines * lineHeight + 4;
                width = Mathf.Max(width, Mathf.Min(maxWidth, this.errorState.Suggestion.Length * 7f));
            }

            // Location info
            height += lineHeight + 8;

            return new Vector2(Mathf.Min(width, maxWidth), height + padding);
        }

        private Texture2D? GetSeverityIcon()
        {
            return this.errorState.Severity switch
            {
                ErrorSeverity.Warning => EditorGUIUtility.IconContent("console.warnicon").image as Texture2D,
                _ => EditorGUIUtility.IconContent("console.erroricon").image as Texture2D,
            };
        }

        private Color GetSeverityColor()
        {
            return this.errorState.Severity switch
            {
                ErrorSeverity.Warning => new Color(1f, 0.65f, 0f), // Orange
                ErrorSeverity.Error => new Color(1f, 0.4f, 0.4f),  // Light red
                ErrorSeverity.Critical => new Color(0.8f, 0.2f, 0.2f), // Dark red
                _ => new Color(1f, 0.4f, 0.4f),
            };
        }
    }
}