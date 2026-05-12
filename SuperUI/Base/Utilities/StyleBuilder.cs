// SuperUI/Base/Utilities/StyleBuilder.cs
//
// Fluent-builder для inline CSS-стилей.
// Используется в SgComponentBase.CreateStyle().
//
// УЛУЧШЕНО:
// 1. Build() — возвращает string (не string?). Пустая строка вместо null.
// 2. Variable(name, int) и Variable(name, double) — перегрузки для числовых значений.
// 3. Property(string, int), Property(string, double) — числовые перегрузки.
// 4. Remove(string property) — убрать стиль по имени свойства.
// 5. IsEmpty — проверить отсутствие стилей.
// 6. Implicit operator string (не string?) — безопаснее для Razor.
// 7. NormalizeStyle защита от бесконечных пробелов.
// 8. Хранит пары (property, value) для поддержки Remove.
//    Дубликаты свойств допустимы (последний Remove удалит первый с конца — LIFO).

namespace SuperUI.Base.Utilities;

/// <summary>
/// Fluent-builder для inline CSS-стилей.
/// </summary>
/// <example>
/// <code>
/// string style = CreateStyle("color: red")
///     .If(Width.HasValue, $"width: {Width}px")
///     .Property("height", Height, "px")
///     .Variable("primary-color", PrimaryColor)
///     .Build();
/// </code>
/// </example>
public sealed class StyleBuilder
{
    private readonly string? _base;
    // Хранит пары (property, value) для возможности Remove
    private List<(string Property, string Value)>? _parts;

    public StyleBuilder(string? baseStyle = null)
    {
        _base = NormalizeStyle(baseStyle);
    }

    /// <summary>Нет ни базовых, ни добавленных стилей.</summary>
    public bool IsEmpty =>
        _base is null && (_parts is null || _parts.Count == 0);

    // ── Добавить стиль ───────────────────────────────────────────────────────

    /// <summary>Добавить стиль безусловно (null/empty игнорируются).</summary>
    public StyleBuilder Add(string? style)
    {
        var s = NormalizeStyle(style);
        if (s is not null)
        {
            // Парсим "property: value" для поддержки Remove
            var idx = s.IndexOf(':', StringComparison.Ordinal);
            if (idx > 0)
            {
                var prop = s[..idx].Trim();
                var val  = s[(idx + 1)..].Trim();
                (_parts ??= new()).Add((prop, val));
            }
            else
            {
                (_parts ??= new()).Add((s, string.Empty));
            }
        }
        return this;
    }

    /// <summary>Добавить CSS-свойство с string-значением.</summary>
    public StyleBuilder Property(string property, string? value)
    {
        if (!string.IsNullOrWhiteSpace(property) && !string.IsNullOrWhiteSpace(value))
            (_parts ??= new()).Add((property.Trim(), value.Trim()));
        return this;
    }

    /// <summary>Добавить CSS-свойство с числовым значением.</summary>
    public StyleBuilder Property(string property, int value, string unit = "")
        => Property(property, $"{value}{unit}");

    /// <summary>Добавить CSS-свойство с числовым значением (double).</summary>
    public StyleBuilder Property(string property, double value, string unit = "")
        => Property(property, FormattableString.Invariant($"{value:G}{unit}"));

    // ── Условные стили ───────────────────────────────────────────────────────

    /// <summary>Добавить стиль по условию.</summary>
    public StyleBuilder If(bool condition, string? style)
    {
        if (condition) Add(style);
        return this;
    }

    /// <summary>Добавить CSS-свойство по условию.</summary>
    public StyleBuilder If(bool condition, string property, string value)
    {
        if (condition) Property(property, value);
        return this;
    }

    /// <summary>Добавить числовое CSS-свойство по условию.</summary>
    public StyleBuilder If(bool condition, string property, int value, string unit = "")
    {
        if (condition) Property(property, value, unit);
        return this;
    }

    // ── CSS-переменные ───────────────────────────────────────────────────────

    /// <summary>Добавить CSS-переменную: --var-name: value.</summary>
    public StyleBuilder Variable(string name, string? value)
    {
        if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(value))
        {
            var varName = $"--{name.TrimStart('-').Trim()}";
            (_parts ??= new()).Add((varName, value.Trim()));
        }
        return this;
    }

    /// <summary>Добавить числовую CSS-переменную.</summary>
    public StyleBuilder Variable(string name, int value, string unit = "")
        => Variable(name, $"{value}{unit}");

    /// <summary>Добавить числовую CSS-переменную (double).</summary>
    public StyleBuilder Variable(string name, double value, string unit = "")
        => Variable(name, FormattableString.Invariant($"{value:G}{unit}"));

    // ── Удаление ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Удалить CSS-свойство по имени (последнее вхождение, если дубликаты).
    /// </summary>
    public StyleBuilder Remove(string property)
    {
        if (!string.IsNullOrWhiteSpace(property) && _parts is not null)
        {
            var trimmed = property.Trim();
            for (int i = _parts.Count - 1; i >= 0; i--)
            {
                if (string.Equals(_parts[i].Property, trimmed, StringComparison.OrdinalIgnoreCase))
                {
                    _parts.RemoveAt(i);
                    break;
                }
            }
        }
        return this;
    }

    // ── Сборка строки ────────────────────────────────────────────────────────

    /// <summary>
    /// Собрать строку стилей.
    /// УЛУЧШЕНО: возвращает string (не string?) — безопаснее для Razor @style.
    /// Пустая строка если нет стилей.
    /// Защита от бесконечных пробелов в значениях.
    /// </summary>
    public string Build()
    {
        if (_parts is null || _parts.Count == 0)
            return _base ?? string.Empty;

        var parts = new List<string>(_parts.Count + 1);
        if (_base is not null) parts.Add(_base);

        foreach (var (prop, val) in _parts!)
        {
            if (string.IsNullOrEmpty(val))
                parts.Add(prop);
            else
                parts.Add($"{prop}: {val}");
        }

        var result = string.Join("; ", parts);
        return result.EndsWith(';') ? result : result + ";";
    }

    // ── Конверсии ────────────────────────────────────────────────────────────

    /// <summary>
    /// Implicit конвертация в string.
    /// УЛУЧШЕНО: string (не string?) — нет null в @style атрибуте Razor.
    /// </summary>
    public static implicit operator string(StyleBuilder builder) => builder.Build();

    /// <summary>Возвращает итоговую строку стилей.</summary>
    public override string ToString() => Build();

    // ── Вспомогательные ──────────────────────────────────────────────────────

    private static string? NormalizeStyle(string? style)
    {
        if (string.IsNullOrWhiteSpace(style)) return null;
        var s = style.Trim().TrimEnd(';').Trim();
        return string.IsNullOrEmpty(s) ? null : s;
    }
}