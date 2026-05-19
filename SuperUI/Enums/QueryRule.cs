namespace SuperUI.Enums;

/// <summary>Оператор сравнения для поля запроса.</summary>
public enum QueryFieldOperator
{
    Equals, NotEquals,
    Contains, NotContains,
    StartsWith, EndsWith,
    GreaterThan, LessThan,
    GreaterOrEqual, LessOrEqual,
    IsEmpty, IsNotEmpty,
    In, NotIn,
    Between, NotBetween,
    IsNull, IsNotNull, GreaterThanOrEqual, LessThanOrEqual
}

/// <summary>Логический оператор между правилами запроса.</summary>
public enum QueryLogicOperator { And, Or }

/// <summary>Одиночное условие в правиле запроса.</summary>
public class QueryRule
{
    public string? FieldName { get; set; }
    public QueryFieldOperator Operator { get; set; } = QueryFieldOperator.Equals;
    public object? Value { get; set; }
}

/// <summary>Defines a filterable data field for query builder.</summary>
public class QueryField
{
    public required string Name { get; set; }
    public required string Label { get; set; }
    public Type Type { get; set; } = typeof(string);
    public string? Category { get; set; }
    public bool ShowTime { get; set; }
    public IReadOnlyList<QueryFieldEnumOption>? EnumOptions { get; set; }
}

public sealed record QueryFieldEnumOption(string Name, string Label);

/// <summary>Logical group of query rules.</summary>
public class QueryGroup
{
    public QueryLogicOperator Operator { get; set; } = QueryLogicOperator.And;
    public List<QueryRule> Rules { get; set; } = new();
    public List<QueryGroup> Groups { get; set; } = new();
}
