using System.Collections.Generic;

namespace SuperUI.Components.Navigation.Tour;

public enum SgTourPlacement
{
    Top,
    TopStart,
    TopEnd,
    Bottom,
    BottomStart,
    BottomEnd,
    Left,
    LeftStart,
    LeftEnd,
    Right,
    RightStart,
    RightEnd,
    Center
}

public class SgTourRect
{
    public double Top { get; set; }
    public double Left { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public double Bottom { get; set; }
    public double Right { get; set; }
}

public class SgTourStepConfig
{
    public string? Target { get; set; }
    public string? Title { get; set; }
    public string? Content { get; set; }
    public SgTourPlacement Placement { get; set; } = SgTourPlacement.Bottom;
    public bool Mask { get; set; } = true;
    public double Padding { get; set; } = 10;
}
