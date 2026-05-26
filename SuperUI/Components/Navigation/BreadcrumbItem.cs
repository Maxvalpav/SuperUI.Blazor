namespace SuperUI.Components;

/// <summary>
/// Represents a single item in a <see cref="SgBreadcrumb"/> component.
/// </summary>
public sealed class BreadcrumbItem
{
    /// <summary>
    /// Creates a new breadcrumb item.
    /// </summary>
    public BreadcrumbItem() { }

    /// <summary>
    /// Creates a new breadcrumb item with the specified text and optional href.
    /// </summary>
    /// <param name="text">The display text.</param>
    /// <param name="href">Optional URL. If null, the item renders as plain text (current page).</param>
    public BreadcrumbItem(string text, string? href = null)
    {
        Text = text;
        Href = href;
    }

    /// <summary>
    /// Gets or sets the display text of the breadcrumb item.
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the URL for the breadcrumb link.
    /// If null or empty, the item renders as plain text (current page indicator).
    /// </summary>
    public string? Href { get; set; }

    /// <summary>
    /// Gets or sets an optional icon to display before the text.
    /// Can be an emoji string, a URL to an image, or SVG markup.
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// Gets or sets whether this item is disabled.
    /// Disabled items are not clickable and appear dimmed.
    /// </summary>
    public bool Disabled { get; set; }

    /// <summary>
    /// Gets or sets the maximum width of the item in pixels.
    /// When set, text exceeding this width is truncated with an ellipsis.
    /// </summary>
    public int? MaxWidth { get; set; }
}
