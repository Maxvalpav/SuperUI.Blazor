using SuperUI.Enums;

namespace SuperUI.Components;

/// <summary>
/// Represents a step in the stepper component.
/// </summary>
public sealed class StepperItem
{
    /// <summary>Step title shown in the label.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Optional longer description shown below the title.</summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the status of this step. Overrides automatic status calculation.
    /// When <c>null</c>, the status is derived from the stepper's <c>Active</c> index.
    /// </summary>
    public SgStepStatus? Status { get; set; }

    /// <summary>
    /// Optional custom icon or text displayed inside the step mark.
    /// When <c>null</c>, the default icon is shown (number, checkmark, or cross).
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// When <c>true</c>, the step is disabled and cannot be navigated to.
    /// Has no effect when the stepper's <c>Clickable</c> is <c>false</c>.
    /// </summary>
    public bool Disabled { get; set; }

    /// <summary>
    /// Optional CSS class applied to this step's root element.
    /// </summary>
    public string? CssClass { get; set; }

    /// <summary>
    /// When <c>true</c>, marks the step as optional (shows an "(Optional)" badge).
    /// </summary>
    public bool Optional { get; set; }
}
