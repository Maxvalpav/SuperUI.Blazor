namespace SuperUI.Enums;

/// <summary>Operational status of a floor-room / cell in a warehouse or floor-plan layout.</summary>
public enum SgFloorRoomStatus
{
    /// <summary>Empty / no SKU stored.</summary>
    Empty = 0,
    /// <summary>Partially filled.</summary>
    Partial = 1,
    /// <summary>Fully occupied.</summary>
    Full = 2,
    /// <summary>Reserved / not available for placement.</summary>
    Reserved = 3,
    /// <summary>Blocked (e.g. damaged).</summary>
    Blocked = 4,
    /// <summary>Under quality-hold.</summary>
    QC = 5,
    /// <summary>Expired / expired-lot product.</summary>
    Expired = 6,
    /// <summary>Available / free to use.</summary>
    Available = 7,
    /// <summary>Currently occupied / in use.</summary>
    Occupied = 8,
    /// <summary>Scheduled for maintenance.</summary>
    Maintenance = 9,
    /// <summary>Temporarily closed.</summary>
    Closed = 10
}
