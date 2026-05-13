// SuperUI/Base/Services/SgComponentRegistry.cs
// FIX CS0425: Добавлен constraint where TComponent : class в метод IsRegistered<T>

namespace SuperUI.Base.Services;

/// <summary>
/// Central registry for all SuperUI components. Provides lookup, metadata,
/// and lazy-initialization support. Thread-safe for Server-side concurrent access.
/// </summary>
public class SgComponentRegistry : IComponentRegistry
{
    private readonly ConcurrentDictionary<string, ComponentRegistration> _components = new();
    private readonly ConcurrentDictionary<Type, string> _typeToName = new();
    private readonly IServiceProvider? _serviceProvider;

    public SgComponentRegistry(IServiceProvider? serviceProvider = null)
    {
        _serviceProvider = serviceProvider;
    }

    public int Count => _components.Count;

    /// <summary>Register a component type with metadata.</summary>
    public SgComponentRegistry Register<TComponent>(string name, ComponentMetadata? metadata = null)
        where TComponent : class
    {
        var type = typeof(TComponent);
        var registration = new ComponentRegistration(name, type, metadata ?? new ComponentMetadata());
        _components[name] = registration;
        _typeToName[type] = name;
        return this;
    }

    /// <summary>Check if a component name is registered.</summary>
    public bool IsRegistered(string name) => _components.ContainsKey(name);

    /// <summary>
    /// Check if a component type is registered.
    /// ✅ FIX CS0425: Добавлен where TComponent : class для соответствия IComponentRegistry.
    /// </summary>
    public bool IsRegistered<TComponent>() where TComponent : class
        => _typeToName.ContainsKey(typeof(TComponent));

    /// <summary>Get component type by name.</summary>
    public Type? ResolveType(string name)
    {
        return _components.TryGetValue(name, out var registration)
            ? registration.ComponentType
            : null;
    }

    /// <summary>Get all registered component names.</summary>
    public IEnumerable<string> GetRegisteredNames() => _components.Keys;

    /// <summary>Get metadata for a registered component.</summary>
    public ComponentMetadata? GetMetadata(string name)
    {
        return _components.TryGetValue(name, out var registration)
            ? registration.Metadata
            : null;
    }

    /// <summary>Try to create an instance via DI.</summary>
    public TComponent? Create<TComponent>() where TComponent : class
    {
        if (_serviceProvider is null)
            throw new InvalidOperationException("ServiceProvider is not set. Cannot resolve components.");
        return _serviceProvider.GetService<TComponent>();
    }

    // --- IComponentRegistry implementation ---
    void IComponentRegistry.Register<TComponent>(string name, ComponentMetadata? metadata)
        => Register<TComponent>(name, metadata);
}

/// <summary>Registration entry for a component.</summary>
public sealed class ComponentRegistration
{
    public string Name { get; }
    public Type ComponentType { get; }
    public ComponentMetadata Metadata { get; }

    public ComponentRegistration(string name, Type componentType, ComponentMetadata metadata)
    {
        Name = name;
        ComponentType = componentType;
        Metadata = metadata;
    }
}

/// <summary>Metadata describing a component.</summary>
public sealed class ComponentMetadata
{
    public string? Description { get; set; }
    public string? Category { get; set; }
    public string? Version { get; set; }
    public bool IsExperimental { get; set; }
    public bool IsDeprecated { get; set; }
    public Dictionary<string, string> Tags { get; set; } = new();

    public ComponentMetadata() { }

    public ComponentMetadata(string description, string category = "General")
    {
        Description = description;
        Category = category;
    }
}

/// <summary>
/// Interface for component registry (used for DI and testing).
/// ✅ Добавлен constraint where T : class для единообразия.
/// </summary>
public interface IComponentRegistry
{
    void Register<T>(string name, ComponentMetadata? metadata = null) where T : class;
    bool IsRegistered(string name);
    bool IsRegistered<T>() where T : class;
    Type? ResolveType(string name);
    ComponentMetadata? GetMetadata(string name);
    IEnumerable<string> GetRegisteredNames();
}
