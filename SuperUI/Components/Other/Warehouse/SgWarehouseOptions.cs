using SuperUI.Enums;

namespace SuperUI.Components;

/// <summary>Visualisation options for <see cref="SgWarehouse"/>.</summary>
/// <summary>Visualisation options for <see cref="SgWarehouse"/>.</summary>
public class SgWarehouseOptions
{
    /// <summary>Color mode for rendering cells.</summary>
    public SgWhColorMode ColorMode { get; set; } = SgWhColorMode.Status;
    /// <summary>Whether to show labels on cells.</summary>
    public bool ShowLabels { get; set; } = true;
    /// <summary>Whether to show labels on aisles.</summary>
    public bool ShowAisleLabels { get; set; } = true;
    /// <summary>Whether to show the floor grid.</summary>
    public bool ShowGrid { get; set; } = true;
    /// <summary>Highlight cells containing this SKU.</summary>
    public string? HighlightSku { get; set; }
    /// <summary>Highlight cells with these statuses.</summary>
    public HashSet<SgWhCellStatus>? HighlightStatuses { get; set; }
    /// <summary>Show occupancy bar chart.</summary>
    public bool ShowOccupancyBar { get; set; } = true;
    /// <summary>Show color legend.</summary>
    public bool ShowLegend { get; set; } = true;
    /// <summary>Whether interaction is disabled.</summary>
    public bool ReadOnly { get; set; } = true;
    /// <summary>Auto-focus a specific rack by its ID.</summary>
    public string? FocusRackId { get; set; }
}
