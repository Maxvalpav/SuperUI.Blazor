using System;
using System.Collections.Generic;

namespace SuperUI.Components.SgMachineScheduler.Models;

/// <summary>Operational status of a machine resource.</summary>
public enum MachineStatus
{
    /// <summary>Machine is running normally.</summary>
    Online,
    /// <summary>Machine is powered off or disconnected.</summary>
    Offline,
    /// <summary>Machine is undergoing scheduled maintenance.</summary>
    Maintenance,
    /// <summary>Machine has a fault or error.</summary>
    Fault
}

/// <summary>Status of a machine reservation (job).</summary>
public enum ReservationStatus
{
    /// <summary>Job is planned for future execution.</summary>
    Planned,
    /// <summary>Job is currently in progress.</summary>
    InProgress,
    /// <summary>Job has been completed.</summary>
    Completed,
    /// <summary>Job is running behind schedule.</summary>
    Delayed,
    /// <summary>Job has passed its deadline.</summary>
    Overdue,
    /// <summary>Job has been cancelled.</summary>
    Cancelled
}

/// <summary>Reason for a machine downtime period.</summary>
public enum DowntimeReason
{
    /// <summary>Machine setup or changeover.</summary>
    Setup,
    /// <summary>Machine breakdown.</summary>
    Breakdown,
    /// <summary>Waiting for material.</summary>
    MaterialWait,
    /// <summary>Scheduled maintenance.</summary>
    Maintenance,
    /// <summary>No operator available.</summary>
    NoOperator
}

/// <summary>Represents a machine or production resource in the scheduler.</summary>
public record MachineResource
{
    /// <summary>Unique identifier.</summary>
    public int Id { get; init; }
    /// <summary>Display name of the machine.</summary>
    public string Name { get; init; } = string.Empty;
    /// <summary>Group or department the machine belongs to.</summary>
    public string Group { get; init; } = string.Empty;
    /// <summary>Physical cell or location.</summary>
    public string Cell { get; init; } = string.Empty;
    /// <summary>Current operational status.</summary>
    public MachineStatus Status { get; set; }
    /// <summary>Maximum simultaneous capacity units.</summary>
    public int MaxCapacityUnits { get; init; } = 1;
    /// <summary>Machine color for the scheduler UI.</summary>
    public string Color { get; init; } = "#4FC3F7";
    /// <summary>Shift patterns defined for this machine.</summary>
    public List<ShiftPattern> Shifts { get; init; } = new();
    /// <summary>Hourly operating cost.</summary>
    public double HourlyRate { get; init; }
}

/// <summary>Represents a scheduled job or operation on a machine.</summary>
public record MachineReservation
{
    /// <summary>Unique identifier.</summary>
    public int Id { get; init; }
    /// <summary>Machine resource this reservation is assigned to.</summary>
    public int MachineId { get; set; }
    /// <summary>Production order number.</summary>
    public string OrderNumber { get; init; } = string.Empty;
    /// <summary>Name of the operation or step.</summary>
    public string OperationName { get; init; } = string.Empty;
    /// <summary>Part number being produced.</summary>
    public string PartNumber { get; init; } = string.Empty;
    /// <summary>Customer name for the order.</summary>
    public string CustomerName { get; init; } = string.Empty;
    /// <summary>Scheduled start time.</summary>
    public DateTime StartTime { get; set; }
    /// <summary>Scheduled end time.</summary>
    public DateTime EndTime { get; set; }
    /// <summary>Actual start time (set when job begins).</summary>
    public DateTime? ActualStart { get; set; }
    /// <summary>Actual end time (set when job completes).</summary>
    public DateTime? ActualEnd { get; set; }
    /// <summary>Current status of the reservation.</summary>
    public ReservationStatus Status { get; set; }
    /// <summary>Priority (lower = higher priority).</summary>
    public int Priority { get; init; } = 50;
    /// <summary>Overlay color for the reservation bar.</summary>
    public string Color { get; set; } = string.Empty;
    /// <summary>Tags for categorization.</summary>
    public string[] Tags { get; init; } = Array.Empty<string>();
    /// <summary>Setup time in minutes.</summary>
    public double SetupTimeMinutes { get; init; }
    /// <summary>Cycle time per part in minutes.</summary>
    public double CycleTimeMinutes { get; init; }
    /// <summary>Number of parts to produce.</summary>
    public int PartsCount { get; init; }
}

/// <summary>Represents a downtime period for a specific machine.</summary>
public record MachineDowntime
{
    /// <summary>Unique identifier.</summary>
    public int Id { get; init; }
    /// <summary>Machine resource affected.</summary>
    public int MachineId { get; init; }
    /// <summary>Start of downtime.</summary>
    public DateTime Start { get; set; }
    /// <summary>End of downtime (null if ongoing).</summary>
    public DateTime? End { get; set; }
    /// <summary>Reason for the downtime.</summary>
    public DowntimeReason Reason { get; init; }
    /// <summary>Optional note about the downtime.</summary>
    public string Note { get; init; } = string.Empty;
}

/// <summary>Defines a recurring shift pattern for a day of the week.</summary>
public record ShiftPattern
{
    /// <summary>Day of the week this shift applies to.</summary>
    public DayOfWeek DayOfWeek { get; init; }
    /// <summary>Shift start time.</summary>
    public TimeSpan StartTime { get; init; }
    /// <summary>Shift end time.</summary>
    public TimeSpan EndTime { get; init; }
    /// <summary>Whether this is a working day.</summary>
    public bool IsWorking { get; init; }
}

/// <summary>View mode for the scheduler timeline.</summary>
public enum TimelineViewType
{
    /// <summary>Timeline grouped by resource.</summary>
    ResourceTimeline,
    /// <summary>Timeline grouped by order.</summary>
    OrderTimeline,
    /// <summary>Compact day view.</summary>
    CompactDay,
    /// <summary>Compact week view.</summary>
    CompactWeek,
    /// <summary>Month heatmap view.</summary>
    MonthHeatmap,
    /// <summary>Utilization chart view.</summary>
    UtilizationChart
}
