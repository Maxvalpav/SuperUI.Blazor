using System;
using System.Collections.Generic;

namespace SuperUI.Components;

public class SgGridState
{
    public string? Search { get; set; }
    public Dictionary<string, string> QuickFilters { get; set; } = new();
    public Dictionary<string, List<string>> ValueFilters { get; set; } = new();
    public Dictionary<string, PersistedConditionFilter> ConditionFilters { get; set; } = new();
    public List<QueryRule> QueryRules { get; set; } = new();
    public List<PersistedSortRule> Sort { get; set; } = new();
    public List<string> HiddenColumns { get; set; } = new();
    public Dictionary<string, int> ColumnWidths { get; set; } = new();
    public Dictionary<string, int> ColumnOrder { get; set; } = new();
    public List<string> GroupBy { get; set; } = new();
    public int PageSize { get; set; }
    public List<PersistedRowHighlightRule> RowHighlightRules { get; set; } = new();
    /// <summary>Aggregate function per column key (None = not set).</summary>
    public Dictionary<string, string> ColumnAggregates { get; set; } = new();
}

public class PersistedConditionFilter
{
    public bool And { get; set; } = true;
    public List<FilterRule>? Rules { get; set; }
}

public class PersistedSortRule
{
    public string Key { get; set; } = string.Empty;
    public SortDirection Dir { get; set; }
}

public class SgGridSavedView
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = string.Empty;
    public SgGridState State { get; set; } = new();
    public bool IsDefault { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
