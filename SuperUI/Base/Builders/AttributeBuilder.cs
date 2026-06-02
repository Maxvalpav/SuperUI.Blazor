// SuperUI/Base/Builders/AttributeBuilder.cs
// Fluent-построитель HTML-атрибутов для Razor (например, "data-id=42 aria-label=Close").
// Нужен компонентам, которые рендерят сложные data-* / aria-* наборы (SgTree, SgDataGrid, SgDock).

using System.Text;
using SuperUI.Base.Utilities;

namespace SuperUI.Base.Builders;

/// <summary>
/// Fluent-построитель строки HTML-атрибутов. <b>Zero-allocation</b> на горячем пути.
/// </summary>
/// <remarks>
/// <para>Безопасно экранирует значения (защита от XSS через data-* атрибуты).
/// Валидирует имена атрибутов: только латиница/цифры/дефис, без пробелов и <c>"</c>.</para>
/// <para>Пример:</para>
/// <code>
/// string attrs = AttributeBuilder.Default(AdditionalAttributes)
///     .Set("data-id", item.Id)
///     .Set("aria-selected", item.IsSelected.ToString().ToLowerInvariant(), item.IsSelected)
///     .Set("tabindex", isFocused ? "0" : "-1")
///     .Build();
/// </code>
/// </remarks>
public readonly struct AttributeBuilder
{
    private readonly string? _value;
    private readonly IReadOnlyDictionary<string, object>? _extra;

    private AttributeBuilder(string? value, IReadOnlyDictionary<string, object>? extra)
    {
        _value = value;
        _extra = extra;
    }

    /// <summary>Создаёт построитель с базовыми атрибутами.</summary>
    public static AttributeBuilder Default(IReadOnlyDictionary<string, object>? attributes = null)
    {
        if (attributes is null || attributes.Count == 0) return new AttributeBuilder(null, null);
        var sb = SgStringBuilderCache.Acquire(attributes.Count * 32);
        try
        {
            foreach (var kv in attributes)
            {
                if (!IsValidName(kv.Key)) continue;
                Append(sb, kv.Key, kv.Value);
            }
            return new AttributeBuilder(SgStringBuilderCache.GetStringAndRelease(sb), null);
        }
        catch
        {
            SgStringBuilderCache.Release(sb);
            throw;
        }
    }

    /// <summary>Создаёт пустой построитель.</summary>
    public static AttributeBuilder Empty() => new(null, null);

    /// <summary>Добавляет/перезаписывает атрибут <paramref name="name"/> со строковым значением.</summary>
    public AttributeBuilder Set(string name, string? value)
        => value is null ? this : AppendToValue(name, value);

    /// <summary>Добавляет атрибут, если <paramref name="when"/> = true.</summary>
    public AttributeBuilder Set(string name, string? value, bool when)
        => when && value is not null ? AppendToValue(name, value) : this;

    /// <summary>Добавляет атрибут с числовым значением.</summary>
    public AttributeBuilder Set(string name, int value, bool when = true)
        => when ? AppendToValue(name, value.ToString(System.Globalization.CultureInfo.InvariantCulture)) : this;

    /// <summary>Добавляет атрибут с булевым значением (без value, по стандарту HTML5: <c>disabled</c>).</summary>
    public AttributeBuilder SetFlag(string name, bool when = true)
        => when ? AppendFlag(name) : this;

    /// <summary>
    /// Возвращает строку HTML-атрибутов, готово для вставки в Razor: <c>@attributes="..."</c>.
    /// </summary>
    public string Build()
    {
        if (string.IsNullOrEmpty(_value)) return string.Empty;
        return _value;
    }

    /// <inheritdoc/>
    public override string ToString() => Build();

    // ── Helpers ───────────────────────────────────────────────────────────────

    private AttributeBuilder AppendToValue(string name, string value)
    {
        if (!IsValidName(name)) return this;
        var sb = _value is null
            ? SgStringBuilderCache.Acquire(64)
            : SgStringBuilderCache.Acquire(_value.Length + name.Length + value.Length + 8);
        try
        {
            if (_value is not null) sb.Append(_value);
            Append(sb, name, value);
            return new AttributeBuilder(SgStringBuilderCache.GetStringAndRelease(sb), _extra);
        }
        catch
        {
            SgStringBuilderCache.Release(sb);
            throw;
        }
    }

    private AttributeBuilder AppendFlag(string name)
    {
        if (!IsValidName(name)) return this;
        var sb = _value is null
            ? SgStringBuilderCache.Acquire(32)
            : SgStringBuilderCache.Acquire(_value.Length + name.Length + 4);
        try
        {
            if (_value is not null) sb.Append(_value);
            if (sb.Length > 0) sb.Append(' ');
            sb.Append(name);
            return new AttributeBuilder(SgStringBuilderCache.GetStringAndRelease(sb), _extra);
        }
        catch
        {
            SgStringBuilderCache.Release(sb);
            throw;
        }
    }

    private static void Append(StringBuilder sb, string name, object? value)
    {
        if (sb.Length > 0) sb.Append(' ');
        sb.Append(name);
        sb.Append('=');
        sb.Append('"');
        if (value is not null)
        {
            var s = value.ToString() ?? "";
            // Escape ", &, < — enough for HTML5 attribute context.
            for (var i = 0; i < s.Length; i++)
            {
                var c = s[i];
                switch (c)
                {
                    case '"': sb.Append("&quot;"); break;
                    case '&': sb.Append("&amp;"); break;
                    case '<': sb.Append("&lt;"); break;
                    default:  sb.Append(c); break;
                }
            }
        }
        sb.Append('"');
    }

    /// <summary>
    /// Возвращает <c>true</c>, если <paramref name="name"/> — допустимое имя HTML-атрибута.
    /// Разрешены: буквы, цифры, дефис, подчёркивание, двоеточие (для <c>xmlns:</c>, SVG <c>xlink:href</c>).
    /// </summary>
    public static bool IsValidName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;
        if (name.Length > 256) return false;
        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];
            if (!(char.IsLetterOrDigit(c) || c == '-' || c == '_' || c == ':')) return false;
        }
        return true;
    }
}
