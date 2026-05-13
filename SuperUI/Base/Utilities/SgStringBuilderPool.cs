// SuperUI/Base/Utilities/SgStringBuilderPool.cs — НОВЫЙ
//
// Что это: потокобезопасный пул StringBuilder'ов для минимизации аллокаций
// при построении CSS-классов, стилей, HTML-атрибутов.
// Существенно снижает GC pressure при частых рендерах.

using System.Runtime.CompilerServices;
using System.Collections.Concurrent;
using System.Text;

namespace SuperUI.Base.Utilities;

/// <summary>
/// Потокобезопасный пул StringBuilder'ов.
/// Используется SgCssBuilder, StyleBuilder, AriaBuilder.
/// 
/// В отличие от ArrayPool, возвращает готовый к использованию очищенный билдер.
/// </summary>
public static class SgStringBuilderPool
{
    private const int MaxPoolSize = 64;
    private const int DefaultCapacity = 256;
    private static readonly ConcurrentQueue<StringBuilder> _pool = new();
    private static int _count;

    /// <summary>
    /// Взять StringBuilder из пула (или создать новый).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static StringBuilder Rent(int minCapacity = DefaultCapacity)
    {
        if (_pool.TryDequeue(out var sb))
        {
            Interlocked.Decrement(ref _count);
            sb.Clear();

            if (sb.Capacity < minCapacity)
                sb.Capacity = minCapacity;

            return sb;
        }

        return new StringBuilder(minCapacity);
    }

    /// <summary>
    /// Вернуть StringBuilder в пул.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Return(StringBuilder sb)
    {
        if (sb is null) return;
        if (_count >= MaxPoolSize) return;

        sb.Clear();

        if (sb.Capacity > 1024)
            sb.Capacity = 1024; // ограничиваем размер в пуле

        _pool.Enqueue(sb);
        Interlocked.Increment(ref _count);
    }

    /// <summary>
    /// Build string using pooled StringBuilder, then return it.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Build(Action<StringBuilder> action)
    {
        var sb = Rent();
        try
        {
            action(sb);
            return sb.ToString();
        }
        finally
        {
            Return(sb);
        }
    }
}
