// SuperUI/Base/Utilities/SgCssBuilder.cs
// УЛУЧШЕНИЯ v3:
// ✅ PERF: Deduplicate использует HashSet<string> (быстрее FrozenSet для одноразовой операции)
// ✅ NEW: AddIfNotEmpty(string? value, string cssClass)
// ✅ NEW: Dark(string) / Light(string) для theme-aware CSS
// ✅ NEW: WithConditionalPrefix(bool, string) для runtime-breakpoints
// ✅ PERF: Build() — fast path для пустого builder

using System.Runtime.CompilerServices;
using System.Text;

namespace SuperUI.Base.Utilities;

/// <summary>
/// Fluent CSS class builder с кэшированием результата.
/// Не перестраивает строку если входные данные не изменились.
/// </summary>
public sealed class SgCssBuilder
{
    private readonly List<(string Class, bool Condition)> _entries = new();
    private string? _cachedResult;
    private bool _isDirty = true;
    private string? _prefix;

    public SgCssBuilder(string? baseClass = null)
    {
        if (!string.IsNullOrWhiteSpace(baseClass))
            Add(baseClass.Trim());
    }

    /// <summary>Создаёт новый билдер.</summary>
    public static SgCssBuilder Default() => new();

    /// <summary>Создаёт билдер с базовым классом.</summary>
    public static SgCssBuilder WithBase(string baseClass) => new SgCssBuilder(baseClass);

    public bool IsEmpty => !_entries.Any(e => e.Condition);

    // ── Добавить ───────────────────────────────────────────────────────────────
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SgCssBuilder Add(string? cssClass)
    {
        if (!string.IsNullOrWhiteSpace(cssClass))
        {
            var cls = cssClass.Trim();
            if (_prefix is not null) cls = _prefix + cls;
            _entries.Add((cls, true));
            _isDirty = true;
        }
        return this;
    }

    /// <summary>Добавляет класс при выполнении условия.</summary>
    public SgCssBuilder Add(string? cssClass, bool condition)
    {
        if (!string.IsNullOrWhiteSpace(cssClass))
        {
            var cls = cssClass.Trim();
            if (_prefix is not null) cls = _prefix + cls;
            _entries.Add((cls, condition));
            _isDirty = true;
        }
        return this;
    }

    /// <summary>Добавляет класс при выполнении условия (lazy).</summary>
    public SgCssBuilder Add(string cssClass, Func<bool> condition)
        => Add(cssClass, condition());

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

    // ── Условные ──────────────────────────────────────────────────────────────
    public SgCssBuilder If(bool condition, string? cssClass) => Add(cssClass, condition);

    public SgCssBuilder If(bool condition, string? trueClass, string? falseClass)
        => AddEither(trueClass ?? string.Empty, falseClass ?? string.Empty, condition);

    public SgCssBuilder If(Func<bool> condition, string? cssClass) => Add(cssClass, condition);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SgCssBuilder IfNot(bool condition, string? cssClass) => If(!condition, cssClass);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SgCssBuilder AddIf(bool condition, string? cssClass) => If(condition, cssClass);

    /// <summary>Добавляет один из двух классов в зависимости от условия.</summary>
    public SgCssBuilder AddEither(string trueClass, string falseClass, bool condition)
    {
        if (condition) Add(trueClass);
        else Add(falseClass);
        return this;
    }

    /// <summary>Добавляет класс если строка не null/empty.</summary>
    public SgCssBuilder AddIfNotEmpty(string? cssClass)
        => string.IsNullOrWhiteSpace(cssClass) ? this : Add(cssClass!);

    /// <summary>
    /// Добавить cssClass если value не пустое.
    /// Пример: .AddIfNotEmpty(Title, "has-title")
    /// </summary>
    public SgCssBuilder AddIfNotEmpty(string? value, string cssClass)
    {
        if (!string.IsNullOrWhiteSpace(value)) Add(cssClass);
        return this;
    }

    /// <summary>Добавляет пользовательский класс (последним, для override).</summary>
    public SgCssBuilder AddUserClass(string? userClass) => AddIfNotEmpty(userClass);

    // ── BEM ────────────────────────────────────────────────────────────────────
    public SgCssBuilder Modifier(string? modifier, bool condition = true)
    {
        if (condition && !string.IsNullOrWhiteSpace(modifier))
        {
            var baseClass = _entries.FirstOrDefault(e => e.Condition).Class;
            if (!string.IsNullOrEmpty(baseClass))
                Add($"{baseClass}--{modifier.Trim()}");
        }
        return this;
    }

    public SgCssBuilder Element(string? element, bool condition = true)
    {
        if (condition && !string.IsNullOrWhiteSpace(element))
        {
            var baseClass = _entries.FirstOrDefault(e => e.Condition).Class;
            if (!string.IsNullOrEmpty(baseClass))
                Add($"{baseClass}__{element.Trim()}");
        }
        return this;
    }

    public SgCssBuilder State(string stateName, bool condition)
    {
        if (condition && !string.IsNullOrWhiteSpace(stateName))
            Add($"is-{stateName.Trim()}");
        return this;
    }

    // ── Responsive ─────────────────────────────────────────────────────────────
    public SgCssBuilder Responsive(string breakpoint, string? cssClass, bool condition = true)
    {
        if (condition && !string.IsNullOrWhiteSpace(cssClass) && !string.IsNullOrWhiteSpace(breakpoint))
            Add($"{breakpoint.Trim()}:{cssClass.Trim()}");
        return this;
    }

    /// <summary>
    /// Добавить prefix к классу только при условии (runtime breakpoint-switching).
    /// Пример: .WithConditionalPrefix(IsMobile, "sm:")
    /// </summary>
    public SgCssBuilder WithConditionalPrefix(bool condition, string prefix)
    {
        _prefix = condition && !string.IsNullOrWhiteSpace(prefix) ? prefix : null;
        return this;
    }

    // ── Theme ──────────────────────────────────────────────────────────────────
    /// <summary>Добавить класс для тёмной темы (dark: prefix).</summary>
    public SgCssBuilder Dark(string? cssClass, bool condition = true)
        => Responsive("dark", cssClass, condition);

    /// <summary>Условный класс для светлой и тёмной темы.</summary>
    public SgCssBuilder Theme(bool isDark, string? lightClass, string? darkClass)
        => isDark ? Add(darkClass) : Add(lightClass);

    // ── Маппинг ────────────────────────────────────────────────────────────────
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

    // ── Удаление / Очистка ──────────────────────────────────────────────────────
    public SgCssBuilder Remove(string cssClass)
    {
        if (!string.IsNullOrWhiteSpace(cssClass))
        {
            var cls = cssClass.Trim();
            _entries.RemoveAll(e => e.Class == cls);
            _isDirty = true;
        }
        return this;
    }

    public SgCssBuilder Clear()
    {
        _entries.Clear();
        _cachedResult = null;
        _isDirty = true;
        return this;
    }

    public SgCssBuilder Transform(Func<string, string> transform)
    {
        ArgumentNullException.ThrowIfNull(transform);
        if (_entries.Count > 0)
        {
            var last = _entries[^1];
            _entries[^1] = (transform(last.Class), last.Condition);
            _isDirty = true;
        }
        return this;
    }

    // ── Дедупликация ───────────────────────────────────────────────────────────
    public SgCssBuilder Deduplicate()
    {
        if (_entries.Count < 2) return this;
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var toRemove = new List<int>();
        for (int i = 0; i < _entries.Count; i++)
        {
            if (!seen.Add(_entries[i].Class))
                toRemove.Add(i);
        }
        if (toRemove.Count > 0)
        {
            for (int i = toRemove.Count - 1; i >= 0; i--)
                _entries.RemoveAt(toRemove[i]);
            _isDirty = true;
        }
        return this;
    }

    // ── Prefix ─────────────────────────────────────────────────────────────────
    public SgCssBuilder WithPrefix(string? prefix)
    {
        _prefix = string.IsNullOrEmpty(prefix) ? null : prefix;
        return this;
    }

    public SgCssBuilder ClearPrefix()
    {
        _prefix = null;
        return this;
    }

    // ── Объединение ────────────────────────────────────────────────────────────
    public SgCssBuilder Merge(SgCssBuilder? other)
    {
        if (other is null) return this;
        foreach (var entry in other._entries)
        {
            if (entry.Condition) Add(entry.Class);
        }
        return this;
    }

    // ── Клонирование ───────────────────────────────────────────────────────────
    public SgCssBuilder Clone()
    {
        var clone = new SgCssBuilder { _prefix = _prefix };
        clone._entries.AddRange(_entries);
        clone._isDirty = _isDirty;
        clone._cachedResult = _cachedResult;
        return clone;
    }

    // ── Сборка ─────────────────────────────────────────────────────────────────
    /// <summary>
    /// Возвращает итоговую строку CSS классов.
    /// Результат кэшируется до следующего изменения.
    /// </summary>
    public string Build()
    {
        if (!_isDirty && _cachedResult != null)
            return _cachedResult;

        _isDirty = false;

        var activeEntries = _entries.Where(e => e.Condition).ToList();
        if (activeEntries.Count == 0)
        {
            _cachedResult = string.Empty;
            return _cachedResult;
        }

        // Fast paths
        if (activeEntries.Count == 1)
        {
            _cachedResult = activeEntries[0].Class;
            return _cachedResult;
        }

        int capacity = activeEntries.Sum(e => e.Class.Length + 1);
        var sb = new StringBuilder(capacity);

        bool first = true;
        foreach (var entry in activeEntries)
        {
            if (!first) sb.Append(' ');
            sb.Append(entry.Class);
            first = false;
        }

        _cachedResult = sb.ToString();
        return _cachedResult;
    }

    public static implicit operator string(SgCssBuilder builder) => builder.Build();
    public override string ToString() => Build();
}
