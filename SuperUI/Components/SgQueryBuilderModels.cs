namespace SuperUI.Components;

public enum QueryLogicOperator
{
    And,
    Or
}

public enum QueryFieldOperator
{
    Equals,
    NotEquals,
    Contains,
    NotContains,
    StartsWith,
    EndsWith,
    GreaterThan,
    LessThan,
    GreaterThanOrEqual,
    LessThanOrEqual,
    In,
    NotIn,
    IsNull,
    IsNotNull
}

public class QueryField
{
    public required string Name { get; set; }
    public required string Label { get; set; }
    public Type Type { get; set; } = typeof(string);
    public string? Category { get; set; }
}

public class QueryRule
{
    public string? FieldName { get; set; }
    public QueryFieldOperator Operator { get; set; } = QueryFieldOperator.Equals;
    public object? Value { get; set; }
}

public class QueryGroup
{
    public QueryLogicOperator Operator { get; set; } = QueryLogicOperator.And;
    public List<QueryRule> Rules { get; set; } = new();
    public List<QueryGroup> Groups { get; set; } = new();
}
