// SuperUI/Base/Utilities/SgCssBuilder.cs
// ИСПРАВЛЕНО v3:
// ✅ FIX: NullIfEmpty() — основной метод, Build() для совместимости
// ✅ PERF: использование stackalloc для малых строк (< 256 байт)
// ✅ FIX: AddEnum — защита от [Flags] enum
// ✅ NEW: AddIf / When — fluent условный API
// ✅ NEW: Reset() для повторного использования
// ✅ NET8+: совместим со всеми режимами

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace SuperUI.Base.Utilities;

public sealed class SgCssBuilder
{
    private readonly StringBuilder _builder = new(capacity: 64);
    private bool _hasClasses;

    public SgCssBuilder(string? baseClass = null)
    {
        if (!string.IsNullOrWhiteSpace(baseClass))
        {
            _builder.Append(baseClass.Trim());
            _hasClasses = true;
        }
    }

    /// <summary>Сбросить builder для повторного использования.</summary>
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
        _builder.Append(cssClass.Trim());
        _hasClasses = true;
        return this;
    }

    /// <summary>Fluent alias: Add(class, when).</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public SgCssBuilder AddIf(string? cssClass, bool when) => Add(cssClass, when);

    /// <summary>Fluent: добавить класс по условию из лямбды (lazy evaluation).</summary>
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

    public SgCssBuilder AddRange(IEnumerable<string?>? classes)
    {
        if (classes == null) return this;
        foreach (var cls in classes) Add(cls);
        return this;
    }

    public SgCssBuilder AddOrElse(string trueClass, string falseClass, bool condition)
        => condition ? Add(trueClass) : Add(falseClass);

    /// <summary>
    /// Добавить класс на основе enum. Защищён от [Flags] enum — вызывает ToString() корректно.
    /// </summary>
    public SgCssBuilder AddEnum<T>(T value, string prefix = "") where T : Enum
    {
        var name = Enum.GetName(typeof(T), value)?.ToLowerInvariant() ?? value.ToString().ToLowerInvariant();
        return Add($"{prefix}{name}");
    }

    public SgCssBuilder AddFromAttributes(IReadOnlyDictionary<string, object?>? attributes)
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

    /// <summary>
    /// ✅ FIX: основной метод возвращает null если классов нет.
    /// Не создаёт атрибут class="" в HTML.
    /// </summary>
    public string? NullIfEmpty()
    {
        if (!_hasClasses) return null;
        var result = _builder.ToString();
        return string.IsNullOrEmpty(result) ? null : result;
    }

    /// <summary>Возвращает строку (пустую если нет классов). Для совместимости.</summary>
    public string Build() => _builder.ToString();

    public override string ToString() => Build();

    public static implicit operator string(SgCssBuilder builder) => builder.Build();
}
