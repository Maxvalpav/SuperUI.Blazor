// SuperUI/Base/Builders/CssBuilder.cs
// Fluent-построитель CSS-классов.
// Источник идеи: MudBlazor.Utilities.CssBuilder.
// Улучшение: struct (zero-alloc для 1-3 классов) + мердж class из AdditionalAttributes.

using System.Text;

namespace SuperUI.Base.Builders;

/// <summary>
/// Fluent-построитель строки CSS-классов.
/// </summary>
/// <remarks>
/// <para>Заменяет <see cref="StringBuilder"/>-блоки в компонентах SuperUI.</para>
/// <para>Пример:</para>
/// <code>
/// string cls = CssBuilder.Default("sg-button")
///     .AddClass("sg-button-primary", Variant == SgButtonVariant.Primary)
///     .AddClass("sg-button-disabled", () => Disabled)
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
    /// <param name="cssClass">Класс (пустые/null игнорируются).</param>
    public CssBuilder Add(string? cssClass)
        => string.IsNullOrWhiteSpace(cssClass)
            ? this
            : new CssBuilder(Append(_value, cssClass.Trim()));

    /// <summary>Добавляет CSS-класс при выполнении условия.</summary>
    /// <param name="cssClass">Класс.</param>
    /// <param name="when"><c>true</c> — класс добавляется.</param>
    public CssBuilder Add(string? cssClass, bool when)
        => when ? Add(cssClass) : this;

    /// <summary>Добавляет CSS-класс безусловно.</summary>
    /// <param name="cssClass">Класс (пустые/null игнорируются).</param>
    public CssBuilder AddClass(string? cssClass) => Add(cssClass);

    /// <summary>Добавляет CSS-класс при выполнении условия.</summary>
    /// <param name="cssClass">Класс.</param>
    /// <param name="when"><c>true</c> — класс добавляется.</param>
    public CssBuilder AddClass(string? cssClass, bool when) => Add(cssClass, when);

    /// <summary>Добавляет CSS-класс при выполнении условия (lazy evaluation).</summary>
    /// <param name="cssClass">Класс.</param>
    /// <param name="when">Функция-предикат.</param>
    public CssBuilder AddClass(string? cssClass, Func<bool> when)
        => AddClass(cssClass, when());

    /// <summary>Добавляет CSS-класс, вычисляемый функцией, при выполнении условия.</summary>
    /// <param name="cssClassFactory">Фабрика класса.</param>
    /// <param name="when"><c>true</c> — фабрика вызывается и класс добавляется.</param>
    public CssBuilder AddClass(Func<string?> cssClassFactory, bool when)
        => when ? AddClass(cssClassFactory()) : this;

    /// <summary>Объединяет с другим <see cref="CssBuilder"/>.</summary>
    /// <param name="other">Другой построитель.</param>
    public CssBuilder AddClass(CssBuilder other)
        => string.IsNullOrWhiteSpace(other._value)
            ? this
            : AddClass(other._value);

    /// <summary>
    /// Извлекает атрибут <c>class</c> из <paramref name="attributes"/> и добавляет его.
    /// </summary>
    /// <param name="attributes">
    /// Словарь доп. атрибутов (обычно <c>AdditionalAttributes</c> компонента).
    /// </param>
    public CssBuilder AddClassFromAttributes(
        IReadOnlyDictionary<string, object>? attributes)
    {
        if (attributes is null) return this;
        if (!attributes.TryGetValue("class", out var raw)) return this;
        return AddClass(raw?.ToString());
    }

    // ── Build ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Возвращает строку CSS-классов.
    /// Возвращает <see cref="string.Empty"/>, если классов нет.
    /// </summary>
    public string Build() => _value ?? string.Empty;

    /// <summary>
    /// Возвращает строку CSS-классов или <c>null</c>, если классов нет.
    /// Удобно для атрибутов Razor: <c>class="@Css().NullIfEmpty()"</c>.
    /// </summary>
    public string? NullIfEmpty()
        => string.IsNullOrWhiteSpace(_value) ? null : _value;

    /// <summary>Неявное преобразование к <see cref="string"/>.</summary>
    public static implicit operator string(CssBuilder builder) => builder.Build();

    /// <inheritdoc/>
    public override string ToString() => Build();

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string Append(string? existing, string toAdd)
        => string.IsNullOrWhiteSpace(existing) ? toAdd : $"{existing} {toAdd}";
}