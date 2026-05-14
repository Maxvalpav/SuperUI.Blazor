// SuperUI/Base/Reactive/SgObservableList.cs
// ИСПРАВЛЕНИЯ v2:
// ✅ W2: Batch — exception-safe: snapshot перед мутацией, rollback при ошибке
// ✅ W2: NotifyChanged без DynamicInvoke — прямой вызов делегатов
// ✅ Dispose идемпотентен через Interlocked

using System;
using System.Collections;
using System.Collections.Generic;

namespace SuperUI.Base.Reactive;

public sealed class SgObservableList<T> : IList<T>, IReadOnlyList<T>, IDisposable
{
    private readonly List<T> _inner = new();
    private readonly object _lock = new();
    private int _disposed; // Interlocked
    private int _version;

    public event Action? Changed;
    public event Action<SgListChange<T>>? ItemChanged;

    public int Count { get { lock (_lock) return _inner.Count; } }
    public bool IsReadOnly => false;

    public T this[int index]
    {
        get { lock (_lock) return _inner[index]; }
        set
        {
            T old;
            lock (_lock)
            {
                old = _inner[index];
                _inner[index] = value;
                _version++;
            }
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
        int index; bool removed;
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
    /// ✅ FIX W2: exception-safe Batch.
    /// Мутатор получает КОПИЮ списка — инкапсуляция сохранена.
    /// После успешной мутации — атомарная замена под lock.
    /// При исключении — список остаётся неизменным.
    /// </summary>
    public void Batch(Action<List<T>> mutations)
    {
        ArgumentNullException.ThrowIfNull(mutations);

        List<T> copy;
        lock (_lock) copy = new List<T>(_inner);

        // Мутируем копию ВНЕ lock — исключение не затрагивает _inner
        mutations(copy);

        lock (_lock)
        {
            _inner.Clear();
            _inner.AddRange(copy);
            _version++;
        }

        // Notify ВНЕ lock
        NotifyChanged(new SgListChange<T>(SgListChangeType.Batch, -1, default));
    }

    public bool Contains(T item) { lock (_lock) return _inner.Contains(item); }
    public int IndexOf(T item) { lock (_lock) return _inner.IndexOf(item); }
    public void CopyTo(T[] array, int arrayIndex) { lock (_lock) _inner.CopyTo(array, arrayIndex); }

    public IReadOnlyList<T> Snapshot()
    {
        lock (_lock) return _inner.ToArray();
    }

    public IEnumerator<T> GetEnumerator()
    {
        T[] snapshot;
        lock (_lock) snapshot = _inner.ToArray();
        return ((IEnumerable<T>)snapshot).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// ✅ FIX: прямой вызов делегатов без DynamicInvoke (нет reflection, нет boxing).
    /// </summary>
    private void NotifyChanged(SgListChange<T> change)
    {
        if (Volatile.Read(ref _disposed) == 1) return;

        var itemChanged = ItemChanged;
        if (itemChanged is not null)
        {
            foreach (var d in itemChanged.GetInvocationList())
            {
                try { ((Action<SgListChange<T>>)d)(change); }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[SgObservableList] ItemChanged handler error: {ex}");
                }
            }
        }

        var changed = Changed;
        if (changed is not null)
        {
            foreach (var d in changed.GetInvocationList())
            {
                try { ((Action)d)(); }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[SgObservableList] Changed handler error: {ex}");
                }
            }
        }
    }

    public void Dispose()
    {
        // ✅ FIX: идемпотентен
        if (Interlocked.Exchange(ref _disposed, 1) == 1) return;
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

    public SgListChange(
        SgListChangeType changeType,
        int index,
        T? newItem,
        T? oldItem = default)
    {
        ChangeType = changeType;
        Index = index;
        NewItem = newItem;
        OldItem = oldItem;
    }
}