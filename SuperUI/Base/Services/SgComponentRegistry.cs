// SuperUI/Base/Services/SgComponentRegistry.cs

using System.Collections.Concurrent;

namespace SuperUI.Base.Services;

/// <summary>
/// Центральный реестр активных компонентов SuperUI.
/// Позволяет найти любой компонент по ID, подписаться на lifecycle-события глобально.
/// </summary>
public interface IComponentRegistry
{
    /// <summary>Зарегистрировать компонент.</summary>
    void Register(SgComponentBase component);

    /// <summary>Удалить из реестра.</summary>
    void Unregister(string componentId);

    /// <summary>Найти компонент по ID.</summary>
    SgComponentBase? Find(string componentId);

    /// <summary>Получить все активные компоненты заданного типа.</summary>
    IEnumerable<T> GetAll<T>() where T : SgComponentBase;

    /// <summary>Количество активных компонентов.</summary>
    int Count { get; }

    /// <summary>Событие: компонент зарегистрирован.</summary>
    event Action<SgComponentBase>? ComponentRegistered;

    /// <summary>Событие: компонент удалён.</summary>
    event Action<SgComponentBase>? ComponentUnregistered;
}

/// <summary>
/// Реализация реестра компонентов на основе ConcurrentDictionary + WeakReference.
/// Автоматически очищает мёртвые ссылки при обходе коллекции.
/// </summary>
public sealed class ComponentRegistry : IComponentRegistry, IDisposable
{
    private readonly ConcurrentDictionary<string, WeakReference<SgComponentBase>> _components = new();

    public event Action<SgComponentBase>? ComponentRegistered;
    public event Action<SgComponentBase>? ComponentUnregistered;

    public int Count => _components.Count;

    public void Register(SgComponentBase component)
    {
        ArgumentNullException.ThrowIfNull(component);

        _components[component.ComponentId] = new WeakReference<SgComponentBase>(component);
        ComponentRegistered?.Invoke(component);
    }

    public void Unregister(string componentId)
    {
        if (_components.TryRemove(componentId, out var weakRef) &&
            weakRef.TryGetTarget(out var component))
        {
            ComponentUnregistered?.Invoke(component);
        }
    }

    public SgComponentBase? Find(string componentId)
    {
        if (_components.TryGetValue(componentId, out var weakRef) &&
            weakRef.TryGetTarget(out var component))
            return component;

        // Мёртвая ссылка — очищаем
        _components.TryRemove(componentId, out _);
        return null;
    }

    public IEnumerable<T> GetAll<T>() where T : SgComponentBase
    {
        var deadKeys = new List<string>();

        foreach (var (key, weakRef) in _components)
        {
            if (weakRef.TryGetTarget(out var component) && component is T typed)
                yield return typed;
            else
                deadKeys.Add(key);
        }

        // Очистка мёртвых ссылок после итерации (избегаем modification during enumeration)
        foreach (var key in deadKeys)
            _components.TryRemove(key, out _);
    }

    public void Dispose()
    {
        _components.Clear();
    }
}