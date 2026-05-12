// SuperUI/Base/Reactive/ComponentSignalGraph.cs
//
// УЛУЧШЕНИЯ:
//   1. Release-режим: минимальные заглушки (no-op)
//   2. DEBUG: GetDependents(componentId) — обратный граф
//   3. DEBUG: Clear() — сброс для тестов
//   4. DEBUG: статистика (SignalCount, SubscriberCount)

namespace SuperUI.Base.Reactive;

/// <summary>
/// Граф зависимостей: сигнал → список подписанных компонентов.
/// В Release-сборке — no-op (нет памяти и CPU оверхеда).
/// В DEBUG — полный граф для DevTools/диагностики.
/// </summary>
public sealed class ComponentSignalGraph
{
#if DEBUG
    // Граф: signalId → ComponentId[]
    private readonly Dictionary<string, HashSet<string>> _signalToComponents = new();
    // Обратный граф: componentId → signalId[]
    private readonly Dictionary<string, HashSet<string>> _componentToSignals = new();
    private readonly object _lock = new();

    /// <summary>Зарегистрировать связь сигнал → компонент.</summary>
    public void Register(string signalId, string componentId)
    {
        lock (_lock)
        {
            if (!_signalToComponents.TryGetValue(signalId, out var compSet))
                _signalToComponents[signalId] = compSet = new HashSet<string>();
            compSet.Add(componentId);

            if (!_componentToSignals.TryGetValue(componentId, out var sigSet))
                _componentToSignals[componentId] = sigSet = new HashSet<string>();
            sigSet.Add(signalId);
        }
    }

    /// <summary>Удалить все связи компонента (при Dispose).</summary>
    public void Unregister(string componentId)
    {
        lock (_lock)
        {
            if (_componentToSignals.TryGetValue(componentId, out var signals))
            {
                foreach (var sig in signals)
                    if (_signalToComponents.TryGetValue(sig, out var comps))
                        comps.Remove(componentId);
                _componentToSignals.Remove(componentId);
            }
            foreach (var set in _signalToComponents.Values)
                set.Remove(componentId);
        }
    }

    /// <summary>Компоненты, подписанные на сигнал.</summary>
    public IReadOnlySet<string> GetSubscribers(string signalId)
    {
        lock (_lock)
            return _signalToComponents.TryGetValue(signalId, out var set)
                ? new HashSet<string>(set)
                : new HashSet<string>();
    }

    /// <summary>Сигналы, на которые подписан компонент (обратный граф).</summary>
    public IReadOnlySet<string> GetDependencies(string componentId)
    {
        lock (_lock)
            return _componentToSignals.TryGetValue(componentId, out var set)
                ? new HashSet<string>(set)
                : new HashSet<string>();
    }

    /// <summary>Снапшот графа для DevTools.</summary>
    public IReadOnlyDictionary<string, IReadOnlySet<string>> GetSnapshot()
    {
        lock (_lock)
            return _signalToComponents.ToDictionary(
                kvp => kvp.Key,
                kvp => (IReadOnlySet<string>)new HashSet<string>(kvp.Value));
    }

    /// <summary>Количество уникальных сигналов в графе.</summary>
    public int SignalCount { get { lock (_lock) return _signalToComponents.Count; } }

    /// <summary>Количество уникальных подписчиков.</summary>
    public int SubscriberCount { get { lock (_lock) return _componentToSignals.Count; } }

    /// <summary>Очистить граф (для тестов).</summary>
    public void Clear()
    {
        lock (_lock)
        {
            _signalToComponents.Clear();
            _componentToSignals.Clear();
        }
    }
#else
    // Release: полный no-op — нет аллокаций, нет CPU
    public void Register(string signalId, string componentId) { }
    public void Unregister(string componentId) { }
    public void Clear() { }
#endif
}
