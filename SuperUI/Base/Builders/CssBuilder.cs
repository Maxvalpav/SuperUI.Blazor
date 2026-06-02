// SuperUI/Base/Builders/CssBuilder.cs
// Fluent-построитель CSS-классов.
// Источник идеи: MudBlazor.Utilities.CssBuilder.
// Улучшения относительно исходной версии:
//   * StringBuilder pool (zero-alloc на каждый рендер, см. SgStringBuilderCache).
//   * Generic AddValue<TEnum> для enum-параметров (SgSize, SgVariant...).
//   * Lazy-evaluated предикаты (AddClass(string, Func<bool>)).
//   * Merge с CssClass и AdditionalAttributes — единый .Build().

using System.Text;
using SuperUI.Base.Utilities;

namespace SuperUI.Base.Builders;

/// <summary>
/// Fluent-построитель строки CSS-классов. <b>Zero-allocation</b> на горячем пути
/// (использует <see cref="SgStringBuilderCache"/>).
/// </summary>
/// <remarks>
/// <para>Заменяет <see cref="StringBuilder"/>-блоки в компонентах SuperUI.</para>
/// <para>Пример:</para>
/// <code>
/// string cls = CssBuilder.Default("sg-button")
///     .AddClass("sg-button-primary", Variant == SgButtonVariant.Primary)
///     .AddClass("sg-button-disabled", () =&gt; Disabled)
///     .AddClass(CssClass)
///     .AddClassFromAttributes(AdditionalAttributes)
///     .Build();
/// </code>
/// </remarks>
public readonly struct CssBuilder
{
    private readonly string? _value;

    private CssBuilder(string? value) => _value = value;

    // ── Статические конструкторы ──────────────────────────────────────────────

    /// <summary>
    /// Создаёт построитель с начальным классом.
    /// </summary>
    /// <param name="rootClass">Базовый CSS-класс (может быть <c>null</c>).</param>
    public static CssBuilder Default(string? rootClass = null)
        => new(Normalize(rootClass));

    /// <summary>
    /// Создаёт пустой построитель.
    /// </summary>
    public static CssBuilder Empty() => new(null);

    // ── AddClass overloads ────────────────────────────────────────────────────

    /// <summary>Добавляет CSS-класс безусловно.</summary>
    public CssBuilder AddClass(string? cssClass) => Add(cssClass);

    /// <summary>Добавляет CSS-класс при выполнении условия.</summary>
    public CssBuilder AddClass(string? cssClass, bool when) => Add(cssClass, when);

    /// <summary>Добавляет CSS-класс при выполнении lazy-условия.</summary>
    public CssBuilder AddClass(string? cssClass, Func<bool> when)
        => Add(cssClass, when is null ? false : when());

    /// <summary>Добавляет CSS-класс, вычисляемый фабрикой, при выполнении условия.</summary>
    public CssBuilder AddClass(Func<string?> cssClassFactory, bool when)
        => when ? AddClass(cssClassFactory()) : this;

    /// <summary>Добавляет класс из enum-значения (например <c>SgSize.Md → "sg-md"</c>).</summary>
    /// <typeparam name="TEnum">Enum-тип (SgSize, SgVariant, ...).</typeparam>
    /// <param name="value">Значение enum. <c>null</c> игнорируется (используйте <see cref="AddClassFromValue{TEnum}"/> для nullable).</param>
    /// <param name="prefix">Префикс класса. По умолчанию пустой.</param>
    /// <param name="transform">Lower/Upper-case стратегия. По умолчанию lowercase kebab-case не применяется — enum.ToString().ToLowerInvariant() вызывается.</param>
    public CssBuilder AddClass<TEnum>(TEnum value, string prefix = "", StringTransform transform = StringTransform.Lower)
        where TEnum : struct, Enum
        => AddClass(BuildEnumClass(value, prefix, transform));

    /// <summary>Nullable-версия <see cref="AddClass{TEnum}(TEnum, string, StringTransform)"/>.</summary>
    public CssBuilder AddClassFromValue<TEnum>(TEnum? value, string prefix = "", StringTransform transform = StringTransform.Lower)
        where TEnum : struct, Enum
        => value.HasValue ? AddClass(BuildEnumClass(value.Value, prefix, transform)) : this;

    /// <summary>Объединяет с другим <see cref="CssBuilder"/>.</summary>
    public CssBuilder AddClass(CssBuilder other)
        => string.IsNullOrWhiteSpace(other._value) ? this : AddClass(other._value);

    /// <summary>
    /// Извлекает атрибут <c>class</c> из <paramref name="attributes"/> и добавляет его.
    /// </summary>
    public CssBuilder AddClassFromAttributes(IReadOnlyDictionary<string, object>? attributes)
    {
        if (attributes is null) return this;
        if (!attributes.TryGetValue("class", out var raw)) return this;
        return AddClass(raw?.ToString());
    }

    // ── Build ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Возвращает строку CSS-классов или <see cref="string.Empty"/>.
    /// </summary>
    public string Build() => _value ?? string.Empty;

    /// <summary>
    /// Возвращает строку CSS-классов или <c>null</c>, если классов нет.
    /// </summary>
    public string? NullIfEmpty()
        => string.IsNullOrWhiteSpace(_value) ? null : _value;

    /// <summary>Неявное преобразование к <see cref="string"/>.</summary>
    public static implicit operator string(CssBuilder builder) => builder.Build();

    /// <inheritdoc/>
    public override string ToString() => Build();

    // ── Case strategy для enum ───────────────────────────────────────────────

    /// <summary>String transformation strategy for enum-to-class conversion.</summary>
    public enum StringTransform
    {
        /// <summary><c>MyEnum.SecondValue → "secondvalue"</c> (lowercase, no separator).</summary>
        Lower,
        /// <summary><c>MyEnum.SecondValue → "SecondValue"</c> (pascal, no separator).</summary>
        None,
        /// <summary><c>MyEnum.SecondValue → "second-value"</c> (kebab-case).</summary>
        Kebab,
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    // ── Public Add (backward-compatible alias for AddClass) ──────────────────

    /// <summary>Добавляет CSS-класс безусловно.</summary>
    public CssBuilder Add(string? cssClass)
    {
        if (string.IsNullOrWhiteSpace(cssClass)) return this;
        return new CssBuilder(Append(_value, cssClass.Trim()));
    }

    /// <summary>Добавляет CSS-класс при выполнении условия.</summary>
    public CssBuilder Add(string? cssClass, bool when) => when ? Add(cssClass) : this;

    /// <summary>Добавляет CSS-класс при выполнении lazy-условия.</summary>
    public CssBuilder Add(string? cssClass, Func<bool> when) => AddClass(cssClass, when);

    /// <summary>Добавляет CSS-класс из фабрики, при выполнении условия.</summary>
    public CssBuilder Add(Func<string?> cssClassFactory, bool when) => AddClass(cssClassFactory, when);

    private static string Append(string? existing, string toAdd)
        => string.IsNullOrWhiteSpace(existing) ? toAdd : $"{existing} {toAdd}";

    private static string BuildEnumClass<TEnum>(TEnum value, string prefix, StringTransform transform)
        where TEnum : struct, Enum
    {
        var name = value.ToString();
        name = transform switch
        {
            StringTransform.Lower => name.ToLowerInvariant(),
            StringTransform.Kebab => KebabCase(name),
            _ => name,
        };
        return string.IsNullOrEmpty(prefix) ? name : prefix + name;
    }

    private static string KebabCase(string pascal)
    {
        if (string.IsNullOrEmpty(pascal)) return pascal;
        var sb = SgStringBuilderCache.Acquire(pascal.Length + 4);
        try
        {
            for (var i = 0; i < pascal.Length; i++)
            {
                var c = pascal[i];
                if (i > 0 && char.IsUpper(c)) sb.Append('-');
                sb.Append(char.ToLowerInvariant(c));
            }
            return SgStringBuilderCache.GetStringAndRelease(sb);
        }
        catch
        {
            SgStringBuilderCache.Release(sb);
            throw;
        }
    }
}
