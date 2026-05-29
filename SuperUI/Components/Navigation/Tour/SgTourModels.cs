using System.Collections.Generic;

namespace SuperUI.Components.Navigation.Tour;

/// <summary>Placement position for a tour step popover relative to its target element.</summary>
public enum SgTourPlacement
{
    /// <summary>Position above the target, centered.</summary>
    Top,
    /// <summary>Position above the target, aligned to the start (left).</summary>
    TopStart,
    /// <summary>Position above the target, aligned to the end (right).</summary>
    TopEnd,
    /// <summary>Position below the target, centered.</summary>
    Bottom,
    /// <summary>Position below the target, aligned to the start (left).</summary>
    BottomStart,
    /// <summary>Position below the target, aligned to the end (right).</summary>
    BottomEnd,
    /// <summary>Position to the left of the target, centered vertically.</summary>
    Left,
    /// <summary>Position to the left of the target, aligned to the top.</summary>
    LeftStart,
    /// <summary>Position to the left of the target, aligned to the bottom.</summary>
    LeftEnd,
    /// <summary>Position to the right of the target, centered vertically.</summary>
    Right,
    /// <summary>Position to the right of the target, aligned to the top.</summary>
    RightStart,
    /// <summary>Position to the right of the target, aligned to the bottom.</summary>
    RightEnd,
    /// <summary>Position at the center of the screen, ignoring the target.</summary>
    Center
}

/// <summary>Bounding rectangle of a target element on the page.</summary>
public class SgTourRect
{
    /// <summary>Distance from the top of the viewport.</summary>
    public double Top { get; set; }
    /// <summary>Distance from the left of the viewport.</summary>
    public double Left { get; set; }
    /// <summary>Width of the element.</summary>
    public double Width { get; set; }
    /// <summary>Height of the element.</summary>
    public double Height { get; set; }
    /// <summary>Distance from the bottom of the viewport (Top + Height).</summary>
    public double Bottom { get; set; }
    /// <summary>Distance from the right of the viewport (Left + Width).</summary>
    public double Right { get; set; }
}

/// <summary>Configuration for a tour step used with <see cref="SgTour"/>.</summary>
public class SgTourStepConfig
{
    /// <summary>CSS selector of the target element to highlight.</summary>
    public string? Target { get; set; }
    /// <summary>Title text for the step.</summary>
    public string? Title { get; set; }
    /// <summary>Body content text for the step.</summary>
    public string? Content { get; set; }
    /// <summary>Placement of the popover relative to the target.</summary>
    public SgTourPlacement Placement { get; set; } = SgTourPlacement.Bottom;
    /// <summary>Whether to show a dark overlay mask behind the target.</summary>
    public bool Mask { get; set; } = true;
    /// <summary>Padding around the target highlight area in pixels.</summary>
    public double Padding { get; set; } = 10;
}
