using SuperUI.Enums;

namespace SuperUI.Components;

// ── Layout entities ───────────────────────────────────────────────────────────

/// <summary>A logical zone inside the warehouse (free-form rectangle on the floor).</summary>
public class SgWhZone
{
    /// <summary>Unique zone identifier.</summary>
    public string Id { get; set; } = string.Empty;
    /// <summary>Display name of the zone.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Zone type (storage, receiving, shipping, etc.).</summary>
    public SgWhZoneType Type { get; set; } = SgWhZoneType.Storage;
    /// <summary>X coordinate of the zone on the warehouse floor.</summary>
    public double X { get; set; }
    /// <summary>Y coordinate of the zone on the warehouse floor.</summary>
    public double Y { get; set; }
    /// <summary>Width of the zone rectangle.</summary>
    public double Width { get; set; }
    /// <summary>Height of the zone rectangle.</summary>
    public double Height { get; set; }
    /// <summary>Custom color for the zone.</summary>
    public string? Color { get; set; }
}

/// <summary>A non-storage corridor / aisle drawn on the floor.</summary>
public class SgWhAisle
{
    /// <summary>X coordinate of the aisle.</summary>
    public double X { get; set; }
    /// <summary>Y coordinate of the aisle.</summary>
    public double Y { get; set; }
    /// <summary>Width of the aisle.</summary>
    public double Width { get; set; }
    /// <summary>Height of the aisle.</summary>
    public double Height { get; set; }
    /// <summary>Optional label for the aisle.</summary>
    public string? Label { get; set; }
}

/// <summary>A rack (shelf unit) standing inside a zone.</summary>
public class SgWhRack
{
    /// <summary>Unique rack identifier.</summary>
    public string Id { get; set; } = string.Empty;
    /// <summary>Human-readable rack code (e.g. "A-01").</summary>
    public string Code { get; set; } = string.Empty;
    /// <summary>Zone this rack belongs to.</summary>
    public string? ZoneId { get; set; }
    /// <summary>X coordinate on the warehouse floor.</summary>
    public double X { get; set; }
    /// <summary>Y coordinate on the warehouse floor.</summary>
    public double Y { get; set; }
    /// <summary>Rack visual width.</summary>
    public double Width { get; set; }
    /// <summary>Rack visual height.</summary>
    public double Height { get; set; }
    /// <summary>Number of shelf levels.</summary>
    public int Levels { get; set; } = 4;
    /// <summary>Number of cell positions per level.</summary>
    public int CellsPerLevel { get; set; } = 6;
    /// <summary>Temperature zone classification.</summary>
    public SgWhTemperature Temperature { get; set; } = SgWhTemperature.Ambient;
    /// <summary>Maximum load capacity in kg.</summary>
    public double? MaxLoadKg { get; set; }
    /// <summary>List of cells in this rack.</summary>
    public List<SgWhCell> Cells { get; set; } = new();
}

/// <summary>A single addressable cell inside a rack.</summary>
public class SgWhCell
{
    /// <summary>Unique cell identifier.</summary>
    public string Id { get; set; } = string.Empty;
    /// <summary>Shelf level (1 = ground level).</summary>
    public int Level { get; set; }
    /// <summary>Position within the level (1-based).</summary>
    public int Position { get; set; }
    /// <summary>Occupancy status of the cell.</summary>
    public SgWhCellStatus Status { get; set; } = SgWhCellStatus.Empty;
    /// <summary>ABC classification for slotting optimization.</summary>
    public SgWhAbcClass Abc { get; set; } = SgWhAbcClass.None;
    /// <summary>SKU stored in the cell.</summary>
    public string? Sku { get; set; }
    /// <summary>Product name.</summary>
    public string? ProductName { get; set; }
    /// <summary>Current quantity stored.</summary>
    public int? Quantity { get; set; }
    /// <summary>Maximum capacity of the cell.</summary>
    public int? Capacity { get; set; }
    /// <summary>Weight in kg.</summary>
    public double? WeightKg { get; set; }
    /// <summary>Lot or batch number.</summary>
    public string? Lot { get; set; }
    /// <summary>Expiration date of the product.</summary>
    public DateTime? ExpiresOn { get; set; }
    /// <summary>30-day turnover count.</summary>
    public int? Turnover30d { get; set; }
    /// <summary>Timestamp of last movement.</summary>
    public DateTime? LastMovedAt { get; set; }
    /// <summary>Custom metadata.</summary>
    public string? Meta { get; set; }
}

/// <summary>Top-level warehouse layout.</summary>
public class SgWhLayout
{
    /// <summary>Warehouse display name.</summary>
    public string Name { get; set; } = "Warehouse";
    /// <summary>Floor plan width.</summary>
    public double Width { get; set; } = 1200;
    /// <summary>Floor plan height.</summary>
    public double Height { get; set; } = 800;
    /// <summary>Zones on the warehouse floor.</summary>
    public List<SgWhZone> Zones { get; set; } = new();
    /// <summary>Aisles / corridors.</summary>
    public List<SgWhAisle> Aisles { get; set; } = new();
    /// <summary>Racks placed on the floor.</summary>
    public List<SgWhRack> Racks { get; set; } = new();
}

// ── Events ────────────────────────────────────────────────────────────────────

/// <summary>Cell click event arguments.</summary>
public class SgWhCellEventArgs
{
    /// <summary>ID of the parent rack.</summary>
    public string RackId { get; set; } = string.Empty;
    /// <summary>Human-readable rack code.</summary>
    public string RackCode { get; set; } = string.Empty;
    /// <summary>ID of the clicked cell.</summary>
    public string CellId { get; set; } = string.Empty;
    /// <summary>Shelf level of the cell.</summary>
    public int Level { get; set; }
    /// <summary>Position within the level.</summary>
    public int Position { get; set; }
    /// <summary>Full cell data.</summary>
    public SgWhCell? Cell { get; set; }
}

/// <summary>Rack click event arguments.</summary>
public class SgWhRackEventArgs
{
    /// <summary>ID of the clicked rack.</summary>
    public string RackId { get; set; } = string.Empty;
    /// <summary>Full rack data.</summary>
    public SgWhRack? Rack { get; set; }
}
