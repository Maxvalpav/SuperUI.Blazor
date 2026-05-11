namespace SuperUI.Utilities;

/// <summary>
/// Fluent inline-style builder.
/// </summary>
public readonly struct StyleBuilder
{
    private readonly List<(string Prop, string Value)> _styles;

    public StyleBuilder()
    {
        _styles = [];
    }

    public StyleBuilder Add(string property, string? value, bool condition = true)
    {
        if (!condition || string.IsNullOrWhiteSpace(value)) return this;
        var result = Clone();
        result._styles.Add((property, value));
        return result;
    }

    public StyleBuilder AddIf(string property, string? value, Func<bool> condition)
        => condition() ? Add(property, value) : this;

    public StyleBuilder AddVar(string cssVar, string? value, bool condition = true)
        => Add($"--{cssVar}", value, condition);

    /// <summary>Добавить пользовательский Style параметр в конец.</summary>
    public StyleBuilder AddUserStyle(string? userStyle)
    {
        if (string.IsNullOrWhiteSpace(userStyle)) return this;
        var result = Clone();
        foreach (var part in userStyle.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var kv = part.Split(':', 2);
            if (kv.Length == 2)
                result._styles.Add((kv[0].Trim(), kv[1].Trim()));
        }
        return result;
    }

    public string? Build()
    {
        if (_styles.Count == 0) return null;
        return string.Join("; ", _styles.Select(s => $"{s.Prop}: {s.Value}"));
    }

    private StyleBuilder Clone()
    {
        var r = new StyleBuilder();
        _styles.ForEach(s => r._styles.Add(s));
        return r;
    }

    public static implicit operator string?(StyleBuilder builder) => builder.Build();
}
