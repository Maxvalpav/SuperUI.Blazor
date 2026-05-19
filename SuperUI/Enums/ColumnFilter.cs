namespace SuperUI.Enums;

/// <summary>Condition-based column filter.</summary>
public sealed class ColumnFilter
{
    public ColumnFilter(IEnumerable<FilterRule> rules, bool and = true)
    {
        Rules = rules.ToList();
        And = and;
    }
    public List<FilterRule> Rules { get; }
    public bool And { get; }
}
