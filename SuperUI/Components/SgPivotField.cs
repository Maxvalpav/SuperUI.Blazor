namespace SuperUI.Components;

public enum SgPivotAggregateType
{
    Sum,
    Count,
    Average,
    Min,
    Max
}

public class SgPivotField<TItem>
{
    public string Name { get; set; } = default!;
    public string Label { get; set; } = default!;
    public Func<TItem, object?> Selector { get; set; } = default!;
    public SgPivotAggregateType AggregateType { get; set; } = SgPivotAggregateType.Sum;
    public string? Format { get; set; }
    
    /// <summary>
    /// Optional custom aggregate function that takes a group of items and returns a value.
    /// </summary>
    public Func<IEnumerable<TItem>, object?>? CustomAggregate { get; set; }

    /// <summary>
    /// Optional function to determine CSS style for a cell based on its value.
    /// </summary>
    public Func<object?, string?>? CellStyleFunc { get; set; }

    /// <summary>
    /// List of values to exclude from the pivot calculation.
    /// </summary>
    public HashSet<string> ExcludedValues { get; set; } = new();
}

public class SgPivotState<TItem>
{
    public List<SgPivotField<TItem>> RowFields { get; set; } = new();
    public List<SgPivotField<TItem>> ColumnFields { get; set; } = new();
    public List<SgPivotField<TItem>> ValueFields { get; set; } = new();
    
    public event Action? OnChanged;
    public void NotifyChanged() => OnChanged?.Invoke();
}
