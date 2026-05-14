// SuperUI/Base/Utilities/StyleBuilder.cs
// ИСПРАВЛЕНИЯ v2:
// ✅ Add(property, double): InvariantCulture для дробного разделителя
// ✅ AddRaw: корректная проверка последнего символа ПОСЛЕ append
// ✅ NullIfEmpty: trim перед проверкой

using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace SuperUI.Base.Utilities;

public sealed class StyleBuilder
{
    private readonly StringBuilder _builder = new();

    public static StyleBuilder Default() => new();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public StyleBuilder Add(string property, string? value, bool condition = true)
    {
        if (!condition || string.IsNullOrWhiteSpace(value)) return this;
        EnsureSeparator();
        _builder.Append(property);
        _builder.Append(':');
        _builder.Append(value.Trim());
        _builder.Append(';');
        return this;
    }

    /// <summary>
    /// ✅ FIX: используем InvariantCulture для корректного форматирования чисел.
    /// Без этого "1.5px" в культуре ru-RU → "1,5px" (невалидный CSS).
    /// </summary>
    public StyleBuilder Add(string property, double value, string unit = "px", bool condition = true)
        => Add(property, value.ToString("G", System.Globalization.CultureInfo.InvariantCulture) + unit, condition);

    /// <summary>
    /// ✅ FIX: проверка последнего символа ';' выполняется ПОСЛЕ Append(trimmed),
    /// а не после EnsureSeparator().
    /// </summary>
    public StyleBuilder AddRaw(string? rawStyles, bool condition = true)
    {
        if (!condition || string.IsNullOrWhiteSpace(rawStyles)) return this;
        var trimmed = rawStyles.AsSpan().Trim();
        if (trimmed.IsEmpty) return this;

        EnsureSeparator();
        _builder.Append(trimmed);

        // Проверяем последний символ ПОСЛЕ добавления trimmed
        if (_builder.Length > 0 && _builder[^1] != ';')
            _builder.Append(';');

        return this;
    }

    public StyleBuilder AddUserStyle(string? userStyle) => AddRaw(userStyle);

    public StyleBuilder AddFromAttributes(IReadOnlyDictionary<string, object>? attributes)
    {
        if (attributes is null) return this;
        if (attributes.TryGetValue("style", out var styleValue) && styleValue is string styleString)
            AddRaw(styleString);
        return this;
    }

    public StyleBuilder AddCustomProperty(string name, string? value, bool condition = true)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("CSS custom property name cannot be empty.", nameof(name));
        var fullName = name.StartsWith("--", StringComparison.Ordinal) ? name : $"--{name}";
        return Add(fullName, value, condition);
    }

    public StyleBuilder AddIf(string property, string? value, Func<bool> condition)
        => condition() ? Add(property, value) : this;

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EnsureSeparator()
    {
        if (_builder.Length > 0 && _builder[^1] != ' ' && _builder[^1] != ';')
            _builder.Append(' ');
    }
}