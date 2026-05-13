// SuperUI/Base/Services/SgComponentRegistry.cs
// ИСПРАВЛЕНО:
// ✅ CS0311: SgComponentRegistry реализует ISgComponentTypeRegistry (новое имя, нет конфликта)
// ✅ CS0104: убран конфликт имён между Base.IComponentRegistry и Services.IComponentRegistry
// ✅ Добавлена поддержка тегирования, поиска по Category
// ✅ Thread-safe для Server-side concurrent access

using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;

namespace SuperUI.Base.Services;

/// <summary>
/// Интерфейс реестра типов компонентов SuperUI.
/// Переименован из IComponentRegistry → ISgComponentTypeRegistry для устранения CS0104.
/// </summary>
public interface ISgComponentTypeRegistry
{
    void Register<T>(string name, ComponentMetadata? metadata = null) where T : class;
    bool IsRegistered(string name);
    bool IsRegistered<T>() where T : class;
    Type? ResolveType(string name);
    ComponentMetadata? GetMetadata(string name);
    IEnumerable<string> GetRegisteredNames();
    IEnumerable<string> GetRegisteredNamesByCategory(string category);
}

/// <summary>
/// Центральный реестр компонентов SuperUI.
/// Предоставляет поиск, метаданные, ленивую инициализацию.
/// Потокобезопасен для Server-side.
/// </summary>
public sealed class SgComponentRegistry : ISgComponentTypeRegistry
{
    private readonly ConcurrentDictionary<string, ComponentRegistration> _components = new();
    private readonly ConcurrentDictionary<Type, string> _typeToName = new();
    private readonly IServiceProvider? _serviceProvider;

    public SgComponentRegistry(IServiceProvider? serviceProvider = null)
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>Количество зарегистрированных компонентов.</summary>
    public int Count => _components.Count;

    /// <summary>Зарегистрировать тип компонента с метаданными.</summary>
    public SgComponentRegistry Register<TComponent>(
        string name,
        ComponentMetadata? metadata = null) where TComponent : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var type = typeof(TComponent);
        var registration = new ComponentRegistration(name, type, metadata ?? new ComponentMetadata());

        _components[name] = registration;
        _typeToName[type] = name;

        return this;
    }

    // ISgComponentTypeRegistry explicit implementation
    void ISgComponentTypeRegistry.Register<T>(string name, ComponentMetadata? metadata)
        => Register<T>(name, metadata);

    /// <summary>Проверить, зарегистрирован ли компонент по имени.</summary>
    public bool IsRegistered(string name) => _components.ContainsKey(name);

    /// <summary>Проверить, зарегистрирован ли тип компонента.</summary>
    public bool IsRegistered<TComponent>() where TComponent : class
        => _typeToName.ContainsKey(typeof(TComponent));

    /// <summary>Получить тип компонента по имени.</summary>
    public Type? ResolveType(string name)
        => _components.TryGetValue(name, out var reg) ? reg.ComponentType : null;

    /// <summary>Получить все зарегистрированные имена.</summary>
    public IEnumerable<string> GetRegisteredNames() => _components.Keys;

    /// <summary>Получить имена компонентов по категории.</summary>
    public IEnumerable<string> GetRegisteredNamesByCategory(string category)
        => _components.Values
            .Where(r => string.Equals(r.Metadata.Category, category, StringComparison.OrdinalIgnoreCase))
            .Select(r => r.Name);

    /// <summary>Получить метаданные компонента.</summary>
    public ComponentMetadata? GetMetadata(string name)
        => _components.TryGetValue(name, out var reg) ? reg.Metadata : null;

    /// <summary>Попытаться создать экземпляр через DI.</summary>
    public TComponent? Create<TComponent>() where TComponent : class
    {
        if (_serviceProvider is null)
            throw new InvalidOperationException("ServiceProvider is not set. Cannot resolve components.");

        return _serviceProvider.GetService<TComponent>();
    }

    /// <summary>Получить все регистрации (для диагностики).</summary>
    public IReadOnlyCollection<ComponentRegistration> GetAllRegistrations()
        => _components.Values.ToList().AsReadOnly();
}

/// <summary>Запись регистрации компонента.</summary>
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

/// <summary>Метаданные компонента.</summary>
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
