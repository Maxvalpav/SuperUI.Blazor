// SuperUI/Base/Utilities/StyleBuilder.cs
// УЛУЧШЕНИЯ v2:
// ✅ НОВОЕ: Important() — добавление !important к последнему свойству
// ✅ НОВОЕ: Animation() — shorthand для CSS animation
// ✅ НОВОЕ: Transform() — CSS transform shorthand
// ✅ НОВОЕ: MaxSize() / MinSize() shorthands
// ✅ НОВОЕ: ZIndex() shorthand
// ✅ НОВОЕ: Overflow() shorthand
// ✅ FIX: Build() — корректный separator при base + parts

namespace SuperUI.Base.Utilities;

public sealed class StyleBuilder
{
    private readonly string? _base;
    private List<(string Property, string Value, bool Important)>? _parts;

    public StyleBuilder(string? baseStyle = null)
    {
        _base = NormalizeStyle(baseStyle);
    }

    public bool IsEmpty =>
        _base is null && (_parts is null || _parts.Count == 0);

    // ── Добавить ─────────────────────────────────────────────────────────────────
    public StyleBuilder Add(string? style)
    {
        var s = NormalizeStyle(style);
        if (s is not null)
        {
            foreach (var part in s.Split(';', StringSplitOptions.RemoveEmptyEntries))
            {
                var trimmed = part.Trim();
                if (string.IsNullOrEmpty(trimmed)) continue;
                var idx = trimmed.IndexOf(':', StringComparison.Ordinal);
                if (idx > 0)
                    (_parts ??= new()).Add((trimmed[..idx].Trim(), trimmed[(idx + 1)..].Trim(), false));
                else
                    (_parts ??= new()).Add((trimmed, string.Empty, false));
            }
        }
        return this;
    }

    public StyleBuilder Property(string property, string? value, bool important = false)
    {
        if (!string.IsNullOrWhiteSpace(property) && !string.IsNullOrWhiteSpace(value))
            (_parts ??= new()).Add((property.Trim(), value.Trim(), important));
        return this;
    }

    public StyleBuilder Property(string property, int value, string unit = "", bool important = false) =>
        Property(property, $"{value}{unit}", important);

    public StyleBuilder Property(string property, double value, string unit = "", bool important = false) =>
        Property(property, FormattableString.Invariant($"{value:G}{unit}"), important);

    // НОВОЕ: добавить !important к последнему свойству
    public StyleBuilder Important()
    {
        if (_parts is { Count: > 0 })
        {
            var last = _parts[^1];
            _parts[^1] = (last.Property, last.Value, true);
        }
        return this;
    }

    // ── Shorthand helpers ────────────────────────────────────────────────────────
    public StyleBuilder Size(string width, string? height = null)
    {
        Property("width", width);
        Property("height", height ?? width);
        return this;
    }

    public StyleBuilder Size(int widthPx, int? heightPx = null)
    {
        Property("width", widthPx, "px");
        Property("height", heightPx ?? widthPx, "px");
        return this;
    }

    public StyleBuilder MaxSize(string? maxWidth = null, string? maxHeight = null)
    {
        if (maxWidth != null) Property("max-width", maxWidth);
        if (maxHeight != null) Property("max-height", maxHeight);
        return this;
    }

    public StyleBuilder MinSize(string? minWidth = null, string? minHeight = null)
    {
        if (minWidth != null) Property("min-width", minWidth);
        if (minHeight != null) Property("min-height", minHeight);
        return this;
    }

    public StyleBuilder Display(string value) => Property("display", value);

    public StyleBuilder ZIndex(int zIndex) =>
        Property("z-index", zIndex.ToString(System.Globalization.CultureInfo.InvariantCulture));

    public StyleBuilder Overflow(string overflow = "hidden", string? overflowX = null, string? overflowY = null)
    {
        if (overflowX is null && overflowY is null)
            return Property("overflow", overflow);
        if (overflowX != null) Property("overflow-x", overflowX);
        if (overflowY != null) Property("overflow-y", overflowY);
        return this;
    }

    public StyleBuilder Flex(
        string direction = "row",
        string wrap = "nowrap",
        string? justify = null,
        string? align = null)
    {
        Property("display", "flex");
        if (direction != "row") Property("flex-direction", direction);
        if (wrap != "nowrap") Property("flex-wrap", wrap);
        if (justify != null) Property("justify-content", justify);
        if (align != null) Property("align-items", align);
        return this;
    }

    public StyleBuilder Grid(string columns, string? rows = null, string? gap = null)
    {
        Property("display", "grid");
        Property("grid-template-columns", columns);
        if (rows != null) Property("grid-template-rows", rows);
        if (gap != null) Property("gap", gap);
        return this;
    }

    public StyleBuilder AbsoluteAt(
        int? top = null, int? right = null, int? bottom = null, int? left = null)
    {
        Property("position", "absolute");
        if (top.HasValue) Property("top", top.Value, "px");
        if (right.HasValue) Property("right", right.Value, "px");
        if (bottom.HasValue) Property("bottom", bottom.Value, "px");
        if (left.HasValue) Property("left", left.Value, "px");
        return this;
    }

    // НОВОЕ: CSS transform
    public StyleBuilder Transform(string transformValue) =>
        Property("transform", transformValue);

    // НОВОЕ: CSS animation shorthand
    public StyleBuilder Animation(
        string name,
        int durationMs = 300,
        string easing = "ease",
        int delayMs = 0,
        string fillMode = "both",
        int iterations = 1)
    {
        var iterStr = iterations == int.MaxValue ? "infinite" :
            iterations.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var delay = delayMs > 0 ? $" {delayMs}ms" : "";
        return Property("animation",
            $"{name} {durationMs}ms {easing}{delay} {fillMode} {iterStr}");
    }

    public StyleBuilder Calc(string property, string expression) =>
        Property(property, $"calc({expression})");

    // ── Условные ────────────────────────────────────────────────────────────────
    public StyleBuilder If(bool condition, string? style) { if (condition) Add(style); return this; }
    public StyleBuilder If(bool condition, string property, string value) { if (condition) Property(property, value); return this; }
    public StyleBuilder If(bool condition, string property, int value, string unit = "") { if (condition) Property(property, value, unit); return this; }
    public StyleBuilder If(bool condition, string property, double value, string unit = "") { if (condition) Property(property, value, unit); return this; }

    // ── CSS переменные ──────────────────────────────────────────────────────────
    public StyleBuilder Variable(string name, string? value)
    {
        if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(value))
        {
            var varName = $"--{name.TrimStart('-').Trim()}";
            (_parts ??= new()).Add((varName, value.Trim(), false));
        }
        return this;
    }

    public StyleBuilder Variable(string name, int value, string unit = "") =>
        Variable(name, $"{value}{unit}");

    public StyleBuilder Variable(string name, double value, string unit = "") =>
        Variable(name, FormattableString.Invariant($"{value:G}{unit}"));

    public StyleBuilder UseVariable(string property, string varName, string? fallback = null)
    {
        if (string.IsNullOrWhiteSpace(property) || string.IsNullOrWhiteSpace(varName))
            return this;
        var clean = varName.TrimStart('-');
        var val = fallback is not null ? $"var(--{clean}, {fallback})" : $"var(--{clean})";
        return Property(property, val);
    }

    public StyleBuilder Var(string property, string varName, string? fallback = null) =>
        UseVariable(property, varName, fallback);

    // ── Transition ──────────────────────────────────────────────────────────────
    public StyleBuilder Transition(
        string properties = "all",
        int durationMs = 300,
        string easing = "ease",
        int delayMs = 0)
    {
        var val = delayMs > 0
            ? $"{properties} {durationMs}ms {easing} {delayMs}ms"
            : $"{properties} {durationMs}ms {easing}";
        return Property("transition", val);
    }

    // ── Удаление ────────────────────────────────────────────────────────────────
    public StyleBuilder Remove(string property)
    {
        if (!string.IsNullOrWhiteSpace(property) && _parts is not null)
        {
            var trimmed = property.Trim();
            for (var i = _parts.Count - 1; i >= 0; i--)
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

    public bool HasProperty(string property)
    {
        if (_parts is null) return false;
        return _parts.Exists(p =>
            string.Equals(p.Property, property.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    // ── Объединение / Клон ──────────────────────────────────────────────────────
    public StyleBuilder Merge(StyleBuilder? other)
    {
        if (other is null) return this;
        if (other._parts is not null)
            foreach (var (prop, val, imp) in other._parts)
                (_parts ??= new()).Add((prop, val, imp));
        return this;
    }

    public StyleBuilder Clone()
    {
        var clone = new StyleBuilder(_base);
        if (_parts is not null) clone._parts = new List<(string, string, bool)>(_parts);
        return this;
    }

    // ── Сборка ──────────────────────────────────────────────────────────────────
    public string Build()
    {
        var hasParts = _parts is { Count: > 0 };
        if (!hasParts)
        {
            if (_base is null) return string.Empty;
            return _base.EndsWith(';') ? _base : _base + ";";
        }

        int capacity = (_base?.Length ?? 0) + 1;
        foreach (var (p, v, _) in _parts!) capacity += p.Length + v.Length + 12;

        var sb = new System.Text.StringBuilder(capacity);
        if (_base is not null)
        {
            sb.Append(_base.TrimEnd(';'));
            sb.Append(';');
        }
        foreach (var (prop, val, important) in _parts!)
        {
            sb.Append(prop);
            if (!string.IsNullOrEmpty(val))
            {
                sb.Append(':');
                sb.Append(val);
                if (important) sb.Append(" !important");
            }
            sb.Append(';');
        }
        return sb.ToString();
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
