// SuperUI/Base/Utilities/SgComponentPool.cs
// НОВЫЙ: пул компонентов-данных (не Blazor компонентов, а VM объектов)
// Для DataGrid rows, TreeView nodes, VirtualList items

using System.Collections.Concurrent;
using System.Threading;

namespace SuperUI.Base.Utilities;

/// <summary>
/// Thread-safe пул объектов для переиспользования.
/// Уменьшает GC pressure при виртуализации больших списков.
/// </summary>
public sealed class SgObjectPool<T> where T : class, new()
{
    private readonly ConcurrentBag<T> _pool = new();
    private readonly Action<T>? _resetAction;
    private readonly int _maxSize;
    private int _count;

    /// <param name="maxSize">Максимальный размер пула (по умолчанию 256).</param>
    /// <param name="resetAction">Действие сброса объекта перед возвратом в пул.</param>
    public SgObjectPool(int maxSize = 256, Action<T>? resetAction = null)
    {
        _maxSize = maxSize;
        _resetAction = resetAction;
    }

    /// <summary>Взять объект из пула или создать новый.</summary>
    public T Rent()
    {
        if (_pool.TryTake(out var item))
        {
            Interlocked.Decrement(ref _count);
            return item;
        }
        return new T();
    }

    /// <summary>Вернуть объект в пул.</summary>
    public void Return(T item)
    {
        ArgumentNullException.ThrowIfNull(item);
        if (Volatile.Read(ref _count) >= _maxSize) return;
        _resetAction?.Invoke(item);
        _pool.Add(item);
        Interlocked.Increment(ref _count);
    }

    /// <summary>Аренда с автоматическим возвратом (using pattern).</summary>
    public PooledItem<T> RentScoped() => new(this, Rent());

    /// <summary>Текущий размер пула.</summary>
    public int Count => Volatile.Read(ref _count);

    /// <summary>Очистить пул.</summary>
    public void Clear()
    {
        while (_pool.TryTake(out _))
            Interlocked.Decrement(ref _count);
    }
}

/// <summary>RAII wrapper для объекта из пула.</summary>
public sealed class PooledItem<T> : IDisposable where T : class, new()
{
    private readonly SgObjectPool<T> _pool;
    private T? _item;

    internal PooledItem(SgObjectPool<T> pool, T item)
    {
        _pool = pool;
        _item = item;
    }

    public T Value => _item ?? throw new ObjectDisposedException(nameof(PooledItem<T>));

    public void Dispose()
    {
        var item = Interlocked.Exchange(ref _item, null);
        if (item is not null) _pool.Return(item);
    }
}
