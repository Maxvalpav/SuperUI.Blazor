namespace SuperUI.Base;

public record SgDataRequest
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 25;
    public SgSortDescriptor? Sort { get; init; }
    public List<SgFilterDescriptor> Filters { get; init; } = [];
}

public record SgDataResult<T>(IEnumerable<T> Items, int TotalCount);
public record SgSortDescriptor(string Field, SgSortDirection Direction);
public record SgFilterDescriptor(string Field, object? Value, SgFilterOperator Operator = SgFilterOperator.Contains);

public enum SgSortDirection { Asc, Desc }
public enum SgFilterOperator { Equals, Contains, StartsWith, EndsWith, GreaterThan, LessThan }
