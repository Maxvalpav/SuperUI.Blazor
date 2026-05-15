namespace SuperUI.Enums;

/// <summary>Typology of warehouse logical zone.</summary>
public enum SgWhZoneType
{
    /// <summary>General-purpose storage zone.</summary>
    Storage = 0,
    /// <summary>Receiving / inbound area.</summary>
    Receiving = 1,
    /// <summary>Shipping / outbound area.</summary>
    Shipping = 2,
    /// <summary>Quality-control inspection zone.</summary>
    QualityControl = 3,
    /// <summary>Staging / temporary holding area.</summary>
    Staging = 4,
    /// <summary>Temperature-controlled zone.</summary>
    ColdStorage = 5,
    /// <summary>Freezing / deep-freeze zone.</summary>
    Freezer = 6,
    /// <summary>Hazardous / flammable materials area.</summary>
    Hazardous = 7,
    /// <summary>Overflow / excess-inventory area.</summary>
    Overflow = 8,
    /// <summary>Returns processing area.</summary>
    Returns = 9,
    /// <summary>Active picking / order-pick zone.</summary>
    Picking = 10,
    /// <summary>Quarantine / hold zone.</summary>
    Quarantine = 11,
    /// <summary>Cold storage zone.</summary>
    Cold = 12,
    /// <summary>Hazardous materials area.</summary>
    Hazard = 13,
    /// <summary>Office / admin room.</summary>
    Office = 14
}
