// SuperUI/Base/Utilities/StyleBuilder.cs
//
// ДОРАБОТКИ:
// 1. Merge(StyleBuilder) — объединить два builder'а
// 2. Clone() — скопировать
// 3. HasProperty(string) — проверить наличие свойства
// 4. Build() — корректная расстановка ";" между элементами

namespace SuperUI.Base.Utilities;

public sealed class StyleBuilder
{
    private readonly string? _base;
    private List<(string Property, string Value)>? _parts;

    public StyleBuilder(string? baseStyle = null)
    {
        _base = NormalizeStyle(baseStyle);
    }

    public bool IsEmpty => _base is null && (_parts is null || _parts.Count == 0);

    // ── Добавить ──────────────────────────────────────────────────────────────────
    public StyleBuilder Add(string? style)
    {
        var s = NormalizeStyle(style);
        if (s is not null)
        {
            var idx = s.IndexOf(':', StringComparison.Ordinal);
            if (idx > 0)
                (_parts ??= new()).Add((s[..idx].Trim(), s[(idx + 1)..].Trim()));
            else
                (_parts ??= new()).Add((s, string.Empty));
        }
        return this;
    }

    public StyleBuilder Property(string property, string? value)
    {
        if (!string.IsNullOrWhiteSpace(property) && !string.IsNullOrWhiteSpace(value))
            (_parts ??= new()).Add((property.Trim(), value.Trim()));
        return this;
    }

    public StyleBuilder Property(string property, int value, string unit = "") =>
        Property(property, $"{value}{unit}");

    public StyleBuilder Property(string property, double value, string unit = "") =>
        Property(property, FormattableString.Invariant($"{value:G}{unit}"));

    // ── CSS calc() ─────────────────────────────────────────────────────────────────
    /// <summary>Добавить CSS calc() выражение.</summary>
    public StyleBuilder Calc(string property, string expression)
        => Property(property, $"calc({expression})");

    // ── Условные ─────────────────────────────────────────────────────────────────
    public StyleBuilder If(bool condition, string? style)                              { if (condition) Add(style);                    return this; }
    public StyleBuilder If(bool condition, string property, string value)              { if (condition) Property(property, value);     return this; }
    public StyleBuilder If(bool condition, string property, int value, string unit = "") { if (condition) Property(property, value, unit); return this; }

    // ── CSS-переменные ────────────────────────────────────────────────────────────
    public StyleBuilder Variable(string name, string? value)
    {
        if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(value))
        {
            var varName = $"--{name.TrimStart('-').Trim()}";
            (_parts ??= new()).Add((varName, value.Trim()));
        }
        return this;
    }

    public StyleBuilder Variable(string name, int value, string unit = "") =>
        Variable(name, $"{value}{unit}");

    public StyleBuilder Variable(string name, double value, string unit = "") =>
        Variable(name, FormattableString.Invariant($"{value:G}{unit}"));

    /// <summary>
    /// Добавить transition свойство.
    /// </summary>
    public StyleBuilder Transition(string properties = "all", int durationMs = 300, string easing = "ease")
        => Property("transition", $"{properties} {durationMs}ms {easing}");

    /// <summary>
    /// Добавить CSS custom property (var()) ссылку.
    /// Пример: .UseVariable("color", "primary-color") → "color: var(--primary-color)"
    /// </summary>
    public StyleBuilder UseVariable(string property, string varName, string? fallback = null)
    {
        if (string.IsNullOrWhiteSpace(property) || string.IsNullOrWhiteSpace(varName))
            return this;

        var cleanVarName = varName.TrimStart('-');
        var val = fallback is not null
            ? $"var(--{cleanVarName}, {fallback})"
            : $"var(--{cleanVarName})";

        return Property(property, val);
    }

    // ── Удаление ─────────────────────────────────────────────────────────────────
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

    // ── Проверка ──────────────────────────────────────────────────────────────────
    /// <summary>Проверить наличие CSS-свойства.</summary>
    public bool HasProperty(string property)
    {
        if (_parts is null) return false;
        return _parts.Exists(p => string.Equals(p.Property, property.Trim(),
            StringComparison.OrdinalIgnoreCase));
    }

    // ── Объединение ───────────────────────────────────────────────────────────────
    /// <summary>Слить два StyleBuilder'а (части other добавляются после this).</summary>
    public StyleBuilder Merge(StyleBuilder? other)
    {
        if (other is null) return this;
        if (other._parts is not null)
            foreach (var (prop, val) in other._parts)
                (_parts ??= new()).Add((prop, val));
        return this;
    }

    // ── Клонирование ─────────────────────────────────────────────────────────────
    public StyleBuilder Clone()
    {
        var clone = new StyleBuilder(_base);
        if (_parts is not null) clone._parts = new List<(string, string)>(_parts);
        return clone;
    }

    // ── Сборка ────────────────────────────────────────────────────────────────────
    public string Build()
    {
        if (_parts is null || _parts.Count == 0)
            return _base is null ? string.Empty : (_base.EndsWith(';') ? _base : _base + ";");

        var parts = new List<string>(_parts.Count + 1);
        if (_base is not null) parts.Add(_base);

        foreach (var (prop, val) in _parts!)
        {
            if (string.IsNullOrEmpty(val))
                parts.Add(prop);
            else
                parts.Add($"{prop}:{val}");
        }

        var result = string.Join(";", parts);
        return result.EndsWith(';') ? result : result + ";";
    }

    public static implicit operator string(StyleBuilder builder) => builder.Build();
    public override string ToString() => Build();

    private static string? NormalizeStyle(string? style)
    {
        if (string.IsNullOrWhiteSpace(style)) return null;
        var s = style.Trim().TrimEnd(';').Trim();
        return string.IsNullOrEmpty(s) ? null : s;
    }
}
