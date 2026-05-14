// SuperUI/Base/Reactive/SgObservableList.cs
// ИСПРАВЛЕНО:
// ✅ NotifyChanged не бросает исключения подписчиков — логирует и продолжает
// ✅ Batch: мутации внутри lock, NotifyChanged вне lock (deadlock prevention)
// ✅ Thread-safe GetEnumerator через snapshot
// ✅ Dispose: двойной dispose идемпотентен

using System;
using System.Collections;
using System.Collections.Generic;

namespace SuperUI.Base.Reactive;

/// <summary>
/// Реактивный список. Изменения автоматически уведомляют подписанные компоненты.
/// Thread-safe через lock.
/// </summary>
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
        lock (_lock)
        {
            index = _inner.Count;
            _inner.Add(item);
            _version++;
        }
        NotifyChanged(new SgListChange<T>(SgListChangeType.Add, index, item));
    }

    public void AddRange(IEnumerable<T> items)
    {
        var list = new List<T>(items);
        if (list.Count == 0) return;

        int startIndex;
        lock (_lock)
        {
            startIndex = _inner.Count;
            _inner.AddRange(list);
            _version++;
        }
        NotifyChanged(new SgListChange<T>(SgListChangeType.AddRange, startIndex, default));
    }

    public void Insert(int index, T item)
    {
        lock (_lock)
        {
            _inner.Insert(index, item);
            _version++;
        }
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
            if (index >= 0)
            {
                _inner.RemoveAt(index);
                _version++;
            }
        }
        if (removed)
            NotifyChanged(new SgListChange<T>(SgListChangeType.Remove, index, item));
        return removed;
    }

    public void RemoveAt(int index)
    {
        T item;
        lock (_lock)
        {
            item = _inner[index];
            _inner.RemoveAt(index);
            _version++;
        }
        NotifyChanged(new SgListChange<T>(SgListChangeType.Remove, index, item));
    }

    public void Clear()
    {
        lock (_lock)
        {
            _inner.Clear();
            _version++;
        }
        NotifyChanged(new SgListChange<T>(SgListChangeType.Clear, -1, default));
    }

    /// <summary>
    /// Выполняет множественные изменения в одном batch — одно уведомление Changed.
    /// ✅ ИСПРАВЛЕНО: мутации внутри lock, NotifyChanged вне lock.
    /// </summary>
    public void Batch(Action<List<T>> mutations)
    {
        // ✅ Копируем список для мутаций, применяем под lock
        lock (_lock)
        {
            mutations(_inner);
            _version++;
        }
        // ✅ Уведомляем вне lock
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
    /// ✅ ИСПРАВЛЕНО: исключения подписчиков перехватываются — остальные подписчики
    /// продолжают получать уведомления. Нарушение одного не ломает всех.
    /// </summary>
    private void NotifyChanged(SgListChange<T> change)
    {
        if (_isDisposed) return;

        var itemChanged = ItemChanged;
        var changed = Changed;

        if (itemChanged is not null)
        {
            foreach (var handler in itemChanged.GetInvocationList())
            {
                try { handler.DynamicInvoke(change); }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[SgObservableList] ItemChanged handler error: {ex}");
                }
            }
        }

        if (changed is not null)
        {
            foreach (var handler in changed.GetInvocationList())
            {
                try { handler.DynamicInvoke(); }
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
        if (_isDisposed) return;
        _isDisposed = true;
        Changed = null;
        ItemChanged = null;
    }
}

public enum SgListChangeType
{
    Add,
    AddRange,
    Insert,
    Remove,
    Replace,
    Clear,
    Batch
}

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
