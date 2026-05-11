// Файл: Utilities/StyleBuilder.cs
// Зависимости: NONE

namespace SuperUI.Utilities;

/// <summary>
/// Fluent inline style builder.
/// </summary>
public sealed class StyleBuilder
{
    // Хранит (property, value) пары чтобы поддерживать override и порядок
    private readonly List<(string Property, string Value)> _styles = new(4);

    public StyleBuilder() { }
    public StyleBuilder(string? initialStyle)
    {
        if (!string.IsNullOrWhiteSpace(initialStyle))
            ParseAndAdd(initialStyle);
    }

    /// <summary>Добавить CSS-свойство.</summary>
    public StyleBuilder AddStyle(string property, string? value)
    {
        if (!string.IsNullOrWhiteSpace(property) && !string.IsNullOrWhiteSpace(value))
            _styles.Add((property.Trim(), value.Trim()));
        return this;
    }

    /// <summary>Добавить CSS-свойство условно.</summary>
    public StyleBuilder AddStyle(string property, string? value, bool when)
    {
        if (when) AddStyle(property, value);
        return this;
    }

    public StyleBuilder AddStyle(string property, Func<string?> valueFactory, bool when)
    {
        if (when) AddStyle(property, valueFactory());
        return this;
    }

    /// <summary>Добавить готовую строку стилей (парсим и добавляем).</summary>
    public StyleBuilder AddStyle(string? rawStyle)
    {
        if (!string.IsNullOrWhiteSpace(rawStyle))
            ParseAndAdd(rawStyle);
        return this;
    }

    /// <summary>Добавить стили из атрибутов.</summary>
    public StyleBuilder AddStyleFromAttributes(IDictionary<string, object>? attributes)
    {
        if (attributes is not null && attributes.TryGetValue("style", out var s) && s is string raw)
            AddStyle(raw);
        return this;
    }

    public string? Build()
    {
        if (_styles.Count == 0) return null;

        // Последнее значение побеждает (как в CSS cascade)
        // Собираем через Dictionary для dedupe
        var dict = new Dictionary<string, string>(_styles.Count, StringComparer.OrdinalIgnoreCase);
        foreach (var (prop, val) in _styles)
            dict[prop] = val;

        return string.Join("; ", dict.Select(kv => $"{kv.Key}: {kv.Value}"));
    }

    private void ParseAndAdd(string rawStyle)
    {
        // Парсим "prop: val; prop2: val2"
        foreach (var declaration in rawStyle.AsSpan().Split(';'))
        {
            var decl = rawStyle.AsSpan()[declaration.Range].Trim();
            var colonIdx = decl.IndexOf(':');
            if (colonIdx > 0)
            {
                var prop = decl[..colonIdx].Trim().ToString();
                var val = decl[(colonIdx + 1)..].Trim().ToString();
                if (!string.IsNullOrEmpty(prop) && !string.IsNullOrEmpty(val))
                    _styles.Add((prop, val));
            }
        }
    }

    public static implicit operator string?(StyleBuilder builder) => builder.Build();
    public static StyleBuilder Default(string? initialStyle = null) => new(initialStyle);
}
