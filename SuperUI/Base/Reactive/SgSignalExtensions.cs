// SuperUI/Base/Reactive/SgSignalExtensions.cs
// НОВЫЙ ФАЙЛ: extension методы для SgSignal<T>

namespace SuperUI.Base.Reactive;

/// <summary>
/// Extension методы для SgSignal&lt;T&gt; и SgComputed&lt;T&gt;.
/// </summary>
public static class SgSignalExtensions
{
    /// <summary>
    /// Создать derived сигнал (map).
    /// При изменении source — автоматически пересчитывается mapped.
    /// </summary>
    public static SgComputed<TResult> Select<T, TResult>(
        this SgSignal<T> source,
        Func<T, TResult> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);
        return new SgComputed<TResult>(() => selector(source.Value));
    }

    /// <summary>
    /// Создать filtered computed (вернуть null если не прошло фильтр).
    /// </summary>
    public static SgComputed<T?> Where<T>(
        this SgSignal<T> source,
        Func<T, bool> predicate)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return new SgComputed<T?>(() =>
        {
            var val = source.Value;
            return predicate(val) ? val : null;
        });
    }

    /// <summary>
    /// Объединить два сигнала в один computed.
    /// </summary>
    public static SgComputed<TResult> Combine<T1, T2, TResult>(
        SgSignal<T1> signal1,
        SgSignal<T2> signal2,
        Func<T1, T2, TResult> combiner)
    {
        ArgumentNullException.ThrowIfNull(combiner);
        return new SgComputed<TResult>(() => combiner(signal1.Value, signal2.Value));
    }

    /// <summary>
    /// Подписаться на изменения с авто-отпиской через CancellationToken.
    /// Полезно для Server-side с circuit disconnect.
    /// </summary>
    public static IDisposable Subscribe<T>(
        this SgSignal<T> signal,
        Action<T> onNext,
        CancellationToken cancellationToken = default)
    {
        var subscription = signal.AsObservable()
            .Subscribe(new SgObserver<T>(onNext));

        if (cancellationToken.CanBeCanceled)
            cancellationToken.Register(subscription.Dispose);

        return subscription;
    }

    /// <summary>
    /// Toggle bool-сигнала.
    /// </summary>
    public static void Toggle(this SgSignal<bool> signal)
        => signal.Update(v => !v);

    /// <summary>
    /// Increment int-сигнала.
    /// </summary>
    public static void Increment(this SgSignal<int> signal, int step = 1)
        => signal.Update(v => v + step);

    /// <summary>
    /// Decrement int-сигнала.
    /// </summary>
    public static void Decrement(this SgSignal<int> signal, int step = 1)
        => signal.Update(v => v - step);

    /// <summary>
    /// Добавить элемент в список (ImmutableList-like pattern).
    /// </summary>
    public static void Add<T>(this SgSignal<IReadOnlyList<T>> signal, T item)
        => signal.Update(list =>
        {
            var newList = new List<T>(list) { item };
            return newList.AsReadOnly();
        });

    /// <summary>
    /// Убрать элемент из списка.
    /// </summary>
    public static void Remove<T>(this SgSignal<IReadOnlyList<T>> signal, T item)
        => signal.Update(list =>
        {
            var newList = new List<T>(list);
            newList.Remove(item);
            return newList.AsReadOnly();
        });
}
