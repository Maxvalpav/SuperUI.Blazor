// SuperUI/Base/Utilities/SgCssBuilder.cs
namespace SuperUI.Base.Utilities;

using System;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// Fluent CSS class builder с пулом буферов и кэшем результата.
/// </summary>
/// <remarks>
/// - Кэширует результат <see cref="Build"/> — повторные вызовы бесплатны.
/// - Использует <see cref="System.Buffers.ArrayPool{T}"/> для composition строк.
/// - Условие <see cref="Add(string, bool)"/> вычисляется немедленно (eager).
/// - Не thread-safe; рассчитан на локальное использование внутри одного рендера.
/// - Совместим с WASM и Server.
/// </remarks>
public sealed class SgCssBuilder
{
    private static readonly System.Buffers.ArrayPool<char> Pool = System.Buffers.ArrayPool<char>.Shared;

    private List<string>? _classes;
    private string? _cached;

    public SgCssBuilder(string? baseClass = null)
    {
        if (!string.IsNullOrWhiteSpace(baseClass))
        {
            _classes = new List<string> { baseClass };
        }
    }

    /// Добавить класс безусловно.
    public SgCssBuilder Add(string? cssClass)
    {
        if (!string.IsNullOrWhiteSpace(cssClass))
        {
            _cached = null;
            (_classes ??= new()).Add(cssClass);
        }
        return this;
    }

    /// Добавить класс с условием.
    public SgCssBuilder Add(string? cssClass, bool condition)
    {
        if (condition && !string.IsNullOrWhiteSpace(cssClass))
        {
            _cached = null;
            (_classes ??= new()).Add(cssClass);
        }
        return this;
    }

    /// Добавить класс с ленивым условием (Func вычисляется при вызове).
    public SgCssBuilder Add(string? cssClass, Func<bool> condition)
        => condition() ? Add(cssClass, true) : this;

    /// Добавить класс если условие истинно (альтернативный синтаксис).
    public SgCssBuilder AddIf(bool condition, string cssClass)
        => Add(cssClass, condition);

    /// Добавить несколько классов из строки (разделённых пробелом).
    public SgCssBuilder AddRange(string? classes)
    {
        if (string.IsNullOrWhiteSpace(classes)) return this;
        foreach (var cls in classes.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            Add(cls);
        return this;
    }

    /// Добавить пользовательский Class параметр снаружи.
    public SgCssBuilder AddUserClass(string? userClass) => AddRange(userClass);

    /// Добавить класс-модификатор BEM.
    public SgCssBuilder AddBem(string block, string? modifier, bool condition = true)
        => condition && modifier != null ? Add($"{block}--{modifier}") : this;

    /// Добавить класс из атрибутов.
    public SgCssBuilder AddFromAttributes(IReadOnlyDictionary<string, object>? attrs)
    {
        if (attrs is not null && attrs.TryGetValue("class", out var cls))
            Add(cls?.ToString());
        return this;
    }

    /// Построить строку CSS классов. Кэшируется.
    /// <returns>null если нет классов (не рендерит пустой атрибут)</returns>
    public string? Build()
    {
        if (_cached is not null) return _cached;
        if (_classes is null || _classes.Count == 0) return _cached = null;

        // Подсчёт нужных символов для оптимального размера буфера
        var totalLength = 0;
        var count = 0;
        foreach (var cls in _classes)
        {
            totalLength += cls.Length + 1; // +1 for space
            count++;
        }

        if (count == 0) return _cached = null;

        // Убираем последний лишний пробел
        totalLength -= 1;

        // Аренда буфера из пула
        var buffer = Pool.Rent(totalLength);
        var span = buffer.AsSpan();
        var pos = 0;
        var first = true;

        foreach (var cls in _classes)
        {
            if (!first) span[pos++] = ' ';
            cls.AsSpan().CopyTo(span[pos..]);
            pos += cls.Length;
            first = false;
        }

        _cached = new string(span[..pos]);
        Pool.Return(buffer);

        return _cached;
    }

    public static implicit operator string?(SgCssBuilder builder) => builder.Build();

    public override string? ToString() => Build();
}