using SuperUI.Enums;

namespace SuperUI.Components;

/// <summary>
/// Represents a step in the stepper component.
/// </summary>
public sealed class StepperItem
{
    /// <summary>Step title shown in the label.</summary>
    public string Title { get; set; } = string.Empty;
    
    /// <summary>Optional longer description.</summary>
    public string? Description { get; set; }
    
    /// <summary>Gets or sets the status of this step. Overrides automatic status calculation.</summary>
    public SgStepStatus? Status { get; set; }
}
