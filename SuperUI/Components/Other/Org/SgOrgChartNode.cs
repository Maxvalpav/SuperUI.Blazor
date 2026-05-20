namespace SuperUI.Components;

/// <summary>
/// Represents a node in the <see cref="SgOrgChart"/> hierarchy.
/// </summary>
public class SgOrgChartNode
{
    /// <summary>Unique identifier for the node.</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>Display name of the person or unit.</summary>
    public string Name { get; set; } = "";

    /// <summary>Job title or role.</summary>
    public string? Title { get; set; }

    /// <summary>Department or division name.</summary>
    public string? Department { get; set; }

    /// <summary>URL to the avatar image.</summary>
    public string? AvatarUrl { get; set; }

    /// <summary>Initials to display when no avatar image is provided.</summary>
    public string? Initials { get; set; }

    /// <summary>Custom accent color for the node card.</summary>
    public string? Color { get; set; }

    /// <summary>Whether the node's children are visible. Default is true.</summary>
    public bool Expanded { get; set; } = true;

    /// <summary>Child nodes in the hierarchy.</summary>
    public List<SgOrgChartNode> Children { get; set; } = new();

    /// <summary>
    /// Optional numeric value assigned to this node (leaf value).
    /// When <see cref="SgOrgChart.ShowSum"/> is enabled, leaf nodes use this value
    /// and parent nodes accumulate the sum of all their descendants.
    /// </summary>
    public double? Value { get; set; }

    /// <summary>
    /// Computed cumulative sum for this node (populated by <see cref="SgOrgChart"/>
    /// when <see cref="SgOrgChart.ShowSum"/> is true).
    /// Equals own <see cref="Value"/> plus the sum of all children's <see cref="ComputedSum"/>.
    /// Do not set manually.
    /// </summary>
    public double ComputedSum { get; internal set; }

    /// <summary>
    /// Sum contributed by children only (ComputedSum - Value).
    /// Populated alongside <see cref="ComputedSum"/>. Do not set manually.
    /// </summary>
    public double ChildrenSum { get; internal set; }
}
