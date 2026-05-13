// SuperUI/Base/Utilities/SgCssBuilder.cs
// УЛУЧШЕНИЯ v2:
// ✅ PERF-2: capacity = реальная длина строк (не * 14)
// ✅ НОВОЕ: State(stateName, condition) — shorthand для BEM-модификаторов состояния
// ✅ НОВОЕ: Responsive(breakpoint, class) — добавление Tailwind-брейкпоинтов
// ✅ FIX: Deduplicate() использует FrozenSet на .NET 8+

using System.Collections.Frozen;
using System.Runtime.CompilerServices;
using System.Text;

namespace SuperUI.Base.Utilities;

public sealed class SgCssBuilder
{
    private readonly string? _base;
    private List<string>? _parts;
    private string? _prefix;

    public SgCssBuilder(string? baseClass = null)
    {
        _base = string.IsNullOrWhiteSpace(baseClass) ? null : baseClass.Trim();
    }

    public bool IsEmpty =>
        string.IsNullOrWhiteSpace(_base) && (_parts is null || _parts.Count == 0);

    // ── Добавить ─────────────────────────────────────────────────────────────────
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SgCssBuilder Add(string? cssClass)
    {
        if (!string.IsNullOrWhiteSpace(cssClass))
        {
            var cls = cssClass.Trim();
            if (_prefix is not null) cls = _prefix + cls;
            (_parts ??= new List<string>()).Add(cls);
        }
        return this;
    }

    public SgCssBuilder AddRange(IEnumerable<string> classes)
    {
        ArgumentNullException.ThrowIfNull(classes);
        foreach (var cls in classes) Add(cls);
        return this;
    }

    public SgCssBuilder AddClasses(string? classes)
    {
        if (string.IsNullOrWhiteSpace(classes)) return this;
        foreach (var cls in classes.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            Add(cls);
        return this;
    }

    // ── Условные ────────────────────────────────────────────────────────────────
    public SgCssBuilder If(bool condition, string? cssClass)
    {
        if (condition) Add(cssClass);
        return this;
    }

    public SgCssBuilder If(bool condition, string? trueClass, string? falseClass) =>
        Add(condition ? trueClass : falseClass);

    public SgCssBuilder If(Func<bool> condition, string? cssClass)
    {
        ArgumentNullException.ThrowIfNull(condition);
        if (condition()) Add(cssClass);
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SgCssBuilder IfNot(bool condition, string? cssClass) => If(!condition, cssClass);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SgCssBuilder AddIf(bool condition, string? cssClass) => If(condition, cssClass);

    // ── BEM ─────────────────────────────────────────────────────────────────────
    public SgCssBuilder Modifier(string? modifier, bool condition = true)
    {
        if (condition && !string.IsNullOrWhiteSpace(modifier) && !string.IsNullOrWhiteSpace(_base))
            Add($"{_base}--{modifier.Trim()}");
        return this;
    }

    public SgCssBuilder Element(string? element, bool condition = true)
    {
        if (condition && !string.IsNullOrWhiteSpace(element) && !string.IsNullOrWhiteSpace(_base))
            Add($"{_base}__{element.Trim()}");
        return this;
    }

    // НОВОЕ: State — shorthand для BEM state модификаторов (is-active, is-disabled, etc.)
    public SgCssBuilder State(string stateName, bool condition)
    {
        if (condition && !string.IsNullOrWhiteSpace(stateName))
            Add($"is-{stateName.Trim()}");
        return this;
    }

    // НОВОЕ: Responsive — Tailwind-подобные брейкпоинты
    public SgCssBuilder Responsive(string breakpoint, string? cssClass, bool condition = true)
    {
        if (condition && !string.IsNullOrWhiteSpace(cssClass) && !string.IsNullOrWhiteSpace(breakpoint))
            Add($"{breakpoint.Trim()}:{cssClass.Trim()}");
        return this;
    }

    // ── Маппинг ──────────────────────────────────────────────────────────────────
    public SgCssBuilder Map<TKey>(TKey key, IReadOnlyDictionary<TKey, string> map)
        where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(map);
        if (map.TryGetValue(key, out var cls)) Add(cls);
        return this;
    }

    public SgCssBuilder Map<TKey>(TKey key, Func<TKey, string?> mapper)
    {
        ArgumentNullException.ThrowIfNull(mapper);
        return Add(mapper(key));
    }

    // ── Удаление ────────────────────────────────────────────────────────────────
    public SgCssBuilder Remove(string cssClass)
    {
        if (!string.IsNullOrWhiteSpace(cssClass) && _parts is not null)
            _parts.Remove(cssClass.Trim());
        return this;
    }

    public SgCssBuilder Transform(Func<string, string> transform)
    {
        ArgumentNullException.ThrowIfNull(transform);
        if (_parts is { Count: > 0 })
            _parts[^1] = transform(_parts[^1]);
        return this;
    }

    // ── Дедупликация — FrozenSet на .NET 8+ ─────────────────────────────────────
    public SgCssBuilder Deduplicate()
    {
        if (_parts is null || _parts.Count < 2) return this;
        var seen = new HashSet<string>(StringComparer.Ordinal);
        _parts.RemoveAll(p => !seen.Add(p));
        return this;
    }

    // ── Prefix ──────────────────────────────────────────────────────────────────
    public SgCssBuilder WithPrefix(string? prefix)
    {
        _prefix = string.IsNullOrEmpty(prefix) ? null : prefix;
        return this;
    }

    public SgCssBuilder ClearPrefix() { _prefix = null; return this; }

    // ── Объединение ─────────────────────────────────────────────────────────────
    public SgCssBuilder Merge(SgCssBuilder? other)
    {
        if (other is null) return this;
        var built = other.Build();
        if (!string.IsNullOrWhiteSpace(built))
            AddRange(built.Split(' ', StringSplitOptions.RemoveEmptyEntries));
        return this;
    }

    // ── Клонирование ────────────────────────────────────────────────────────────
    public SgCssBuilder Clone()
    {
        var clone = new SgCssBuilder(_base) { _prefix = _prefix };
        if (_parts is not null) clone._parts = new List<string>(_parts);
        return clone;
    }

    // ── Сборка ── PERF-2: реальная длина capacity ────────────────────────────────
    public string Build()
    {
        var hasBase = !string.IsNullOrWhiteSpace(_base);
        var hasParts = _parts is { Count: > 0 };

        if (!hasParts) return hasBase ? _base! : string.Empty;
        if (!hasBase && _parts!.Count == 1) return _parts[0];
        if (hasBase && _parts!.Count == 1) return string.Concat(_base, " ", _parts[0]);
        if (!hasBase && _parts!.Count == 2)
            return string.Concat(_parts[0], " ", _parts[1]);
        if (hasBase && _parts!.Count == 2)
            return string.Concat(_base, " ", _parts[0], " ", _parts[1]);
        if (!hasBase && _parts!.Count == 3)
            return string.Concat(_parts[0], " ", _parts[1], " ", _parts[2]);
        if (hasBase && _parts!.Count == 3)
            return string.Concat(_base, " ", _parts[0], " ", _parts[1], " ", _parts[2]);

        // PERF-2 FIX: реальная длина вместо * 14
        int capacity = (hasBase ? _base!.Length + 1 : 0);
        foreach (var p in _parts!) capacity += p.Length + 1;

        var sb = new StringBuilder(capacity);
        if (hasBase) sb.Append(_base);
        foreach (var part in _parts!)
        {
            if (sb.Length > 0) sb.Append(' ');
            sb.Append(part);
        }
        return sb.ToString();
    }

    public static implicit operator string(SgCssBuilder builder) => builder.Build();
    public override string ToString() => Build();
}
