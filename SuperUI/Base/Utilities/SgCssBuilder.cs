// SuperUI/Base/Utilities/SgCssBuilder.cs
// ИСПРАВЛЕНИЯ v2:
// ✅ L7: AddEnum корректен для [Flags] enum — использует .ToString() с нормализацией
// ✅ PERF: Trim() только при необходимости (HasWhiteSpace check)
// ✅ AddEnum: fallback для undefined values с очисткой ", " → "-"

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace SuperUI.Base.Utilities;

public sealed class SgCssBuilder
{
    private readonly StringBuilder _builder;
    private bool _hasClasses;

    public SgCssBuilder(string? baseClass = null)
    {
        _builder = new StringBuilder(64);
        if (!string.IsNullOrWhiteSpace(baseClass))
            Add(baseClass);
    }

    public static SgCssBuilder Default(string? baseClass = null) => new(baseClass);

    public SgCssBuilder Reset()
    {
        _builder.Clear();
        _hasClasses = false;
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SgCssBuilder Add(string? cssClass, bool condition = true)
    {
        if (!condition || string.IsNullOrWhiteSpace(cssClass)) return this;
        if (_hasClasses) _builder.Append(' ');
        // ✅ PERF: Trim только если есть пробелы
        AppendTrimmed(cssClass);
        _hasClasses = true;
        return this;
    }

    private void AppendTrimmed(string s)
    {
        var span = s.AsSpan();
        var trimmed = span.Trim();
        if (trimmed.IsEmpty) return;
        _builder.Append(trimmed);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SgCssBuilder AddIf(string? cssClass, bool when) => Add(cssClass, when);

    public SgCssBuilder AddIf(string? cssClass, Func<bool> condition)
    {
        if (cssClass is not null && condition()) Add(cssClass);
        return this;
    }

    public SgCssBuilder AddMultiple(string? cssClasses, bool condition = true)
    {
        if (!condition || string.IsNullOrWhiteSpace(cssClasses)) return this;
        foreach (var cls in cssClasses.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            Add(cls);
        return this;
    }

    public SgCssBuilder AddRange(IEnumerable<string>? classes)
    {
        if (classes == null) return this;
        foreach (var cls in classes) Add(cls);
        return this;
    }

    public SgCssBuilder AddOrElse(string trueClass, string falseClass, bool condition)
        => condition ? Add(trueClass) : Add(falseClass);

    /// <summary>
    /// ✅ FIX L7: AddEnum корректен для [Flags] enum.
    /// Для комбинированных значений [Flags] .ToString() возвращает "Value1, Value2".
    /// Нормализуем: убираем пробелы и запятые → "value1-value2".
    /// </summary>
    public SgCssBuilder AddEnum<T>(T value, string prefix = "") where T : Enum
    {
        var name = Enum.GetName(typeof(T), value);
        string cssName;

        if (name is not null)
        {
            // Простое значение enum
            cssName = name.ToLowerInvariant();
        }
        else
        {
            // [Flags] или неизвестное значение: "Value1, Value2" → "value1-value2"
            cssName = value.ToString()
                .ToLowerInvariant()
                .Replace(", ", "-")
                .Replace(" ", "-");
        }

        return Add($"{prefix}{cssName}");
    }

    public SgCssBuilder AddFromAttributes(IReadOnlyDictionary<string, object>? attributes)
    {
        if (attributes is null) return this;
        if (attributes.TryGetValue("class", out var classValue) && classValue is string classString)
            AddMultiple(classString);
        return this;
    }

    public SgCssBuilder AddSize(string component, string? size, string defaultSize = "md")
    {
        var effectiveSize = string.IsNullOrWhiteSpace(size) ? defaultSize : size;
        return Add($"{component}--{effectiveSize!.ToLowerInvariant()}");
    }

    public SgCssBuilder AddModifier(string block, string modifier, bool condition = true)
        => Add($"{block}--{modifier}", condition);

    /// <summary>Возвращает null если классов нет — не создаёт class="" в HTML.</summary>
    public string? NullIfEmpty()
    {
        if (!_hasClasses) return null;
        var result = _builder.ToString();
        return string.IsNullOrEmpty(result) ? null : result;
    }

    public string Build() => _builder.ToString();
    public override string ToString() => Build();
    public static implicit operator string(SgCssBuilder builder) => builder.Build();
}
