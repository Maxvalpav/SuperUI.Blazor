// SuperUI/Base/Reactive/SgObservableList.cs
// НОВЫЙ КЛАСС:
// ✅ Реактивный список — изменения триггерят компоненты автоматически
// ✅ Thread-safe через lock
// ✅ IList<T>, IReadOnlyList<T> совместимость
// ✅ Поддержка batch-изменений

using System;
using System.Collections;
using System.Collections.Generic;

namespace SuperUI.Base.Reactive;

/// <summary>
/// Реактивный список. Изменения автоматически уведомляют подписанные компоненты.
/// Аналог ObservableCollection&lt;T&gt;, но интегрированный с Signal-системой SuperUI.
/// </summary>
/// <example>
/// <code>
/// private readonly SgObservableList&lt;string&gt; _items = new();
///
/// protected override void OnInitialized()
/// {
///     _items.Changed += () => StateHasChanged();
///     _items.Add("Item 1");
/// }
/// </code>
/// </example>
public sealed class SgObservableList<T> : IList<T>, IReadOnlyList<T>, IDisposable
{
    private readonly List<T> _inner = new();
    private readonly object _lock = new();
    private volatile bool _isDisposed;
    private int _version;

    /// <summary>Вызывается при любом изменении списка.</summary>
    public event Action? Changed;

    /// <summary>Вызывается с описанием конкретного изменения.</summary>
    public event Action<SgListChange<T>>? ItemChanged;

    public int Count { get { lock (_lock) return _inner.Count; } }

    public bool IsReadOnly => false;

    public T this[int index]
    {
        get { lock (_lock) return _inner[index]; }
        set
        {
            T old;
            lock (_lock) { old = _inner[index]; _inner[index] = value; _version++; }
            NotifyChanged(new SgListChange<T>(SgListChangeType.Replace, index, value, old));
        }
    }

    public void Add(T item)
    {
        int index;
        lock (_lock) { index = _inner.Count; _inner.Add(item); _version++; }
        NotifyChanged(new SgListChange<T>(SgListChangeType.Add, index, item));
    }

    public void AddRange(IEnumerable<T> items)
    {
        var list = new List<T>(items);
        if (list.Count == 0) return;

        int startIndex;
        lock (_lock) { startIndex = _inner.Count; _inner.AddRange(list); _version++; }
        NotifyChanged(new SgListChange<T>(SgListChangeType.AddRange, startIndex, default));
    }

    public void Insert(int index, T item)
    {
        lock (_lock) { _inner.Insert(index, item); _version++; }
        NotifyChanged(new SgListChange<T>(SgListChangeType.Insert, index, item));
    }

    public bool Remove(T item)
    {
        int index;
        bool removed;
        lock (_lock)
        {
            index = _inner.IndexOf(item);
            removed = index >= 0;
            if (index >= 0) { _inner.RemoveAt(index); _version++; }
        }

        if (removed)
            NotifyChanged(new SgListChange<T>(SgListChangeType.Remove, index, item));

        return removed;
    }

    public void RemoveAt(int index)
    {
        T item;
        lock (_lock) { item = _inner[index]; _inner.RemoveAt(index); _version++; }
        NotifyChanged(new SgListChange<T>(SgListChangeType.Remove, index, item));
    }

    public void Clear()
    {
        lock (_lock) { _inner.Clear(); _version++; }
        NotifyChanged(new SgListChange<T>(SgListChangeType.Clear, -1, default));
    }

    /// <summary>
    /// Выполняет множественные изменения в одном batch — одно уведомление Changed.
    /// </summary>
    public void Batch(Action<IList<T>> mutations)
    {
        lock (_lock)
        {
            mutations(_inner);
            _version++;
        }
        NotifyChanged(new SgListChange<T>(SgListChangeType.Batch, -1, default));
    }

    public bool Contains(T item) { lock (_lock) return _inner.Contains(item); }

    public int IndexOf(T item) { lock (_lock) return _inner.IndexOf(item); }

    public void CopyTo(T[] array, int arrayIndex) { lock (_lock) _inner.CopyTo(array, arrayIndex); }

    public IReadOnlyList<T> Snapshot() { lock (_lock) return _inner.ToArray(); }

    public IEnumerator<T> GetEnumerator()
    {
        T[] snapshot;
        lock (_lock) snapshot = _inner.ToArray();
        return ((IEnumerable<T>)snapshot).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private void NotifyChanged(SgListChange<T> change)
    {
        if (_isDisposed) return;

        try
        {
            ItemChanged?.Invoke(change);
            Changed?.Invoke();
        }
        catch { /* не глушим подписчиков — пусть падают видимо */ throw; }
    }

    public void Dispose()
    {
        _isDisposed = true;
        Changed = null;
        ItemChanged = null;
    }
}

public enum SgListChangeType { Add, AddRange, Insert, Remove, Replace, Clear, Batch }

public readonly struct SgListChange<T>
{
    public SgListChangeType ChangeType { get; }
    public int Index { get; }
    public T? NewItem { get; }
    public T? OldItem { get; }

    public SgListChange(SgListChangeType changeType, int index, T? newItem, T? oldItem = default)
    {
        ChangeType = changeType;
        Index = index;
        NewItem = newItem;
        OldItem = oldItem;
    }
}
