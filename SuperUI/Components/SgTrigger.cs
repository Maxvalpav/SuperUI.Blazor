namespace SuperUI.Components;

/// <summary>
/// Defines how an overlay component (<see cref="SgTooltip"/>, <see cref="SgPopover"/>)
/// is opened and closed.
/// </summary>
public enum SgTrigger
{
    /// <summary>Opens on mouse hover, closes on mouse leave.</summary>
    Hover,
    /// <summary>Opens on focus, closes on blur.</summary>
    Focus,
    /// <summary>Toggles on click.</summary>
    Click,
    /// <summary>Controlled programmatically — no automatic open/close.</summary>
    Manual
}
