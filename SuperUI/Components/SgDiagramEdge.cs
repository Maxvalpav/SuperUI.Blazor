namespace SuperUI.Components;

/// <summary>
/// Represents a connection between two nodes in <see cref="SgDiagram"/>.
/// </summary>
public class SgDiagramEdge
{
    /// <summary>Unique identifier.</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>Source node id.</summary>
    public string SourceId { get; set; } = "";

    /// <summary>Target node id.</summary>
    public string TargetId { get; set; } = "";

    /// <summary>Optional label shown on the edge.</summary>
    public string? Label { get; set; }

    /// <summary>Edge line style. Default is Straight.</summary>
    public SgDiagramEdgeType Type { get; set; } = SgDiagramEdgeType.Straight;

    /// <summary>Accent color for the edge.</summary>
    public string? Color { get; set; }
}

/// <summary>
/// Edge routing style.
/// </summary>
public enum SgDiagramEdgeType
{
    /// <summary>Straight line from source to target.</summary>
    Straight,
    /// <summary>Curved bezier line.</summary>
    Curved,
    /// <summary>Orthogonal line with right angles.</summary>
    Orthogonal
}
