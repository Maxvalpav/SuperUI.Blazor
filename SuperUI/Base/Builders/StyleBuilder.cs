// SuperUI/Base/Builders/StyleBuilder.cs
// Fluent-построитель inline CSS-стилей.
// Заменяет StringBuilder-блоки в SgStack.razor.cs (ComputedStyle).

using System.Text;

namespace SuperUI.Base.Builders;

/// <summary>
/// Fluent-построитель строки inline CSS-стилей.
/// </summary>
/// <remarks>
/// <para>Пример:</para>
/// <code>
/// string style = StyleBuilder.Default(Style)
///     .AddStyle("display", Inline ? "inline-flex" : "flex")
///     .AddStyle("flex-direction", FlexDirectionCss)
///     .AddStyle("width", "100%", FullWidth)
///     .AddStyle("height", Height, !string.IsNullOrEmpty(Height))
///     .Build();
/// </code>
/// </remarks>
public readonly struct StyleBuilder
{
    private readonly string? _value;

    private StyleBuilder(string? value) => _value = value;

    // ── Статические конструкторы ──────────────────────────────────────────────

    /// <summary>
    /// Создаёт построитель с начальными стилями.
    /// </summary>
    /// <param name="rawDeclarations">
    /// Существующая строка стилей (например, параметр <c>Style</c> компонента).
    /// </param>
    public static StyleBuilder Default(string? rawDeclarations = null)
        => new(Normalize(rawDeclarations));

    /// <summary>Создаёт пустой построитель.</summary>
    public static StyleBuilder Empty() => new(null);

    // ── AddStyle overloads ────────────────────────────────────────────────────

    /// <summary>Добавляет CSS-свойство безусловно.</summary>
    /// <param name="property">Имя свойства (e.g. <c>"display"</c>).</param>
    /// <param name="value">Значение (e.g. <c>"flex"</c>).</param>
    public StyleBuilder AddStyle(string property, string? value)
    {
        if (string.IsNullOrWhiteSpace(property) || string.IsNullOrWhiteSpace(value))
            return this;
        return new StyleBuilder(Append(_value, $"{property.Trim()}:{value.Trim()}"));
    }

    /// <summary>Добавляет CSS-свойство при выполнении условия.</summary>
    /// <param name="property">Имя свойства.</param>
    /// <param name="value">Значение.</param>
    /// <param name="when"><c>true</c> — свойство добавляется.</param>
    public StyleBuilder AddStyle(string property, string? value, bool when)
        => when ? AddStyle(property, value) : this;

    /// <summary>Добавляет CSS-свойство при выполнении условия (lazy value).</summary>
    /// <param name="property">Имя свойства.</param>
    /// <param name="valueFactory">Фабрика значения (вызывается только если <paramref name="when"/> = true).</param>
    /// <param name="when"><c>true</c> — свойство добавляется.</param>
    public StyleBuilder AddStyle(string property, Func<string?> valueFactory, bool when)
        => when ? AddStyle(property, valueFactory()) : this;

    /// <summary>
    /// Добавляет готовую строку деклараций (e.g. <c>"color:red;font-size:14px"</c>).
    /// </summary>
    /// <param name="rawDeclarations">Строка CSS-деклараций.</param>
    public StyleBuilder AddStyle(string? rawDeclarations)
    {
        if (string.IsNullOrWhiteSpace(rawDeclarations)) return this;
        return new StyleBuilder(Append(_value, rawDeclarations.Trim().TrimEnd(';')));
    }

    /// <summary>
    /// Извлекает атрибут <c>style</c> из <paramref name="attributes"/> и добавляет его.
    /// </summary>
    public StyleBuilder AddStyleFromAttributes(
        IReadOnlyDictionary<string, object>? attributes)
    {
        if (attributes is null) return this;
        if (!attributes.TryGetValue("style", out var raw)) return this;
        return AddStyle(raw?.ToString());
    }

    // ── Build ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Возвращает строку inline-стилей.
    /// Каждое свойство завершается <c>;</c>.
    /// </summary>
    public string Build()
    {
        if (string.IsNullOrWhiteSpace(_value)) return string.Empty;
        // Гарантируем финальную точку с запятой.
        return _value.EndsWith(';') ? _value : _value + ";";
    }

    /// <summary>
    /// Возвращает строку или <c>null</c>, если стилей нет.
    /// </summary>
    public string? NullIfEmpty()
        => string.IsNullOrWhiteSpace(_value) ? null : Build();

    /// <summary>Неявное преобразование к <see cref="string"/>.</summary>
    public static implicit operator string(StyleBuilder builder) => builder.Build();

    /// <inheritdoc/>
    public override string ToString() => Build();

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string? Normalize(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        return raw.Trim().TrimEnd(';');
    }

    private static string Append(string? existing, string toAdd)
        => string.IsNullOrWhiteSpace(existing) ? toAdd : $"{existing};{toAdd}";
}