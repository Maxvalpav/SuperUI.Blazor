// SuperUI/Base/Utilities/ComponentMemoryPool.cs
// НОВОЕ: Object pool для Dictionary<string,object> используемых в ARIA и CSS.
// Снижает нагрузку на GC при большом количестве рендеров.
//
// На 1000 компонентов: экономит ~1000 аллокаций Dictionary за цикл рендера.
// Особенно важно для:
// - Virtualize компонентов (строки DataGrid)
// - Анимированных списков
// - Высокочастотных обновлений (real-time данные)
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace SuperUI.Base.Utilities;

/// <summary>
/// Пул Dictionary&lt;string, object&gt; для переиспользования в ARIA/CSS builders.
/// Thread-safe. Максимальный размер пула — 64 объекта на тип.
/// </summary>
public static class ComponentDictionaryPool
{
    private static readonly ConcurrentBag<Dictionary<string, object>> _pool = [];
    private const int MaxPoolSize = 64;
    private static int _poolSize;

    /// <summary>Взять Dictionary из пула или создать новый.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Dictionary<string, object> Rent(int initialCapacity = 8)
    {
        if (_pool.TryTake(out var dict))
        {
            Interlocked.Decrement(ref _poolSize);
            dict.Clear();
            return dict;
        }
        return new Dictionary<string, object>(initialCapacity, StringComparer.Ordinal);
    }

    /// <summary>Вернуть Dictionary в пул. Автоматически очищается.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Return(Dictionary<string, object>? dict)
    {
        if (dict is null) return;
        // Не возвращаем слишком большие словари (защита от bloat)
        if (dict.Count > 32) return;

        // CAS-резервирование слота: не превышаем MaxPoolSize даже при гонке.
        while (true)
        {
            var current = Volatile.Read(ref _poolSize);
            if (current >= MaxPoolSize) return;
            if (Interlocked.CompareExchange(ref _poolSize, current + 1, current) == current)
                break;
        }

        dict.Clear();
        _pool.Add(dict);
    }

    /// <summary>
    /// Scope для автоматического возврата в пул через using.
    /// Используется в BuildAriaAttributes():
    ///   using var handle = ComponentDictionaryPool.RentScoped(out var dict);
    /// </summary>
    public static PoolHandle RentScoped(out Dictionary<string, object> dict)
    {
        dict = Rent();
        return new PoolHandle(dict);
    }

    public readonly struct PoolHandle : IDisposable
    {
        private readonly Dictionary<string, object>? _dict;
        internal PoolHandle(Dictionary<string, object> dict) => _dict = dict;
        public void Dispose() => Return(_dict);
    }
}

/// <summary>
/// Пул массивов для временных вычислений в компонентах.
/// Использует встроенный ArrayPool&lt;T&gt; из System.Buffers.
/// </summary>
public static class ComponentArrayPool
{
    public static T[] Rent<T>(int minimumLength)
        => System.Buffers.ArrayPool<T>.Shared.Rent(minimumLength);

    public static void Return<T>(T[] array, bool clearArray = false)
        => System.Buffers.ArrayPool<T>.Shared.Return(array, clearArray);
}