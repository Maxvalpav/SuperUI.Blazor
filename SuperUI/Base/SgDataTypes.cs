// SuperUI/Base/SgDataTypes.cs
namespace SuperUI.Base;

public record SgDataRequest
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 25;
    public SgSortDescriptor? Sort { get; init; }
    public IReadOnlyList<SgFilterDescriptor> Filters { get; init; } = [];
    public IReadOnlyDictionary<string, object?>? ExtraParams { get; init; }
}

public record SgDataResult<TItem>(IReadOnlyList<TItem> Items, int TotalCount)
{
    public static SgDataResult<TItem> Empty => new([], 0);
}

public record SgSortDescriptor(string Field, SgSortDirection Direction);

public record SgFilterDescriptor(
    string Field,
    object? Value,
    SgFilterOperator Operator = SgFilterOperator.Contains,
    string? Value2 = null  // для Between
);

public enum SgSortDirection { Asc, Desc }

public enum SgFilterOperator
{
    Equals,
    NotEquals,
    Contains,
    NotContains,
    StartsWith,
    EndsWith,
    GreaterThan,
    GreaterThanOrEqual,
    LessThan,
    LessThanOrEqual,
    Between,
    In,
    NotIn,
    IsNull,
    IsNotNull
}
