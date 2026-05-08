namespace SuperUI.Components;

/// <summary>
/// Defines the visual style variant for the <see cref="SgTabs"/> component.
/// </summary>
public enum SgTabsVariant
{
    /// <summary>Underline indicator below the active tab (default).</summary>
    Default,
    /// <summary>Filled pill-shaped tabs with rounded corners.</summary>
    Pills,
    /// <summary>Boxed tabs with borders and a connected body.</summary>
    Boxed,
    /// <summary>Soft segmented control with an inset background.</summary>
    Segmented,
    /// <summary>Browser-like tabs with rounded top corners.</summary>
    Card,
    /// <summary>No visible chrome — text-only tabs.</summary>
    Ghost
}
