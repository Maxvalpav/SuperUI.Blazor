using System;
using System.Collections.Generic;

namespace SuperUI.Components.SgMachineScheduler.Models;

public enum MachineStatus
{
    Online,
    Offline,
    Maintenance,
    Fault
}

public enum ReservationStatus
{
    Planned,
    InProgress,
    Completed,
    Delayed,
    Overdue,
    Cancelled
}

public enum DowntimeReason
{
    Setup,
    Breakdown,
    MaterialWait,
    Maintenance,
    NoOperator
}

public record MachineResource
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Group { get; init; } = string.Empty;
    public string Cell { get; init; } = string.Empty;
    public MachineStatus Status { get; set; }
    public int MaxCapacityUnits { get; init; } = 1;
    public string Color { get; init; } = "#4FC3F7";
    public List<ShiftPattern> Shifts { get; init; } = new();
    public double HourlyRate { get; init; }
}

public record MachineReservation
{
    public int Id { get; init; }
    public int MachineId { get; set; }
    public string OrderNumber { get; init; } = string.Empty;
    public string OperationName { get; init; } = string.Empty;
    public string PartNumber { get; init; } = string.Empty;
    public string CustomerName { get; init; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public DateTime? ActualStart { get; set; }
    public DateTime? ActualEnd { get; set; }
    public ReservationStatus Status { get; set; }
    public int Priority { get; init; } = 50;
    public string Color { get; set; } = string.Empty;
    public string[] Tags { get; init; } = Array.Empty<string>();
    public double SetupTimeMinutes { get; init; }
    public double CycleTimeMinutes { get; init; }
    public int PartsCount { get; init; }
}

public record MachineDowntime
{
    public int Id { get; init; }
    public int MachineId { get; init; }
    public DateTime Start { get; set; }
    public DateTime? End { get; set; }
    public DowntimeReason Reason { get; init; }
    public string Note { get; init; } = string.Empty;
}

public record ShiftPattern
{
    public DayOfWeek DayOfWeek { get; init; }
    public TimeSpan StartTime { get; init; }
    public TimeSpan EndTime { get; init; }
    public bool IsWorking { get; init; }
}

public enum TimelineViewType
{
    ResourceTimeline,
    OrderTimeline,
    CompactDay,
    CompactWeek,
    MonthHeatmap,
    UtilizationChart
}
