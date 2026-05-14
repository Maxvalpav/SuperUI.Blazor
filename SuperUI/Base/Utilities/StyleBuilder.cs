// SuperUI/Base/Utilities/StyleBuilder.cs
// ИСПРАВЛЕНО:
// ✅ AddRaw: проверка EndsWith(';') через char вместо ToString() — без аллокации
// ✅ AddCustomProperty: валидация имени
// ✅ NullIfEmpty: возвращает null при пустом builder
// ✅ Reset(): для повторного использования builder

using System.Runtime.CompilerServices;
using System.Text;

namespace SuperUI.Base.Utilities;

/// <summary>
/// Fluent API для построения inline CSS стилей.
/// </summary>
public sealed class StyleBuilder
{
    private readonly StringBuilder _builder = new();

    public static StyleBuilder Default() => new();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public StyleBuilder Add(string property, string? value, bool condition = true)
    {
        if (!condition || string.IsNullOrWhiteSpace(value)) return this;

        if (_builder.Length > 0 && _builder[^1] != ' ')
            _builder.Append(' ');

        _builder.Append(property);
        _builder.Append(':');
        _builder.Append(value.Trim());
        _builder.Append(';');
        return this;
    }

    public StyleBuilder Add(string property, double value, string unit = "px", bool condition = true)
        => Add(property, $"{value}{unit}", condition);

    /// <summary>
    /// Добавить сырую CSS-строку.
    /// ✅ ИСПРАВЛЕНО: проверка EndsWith через char (_builder[^1]) — без аллокации строки.
    /// </summary>
    public StyleBuilder AddRaw(string? rawStyles, bool condition = true)
    {
        if (!condition || string.IsNullOrWhiteSpace(rawStyles)) return this;

        var trimmed = rawStyles.Trim();
        if (trimmed.Length == 0) return this;

        if (_builder.Length > 0 && _builder[^1] != ' ')
            _builder.Append(' ');

        _builder.Append(trimmed);

        // ✅ Проверяем последний символ StringBuilder напрямую — без аллокации
        if (_builder[^1] != ';')
            _builder.Append(';');

        return this;
    }

    /// <summary>Добавить пользовательский style (из @Style параметра).</summary>
    public StyleBuilder AddUserStyle(string? userStyle) => AddRaw(userStyle);

    /// <summary>Добавить стили из AdditionalAttributes["style"].</summary>
    public StyleBuilder AddFromAttributes(IReadOnlyDictionary<string, object>? attributes)
    {
        if (attributes is null) return this;
        if (attributes.TryGetValue("style", out var styleValue) && styleValue is string styleString)
            AddRaw(styleString);
        return this;
    }

    /// <summary>
    /// Добавить CSS custom property (переменную).
    /// ✅ УЛУЧШЕНО: валидация имени переменной.
    /// </summary>
    public StyleBuilder AddCustomProperty(string name, string? value, bool condition = true)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("CSS custom property name cannot be empty.", nameof(name));

        // Имена CSS custom properties начинаются с "--"
        var fullName = name.StartsWith("--", StringComparison.Ordinal) ? name : $"--{name}";
        return Add(fullName, value, condition);
    }

    /// <summary>Условное добавление с лямбдой.</summary>
    public StyleBuilder AddIf(string property, string? value, Func<bool> condition)
        => condition() ? Add(property, value) : this;

    /// <summary>Сбросить builder для повторного использования.</summary>
    public StyleBuilder Reset()
    {
        _builder.Clear();
        return this;
    }

    public string Build() => _builder.ToString();

    /// <summary>Возвращает null если стилей нет — не создаёт style="" атрибут.</summary>
    public string? NullIfEmpty()
    {
        if (_builder.Length == 0) return null;
        var result = _builder.ToString().Trim();
        return string.IsNullOrEmpty(result) ? null : result;
    }

    public override string ToString() => Build();
    public static implicit operator string(StyleBuilder builder) => builder.Build();
}