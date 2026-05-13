// SuperUI/Base/Reactive/SgObservableDictionary.cs
// НОВЫЙ КЛАСС: реактивный словарь с интеграцией в Signal-систему

using System;
using System.Collections;
using System.Collections.Generic;

namespace SuperUI.Base.Reactive;

/// <summary>
/// Реактивный словарь. Уведомляет подписчиков при любом изменении.
/// </summary>
public sealed class SgObservableDictionary<TKey, TValue> : IDictionary<TKey, TValue>, IDisposable
    where TKey : notnull
{
    private readonly Dictionary<TKey, TValue> _inner;
    private readonly object _lock = new();
    private volatile bool _isDisposed;

    public event Action? Changed;
    public event Action<TKey, TValue?, DictionaryChangeType>? ItemChanged;

    public SgObservableDictionary() => _inner = new();

    public SgObservableDictionary(IEqualityComparer<TKey> comparer) => _inner = new(comparer);

    public TValue this[TKey key]
    {
        get { lock (_lock) return _inner[key]; }
        set
        {
            bool exists;
            TValue? old = default;
            lock (_lock)
            {
                exists = _inner.TryGetValue(key, out old);
                _inner[key] = value;
            }
            NotifyChanged(key, value, exists ? DictionaryChangeType.Update : DictionaryChangeType.Add);
        }
    }

    public int Count { get { lock (_lock) return _inner.Count; } }

    public bool IsReadOnly => false;

    public ICollection<TKey> Keys { get { lock (_lock) return _inner.Keys.ToArray(); } }

    public ICollection<TValue> Values { get { lock (_lock) return _inner.Values.ToArray(); } }

    public void Add(TKey key, TValue value)
    {
        lock (_lock) _inner.Add(key, value);
        NotifyChanged(key, value, DictionaryChangeType.Add);
    }

    public bool Remove(TKey key)
    {
        TValue? old = default;
        bool removed;
        lock (_lock) removed = _inner.Remove(key, out old);

        if (removed) NotifyChanged(key, old, DictionaryChangeType.Remove);
        return removed;
    }

    public void Clear()
    {
        lock (_lock) _inner.Clear();
        NotifyChanged(default!, default, DictionaryChangeType.Clear);
    }

    public bool ContainsKey(TKey key) { lock (_lock) return _inner.ContainsKey(key); }

    public bool TryGetValue(TKey key, out TValue value) { lock (_lock) return _inner.TryGetValue(key, out value!); }

    public void Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);

    public bool Contains(KeyValuePair<TKey, TValue> item) { lock (_lock) return ((ICollection<KeyValuePair<TKey, TValue>>)_inner).Contains(item); }

    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) { lock (_lock) ((ICollection<KeyValuePair<TKey, TValue>>)_inner).CopyTo(array, arrayIndex); }

    public bool Remove(KeyValuePair<TKey, TValue> item) => Remove(item.Key);

    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        KeyValuePair<TKey, TValue>[] snapshot;
        lock (_lock) snapshot = _inner.ToArray();
        return ((IEnumerable<KeyValuePair<TKey, TValue>>)snapshot).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private void NotifyChanged(TKey key, TValue? value, DictionaryChangeType type)
    {
        if (_isDisposed) return;
        ItemChanged?.Invoke(key, value, type);
        Changed?.Invoke();
    }

    public void Dispose()
    {
        _isDisposed = true;
        Changed = null;
        ItemChanged = null;
    }
}

public enum DictionaryChangeType { Add, Update, Remove, Clear }
