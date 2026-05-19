namespace SuperUI.Enums;

/// <summary>Оператор сравнения для фильтрации DataGrid.</summary>
public enum FilterCondition
{
    Contains,
    NotContains,
    Equals,
    NotEquals,
    StartsWith,
    EndsWith,
    GreaterThan,
    LessThan,
    GreaterOrEqual,
    LessOrEqual,
    IsEmpty,
    IsNotEmpty
}

/// <summary>Single condition in a data-grid filter rule.</summary>
public sealed record FilterRule
{
    public FilterCondition Condition { get; init; } = FilterCondition.Contains;
    public string? Value { get; init; }
}

/// <summary>Aggregate function for a DataGrid column.</summary>
public enum Aggregate { None, Sum, Average, Min, Max, Count }

/// <summary>Detail panel placement.</summary>
public enum DetailPlacement { Inline, Drawer, Window }

/// <summary>Row highlight rule for conditional colouring.</summary>
public sealed class RowHighlightRule
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = string.Empty;
    public List<QueryRule> Rules { get; set; } = new();
    public bool RulesAnd { get; set; } = true;
    public string? BackgroundColor { get; set; }
    public string? TextColor { get; set; }
    public bool IsEnabled { get; set; } = true;
    public string? TargetColumnKey { get; set; }
}

/// <summary>Persisted row highlight rule (serialised form).</summary>
public sealed class PersistedRowHighlightRule
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = string.Empty;
    public List<QueryRule> Rules { get; set; } = new();
    public bool RulesAnd { get; set; } = true;
    public string? BackgroundColor { get; set; }
    public string? TextColor { get; set; }
    public bool IsEnabled { get; set; } = true;
    public string? TargetColumnKey { get; set; }
}
