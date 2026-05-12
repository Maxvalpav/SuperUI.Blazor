using System.Collections.Concurrent;

namespace SuperUI.Base.Services;

/// <summary>
/// Реализация по умолчанию — хранит опции в ConcurrentDictionary.
/// Singleton: thread-safe для WASM и Server.
/// </summary>
public sealed class ComponentOptionsService : IComponentOptionsService
{
    // Key: (ComponentType, OptionsType) → object
    private readonly ConcurrentDictionary<(Type, Type), object> _options = new();

    /// <summary>
    /// Зарегистрировать опции для типа компонента.
    /// </summary>
    public ComponentOptionsService Register<TComponent, TOptions>(TOptions options)
        where TOptions : class
    {
        ArgumentNullException.ThrowIfNull(options);
        _options[(typeof(TComponent), typeof(TOptions))] = options;
        return this;
    }

    public TOptions? GetOptions<TComponent, TOptions>()
        where TOptions : class
    {
        var key = (typeof(TComponent), typeof(TOptions));
        return _options.TryGetValue(key, out var value) ? (TOptions)value : null;
    }

    public TOptions GetOrDefault<TComponent, TOptions>(Func<TOptions> defaultFactory)
        where TOptions : class
    {
        ArgumentNullException.ThrowIfNull(defaultFactory);
        var key = (typeof(TComponent), typeof(TOptions));
        return (TOptions)_options.GetOrAdd(key, _ => defaultFactory());
    }
}

/// <summary>
/// Null-реализация: всегда возвращает null / default.
/// Используется как fallback если DI не зарегистрировал ComponentOptionsService.
/// </summary>
public sealed class NullComponentOptionsService : IComponentOptionsService
{
    public static readonly NullComponentOptionsService Instance = new();

    public TOptions? GetOptions<TComponent, TOptions>()
        where TOptions : class => null;

    public TOptions GetOrDefault<TComponent, TOptions>(Func<TOptions> defaultFactory)
        where TOptions : class => defaultFactory();
}
