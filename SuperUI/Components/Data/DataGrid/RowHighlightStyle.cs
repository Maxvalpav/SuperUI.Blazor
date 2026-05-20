namespace SuperUI.Components;

/// <summary>
/// Represents styling options for a data grid row.
/// Used with <see cref="SgDataGrid{TItem}.RowStyle"/> parameter for programmatic row coloring.
/// </summary>
public sealed class RowHighlightStyle
{
    /// <summary>
    /// Gets or sets the background color for the row (e.g., "#fff3cd", "rgba(255, 243, 205, 0.5)").
    /// </summary>
    public string? BackgroundColor { get; set; }

    /// <summary>
    /// Gets or sets the text color for the row (e.g., "#856404", "rgb(133, 100, 4)").
    /// </summary>
    public string? TextColor { get; set; }

    /// <summary>
    /// Gets or sets the font weight for the row (e.g., "bold", "600").
    /// </summary>
    public string? FontWeight { get; set; }

    /// <summary>
    /// Gets or sets the font style for the row (e.g., "italic", "normal").
    /// </summary>
    public string? FontStyle { get; set; }

    /// <summary>
    /// Gets or sets additional CSS properties as a semicolon-separated string.
    /// Example: "border-left: 3px solid red; opacity: 0.8;"
    /// </summary>
    public string? AdditionalCss { get; set; }

    /// <summary>
    /// Creates a new <see cref="RowHighlightStyle"/> with the specified background and text colors.
    /// </summary>
    public static RowHighlightStyle Create(string? backgroundColor = null, string? textColor = null)
    {
        return new RowHighlightStyle
        {
            BackgroundColor = backgroundColor,
            TextColor = textColor
        };
    }

    /// <summary>
    /// Converts the style to an inline CSS string.
    /// </summary>
    internal string ToCssString()
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(BackgroundColor))
            parts.Add($"background-color: {BackgroundColor}");

        if (!string.IsNullOrWhiteSpace(TextColor))
            parts.Add($"color: {TextColor}");

        if (!string.IsNullOrWhiteSpace(FontWeight))
            parts.Add($"font-weight: {FontWeight}");

        if (!string.IsNullOrWhiteSpace(FontStyle))
            parts.Add($"font-style: {FontStyle}");

        if (!string.IsNullOrWhiteSpace(AdditionalCss))
            parts.Add(AdditionalCss.TrimEnd(';'));

        return parts.Count > 0 ? string.Join("; ", parts) + ";" : string.Empty;
    }
}
