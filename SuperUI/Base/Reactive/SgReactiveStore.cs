// SuperUI/Base/Reactive/SgReactiveStore.cs — НОВЫЙ
//
// Что это: глобальное реактивное хранилище состояния с сериализацией.
// Аналог: Redux store, Pinia, но сигнал-ориентированное для Blazor.

namespace SuperUI.Base.Reactive;

/// <summary>
/// Глобальное реактивное хранилище ключ-значение.
/// Поддерживает подписку на изменения, персистентность, снапшоты.
/// 
/// Использование:
/// <code>
/// var store = new SgReactiveStore();
/// var counter = store.GetSignal&lt;int&gt;("counter", 0);
/// counter.Set(counter.Value + 1);
/// </code>
/// </summary>
public sealed class SgReactiveStore : IDisposable
{
    private readonly Dictionary<string, object> _signals = new();
    private readonly object _lock = new();
    private readonly SgReactiveStore? _parent;

    public SgReactiveStore(SgReactiveStore? parent = null)
    {
        _parent = parent;
    }

    /// <summary>
    /// Получить или создать сигнал по ключу.
    /// </summary>
    public SgSignal<T> GetSignal<T>(string key, T defaultValue = default!, string? debugName = null)
    {
        lock (_lock)
        {
            if (_signals.TryGetValue(key, out var existing))
                return (SgSignal<T>)existing;

            var signal = new SgSignal<T>(defaultValue, debugName ?? key);
            _signals[key] = signal;
            return signal;
        }
    }

    /// <summary>
    /// Получить Computed из нескольких ключей.
    /// </summary>
    public SgComputed<TResult> GetComputed<TResult>(string[] keys,
        Func<IReadOnlyDictionary<string, object>, TResult> compute,
        string? debugName = null)
    {
        var signals = keys.Select(k => GetSignal<object>(k)).ToArray();

        return new SgComputed<TResult>(() =>
        {
            var dict = new Dictionary<string, object>();
            for (int i = 0; i < keys.Length; i++)
                dict[keys[i]] = signals[i].Value;
            return compute(dict);
        }, null, debugName);
    }

    /// <summary>
    /// Получить все сигналы как словарь значений.
    /// </summary>
    public IReadOnlyDictionary<string, object> Snapshot()
    {
        lock (_lock)
        {
            var dict = new Dictionary<string, object>(_signals.Count);
            foreach (var kv in _signals)
                dict[kv.Key] = ((dynamic)kv.Value).Value;
            return dict;
        }
    }

    /// <summary>
    /// Экспортировать все значения как JSON-сериализуемый словарь.
    /// </summary>
    public Dictionary<string, object?> Export()
    {
        lock (_lock)
        {
            var result = new Dictionary<string, object?>(_signals.Count);
            foreach (var kv in _signals)
            {
                var value = ((dynamic)kv.Value).Value;
                result[kv.Key] = value;
            }
            return result;
        }
    }

    /// <summary>
    /// Импортировать значения из словаря.
    /// </summary>
    public void Import(Dictionary<string, object?> data)
    {
        lock (_lock)
        {
            foreach (var kv in data)
            {
                if (_signals.TryGetValue(kv.Key, out var signal))
                {
                    // Используем dynamic чтобы избежать рефлексии
                    ((dynamic)signal).Set((dynamic?)kv.Value);
                }
            }
        }
    }

    /// <summary>
    /// Сбросить все сигналы в значения по умолчанию.
    /// </summary>
    public void Reset()
    {
        lock (_lock)
        {
            foreach (var kv in _signals)
            {
                try
                {
                    ((dynamic)kv.Value).Set(default);
                }
                catch { }
            }
        }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            foreach (var signal in _signals.Values)
            {
                if (signal is IDisposable d)
                    d.Dispose();
            }
            _signals.Clear();
        }
    }
}
