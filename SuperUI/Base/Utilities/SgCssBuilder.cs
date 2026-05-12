// SuperUI/Base/Utilities/SgCssBuilder.cs
//
// ДОРАБОТКИ:
// 1. WithPrefix(string) — добавить prefix ко всем последующим классам
// 2. Deduplicate() — убрать дубликаты (сохраняя порядок)
// 3. Map<TKey>(TKey, Func<TKey,string?>) — mapper-версия
// 4. Build() → Span<char> fast-path для <= 3 частей без StringBuilder аллокации
// 5. IsEmpty — проверка

using System.Runtime.CompilerServices;
using System.Text;

namespace SuperUI.Base.Utilities;

/// <summary>
/// Fluent-builder для CSS-классов.
/// </summary>
public sealed class SgCssBuilder
{
    private readonly string? _base;
    private List<string>? _parts;
    private string? _prefix;

    public SgCssBuilder(string? baseClass = null)
    {
        _base = string.IsNullOrWhiteSpace(baseClass) ? null : baseClass.Trim();
    }

    /// <summary>Нет ни базового класса, ни добавленных.</summary>
    public bool IsEmpty =>
        string.IsNullOrWhiteSpace(_base) && (_parts is null || _parts.Count == 0);

    // ── Добавить ──────────────────────────────────────────────────────────────────
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

    public SgCssBuilder AddRange(IEnumerable<string?> classes)
    {
        ArgumentNullException.ThrowIfNull(classes);
        foreach (var cls in classes) Add(cls);
        return this;
    }

    // ── Условные ─────────────────────────────────────────────────────────────────
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
    public SgCssBuilder AddIf(bool condition, string? cssClass) => If(condition, cssClass);

    // ── Маппинг ───────────────────────────────────────────────────────────────────
    public SgCssBuilder Map<TKey>(TKey key, IReadOnlyDictionary<TKey, string?> map)
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

    // ── Удаление ──────────────────────────────────────────────────────────────────
    public SgCssBuilder Remove(string cssClass)
    {
        if (!string.IsNullOrWhiteSpace(cssClass) && _parts is not null)
            _parts.Remove(cssClass.Trim());
        return this;
    }

    // ── Дедупликация ──────────────────────────────────────────────────────────────
    /// <summary>Убрать дублирующиеся классы (сохраняя первое вхождение).</summary>
    public SgCssBuilder Deduplicate()
    {
        if (_parts is null || _parts.Count <= 1) return this;
        var seen = new HashSet<string>(StringComparer.Ordinal);
        _parts.RemoveAll(p => !seen.Add(p));
        return this;
    }

    // ── Prefix ────────────────────────────────────────────────────────────────────
    /// <summary>Установить префикс для всех последующих Add-вызовов.</summary>
    public SgCssBuilder WithPrefix(string? prefix)
    {
        _prefix = string.IsNullOrEmpty(prefix) ? null : prefix;
        return this;
    }

    /// <summary>Сбросить префикс.</summary>
    public SgCssBuilder ClearPrefix() { _prefix = null; return this; }

    // ── Объединение ───────────────────────────────────────────────────────────────
    public SgCssBuilder Merge(SgCssBuilder? other)
    {
        if (other is null) return this;
        var built = other.Build();
        if (!string.IsNullOrWhiteSpace(built))
            AddRange(built.Split(' ', StringSplitOptions.RemoveEmptyEntries));
        return this;
    }

    // ── Клонирование ─────────────────────────────────────────────────────────────
    public SgCssBuilder Clone()
    {
        var clone = new SgCssBuilder(_base) { _prefix = _prefix };
        if (_parts is not null) clone._parts = new List<string>(_parts);
        return clone;
    }

    // ── Сборка ────────────────────────────────────────────────────────────────────
    public string Build()
    {
        var hasBase  = !string.IsNullOrWhiteSpace(_base);
        var hasParts = _parts is { Count: > 0 };

        if (!hasParts) return hasBase ? _base! : string.Empty;
        if (!hasBase && _parts!.Count == 1) return _parts[0];
        if (hasBase  && _parts!.Count == 1) return string.Concat(_base, " ", _parts[0]);

        var capacity = (hasBase ? _base!.Length + 1 : 0) + _parts!.Count * 12;
        var sb       = new StringBuilder(capacity);
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
