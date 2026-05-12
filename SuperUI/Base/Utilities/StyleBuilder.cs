// SuperUI/Base/Utilities/StyleBuilder.cs
// ✅ ИСПРАВЛЕНИЯ:
//   - Build(): корректная расстановка ";" между элементами (не двойные точки запятые)
//   - Property(string, double): FormattableString.Invariant для culture-safety
// ✅ УЛУЧШЕНИЯ:
//   - Flex() / Grid() helper методы
//   - Size() shorthand для width + height
//   - Position() для абсолютного/фиксированного позиционирования
//   - Display() shorthand

namespace SuperUI.Base.Utilities;

/// <summary>
/// Fluent-builder для inline CSS стилей.
/// </summary>
/// <example>
/// <code>
/// var style = new StyleBuilder()
///     .Property("width", Width, "px")
///     .If(Hidden, "display", "none")
///     .Variable("primary-color", "#0066cc")
///     .Build();
/// </code>
/// </example>
public sealed class StyleBuilder
{
    private readonly string? _base;
    private List<(string Property, string Value)>? _parts;

    /// <summary>Создать builder с базовым стилем (строка "prop:value").</summary>
    public StyleBuilder(string? baseStyle = null)
    {
        _base = NormalizeStyle(baseStyle);
    }

    /// <summary>true если нет ни базового стиля, ни добавленных свойств.</summary>
    public bool IsEmpty => _base is null && (_parts is null || _parts.Count == 0);

    // ── Добавить ──────────────────────────────────────────────────────────────

    /// <summary>Добавить стиль в виде строки "prop:value" или "prop:value;...".</summary>
    public StyleBuilder Add(string? style)
    {
        var s = NormalizeStyle(style);
        if (s is not null)
        {
            // Поддержка нескольких свойств в одной строке ("width:100px;height:50px")
            foreach (var part in s.Split(';', StringSplitOptions.RemoveEmptyEntries))
            {
                var trimmed = part.Trim();
                if (string.IsNullOrEmpty(trimmed)) continue;
                var idx = trimmed.IndexOf(':', StringComparison.Ordinal);
                if (idx > 0)
                    (_parts ??= new()).Add((trimmed[..idx].Trim(), trimmed[(idx + 1)..].Trim()));
                else
                    (_parts ??= new()).Add((trimmed, string.Empty));
            }
        }
        return this;
    }

    /// <summary>Добавить CSS-свойство с текстовым значением.</summary>
    public StyleBuilder Property(string property, string? value)
    {
        if (!string.IsNullOrWhiteSpace(property) && !string.IsNullOrWhiteSpace(value))
            (_parts ??= new()).Add((property.Trim(), value.Trim()));
        return this;
    }

    /// <summary>Добавить CSS-свойство с числовым значением и единицей измерения.</summary>
    public StyleBuilder Property(string property, int value, string unit = "")
        => Property(property, $"{value}{unit}");

    /// <summary>Добавить CSS-свойство с числовым значением (culture-safe).</summary>
    public StyleBuilder Property(string property, double value, string unit = "")
        => Property(property, FormattableString.Invariant($"{value:G}{unit}"));

    // ── Shorthand helpers ────────────────────────────────────────────────────

    /// <summary>Установить width и height одновременно.</summary>
    public StyleBuilder Size(string width, string? height = null)
    {
        Property("width", width);
        Property("height", height ?? width);
        return this;
    }

    /// <summary>Установить width и height в пикселях.</summary>
    public StyleBuilder Size(int widthPx, int? heightPx = null)
    {
        Property("width", widthPx, "px");
        Property("height", heightPx ?? widthPx, "px");
        return this;
    }

    /// <summary>Задать display.</summary>
    public StyleBuilder Display(string value) => Property("display", value);

    /// <summary>Включить Flexbox с настройками направления и переноса.</summary>
    public StyleBuilder Flex(string direction = "row",
        string wrap = "nowrap",
        string? justify = null,
        string? align = null)
    {
        Property("display", "flex");
        if (direction != "row") Property("flex-direction", direction);
        if (wrap != "nowrap")   Property("flex-wrap", wrap);
        if (justify != null)    Property("justify-content", justify);
        if (align != null)      Property("align-items", align);
        return this;
    }

    /// <summary>Задать CSS Grid template.</summary>
    public StyleBuilder Grid(string columns, string? rows = null, string? gap = null)
    {
        Property("display", "grid");
        Property("grid-template-columns", columns);
        if (rows != null) Property("grid-template-rows", rows);
        if (gap != null)  Property("gap", gap);
        return this;
    }

    /// <summary>Абсолютное позиционирование (top/right/bottom/left в px).</summary>
    public StyleBuilder AbsoluteAt(int? top = null, int? right = null,
        int? bottom = null, int? left = null)
    {
        Property("position", "absolute");
        if (top.HasValue)    Property("top", top.Value, "px");
        if (right.HasValue)  Property("right", right.Value, "px");
        if (bottom.HasValue) Property("bottom", bottom.Value, "px");
        if (left.HasValue)   Property("left", left.Value, "px");
        return this;
    }

    // ── CSS calc() ───────────────────────────────────────────────────────────

    /// <summary>Добавить CSS calc() выражение.</summary>
    public StyleBuilder Calc(string property, string expression)
        => Property(property, $"calc({expression})");

    // ── Условные ─────────────────────────────────────────────────────────────

    /// <summary>Добавить стиль при условии.</summary>
    public StyleBuilder If(bool condition, string? style)
    {
        if (condition) Add(style);
        return this;
    }

    /// <summary>Добавить свойство при условии.</summary>
    public StyleBuilder If(bool condition, string property, string value)
    {
        if (condition) Property(property, value);
        return this;
    }

    /// <summary>Добавить числовое свойство при условии.</summary>
    public StyleBuilder If(bool condition, string property, int value, string unit = "")
    {
        if (condition) Property(property, value, unit);
        return this;
    }

    /// <summary>Добавить числовое свойство при условии.</summary>
    public StyleBuilder If(bool condition, string property, double value, string unit = "")
    {
        if (condition) Property(property, value, unit);
        return this;
    }

    // ── CSS-переменные ───────────────────────────────────────────────────────

    /// <summary>Добавить CSS custom property (--name: value).</summary>
    public StyleBuilder Variable(string name, string? value)
    {
        if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(value))
        {
            var varName = $"--{name.TrimStart('-').Trim()}";
            (_parts ??= new()).Add((varName, value.Trim()));
        }
        return this;
    }

    /// <summary>Добавить CSS custom property с числовым значением.</summary>
    public StyleBuilder Variable(string name, int value, string unit = "")
        => Variable(name, $"{value}{unit}");

    /// <summary>Добавить CSS custom property с float значением (culture-safe).</summary>
    public StyleBuilder Variable(string name, double value, string unit = "")
        => Variable(name, FormattableString.Invariant($"{value:G}{unit}"));

    /// <summary>
    /// Ссылка на CSS custom property: "prop: var(--name)".
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

    /// <summary>Shorthand для UseVariable.</summary>
    public StyleBuilder Var(string property, string varName, string? fallback = null)
        => UseVariable(property, varName, fallback);

    // ── Transition / Animation ───────────────────────────────────────────────

    /// <summary>Добавить transition.</summary>
    public StyleBuilder Transition(string properties = "all",
        int durationMs = 300,
        string easing = "ease",
        int delayMs = 0)
    {
        var val = delayMs > 0
            ? $"{properties} {durationMs}ms {easing} {delayMs}ms"
            : $"{properties} {durationMs}ms {easing}";
        return Property("transition", val);
    }

    // ── Удаление ─────────────────────────────────────────────────────────────

    /// <summary>Удалить CSS-свойство по имени.</summary>
    public StyleBuilder Remove(string property)
    {
        if (!string.IsNullOrWhiteSpace(property) && _parts is not null)
        {
            var trimmed = property.Trim();
            for (var i = _parts.Count - 1; i >= 0; i--)
            {
                if (string.Equals(_parts[i].Property, trimmed,
                    StringComparison.OrdinalIgnoreCase))
                {
                    _parts.RemoveAt(i);
                    break;
                }
            }
        }
        return this;
    }

    // ── Проверка ─────────────────────────────────────────────────────────────

    /// <summary>Проверить наличие CSS-свойства.</summary>
    public bool HasProperty(string property)
    {
        if (_parts is null) return false;
        return _parts.Exists(p =>
            string.Equals(p.Property, property.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    // ── Объединение ──────────────────────────────────────────────────────────

    /// <summary>Слить два StyleBuilder-а (части other добавляются после this).</summary>
    public StyleBuilder Merge(StyleBuilder? other)
    {
        if (other is null) return this;
        if (other._parts is not null)
            foreach (var (prop, val) in other._parts)
                (_parts ??= new()).Add((prop, val));
        return this;
    }

    // ── Клонирование ─────────────────────────────────────────────────────────

    /// <summary>Создать независимую копию.</summary>
    public StyleBuilder Clone()
    {
        var clone = new StyleBuilder(_base);
        if (_parts is not null)
            clone._parts = new List<(string, string)>(_parts);
        return clone;
    }

    // ── Сборка ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Собрать итоговую строку стилей.
    /// Формат: "prop1:value1;prop2:value2;"
    /// </summary>
    public string Build()
    {
        var hasParts = _parts is { Count: > 0 };

        // Only base
        if (!hasParts)
        {
            if (_base is null) return string.Empty;
            return _base.EndsWith(';') ? _base : _base + ";";
        }

        // Estimate capacity
        var capacity = (_base?.Length ?? 0) + _parts!.Count * 20;
        var sb = new System.Text.StringBuilder(capacity);

        if (_base is not null)
        {
            sb.Append(_base.TrimEnd(';'));
            sb.Append(';');
        }

        foreach (var (prop, val) in _parts!)
        {
            sb.Append(prop);
            if (!string.IsNullOrEmpty(val))
            {
                sb.Append(':');
                sb.Append(val);
            }
            sb.Append(';');
        }

        return sb.ToString();
    }

    /// <summary>Неявное приведение к string (вызывает Build()).</summary>
    public static implicit operator string(StyleBuilder builder) => builder.Build();

    /// <inheritdoc/>
    public override string ToString() => Build();

    // ── Нормализация ─────────────────────────────────────────────────────────

    private static string? NormalizeStyle(string? style)
    {
        if (string.IsNullOrWhiteSpace(style)) return null;
        var s = style.Trim().TrimEnd(';').Trim();
        return string.IsNullOrEmpty(s) ? null : s;
    }
}
