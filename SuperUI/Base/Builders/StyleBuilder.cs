// SuperUI/Base/Builders/StyleBuilder.cs
// Fluent-построитель inline CSS-стилей. Zero-allocation горячий путь через
// SgStringBuilderCache. Валидация CSS-property имён (anti-injection).
// Enum-значения: см. AddStyle<TEnum>(value, prefix, ...).

using System.Text;
using SuperUI.Base.Utilities;

namespace SuperUI.Base.Builders;

/// <summary>
/// Fluent-построитель строки inline CSS-стилей. <b>Zero-allocation</b> на горячем пути.
/// </summary>
/// <remarks>
/// <para>Заменяет <c>List&lt;string&gt; parts</c> + <c>string.Join(";", parts)</c> блоки
/// в SgStack/SgSpace/SgResponsiveContainer/SgModal и т.д.</para>
/// <para><b>Безопасность:</b> <see cref="AddStyle(string, string?)"/> валидирует
/// имя CSS-свойства (<see cref="IsValidPropertyName"/>) — это защищает от
/// инъекций вида <c>style="color:red;background:url(javascript:...)"</c>
/// когда значение параметра приходит от пользователя.</para>
/// <para>Пример:</para>
/// <code>
/// string style = StyleBuilder.Default(Style)
///     .AddStyle("display", Inline ? "inline-flex" : "flex")
///     .AddStyle("width", "100%", FullWidth)
///     .AddStyle("z-index", ZIndexValue.ToString(), ZIndexValue &gt; 0)
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
    public static StyleBuilder Default(string? rawDeclarations = null)
        => new(Normalize(rawDeclarations));

    /// <summary>Создаёт пустой построитель.</summary>
    public static StyleBuilder Empty() => new(null);

    // ── AddStyle overloads ────────────────────────────────────────────────────

    /// <summary>Добавляет CSS-свойство безусловно. Имя свойства валидируется.</summary>
    public StyleBuilder AddStyle(string property, string? value) => Add(property, value);

    /// <summary>Добавляет CSS-свойство при выполнении условия.</summary>
    public StyleBuilder AddStyle(string property, string? value, bool when) => Add(property, value, when);

    /// <summary>Добавляет CSS-свойство с lazy-значением.</summary>
    public StyleBuilder AddStyle(string property, Func<string?> valueFactory, bool when)
        => when ? AddStyle(property, valueFactory()) : this;

    /// <summary>Добавляет CSS-свойство из enum-значения (например, <c>Size → "size-md"</c>).</summary>
    public StyleBuilder AddStyleFromEnum<TEnum>(TEnum value, string propertyPrefix = "--sg-", string suffix = "")
        where TEnum : struct, Enum
        => AddStyle(propertyPrefix + value.ToString().ToLowerInvariant() + suffix, "");

    /// <summary>
    /// Добавляет готовую строку деклараций (e.g. <c>"color:red;font-size:14px"</c>).
    /// </summary>
    public StyleBuilder AddStyle(string? rawDeclarations)
    {
        if (string.IsNullOrWhiteSpace(rawDeclarations)) return this;
        return new StyleBuilder(Append(_value, rawDeclarations.Trim().TrimEnd(';')));
    }

    /// <summary>
    /// Извлекает атрибут <c>style</c> из <paramref name="attributes"/> и добавляет его.
    /// </summary>
    public StyleBuilder AddStyleFromAttributes(IReadOnlyDictionary<string, object>? attributes)
    {
        if (attributes is null) return this;
        if (!attributes.TryGetValue("style", out var raw)) return this;
        return AddStyle(raw?.ToString());
    }

    // ── Build ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Возвращает строку inline-стилей. Каждое свойство завершается <c>;</c>.
    /// </summary>
    public string Build()
    {
        if (string.IsNullOrWhiteSpace(_value)) return string.Empty;
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

    // ── Public Add (backward-compatible alias for AddStyle) ─────────────────

    /// <summary>Добавляет CSS-свойство безусловно. Имя свойства валидируется.</summary>
    public StyleBuilder Add(string property, string? value)
    {
        if (string.IsNullOrWhiteSpace(property) || string.IsNullOrWhiteSpace(value))
            return this;
        if (!IsValidPropertyName(property))
        {
            throw new ArgumentException(
                $"Invalid CSS property name: '{property}'. Names must start with a letter or '--' (custom property).",
                nameof(property));
        }
        return new StyleBuilder(Append(_value, $"{property.Trim()}:{value.Trim()}"));
    }

    /// <summary>Добавляет CSS-свойство при выполнении условия.</summary>
    public StyleBuilder Add(string property, string? value, bool when) => when ? Add(property, value) : this;

    /// <summary>Добавляет CSS-свойство из lazy-значения.</summary>
    public StyleBuilder Add(string property, Func<string?> valueFactory, bool when)
        => AddStyle(property, valueFactory, when);

    private static string Append(string? existing, string toAdd)
        => string.IsNullOrWhiteSpace(existing) ? toAdd : $"{existing};{toAdd}";

    /// <summary>
    /// Возвращает <c>true</c>, если <paramref name="name"/> похоже на валидное имя CSS-свойства:
    /// латиница/цифры/дефис, не начинается с цифры, не содержит <c>;</c>, <c>{}</c> и т.п.
    /// </summary>
    public static bool IsValidPropertyName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;
        if (name.Length > 128) return false;
        var span = name.AsSpan();

        // Custom property: "--foo-bar".
        if (span.Length > 2 && span[0] == '-' && span[1] == '-')
        {
            for (var i = 2; i < span.Length; i++)
            {
                var c = span[i];
                if (!(char.IsLetterOrDigit(c) || c == '-' || c == '_')) return false;
            }
            return true;
        }

        // Standard property: "color", "background-color", "-webkit-transform".
        if (!char.IsLetter(span[0]) && span[0] != '-') return false;
        for (var i = 0; i < span.Length; i++)
        {
            var c = span[i];
            if (!(char.IsLetterOrDigit(c) || c == '-' || c == '_')) return false;
        }
        return true;
    }
}
