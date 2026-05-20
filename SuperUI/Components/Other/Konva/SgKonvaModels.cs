using SuperUI.Enums;

namespace SuperUI.Components;

// ── Shape types ───────────────────────────────────────────────────────────────
// SgKonvaShapeType — moved to SuperUI.Enums.SgKonvaShapeType

// ── Floor plan room types ─────────────────────────────────────────────────────
// SgFloorRoomType — moved to SuperUI.Enums.SgFloorRoomType
// SgFloorRoomStatus — moved to SuperUI.Enums.SgFloorRoomStatus

// ── Floor plan models ─────────────────────────────────────────────────────────

/// <summary>A single room or zone on the floor plan.</summary>
public class SgFloorRoom
{
    /// <summary>Unique room identifier.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Display name (e.g. "Conference A", "Kitchen").</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Functional type of the room.</summary>
    public SgFloorRoomType Type { get; set; } = SgFloorRoomType.Office;

    /// <summary>Occupancy status.</summary>
    public SgFloorRoomStatus Status { get; set; } = SgFloorRoomStatus.Available;

    /// <summary>X position in logical units.</summary>
    public double X { get; set; }

    /// <summary>Y position in logical units.</summary>
    public double Y { get; set; }

    /// <summary>Width in logical units.</summary>
    public double Width { get; set; }

    /// <summary>Height in logical units.</summary>
    public double Height { get; set; }

    /// <summary>Number of seats / workstations.</summary>
    public int? Capacity { get; set; }

    /// <summary>Current number of people in the room.</summary>
    public int? CurrentOccupancy { get; set; }

    /// <summary>Optional floor number.</summary>
    public int Floor { get; set; } = 1;

    /// <summary>Optional extra metadata (JSON string).</summary>
    public string? Meta { get; set; }
}

/// <summary>A wall segment on the floor plan.</summary>
public class SgFloorWall
{
    public double X1 { get; set; }
    public double Y1 { get; set; }
    public double X2 { get; set; }
    public double Y2 { get; set; }
    public double Thickness { get; set; } = 8;
}

/// <summary>Complete floor plan layout.</summary>
public class SgFloorPlan
{
    /// <summary>Floor plan name.</summary>
    public string Name { get; set; } = "Floor Plan";

    /// <summary>Logical width of the plan.</summary>
    public double Width { get; set; } = 1000;

    /// <summary>Logical height of the plan.</summary>
    public double Height { get; set; } = 700;

    /// <summary>Rooms / zones.</summary>
    public List<SgFloorRoom> Rooms { get; set; } = new();

    /// <summary>Wall segments (optional, drawn on top of rooms).</summary>
    public List<SgFloorWall> Walls { get; set; } = new();
}

// ── Events ────────────────────────────────────────────────────────────────────

/// <summary>Arguments passed when a shape or room is clicked.</summary>
public class SgKonvaClickEventArgs
{
    /// <summary>ID of the clicked element.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Type name of the clicked element (room type or shape type).</summary>
    public string TypeName { get; set; } = string.Empty;

    /// <summary>Canvas X coordinate of the click.</summary>
    public double X { get; set; }

    /// <summary>Canvas Y coordinate of the click.</summary>
    public double Y { get; set; }

    /// <summary>Optional JSON data payload attached to the element.</summary>
    public string? Data { get; set; }
}

/// <summary>Arguments passed when a draggable shape is moved.</summary>
public class SgKonvaDragEventArgs
{
    /// <summary>ID of the moved element.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>New X position.</summary>
    public double X { get; set; }

    /// <summary>New Y position.</summary>
    public double Y { get; set; }
}
