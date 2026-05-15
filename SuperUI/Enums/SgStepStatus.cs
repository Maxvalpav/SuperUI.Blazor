namespace SuperUI.Enums;

/// <summary>Defines the status of a step in the stepper component.</summary>
public enum SgStepStatus
{
    /// <summary>Step is pending (not yet reached).</summary>
    Pending,
    /// <summary>Step is currently in progress.</summary>
    Process,
    /// <summary>Step has been completed successfully.</summary>
    Done,
    /// <summary>Step has an error.</summary>
    Error
}
