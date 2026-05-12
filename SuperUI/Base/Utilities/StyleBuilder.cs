// SuperUI/Base/Utilities/StyleBuilder.cs
//
// Fluent-builder для inline CSS-стилей.
// Используется в SgComponentBase.CreateStyle().

namespace SuperUI.Base.Utilities;

/// <summary>
/// Fluent-builder для inline CSS-стилей.
/// </summary>
/// <example>
/// <code>
/// string style = CreateStyle("color: red")
///     .If(Width.HasValue, $"width: {Width}px")
///     .If(IsHidden, "display: none")
///     .Build();
/// </code>
/// </example>
public sealed class StyleBuilder
{
    private readonly string? _base;
    private List<string>? _parts;

    public StyleBuilder(string? baseStyle = null)
    {
        _base = NormalizeStyle(baseStyle);
    }

    /// <summary>Добавить стиль безусловно.</summary>
    public StyleBuilder Add(string? style)
    {
        var s = NormalizeStyle(style);
        if (s is not null) (_parts ??= new()).Add(s);
        return this;
    }

    /// <summary>Добавить стиль по условию.</summary>
    public StyleBuilder If(bool condition, string? style)
    {
        if (condition) Add(style);
        return this;
    }

    /// <summary>Добавить CSS-свойство по условию.</summary>
    public StyleBuilder If(bool condition, string property, string value)
    {
        if (condition && !string.IsNullOrWhiteSpace(property))
            (_parts ??= new()).Add($"{property}: {value}");
        return this;
    }

    /// <summary>Добавить CSS-свойство безусловно.</summary>
    public StyleBuilder Property(string property, string? value)
    {
        if (!string.IsNullOrWhiteSpace(property) && !string.IsNullOrWhiteSpace(value))
            (_parts ??= new()).Add($"{property}: {value}");
        return this;
    }

    /// <summary>Добавить CSS-переменную: --var-name: value.</summary>
    public StyleBuilder Variable(string name, string? value)
    {
        if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(value))
            (_parts ??= new()).Add($"--{name.TrimStart('-')}: {value}");
        return this;
    }

    /// <summary>Собрать строку стилей.</summary>
    public string? Build()
    {
        if (_parts is null || _parts.Count == 0) return _base;

        var parts = new List<string>(_parts.Count + 1);
        if (_base is not null) parts.Add(_base);
        parts.AddRange(_parts);

        var result = string.Join("; ", parts);
        // Гарантируем завершающую точку с запятой
        return result.EndsWith(';') ? result : result + ";";
    }

    public static implicit operator string?(StyleBuilder builder) => builder.Build();

    public override string? ToString() => Build();

    private static string? NormalizeStyle(string? style)
    {
        if (string.IsNullOrWhiteSpace(style)) return null;
        var s = style.Trim();
        // Убираем лишние точки с запятой в конце для нормализации
        return s.TrimEnd(';').Trim();
    }
}
