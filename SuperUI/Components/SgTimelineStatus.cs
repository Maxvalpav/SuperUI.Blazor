namespace SuperUI.Components;

/// <summary>
/// Defines the semantic status colour for a <see cref="TimelineItem"/> dot.
/// </summary>
public enum SgTimelineStatus
{
    /// <summary>Default neutral colour.</summary>
    Default,
    /// <summary>Active / in-progress (blue).</summary>
    Active,
    /// <summary>Completed successfully (green).</summary>
    Done,
    /// <summary>Error state (red).</summary>
    Error,
    /// <summary>Pending / not started (gray).</summary>
    Pending
}
