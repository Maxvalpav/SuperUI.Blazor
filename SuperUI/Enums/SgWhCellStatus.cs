namespace SuperUI.Enums;

/// <summary>Occupancy status of a warehouse cell / bin.</summary>
public enum SgWhCellStatus
{
    /// <summary>Empty slot.</summary>
    Empty = 0,
    /// <summary>Partially filled.</summary>
    Partial = 1,
    /// <summary>Fully occupied.</summary>
    Full = 2,
    /// <summary>Blocked / inaccessible.</summary>
    Blocked = 3,
    /// <summary>Reserved for a specific order.</summary>
    Reserved = 4,
    /// <summary>On quality hold.</summary>
    QcHold = 5,
    /// <summary>Expired / expired-lot item.</summary>
    Expired = 6,
    /// <summary>Fully occupied / in use.</summary>
    Occupied = 7,
    /// <summary>Active picking / order-pick slot.</summary>
    Picking = 8,
    /// <summary>Damaged / broken cell.</summary>
    Damaged = 9
}
