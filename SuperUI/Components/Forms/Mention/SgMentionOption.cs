namespace SuperUI.Components;

/// <summary>
/// Represents a single suggestion item in the <see cref="SgMention"/> component.
/// </summary>
public class SgMentionOption
{
    /// <summary>The value inserted into the text when selected.</summary>
    public string Value { get; set; } = "";

    /// <summary>Display text shown in the suggestion list.</summary>
    public string DisplayText { get; set; } = "";

    /// <summary>Optional image/avatar URL shown next to the display text.</summary>
    public string? ImageUrl { get; set; }

    /// <summary>Optional group heading under which this item appears.</summary>
    public string? Group { get; set; }
}
