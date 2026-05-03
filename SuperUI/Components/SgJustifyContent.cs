namespace SuperUI.Components;

/// <summary>
/// Defines the main-axis alignment of items in <see cref="SgStack"/>.
/// Maps to CSS <c>justify-content</c>.
/// </summary>
public enum SgJustifyContent
{
    /// <summary>Items pack toward the start of the main axis (default).</summary>
    Start,
    /// <summary>Items are centered along the main axis.</summary>
    Center,
    /// <summary>Items pack toward the end of the main axis.</summary>
    End,
    /// <summary>Items are evenly distributed with space between them.</summary>
    SpaceBetween,
    /// <summary>Items are evenly distributed with space around them.</summary>
    SpaceAround
}
