// ================================================================
// Файл: SuperUI/Base/Services/SgComponentFactory.cs
// ДОБАВЛЕНО: интерфейс IComponentFactory, класс сделан public
// ================================================================

namespace SuperUI.Base.Services;

/// <summary>
/// Интерфейс фабрики компонентов.
/// </summary>
public interface IComponentFactory
{
    T Create<T>() where T : class;
    T Create<T>(bool usePooling) where T : class, IPoolableComponent, new();
    void Return<T>(T component) where T : class, IPoolableComponent;
    object CreateByName(string name);
    ValueTask<T> CreateAsync<T>() where T : class;
}

/// <summary>
/// Factory for creating component instances with support for
/// dependency injection, pooling, and async initialization.
/// Optimized for both WASM (low overhead) and Server-side (circuit-aware).
/// Implements IComponentFactory for DI registration.
/// </summary>
public class SgComponentFactory : IComponentFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IComponentRegistry _registry;
    private readonly ConcurrentDictionary<Type, ObjectPool> _pools = new();

    public SgComponentFactory(IServiceProvider serviceProvider, IComponentRegistry registry)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
    }

    /// <summary>Create a component by type with full DI resolution.</summary>
    public T Create<T>() where T : class
    {
        return _serviceProvider.GetRequiredService<T>();
    }

    /// <summary>Create a component by type, with optional pooling.</summary>
    public T Create<T>(bool usePooling) where T : class, IPoolableComponent, new()
    {
        if (!usePooling)
            return Create<T>();

        var pool = _pools.GetOrAdd(typeof(T), _ => new ObjectPool(() => Create<T>()));
        return (T)pool.Rent();
    }

    /// <summary>Return a component to the pool for reuse.</summary>
    public void Return<T>(T component) where T : class, IPoolableComponent
    {
        if (_pools.TryGetValue(typeof(T), out var pool))
        {
            component.Reset();
            pool.Return(component);
        }
    }

    /// <summary>Create a component by registered name via DI.</summary>
    public object CreateByName(string name)
    {
        var type = _registry.ResolveType(name)
            ?? throw new InvalidOperationException($"Component '{name}' is not registered.");
        return _serviceProvider.GetRequiredService(type);
    }

    /// <summary>Async factory for components requiring async init.</summary>
    public async ValueTask<T> CreateAsync<T>() where T : class
    {
        var component = Create<T>();
        if (component is IAsyncInitializable asyncInit)
            await asyncInit.InitializeAsync();
        return component;
    }
}

/// <summary>Simple object pool for component reuse (WASM-friendly, no ArrayPool dependency).</summary>
internal sealed class ObjectPool
{
    private readonly ConcurrentBag<object> _bag = new();
    private readonly Func<object> _factory;

    public ObjectPool(Func<object> factory)
    {
        _factory = factory;
    }

    public object Rent() => _bag.TryTake(out var obj) ? obj : _factory();

    public void Return(object obj) => _bag.Add(obj);
}

/// <summary>Components that support pooling must implement this.</summary>
public interface IPoolableComponent
{
    void Reset();
}

/// <summary>Components needing async initialization.</summary>
public interface IAsyncInitializable
{
    ValueTask InitializeAsync();
}
