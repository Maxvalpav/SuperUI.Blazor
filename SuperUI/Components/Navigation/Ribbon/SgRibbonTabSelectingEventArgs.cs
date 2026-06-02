namespace SuperUI.Components;

/// <summary>Event arguments for <c>SgRibbon.OnTabSelecting</c> — fired before a tab switch.</summary>
public sealed class SgRibbonTabSelectingEventArgs
{
    /// <summary>The tab the user is trying to switch to.</summary>
    public required SgRibbonTab Tab { get; init; }

    /// <summary>The currently active tab (null if none).</summary>
    public SgRibbonTab? OldTab { get; init; }

    /// <summary>Index of the new tab in the ribbon's tab list.</summary>
    public int NewIndex { get; init; }

    /// <summary>Index of the currently active tab (-1 if none).</summary>
    public int OldIndex { get; init; }

    /// <summary>Set to true to cancel the tab switch.</summary>
    public bool Cancel { get; set; }
}
