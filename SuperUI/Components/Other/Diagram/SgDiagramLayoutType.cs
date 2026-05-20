namespace SuperUI.Components;

/// <summary>
/// Auto-layout algorithm for <see cref="SgDiagram"/>.
/// </summary>
public enum SgDiagramLayoutType
{
    /// <summary>Use the X/Y coordinates provided on each node.</summary>
    None,
    /// <summary>Arrange nodes in a top-down hierarchical tree.</summary>
    Hierarchy
}
