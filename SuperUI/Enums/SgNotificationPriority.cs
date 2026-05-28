namespace SuperUI.Enums;

/// <summary>
/// Priority level for a notification item.
/// </summary>
public enum SgNotificationPriority
{
    /// <summary>Default / unspecified priority.</summary>
    Default = 0,

    /// <summary>Low-priority informational notification.</summary>
    Low = 1,

    /// <summary>Normal-priority notification.</summary>
    Normal = 2,

    /// <summary>High-priority notification.</summary>
    High = 3,

    /// <summary>Urgent notification requiring immediate attention.</summary>
    Urgent = 4
}
