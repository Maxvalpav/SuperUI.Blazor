// SuperUI/Base/Reactive/ComponentSignalGraph.cs
// ИСПРАВЛЕНО CS0101 + CS0111:
// УДАЛЕНЫ дубликаты Signal<T> и SignalTracker из этого файла.
// Этот файл содержит ТОЛЬКО ComponentSignalGraph — граф зависимостей.

namespace SuperUI.Base.Reactive;

/// <summary>
/// Граф зависимостей между компонентами и сигналами.
/// Используется для отладки реактивной системы в DevTools.
/// </summary>
/// <remarks>
/// В продакшн-сборке (Release) граф не ведётся для экономии памяти.
/// В DEBUG режиме граф позволяет визуализировать дерево зависимостей.
/// </remarks>
public sealed class ComponentSignalGraph
{
#if DEBUG
    // Граф: SignalId → список ComponentId подписчиков
    private readonly Dictionary<string, HashSet<string>> _graph = new();
    private readonly object _lock = new();

    /// <summary>Зарегистрировать связь сигнал → компонент.</summary>
    public void Register(string signalId, string componentId)
    {
        lock (_lock)
        {
            if (!_graph.TryGetValue(signalId, out var set))
                _graph[signalId] = set = new HashSet<string>();
            set.Add(componentId);
        }
    }

    /// <summary>Удалить все связи для компонента (при Dispose).</summary>
    public void Unregister(string componentId)
    {
        lock (_lock)
        {
            foreach (var set in _graph.Values)
                set.Remove(componentId);
        }
    }

    /// <summary>Получить снапшот графа для DevTools.</summary>
    public IReadOnlyDictionary<string, IReadOnlySet<string>> GetSnapshot()
    {
        lock (_lock)
        {
            return _graph.ToDictionary(
                kvp => kvp.Key,
                kvp => (IReadOnlySet<string>)new HashSet<string>(kvp.Value));
        }
    }

    /// <summary>Получить список компонентов, подписанных на сигнал.</summary>
    public IReadOnlySet<string> GetSubscribers(string signalId)
    {
        lock (_lock)
        {
            return _graph.TryGetValue(signalId, out var set)
                ? new HashSet<string>(set)
                : new HashSet<string>();
        }
    }
#else
    // Release: no-op граф
    public void Register(string signalId, string componentId) { }
    public void Unregister(string componentId) { }
#endif
}