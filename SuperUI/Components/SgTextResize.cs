namespace SuperUI.Components;

/// <summary>
/// Controls the resize handle behaviour of a textarea.
/// </summary>
public enum SgTextResize
{
    /// <summary>No resize handle is shown.</summary>
    None,

    /// <summary>The textarea can be resized vertically (default).</summary>
    Vertical,

    /// <summary>The textarea can be resized horizontally.</summary>
    Horizontal,

    /// <summary>The textarea can be resized in both directions.</summary>
    Both
}
