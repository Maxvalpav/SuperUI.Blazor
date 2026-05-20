namespace SuperUI.Components;

/// <summary>
/// Visual shape of a node in <see cref="SgDiagram"/>.
/// </summary>
public enum SgDiagramNodeShape
{
    /// <summary>Rectangle with rounded corners. Default.</summary>
    Rectangle,
    /// <summary>Circle. Best with equal width and height.</summary>
    Circle,
    /// <summary>Diamond (rotated square) for decision points.</summary>
    Diamond
}
