// SuperUI/Base/Utilities/SgCopyOnWriteList.cs — НОВЫЙ
// ✅ Замена volatile ImmutableArray<T> для .NET 8+
// ✅ Потокобезопасное чтение без блокировок (snapshot semantic)
// ✅ Запись под lock — гарантирует целостность данных
// ✅ Используется в SgEnhancedNavigationService и других местах

using System.Collections;

namespace SuperUI.Base.Utilities;

/// <summary>
/// Потокобезопасный список с copy-on-write семантикой.
/// Чтение (<see cref="Snapshot"/>) — lock-free, запись (<see cref="Add"/>/<see cref="Remove"/>) — под lock.
///
/// Замена <c>volatile ImmutableArray&lt;T&gt;</c> для случаев, где нужен volatile/Interlocked
/// (ImmutableArray — struct, не может быть volatile в .NET 8+).
/// </summary>
public sealed class SgCopyOnWriteList<T> : IEnumerable<T>
{
    private T[] _items;
    private readonly object _writeLock = new();

    public SgCopyOnWriteList()
    {
        _items = Array.Empty<T>();
    }

    public SgCopyOnWriteList(IEnumerable<T> initial)
    {
        _items = initial.ToArray();
    }

    // ── Write operations (under lock) ─────────────────────────────────────────

    /// <summary>Добавить элемент (thread-safe).</summary>
    public void Add(T item)
    {
        lock (_writeLock)
        {
            var snapshot = _items;
            var newItems = new T[snapshot.Length + 1];
            Array.Copy(snapshot, newItems, snapshot.Length);
            newItems[^1] = item;
            Volatile.Write(ref _items, newItems);
        }
    }

    /// <summary>Добавить несколько элементов атомарно (thread-safe).</summary>
    public void AddRange(IEnumerable<T> items)
    {
        lock (_writeLock)
        {
            var snapshot = _items;
            var toAdd = items.ToArray();
            var newItems = new T[snapshot.Length + toAdd.Length];
            Array.Copy(snapshot, 0, newItems, 0, snapshot.Length);
            Array.Copy(toAdd, 0, newItems, snapshot.Length, toAdd.Length);
            Volatile.Write(ref _items, newItems);
        }
    }

    /// <summary>
    /// Удалить первое вхождение элемента (thread-safe).
    /// Возвращает true если элемент был найден и удалён.
    /// </summary>
    public bool Remove(T item)
    {
        lock (_writeLock)
        {
            var snapshot = _items;
            var idx = Array.IndexOf(snapshot, item);
            if (idx < 0) return false;

            var newItems = new T[snapshot.Length - 1];
            if (idx > 0)
                Array.Copy(snapshot, 0, newItems, 0, idx);
            if (idx < snapshot.Length - 1)
                Array.Copy(snapshot, idx + 1, newItems, idx, snapshot.Length - idx - 1);

            Volatile.Write(ref _items, newItems);
            return true;
        }
    }

    /// <summary>Очистить список (thread-safe).</summary>
    public void Clear()
    {
        lock (_writeLock)
            Volatile.Write(ref _items, Array.Empty<T>());
    }

    // ── Read operations (lock-free) ────────────────────────────────────────────

    /// <summary>
    /// Получить атомарный снимок списка.
    /// Безопасно вызывать из любого потока без блокировок.
    /// Возвращённый массив не изменяется — можно итерировать без lock.
    /// </summary>
    public T[] Snapshot() => Volatile.Read(ref _items);

    /// <summary>Количество элементов (на основе последнего снимка, lock-free).</summary>
    public int Count => Volatile.Read(ref _items).Length;

    /// <summary>Проверить наличие элемента (на основе снимка, lock-free).</summary>
    public bool Contains(T item) => Array.IndexOf(Snapshot(), item) >= 0;

    // ── IEnumerable ────────────────────────────────────────────────────────────

    public IEnumerator<T> GetEnumerator()
        => ((IEnumerable<T>)Snapshot()).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
