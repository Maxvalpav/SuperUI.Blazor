namespace SuperUI.Components;

/// <summary>Animation style for the progress bar.</summary>
public enum SgProgressAnimationType
{
    /// <summary>Animated stripes sliding across the bar (current default behavior).</summary>
    Wave,

    /// <summary>Bar slides in from the left edge.</summary>
    Slide,

    /// <summary>Bar scales from 0% to 100% width.</summary>
    Reveal,

    /// <summary>Concentric ripple effect on the bar surface.</summary>
    Ripple,

    /// <summary>Gentle pulse animation at the leading edge of the bar.</summary>
    Pulse
}
