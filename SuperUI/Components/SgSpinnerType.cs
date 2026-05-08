namespace SuperUI.Components;

/// <summary>
/// Defines the visual style of the <see cref="SgSpinner"/> component.
/// </summary>
public enum SgSpinnerType
{
    /// <summary>Classic rotating border spinner (default).</summary>
    Border,

    /// <summary>Growing ring with variable thickness.</summary>
    Ring,

    /// <summary>Bouncing dots animation.</summary>
    Dots,

    /// <summary>Horizontal pulsing bars.</summary>
    Bars,

    /// <summary>Pulsing circle (scale animation).</summary>
    Pulse,

    /// <summary>Spinning circle with dash animation.</summary>
    SpinCircle
}