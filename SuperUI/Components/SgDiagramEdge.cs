using SuperUI.Enums;

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

    /// <summary>Edge line style (moved to SuperUI.Enums.SgDiagramEdgeType).</summary>
    public SgDiagramEdgeType Type { get; set; } = SgDiagramEdgeType.Straight;

    /// <summary>Optional stroke colour for the edge line (default = none = use theme border colour).</summary>
    public string? Color { get; set; }
}