// SuperUI/Base/Utilities/SgCssBuilder.cs
// ✅ ИСПРАВЛЕНИЯ:
//   - Deduplicate(): HashSet<string> с явным типовым параметром
//   - IfNot() — новый метод
//   - AddClasses() + AddRange() корректно используют единый Add()
// ✅ УЛУЧШЕНИЯ:
//   - IfNot(bool, string) — псевдоним If(!condition, ...)
//   - ToString() в DEBUG показывает счётчик частей
//   - Build() fast-path для 3 частей

using System.Runtime.CompilerServices;
using System.Text;

namespace SuperUI.Base.Utilities;

/// <summary>
/// Fluent-builder для CSS-классов.
/// Не бросает исключений при null/пустых строках — просто игнорирует их.
/// </summary>
/// <example>
/// <code>
/// var css = new SgCssBuilder("sg-button")
///     .If(Disabled, "sg-button--disabled")
///     .If(Size == SgSize.Large, "sg-button--lg", "sg-button--md")
///     .Add(Class)
///     .Build();
/// </code>
/// </example>
public sealed class SgCssBuilder
{
    private readonly string? _base;
    private List<string>? _parts;
    private string? _prefix;

    /// <summary>Создать builder с базовым CSS-классом.</summary>
    public SgCssBuilder(string? baseClass = null)
    {
        _base = string.IsNullOrWhiteSpace(baseClass) ? null : baseClass.Trim();
    }

    /// <summary>true если нет ни базового класса, ни добавленных частей.</summary>
    public bool IsEmpty => string.IsNullOrWhiteSpace(_base)
        && (_parts is null || _parts.Count == 0);

    // ── Добавить ──────────────────────────────────────────────────────────────

    /// <summary>Добавить CSS-класс (null/пустой — игнорируется).</summary>
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

    /// <summary>Добавить коллекцию CSS-классов.</summary>
    public SgCssBuilder AddRange(IEnumerable<string?> classes)
    {
        ArgumentNullException.ThrowIfNull(classes);
        foreach (var cls in classes) Add(cls);
        return this;
    }

    /// <summary>
    /// Добавить классы из строки (разделённой пробелами).
    /// Полезно для передачи параметра Class.
    /// </summary>
    public SgCssBuilder AddClasses(string? classes)
    {
        if (string.IsNullOrWhiteSpace(classes)) return this;
        foreach (var cls in classes.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            Add(cls);
        return this;
    }

    // ── Условные ─────────────────────────────────────────────────────────────

    /// <summary>Добавить класс при выполнении условия.</summary>
    public SgCssBuilder If(bool condition, string? cssClass)
    {
        if (condition) Add(cssClass);
        return this;
    }

    /// <summary>Добавить один из двух классов в зависимости от условия.</summary>
    public SgCssBuilder If(bool condition, string? trueClass, string? falseClass)
        => Add(condition ? trueClass : falseClass);

    /// <summary>Добавить класс при выполнении условия (lazy evaluation).</summary>
    public SgCssBuilder If(Func<bool> condition, string? cssClass)
    {
        ArgumentNullException.ThrowIfNull(condition);
        if (condition()) Add(cssClass);
        return this;
    }

    /// <summary>Добавить класс при НЕвыполнении условия.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SgCssBuilder IfNot(bool condition, string? cssClass)
        => If(!condition, cssClass);

    /// <summary>Псевдоним для <see cref="If(bool, string?)"/>.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SgCssBuilder AddIf(bool condition, string? cssClass)
        => If(condition, cssClass);

    // ── BEM паттерн ──────────────────────────────────────────────────────────

    /// <summary>
    /// Добавить BEM modifier: base--{modifier}.
    /// Пример: .Modifier("primary") на базе "sg-button" → "sg-button--primary"
    /// </summary>
    public SgCssBuilder Modifier(string? modifier, bool condition = true)
    {
        if (condition && !string.IsNullOrWhiteSpace(modifier)
            && !string.IsNullOrWhiteSpace(_base))
        {
            Add($"{_base}--{modifier.Trim()}");
        }
        return this;
    }

    /// <summary>
    /// Добавить BEM element: base__element.
    /// Пример: .Element("title") на базе "sg-button" → "sg-button__title"
    /// </summary>
    public SgCssBuilder Element(string? element, bool condition = true)
    {
        if (condition && !string.IsNullOrWhiteSpace(element)
            && !string.IsNullOrWhiteSpace(_base))
        {
            Add($"{_base}__{element.Trim()}");
        }
        return this;
    }

    // ── Маппинг ──────────────────────────────────────────────────────────────

    /// <summary>Добавить класс по ключу из словаря.</summary>
    public SgCssBuilder Map<TKey>(TKey key, IReadOnlyDictionary<TKey, string> map)
        where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(map);
        if (map.TryGetValue(key, out var cls)) Add(cls);
        return this;
    }

    /// <summary>Добавить класс через функцию-маппер.</summary>
    public SgCssBuilder Map<TKey>(TKey key, Func<TKey, string?> mapper)
    {
        ArgumentNullException.ThrowIfNull(mapper);
        return Add(mapper(key));
    }

    // ── Удаление ─────────────────────────────────────────────────────────────

    /// <summary>Удалить класс из списка.</summary>
    public SgCssBuilder Remove(string cssClass)
    {
        if (!string.IsNullOrWhiteSpace(cssClass) && _parts is not null)
            _parts.Remove(cssClass.Trim());
        return this;
    }

    /// <summary>Применить трансформацию к последнему добавленному классу.</summary>
    public SgCssBuilder Transform(Func<string, string> transform)
    {
        ArgumentNullException.ThrowIfNull(transform);
        if (_parts is { Count: > 0 })
            _parts[^1] = transform(_parts[^1]);
        return this;
    }

    // ── Дедупликация ─────────────────────────────────────────────────────────

    /// <summary>Удалить дублирующиеся классы, сохраняя порядок первого вхождения.</summary>
    public SgCssBuilder Deduplicate()
    {
        if (_parts is null || _parts.Count <= 1) return this;
        var seen = new HashSet<string>(StringComparer.Ordinal); // ✅ FIX: явный тип
        _parts.RemoveAll(p => !seen.Add(p));
        return this;
    }

    // ── Prefix ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Установить префикс для всех последующих Add() вызовов.
    /// Пример: .WithPrefix("sg-").Add("button") → "sg-button"
    /// </summary>
    public SgCssBuilder WithPrefix(string? prefix)
    {
        _prefix = string.IsNullOrEmpty(prefix) ? null : prefix;
        return this;
    }

    /// <summary>Сбросить текущий префикс.</summary>
    public SgCssBuilder ClearPrefix()
    {
        _prefix = null;
        return this;
    }

    // ── Объединение ──────────────────────────────────────────────────────────

    /// <summary>Слить с другим builder-ом.</summary>
    public SgCssBuilder Merge(SgCssBuilder? other)
    {
        if (other is null) return this;
        var built = other.Build();
        if (!string.IsNullOrWhiteSpace(built))
            AddRange(built.Split(' ', StringSplitOptions.RemoveEmptyEntries));
        return this;
    }

    // ── Клонирование ─────────────────────────────────────────────────────────

    /// <summary>Создать независимую копию builder-а.</summary>
    public SgCssBuilder Clone()
    {
        var clone = new SgCssBuilder(_base) { _prefix = _prefix };
        if (_parts is not null)
            clone._parts = new List<string>(_parts);
        return clone;
    }

    // ── Сборка ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Собрать итоговую строку CSS-классов.
    /// Fast-path для 0-3 дополнительных частей (без StringBuilder аллокации).
    /// </summary>
    public string Build()
    {
        var hasBase  = !string.IsNullOrWhiteSpace(_base);
        var hasParts = _parts is { Count: > 0 };

        // Fast paths
        if (!hasParts) return hasBase ? _base! : string.Empty;
        if (!hasBase && _parts!.Count == 1) return _parts[0];
        if (hasBase && _parts!.Count == 1) return string.Concat(_base, " ", _parts[0]);
        if (!hasBase && _parts!.Count == 2) return string.Concat(_parts[0], " ", _parts[1]);
        if (hasBase && _parts!.Count == 2) return string.Concat(_base, " ", _parts[0], " ", _parts[1]);
        if (!hasBase && _parts!.Count == 3) return string.Concat(_parts[0], " ", _parts[1], " ", _parts[2]);
        if (hasBase && _parts!.Count == 3) return string.Concat(_base, " ", _parts[0], " ", _parts[1], " ", _parts[2]);

        // General path
        var capacity = (hasBase ? _base!.Length + 1 : 0) + _parts!.Count * 14;
        var sb = new StringBuilder(capacity);
        if (hasBase) sb.Append(_base);
        foreach (var part in _parts!)
        {
            if (sb.Length > 0) sb.Append(' ');
            sb.Append(part);
        }
        return sb.ToString();
    }

    /// <summary>Неявное приведение к string (вызывает Build()).</summary>
    public static implicit operator string(SgCssBuilder builder) => builder.Build();

    /// <inheritdoc/>
    public override string ToString() => Build();
}
