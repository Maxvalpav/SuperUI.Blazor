namespace SuperUI.Components;

/// <summary>Visualisation options for <see cref="SgWarehouse"/>.</summary>
public class SgWarehouseOptions
{
    public SgWhColorMode ColorMode { get; set; } = SgWhColorMode.Status;

    public bool ShowLabels { get; set; } = true;
    public bool ShowAisleLabels { get; set; } = true;
    public bool ShowGrid { get; set; } = true;

    public string? HighlightSku { get; set; }
    public HashSet<SgWhCellStatus>? HighlightStatuses { get; set; }

    public bool ShowOccupancyBar { get; set; } = true;
    public bool ShowLegend { get; set; } = true;
    public bool ReadOnly { get; set; } = true;

    public string? FocusRackId { get; set; }
}
