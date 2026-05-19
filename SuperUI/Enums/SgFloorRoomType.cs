namespace SuperUI.Enums;

/// <summary>Floor-room / zone type used in Konva-based warehouse and floor-plan layouts.</summary>
public enum SgFloorRoomType
{
    /// <summary>Standard storage zone.</summary>
    Storage = 0,
    /// <summary>Staging / buffer zone.</summary>
    Staging = 1,
    /// <summary>Receiving / inbound zone.</summary>
    Receiving = 2,
    /// <summary>Shipping / outbound zone.</summary>
    Shipping = 3,
    /// <summary>Quality-control inspection zone.</summary>
    QualityControl = 4,
    /// <summary>Cold / refrigerated zone.</summary>
    ColdStorage = 5,
    /// <summary>Hazardous materials zone.</summary>
    Hazardous = 6,
    /// <summary>Dead / inactive zone.</summary>
    Dead = 7,
    /// <summary>Office / admin room.</summary>
    Office = 8,
    /// <summary>Reception / front desk.</summary>
    Reception = 9,
    /// <summary>Lobby / entrance area.</summary>
    Lobby = 10,
    /// <summary>Elevator / lift room.</summary>
    Elevator = 11,
    /// <summary>Stairs / stairwell.</summary>
    Stairs = 12,
    /// <summary>Corridor / hallway.</summary>
    Corridor = 13,
    /// <summary>Meeting room / conference.</summary>
    MeetingRoom = 14,
    /// <summary>Server / IT room.</summary>
    ServerRoom = 15,
    /// <summary>Open-plan workspace.</summary>
    OpenSpace = 16,
    /// <summary>Kitchen / pantry.</summary>
    Kitchen = 17,
    /// <summary>Restroom / WC.</summary>
    Restroom = 18
}
