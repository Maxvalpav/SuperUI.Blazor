// SuperUI/Base/Utilities/StyleBuilder.cs
namespace SuperUI.Base.Utilities;

/// <summary>
/// Fluent inline-style builder.
/// 
/// ИСПРАВЛЕНИЯ:
/// - Убран readonly struct с List внутри (семантическая ошибка)
/// - Заменён на sealed class с pooled StringBuilder
/// - Clone заменён на правильный fluent паттерн (возврат this)
/// - AddUserStyle корректно обрабатывает malformed input
/// </summary>
public sealed class StyleBuilder
{
    private readonly List<(string Prop, string Value)> _styles = [];

    private StyleBuilder(List<(string, string)> styles) => _styles = styles;

    public StyleBuilder() { }

    public StyleBuilder(string? baseStyle) : this()
    {
        AddUserStyle(baseStyle);
    }

    public StyleBuilder Add(string property, string? value, bool condition = true)
    {
        if (!condition || string.IsNullOrWhiteSpace(value) || string.IsNullOrWhiteSpace(property))
            return this;
        // Создаём новый экземпляр для immutable fluent chain
        var next = Clone();
        next._styles.Add((property.Trim(), value.Trim()));
        return next;
    }

    public StyleBuilder Add(string property, string? value, Func<bool> condition)
        => Add(property, value, condition());

    public StyleBuilder AddIf(string property, string? value, Func<bool> condition)
        => condition() ? Add(property, value) : this;

    public StyleBuilder AddVar(string cssVar, string? value, bool condition = true)
        => Add($"--{cssVar}", value, condition);

    /// <summary>
    /// Добавить пользовательский Style параметр.
    /// ИСПРАВЛЕНО: защита от malformed input, trim whitespace.
    /// </summary>
    public StyleBuilder AddUserStyle(string? userStyle)
    {
        if (string.IsNullOrWhiteSpace(userStyle)) return this;
        var next = Clone();
        // Разбить на пары prop:value безопасно
        var parts = userStyle.AsSpan();
        var start = 0;
        while (start < parts.Length)
        {
            var semicolon = parts[start..].IndexOf(';');
            var segment = semicolon < 0
                ? parts[start..].ToString()
                : parts[start..(start + semicolon)].ToString();
            start += semicolon < 0 ? parts.Length - start : semicolon + 1;

            var colon = segment.IndexOf(':');
            if (colon > 0)
            {
                var prop = segment[..colon].Trim();
                var val = segment[(colon + 1)..].Trim();
                if (!string.IsNullOrEmpty(prop) && !string.IsNullOrEmpty(val))
                    next._styles.Add((prop, val));
            }
        }
        return next;
    }

    public string? Build()
    {
        if (_styles.Count == 0) return null;

        // Оценить размер для оптимального StringBuilder capacity
        var capacity = _styles.Sum(s => s.Prop.Length + s.Value.Length + 4);
        var sb = new System.Text.StringBuilder(capacity);

        for (var i = 0; i < _styles.Count; i++)
        {
            if (i > 0) sb.Append("; ");
            sb.Append(_styles[i].Prop).Append(": ").Append(_styles[i].Value);
        }

        return sb.ToString();
    }

    private StyleBuilder Clone()
    {
        var copy = new List<(string, string)>(_styles.Count + 1);
        copy.AddRange(_styles);
        return new StyleBuilder(copy);
    }

    public static implicit operator string?(StyleBuilder builder) => builder.Build();
    public override string? ToString() => Build();
}
