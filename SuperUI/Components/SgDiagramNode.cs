namespace SuperUI.Components;

/// <summary>
/// Represents a node in the <see cref="SgDiagram"/> canvas.
/// </summary>
public class SgDiagramNode
{
    /// <summary>Unique identifier.</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>Display label.</summary>
    public string? Label { get; set; }

    /// <summary>X coordinate in pixels.</summary>
    public double X { get; set; }

    /// <summary>Y coordinate in pixels.</summary>
    public double Y { get; set; }

    /// <summary>Node width in pixels. Default is 160.</summary>
    public double Width { get; set; } = 160;

    /// <summary>Node height in pixels. Default is 64.</summary>
    public double Height { get; set; } = 64;

    /// <summary>Accent color.</summary>
    public string? Color { get; set; }

    /// <summary>Visual shape of the node. Default is Rectangle.</summary>
    public SgDiagramNodeShape Shape { get; set; } = SgDiagramNodeShape.Rectangle;

    /// <summary>Optional custom data payload.</summary>
    public object? Data { get; set; }
}
