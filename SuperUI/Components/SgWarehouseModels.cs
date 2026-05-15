using SuperUI.Enums;

namespace SuperUI.Components;

// ── Layout entities ───────────────────────────────────────────────────────────

/// <summary>A logical zone inside the warehouse (free-form rectangle on the floor).</summary>
public class SgWhZone
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public SgWhZoneType Type { get; set; } = SgWhZoneType.Storage;

    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }

    public string? Color { get; set; }
}

/// <summary>A non-storage corridor / aisle drawn on the floor.</summary>
public class SgWhAisle
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public string? Label { get; set; }
}

/// <summary>A rack (shelf unit) standing inside a zone.</summary>
public class SgWhRack
{
    public string Id { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? ZoneId { get; set; }

    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }

    public int Levels { get; set; } = 4;
    public int CellsPerLevel { get; set; } = 6;

    public SgWhTemperature Temperature { get; set; } = SgWhTemperature.Ambient;

    public double? MaxLoadKg { get; set; }

    public List<SgWhCell> Cells { get; set; } = new();
}

/// <summary>A single addressable cell inside a rack.</summary>
public class SgWhCell
{
    public string Id { get; set; } = string.Empty;

    public int Level { get; set; }
    public int Position { get; set; }

    public SgWhCellStatus Status { get; set; } = SgWhCellStatus.Empty;
    public SgWhAbcClass Abc { get; set; } = SgWhAbcClass.None;

    public string? Sku { get; set; }
    public string? ProductName { get; set; }
    public int? Quantity { get; set; }
    public int? Capacity { get; set; }
    public double? WeightKg { get; set; }
    public string? Lot { get; set; }
    public DateTime? ExpiresOn { get; set; }
    public int? Turnover30d { get; set; }
    public DateTime? LastMovedAt { get; set; }
    public string? Meta { get; set; }
}

/// <summary>Top-level warehouse layout.</summary>
public class SgWhLayout
{
    public string Name { get; set; } = "Warehouse";

    public double Width { get; set; } = 1200;
    public double Height { get; set; } = 800;

    public List<SgWhZone> Zones { get; set; } = new();
    public List<SgWhAisle> Aisles { get; set; } = new();
    public List<SgWhRack> Racks { get; set; } = new();
}

// ── Events ────────────────────────────────────────────────────────────────────

/// <summary>Cell click event arguments.</summary>
public class SgWhCellEventArgs
{
    public string RackId { get; set; } = string.Empty;
    public string RackCode { get; set; } = string.Empty;
    public string CellId { get; set; } = string.Empty;
    public int Level { get; set; }
    public int Position { get; set; }
    public SgWhCell? Cell { get; set; }
}

/// <summary>Rack click event arguments.</summary>
public class SgWhRackEventArgs
{
    public string RackId { get; set; } = string.Empty;
    public SgWhRack? Rack { get; set; }
}
