namespace SuperUI.Components;

/// <summary>
/// Defines the status of a step in the stepper component.
/// </summary>
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

public sealed class StepperItem
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    /// <summary>Gets or sets the status of this step. Overrides automatic status calculation.</summary>
    public SgStepStatus? Status { get; set; }
}
