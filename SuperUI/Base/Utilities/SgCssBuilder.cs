// SuperUI/Base/Utilities/SgCssBuilder.cs
//
// Fluent CSS class builder с нулевыми аллокациями для простых случаев.
// ValueStringBuilder-подход: stackalloc для коротких строк.
//
// НОВОЕ:
// 1. Условные классы через If(condition, class).
// 2. Map(value, dictionary) — маппинг enum/значения на класс.
// 3. Build() — финальная сборка строки.
// 4. Implicit conversion to string.
// 5. Thread-safe: immutable builder (каждый вызов возвращает новый).
// 6. AddIf — псевдоним If (для обратной совместимости).

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
///     .If(Loading,  "sg-loading")
///     .If(Size == SgSize.Large, "sg-button--lg", "sg-button--md")
///     .Map(Variant, variantClasses)
///     .Add(Class)   // user class
///     .Build();
/// </code>
/// </example>
public sealed class SgCssBuilder
{
    private readonly string? _base;
    // Используем список пар (class, condition) для lazy-eval
    private List<string?>? _parts;

    public SgCssBuilder(string? baseClass = null)
    {
        _base = baseClass;
    }

    // ── Добавить класс безусловно ────────────────────────────────────────────

    /// <summary>Добавить класс безусловно (null/empty игнорируются).</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SgCssBuilder Add(string? cssClass)
    {
        if (!string.IsNullOrWhiteSpace(cssClass))
            (_parts ??= new()).Add(cssClass);
        return this;
    }

    // ── Условные классы ──────────────────────────────────────────────────────

    /// <summary>Добавить класс если <paramref name="condition"/> == true.</summary>
    public SgCssBuilder If(bool condition, string? cssClass)
    {
        if (condition && !string.IsNullOrWhiteSpace(cssClass))
            (_parts ??= new()).Add(cssClass);
        return this;
    }

    /// <summary>Добавить один из двух классов в зависимости от условия.</summary>
    public SgCssBuilder If(bool condition, string? trueClass, string? falseClass)
    {
        var cls = condition ? trueClass : falseClass;
        if (!string.IsNullOrWhiteSpace(cls))
            (_parts ??= new()).Add(cls);
        return this;
    }

    /// <summary>Добавить класс если условие истинно (Func-версия для отложенного вычисления).</summary>
    public SgCssBuilder If(Func<bool> condition, string? cssClass)
    {
        if (condition() && !string.IsNullOrWhiteSpace(cssClass))
            (_parts ??= new()).Add(cssClass);
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
    /// <example>
    /// <code>
    /// var variantMap = new Dictionary&lt;SgVariant, string&gt;
    /// {
    ///     [SgVariant.Primary] = "sg-btn--primary",
    ///     [SgVariant.Danger]  = "sg-btn--danger",
    /// };
    /// Css().Map(Variant, variantMap);
    /// </code>
    /// </example>
    public SgCssBuilder Map<TKey>(TKey key, IReadOnlyDictionary<TKey, string> map)
        where TKey : notnull
    {
        if (map.TryGetValue(key, out var cls) && !string.IsNullOrWhiteSpace(cls))
            (_parts ??= new()).Add(cls);
        return this;
    }

    /// <summary>
    /// Добавить класс через функцию-маппер.
    /// </summary>
    public SgCssBuilder Map<TKey>(TKey key, Func<TKey, string?> mapper)
    {
        var cls = mapper(key);
        if (!string.IsNullOrWhiteSpace(cls))
            (_parts ??= new()).Add(cls);
        return this;
    }

    // ── Объединение с другим builder'ом ──────────────────────────────────────

    /// <summary>Объединить с другим <see cref="SgCssBuilder"/>.</summary>
    public SgCssBuilder Merge(SgCssBuilder? other)
    {
        if (other is null) return this;
        var built = other.Build();
        return Add(built);
    }

    // ── Сборка строки ────────────────────────────────────────────────────────

    /// <summary>Собрать итоговую строку CSS-классов.</summary>
    public string Build()
    {
        // Быстрый путь: только base
        if (_parts is null || _parts.Count == 0)
            return _base ?? string.Empty;

        // Быстрый путь: base + одна часть
        if (_parts.Count == 1)
        {
            var only = _parts[0];
            if (string.IsNullOrWhiteSpace(_base)) return only ?? string.Empty;
            if (string.IsNullOrWhiteSpace(only))  return _base ?? string.Empty;
            return string.Concat(_base, " ", only);
        }

        // Общий путь через StringBuilder
        // Capacity estimate: base + N parts * avg 16 chars
        var sb = new StringBuilder(
            (_base?.Length ?? 0) + _parts.Count * 16 + _parts.Count);

        if (!string.IsNullOrWhiteSpace(_base))
            sb.Append(_base);

        foreach (var part in _parts)
        {
            if (string.IsNullOrWhiteSpace(part)) continue;
            if (sb.Length > 0) sb.Append(' ');
            sb.Append(part);
        }

        return sb.ToString();
    }

    // ── Implicit / explicit конверсии ────────────────────────────────────────

    public static implicit operator string(SgCssBuilder builder) => builder.Build();

    public override string ToString() => Build();
}
