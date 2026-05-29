namespace SuperUI.Components;

/// <summary>Defines the aggregate function type for a pivot value field.</summary>
public enum SgPivotAggregateType
{
    /// <summary>Sum of all values.</summary>
    Sum,
    /// <summary>Count of items.</summary>
    Count,
    /// <summary>Average of all values.</summary>
    Average,
    /// <summary>Minimum value.</summary>
    Min,
    /// <summary>Maximum value.</summary>
    Max
}

/// <summary>Defines a field in the pivot table, including its data selector and aggregate function.</summary>
/// <typeparam name="TItem">The type of data items.</typeparam>
public class SgPivotField<TItem>
{
    /// <summary>The property name of the field.</summary>
    public string Name { get; set; } = default!;
    /// <summary>The display label for the field.</summary>
    public string Label { get; set; } = default!;
    /// <summary>Function to extract the field value from a data item.</summary>
    public Func<TItem, object?> Selector { get; set; } = default!;
    /// <summary>The aggregate function to apply when this field is used as a value field.</summary>
    public SgPivotAggregateType AggregateType { get; set; } = SgPivotAggregateType.Sum;
    /// <summary>Optional format string for displaying values.</summary>
    public string? Format { get; set; }
    /// <summary>Optional custom aggregate function that takes a group of items and returns a value.</summary>
    public Func<IEnumerable<TItem>, object?>? CustomAggregate { get; set; }
    /// <summary>Optional function to determine CSS style for a cell based on its value.</summary>
    public Func<object?, string?>? CellStyleFunc { get; set; }
    /// <summary>List of values to exclude from the pivot calculation.</summary>
    public HashSet<string> ExcludedValues { get; set; } = new();
}

/// <summary>Represents the current state of the pivot table layout, including row, column, and value field assignments.</summary>
/// <typeparam name="TItem">The type of data items.</typeparam>
public class SgPivotState<TItem>
{
    /// <summary>Fields placed in the row area of the pivot table.</summary>
    public List<SgPivotField<TItem>> RowFields { get; set; } = new();
    /// <summary>Fields placed in the column area of the pivot table.</summary>
    public List<SgPivotField<TItem>> ColumnFields { get; set; } = new();
    /// <summary>Fields placed in the value area of the pivot table.</summary>
    public List<SgPivotField<TItem>> ValueFields { get; set; } = new();
    /// <summary>Fired when the pivot state changes.</summary>
    public event Action? OnChanged;
    /// <summary>Notifies listeners that the pivot state has changed.</summary>
    public void NotifyChanged() => OnChanged?.Invoke();
}
