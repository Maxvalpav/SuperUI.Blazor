// SuperUI/Base/Reactive/SgSelectSignal.cs — НОВЫЙ
// Что это: сигналы с LINQ-подобными операторами.
// Аналог: SELECT из Rx, computed из Vue, selector из Recoil.

using System.Diagnostics;

namespace SuperUI.Base.Reactive;

/// <summary>
/// Static factory для сигналов с LINQ-подобными операторами.
/// </summary>
public static class SgSelectSignal
{
    /// <summary>
    /// Select: трансформировать значение сигнала (map).
    /// </summary>
    public static SgComputed<TResult> Select<T, TResult>(
        this IReadOnlySignal<T> source,
        Func<T, TResult> selector,
        string? debugName = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(selector);

        return new SgComputed<TResult>(
            () => selector(source.Value),
            null,
            debugName);
    }

    /// <summary>
    /// Where: фильтрует сигнал. Возвращает последнее значение, прошедшее фильтр.
    /// </summary>
    public static SgComputed<T> Where<T>(
        this IReadOnlySignal<T> source,
        Func<T, bool> predicate,
        T fallback = default!,
        string? debugName = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(predicate);

        var lastValid = source.Value;
        if (!predicate(lastValid)) lastValid = fallback;
        var last = lastValid;

        return new SgComputed<T>(() =>
        {
            var current = source.Value;
            if (predicate(current)) last = current;
            return last;
        }, null, debugName);
    }

    /// <summary>
    /// Combine: объединить два сигнала.
    /// </summary>
    public static SgComputed<TResult> Combine<T1, T2, TResult>(
        IReadOnlySignal<T1> a,
        IReadOnlySignal<T2> b,
        Func<T1, T2, TResult> combinator,
        string? debugName = null)
    {
        return new SgComputed<TResult>(
            () => combinator(a.Value, b.Value),
            null,
            debugName);
    }

    /// <summary>
    /// Combine три сигнала.
    /// </summary>
    public static SgComputed<TResult> Combine<T1, T2, T3, TResult>(
        IReadOnlySignal<T1> a,
        IReadOnlySignal<T2> b,
        IReadOnlySignal<T3> c,
        Func<T1, T2, T3, TResult> combinator,
        string? debugName = null)
    {
        return new SgComputed<TResult>(
            () => combinator(a.Value, b.Value, c.Value),
            null,
            debugName);
    }

    /// <summary>
    /// DistinctUntilChanged: не уведомляет если значение не изменилось.
    /// </summary>
    public static SgComputed<T> DistinctUntilChanged<T>(
        this IReadOnlySignal<T> source,
        IEqualityComparer<T>? comparer = null,
        string? debugName = null)
    {
        return new SgComputed<T>(
            () => source.Value,
            comparer,
            debugName);
    }

    /// <summary>
    /// Debounce: задерживает обновление на указанный интервал.
    /// </summary>
    public static SgComputed<T> Debounce<T>(
        this IReadOnlySignal<T> source,
        int milliseconds,
        string? debugName = null)
    {
        var latestValue = source.Value;
        var lastEmitted = latestValue;
        long lastUpdate = Stopwatch.GetTimestamp();
        var frequency = TimeSpan.FromMilliseconds(milliseconds);

        // Подписываемся для отслеживания изменений
        source.Subscribe(new DebounceObserver<T>(() =>
        {
            var now = Stopwatch.GetTimestamp();
            var elapsed = Stopwatch.GetElapsedTime(lastUpdate, now);
            if (elapsed >= frequency)
            {
                lastEmitted = source.Value;
                lastUpdate = now;
            }

            latestValue = source.Value;
        }));

        return new SgComputed<T>(() => lastEmitted, null, debugName);
    }

    private sealed class DebounceObserver<T> : ISignalObserver
    {
        private readonly Action _onChanged;

        public DebounceObserver(Action onChanged) => _onChanged = onChanged;

        public void OnSignalChanged(ISgSignal signal) => _onChanged();
    }

    /// <summary>
    /// Throttle: не чаще чем раз в interval.
    /// </summary>
    public static SgComputed<T> Throttle<T>(
        this IReadOnlySignal<T> source,
        int milliseconds,
        string? debugName = null)
    {
        var lastEmitted = source.Value;
        long lastEmitTime = 0;
        var frequency = TimeSpan.FromMilliseconds(milliseconds);

        source.Subscribe(new ThrottleObserver<T>(() =>
        {
            var now = Stopwatch.GetTimestamp();
            var elapsed = Stopwatch.GetElapsedTime(lastEmitTime, now);
            if (elapsed >= frequency)
            {
                lastEmitted = source.Value;
                lastEmitTime = now;
            }
        }));

        return new SgComputed<T>(() => lastEmitted, null, debugName);
    }

    private sealed class ThrottleObserver<T> : ISignalObserver
    {
        private readonly Action _onChanged;

        public ThrottleObserver(Action onChanged) => _onChanged = onChanged;

        public void OnSignalChanged(ISgSignal signal) => _onChanged();
    }
}
