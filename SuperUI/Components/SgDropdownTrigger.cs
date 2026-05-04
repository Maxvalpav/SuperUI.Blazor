namespace SuperUI.Components;

/// <summary>
/// Defines how an <see cref="SgDropdown"/> menu is opened.
/// </summary>
public enum SgDropdownTrigger
{
    /// <summary>Open on trigger click. Default.</summary>
    Click,

    /// <summary>Open on pointer hover. Honours <c>OpenDelay</c> / <c>CloseDelay</c>.</summary>
    Hover,

    /// <summary>Open on right-click (context menu). The browser context menu is suppressed on the trigger.</summary>
    ContextMenu
}
