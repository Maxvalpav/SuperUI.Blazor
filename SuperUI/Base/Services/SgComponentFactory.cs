// ================================================================
// Файл: SuperUI/Base/Services/SgComponentFactory.cs
// ИСПРАВЛЕНО:
// ✅ CS0246: IComponentRegistry → ISgComponentTypeRegistry
// ✅ ISgComponentTypeRegistry — опциональная зависимость (nullable)
// ✅ IComponentFactory — публичный интерфейс для DI
// ✅ ObjectPool — thread-safe через ConcurrentBag
// ✅ .NET 8/9/10: совместим
// ================================================================

using System.Collections.Concurrent;

namespace SuperUI.Base.Services;

/// <summary>Интерфейс фабрики компонентов SuperUI.</summary>
public interface IComponentFactory
{
    /// <summary>Создать компонент через DI.</summary>
    T Create<T>() where T : class;

    /// <summary>Создать компонент с поддержкой пулинга.</summary>
    T Create<T>(bool usePooling) where T : class, IPoolableComponent, new();

    /// <summary>Вернуть компонент в пул.</summary>
    void Return<T>(T component) where T : class, IPoolableComponent;

    /// <summary>Создать компонент по зарегистрированному имени.</summary>
    object CreateByName(string name);

    /// <summary>Асинхронно создать компонент (с поддержкой IAsyncInitializable).</summary>
    ValueTask<T> CreateAsync<T>() where T : class;
}

/// <summary>
/// Фабрика компонентов SuperUI.
/// Поддерживает DI, пулинг и асинхронную инициализацию.
/// Оптимизирована для WASM (низкий overhead) и Server-side (circuit-aware).
/// </summary>
public sealed class SgComponentFactory : IComponentFactory
{
    private readonly IServiceProvider _serviceProvider;
    // ✅ FIX CS0246: ISgComponentTypeRegistry (не IComponentRegistry)
    // ✅ FIX: nullable — реестр не обязателен для базового создания через DI
    private readonly ISgComponentTypeRegistry? _registry;
    private readonly ConcurrentDictionary<Type, ObjectPool<object>> _pools = new();

    /// <summary>
    /// Конструктор с опциональным реестром типов.
    /// </summary>
    /// <param name="serviceProvider">DI контейнер.</param>
    /// <param name="registry">
    /// Реестр типов компонентов. Nullable — требуется только для CreateByName().
    /// </param>
    public SgComponentFactory(IServiceProvider serviceProvider,
        ISgComponentTypeRegistry? registry = null)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _registry = registry;
    }

    /// <summary>Создать компонент через DI (полное разрешение зависимостей).</summary>
    public T Create<T>() where T : class
        => _serviceProvider.GetRequiredService<T>();

    /// <summary>Создать компонент с опциональным пулингом.</summary>
    public T Create<T>(bool usePooling) where T : class, IPoolableComponent, new()
    {
        if (!usePooling) return Create<T>();

        var pool = _pools.GetOrAdd(typeof(T), _ => new ObjectPool<object>(() => Create<T>()));
        return (T)pool.Rent();
    }

    /// <summary>Вернуть компонент в пул для повторного использования.</summary>
    public void Return<T>(T component) where T : class, IPoolableComponent
    {
        ArgumentNullException.ThrowIfNull(component);

        if (_pools.TryGetValue(typeof(T), out var pool))
        {
            component.Reset();
            pool.Return(component);
        }
    }

    /// <summary>
    /// Создать компонент по зарегистрированному имени.
    /// Требует ISgComponentTypeRegistry (должен быть зарегистрирован в DI).
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Если реестр не зарегистрирован или компонент не найден.
    /// </exception>
    public object CreateByName(string name)
    {
        if (_registry is null)
            throw new InvalidOperationException("ISgComponentTypeRegistry is not registered. " +
                "Call services.AddScoped<ISgComponentTypeRegistry, SgComponentRegistry>().");

        var type = _registry.ResolveType(name)
            ?? throw new InvalidOperationException($"Component '{name}' is not registered.");

        return _serviceProvider.GetRequiredService(type);
    }

    /// <summary>Асинхронно создать компонент (с поддержкой IAsyncInitializable).</summary>
    public async ValueTask<T> CreateAsync<T>() where T : class
    {
        var component = Create<T>();
        if (component is IAsyncInitializable asyncInit)
            await asyncInit.InitializeAsync();
        return component;
    }
}

// ── Вспомогательные интерфейсы и классы ─────────────────────────────────────

/// <summary>Компоненты, поддерживающие пулинг, должны реализовывать этот интерфейс.</summary>
public interface IPoolableComponent
{
    /// <summary>Сбросить состояние компонента перед возвратом в пул.</summary>
    void Reset();
}

/// <summary>Компоненты с асинхронной инициализацией.</summary>
public interface IAsyncInitializable
{
    ValueTask InitializeAsync();
}

/// <summary>
/// Простой потокобезопасный пул объектов (WASM-friendly, без зависимости от ArrayPool).
/// </summary>
internal sealed class ObjectPool<T>
{
    private readonly ConcurrentBag<T> _bag = [];
    private readonly Func<T> _factory;

    public ObjectPool(Func<T> factory)
        => _factory = factory ?? throw new ArgumentNullException(nameof(factory));

    public T Rent() => _bag.TryTake(out var obj) ? obj : _factory();

    public void Return(T obj) => _bag.Add(obj);
}
