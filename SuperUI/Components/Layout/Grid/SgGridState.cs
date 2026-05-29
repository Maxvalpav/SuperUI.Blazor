using System;
using System.Collections.Generic;
using SuperUI.Enums;

namespace SuperUI.Components;

/// <summary>Serializable state snapshot of the data grid (filters, sort, columns, etc.).</summary>
public class SgGridState
{
    /// <summary>Global search text.</summary>
    public string? Search { get; set; }
    /// <summary>Quick filter values keyed by column.</summary>
    public Dictionary<string, string> QuickFilters { get; set; } = new();
    /// <summary>Value-based filter selections keyed by column.</summary>
    public Dictionary<string, List<string>> ValueFilters { get; set; } = new();
    /// <summary>Advanced condition filters keyed by column.</summary>
    public Dictionary<string, PersistedConditionFilter> ConditionFilters { get; set; } = new();
    /// <summary>Query rules for custom filtering.</summary>
    public List<QueryRule> QueryRules { get; set; } = new();
    /// <summary>Active sort rules.</summary>
    public List<PersistedSortRule> Sort { get; set; } = new();
    /// <summary>Columns that are currently hidden.</summary>
    public List<string> HiddenColumns { get; set; } = new();
    /// <summary>Columns that are pinned (frozen).</summary>
    public List<string> PinnedColumns { get; set; } = new();
    /// <summary>Custom column widths keyed by column key.</summary>
    public Dictionary<string, int> ColumnWidths { get; set; } = new();
    /// <summary>Column display order keyed by column key.</summary>
    public Dictionary<string, int> ColumnOrder { get; set; } = new();
    /// <summary>Column keys used for grouping.</summary>
    public List<string> GroupBy { get; set; } = new();
    /// <summary>Number of rows per page.</summary>
    public int PageSize { get; set; }
    /// <summary>Row highlighting rules.</summary>
    public List<PersistedRowHighlightRule> RowHighlightRules { get; set; } = new();
    /// <summary>Aggregate function per column key (None = not set).</summary>
    public Dictionary<string, string> ColumnAggregates { get; set; } = new();
}

/// <summary>Persisted condition filter with logical operator and rules.</summary>
public class PersistedConditionFilter
{
    /// <summary>If true, rules are combined with AND; otherwise OR.</summary>
    public bool And { get; set; } = true;
    /// <summary>Filter rules to apply.</summary>
    public List<FilterRule>? Rules { get; set; }
}

/// <summary>Persisted sort rule for a column.</summary>
public class PersistedSortRule
{
    /// <summary>Column key to sort by.</summary>
    public string Key { get; set; } = string.Empty;
    /// <summary>Sort direction.</summary>
    public SgDataGridSortDirection Dir { get; set; }
}

/// <summary>A named, saved view of the grid state.</summary>
public class SgGridSavedView
{
    /// <summary>Unique identifier for the view.</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    /// <summary>Display name of the saved view.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Full grid state snapshot.</summary>
    public SgGridState State { get; set; } = new();
    /// <summary>Whether this view is the default.</summary>
    public bool IsDefault { get; set; }
    /// <summary>Timestamp when the view was created.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
