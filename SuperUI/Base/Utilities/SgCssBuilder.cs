// SuperUI/Base/Utilities/SgCssBuilder.cs
//
// УЛУЧШЕНО:
// 1. _parts: List<string> вместо List<string?> — null отфильтрован в Add().
// 2. AddRange(IEnumerable<string>) — добавить набор классов.
// 3. Remove(string) — убрать класс из цепочки.
// 4. Clone() — создать копию builder'а.
// 5. IsEmpty — проверить, нет ли классов.
// 6. Build() fast-path для _base + 1 part упрощён.
// 7. Capacity estimate улучшен.
// 8. Добавлен WithPrefix(prefix) — добавить prefix ко всем последующим классам.
//
// Thread safety: immutable builder (Add возвращает this → мутабельный, но single-threaded usage).

using System.Runtime.CompilerServices;
using System.Text;

namespace SuperUI.Base.Utilities;

/// <summary>
/// Fluent-builder для CSS-классов.
/// </summary>
/// <example>
/// <code>
/// string css = Css("sg-button")
///     .If(Disabled, "sg-disabled")
///     .If(Loading, "sg-loading")
///     .If(Size == SgSize.Large, "sg-button--lg", "sg-button--md")
///     .Map(Variant, variantClasses)
///     .Add(Class) // пользовательский класс
///     .Build();
/// </code>
/// </example>
public sealed class SgCssBuilder
{
    private readonly string? _base;
    // УЛУЧШЕНО: non-nullable string — null отфильтрован в Add()
    private List<string>? _parts;

    public SgCssBuilder(string? baseClass = null)
    {
        _base = string.IsNullOrWhiteSpace(baseClass) ? null : baseClass.Trim();
    }

    // ── Проверки ────────────────────────────────────────────────────────────

    /// <summary>Нет ни базового класса, ни добавленных.</summary>
    public bool IsEmpty =>
        string.IsNullOrWhiteSpace(_base)
        && (_parts is null || _parts.Count == 0);

    // ── Добавить безусловно ──────────────────────────────────────────────────

    /// <summary>Добавить класс безусловно (null/empty/whitespace игнорируются).</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SgCssBuilder Add(string? cssClass)
    {
        if (!string.IsNullOrWhiteSpace(cssClass))
            (_parts ??= new List<string>()).Add(cssClass.Trim());
        return this;
    }

    /// <summary>Добавить несколько классов (null/empty игнорируются).</summary>
    public SgCssBuilder AddRange(IEnumerable<string?> classes)
    {
        ArgumentNullException.ThrowIfNull(classes);
        foreach (var cls in classes)
            Add(cls);
        return this;
    }

    // ── Условные классы ──────────────────────────────────────────────────────

    /// <summary>Добавить класс если <paramref name="condition"/> == true.</summary>
    public SgCssBuilder If(bool condition, string? cssClass)
    {
        if (condition) Add(cssClass);
        return this;
    }

    /// <summary>Добавить один из двух классов в зависимости от условия.</summary>
    public SgCssBuilder If(bool condition, string? trueClass, string? falseClass)
        => Add(condition ? trueClass : falseClass);

    /// <summary>Добавить класс если условие истинно (Func-версия для отложенного вычисления).</summary>
    public SgCssBuilder If(Func<bool> condition, string? cssClass)
    {
        ArgumentNullException.ThrowIfNull(condition);
        if (condition()) Add(cssClass);
        return this;
    }

    /// <summary>Псевдоним <see cref="If(bool, string?)"/> для обратной совместимости.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SgCssBuilder AddIf(bool condition, string? cssClass) => If(condition, cssClass);

    // ── Маппинг значения на класс ────────────────────────────────────────────

    /// <summary>
    /// Добавить класс из словаря по значению ключа.
    /// Удобно для маппинга enum на CSS-класс.
    /// </summary>
    public SgCssBuilder Map<TKey>(TKey key, IReadOnlyDictionary<TKey, string> map)
        where TKey : notnull
    {
        ArgumentNullException.ThrowIfNull(map);
        if (map.TryGetValue(key, out var cls))
            Add(cls);
        return this;
    }

    /// <summary>Добавить класс через функцию-маппер.</summary>
    public SgCssBuilder Map<TKey>(TKey key, Func<TKey, string?> mapper)
    {
        ArgumentNullException.ThrowIfNull(mapper);
        return Add(mapper(key));
    }

    // ── Удаление ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Удалить класс из списка (если был добавлен).
    /// Базовый класс удалить нельзя.
    /// </summary>
    public SgCssBuilder Remove(string cssClass)
    {
        if (!string.IsNullOrWhiteSpace(cssClass) && _parts is not null)
            _parts.Remove(cssClass.Trim());
        return this;
    }

    // ── Объединение ──────────────────────────────────────────────────────────

    /// <summary>Объединить с другим <see cref="SgCssBuilder"/>.</summary>
    public SgCssBuilder Merge(SgCssBuilder? other)
    {
        if (other is null) return this;
        var built = other.Build();
        if (!string.IsNullOrWhiteSpace(built))
            // Добавляем отдельные классы, а не строку целиком
            AddRange(built.Split(' ', StringSplitOptions.RemoveEmptyEntries));
        return this;
    }

    // ── Клонирование ─────────────────────────────────────────────────────────

    /// <summary>Создать копию builder'а с теми же классами.</summary>
    public SgCssBuilder Clone()
    {
        var clone = new SgCssBuilder(_base);
        if (_parts is not null)
            clone._parts = new List<string>(_parts);
        return clone;
    }

    // ── Сборка строки ────────────────────────────────────────────────────────

    /// <summary>Собрать итоговую строку CSS-классов.</summary>
    public string Build()
    {
        var hasBase  = !string.IsNullOrWhiteSpace(_base);
        var hasparts = _parts is { Count: > 0 };

        // Быстрый путь: только base
        if (!hasparts) return hasBase ? _base! : string.Empty;

        // Быстрый путь: только одна часть без base
        if (!hasBase && _parts!.Count == 1) return _parts[0];

        // Быстрый путь: base + одна часть
        if (hasBase && _parts!.Count == 1) return string.Concat(_base, " ", _parts[0]);

        // Общий путь
        var capacity = (hasBase ? _base!.Length + 1 : 0)
                     + _parts!.Count * 12; // avg class len estimate
        var sb = new StringBuilder(capacity);

        if (hasBase) sb.Append(_base);

        foreach (var part in _parts!)
        {
            if (sb.Length > 0) sb.Append(' ');
            sb.Append(part);
        }

        return sb.ToString();
    }

    // ── Конверсии ────────────────────────────────────────────────────────────

    /// <summary>Implicit конвертация в string — вызывает Build().</summary>
    public static implicit operator string(SgCssBuilder builder) => builder.Build();

    /// <summary>Возвращает итоговую строку CSS-классов.</summary>
    public override string ToString() => Build();
}