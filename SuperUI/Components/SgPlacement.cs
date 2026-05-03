namespace SuperUI.Components;

/// <summary>
/// Defines placement positions for overlay components such as
/// <see cref="SgDrawer"/>, <see cref="SgTooltip"/>, and <see cref="SgPopover"/>.
/// </summary>
public enum SgPlacement
{
    // ── Cardinal ──────────────────────────────────────────────────────────
    /// <summary>Above the trigger, centred.</summary>
    Top,
    /// <summary>Below the trigger, centred.</summary>
    Bottom,
    /// <summary>To the left of the trigger, centred.</summary>
    Left,
    /// <summary>To the right of the trigger, centred.</summary>
    Right,

    // ── Top variants ──────────────────────────────────────────────────────
    /// <summary>Above the trigger, aligned to the start (left) edge.</summary>
    TopStart,
    /// <summary>Above the trigger, aligned to the end (right) edge.</summary>
    TopEnd,

    // ── Bottom variants ───────────────────────────────────────────────────
    /// <summary>Below the trigger, aligned to the start (left) edge.</summary>
    BottomStart,
    /// <summary>Below the trigger, aligned to the end (right) edge.</summary>
    BottomEnd,

    // ── Left variants ─────────────────────────────────────────────────────
    /// <summary>To the left of the trigger, aligned to the start (top) edge.</summary>
    LeftStart,
    /// <summary>To the left of the trigger, aligned to the end (bottom) edge.</summary>
    LeftEnd,

    // ── Right variants ────────────────────────────────────────────────────
    /// <summary>To the right of the trigger, aligned to the start (top) edge.</summary>
    RightStart,
    /// <summary>To the right of the trigger, aligned to the end (bottom) edge.</summary>
    RightEnd
}
