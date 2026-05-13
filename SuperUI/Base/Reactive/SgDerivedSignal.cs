// SuperUI/Base/Reactive/SgDerivedSignal.cs — НОВЫЙ
//
// Что это: расширенные операции над сигналами: map, filter, combine, merge.
// Аналог Rx operators, но для сигналов Blazor.
// Нет аналога в MudBlazor / Radzen / AntBlazor.

namespace SuperUI.Base.Reactive;

/// <summary>
/// Static factory для производных сигналов.
/// Функциональные комбинаторы в стиле Rx.
/// </summary>
public static class SgDerivedSignal
{
    /// <summary>
    /// Map: трансформировать значение сигнала.
    /// SgSignal&lt;int&gt; → SgComputed&lt;string&gt;
    /// </summary>
    public static SgComputed<TResult> Map<TSource, TResult>(IReadOnlySignal<TSource> source,
        Func<TSource, TResult> selector,
        string? debugName = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(selector);

        return new SgComputed<TResult>(() => selector(source.Value), null, debugName);
    }

    /// <summary>
    /// Filter: сигнал, который обновляется только когда предикат возвращает true.
    /// Возвращает последнее значение, прошедшее фильтр.
    /// </summary>
    public static SgComputed<T> Filter<T>(IReadOnlySignal<T> source,
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
            if (predicate(current))
                last = current;
            return last;
        }, null, debugName);
    }

    /// <summary>
    /// Combine: объединить два сигнала в один через комбинатор.
    /// </summary>
    public static SgComputed<TResult> Combine<TA, TB, TResult>(IReadOnlySignal<TA> a,
        IReadOnlySignal<TB> b,
        Func<TA, TB, TResult> combinator,
        string? debugName = null)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);
        ArgumentNullException.ThrowIfNull(combinator);

        return new SgComputed<TResult>(() => combinator(a.Value, b.Value), null, debugName);
    }

    /// <summary>
    /// Combine три сигнала.
    /// </summary>
    public static SgComputed<TResult> Combine<TA, TB, TC, TResult>(IReadOnlySignal<TA> a,
        IReadOnlySignal<TB> b,
        IReadOnlySignal<TC> c,
        Func<TA, TB, TC, TResult> combinator,
        string? debugName = null)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);
        ArgumentNullException.ThrowIfNull(c);
        ArgumentNullException.ThrowIfNull(combinator);

        return new SgComputed<TResult>(() => combinator(a.Value, b.Value, c.Value), null, debugName);
    }

    /// <summary>
    /// Merge: первый изменившийся сигнал задаёт значение.
    /// Приоритет: кто последний изменился — тот и значение.
    /// </summary>
    public static SgSignal<T> Merge<T>(IReadOnlySignal<T> a,
        IReadOnlySignal<T> b,
        string? debugName = null)
    {
        var merged = new SgSignal<T>(a.Value, debugName ?? "Merge");
        var handler = new MergeHandler<T>(merged);
        a.Subscribe(handler);
        b.Subscribe(handler);
        return merged;
    }

    private sealed class MergeHandler<T> : ISignalObserver
    {
        private readonly SgSignal<T> _target;

        public MergeHandler(SgSignal<T> target) => _target = target;

        public void OnSignalChanged(ISgSignal signal)
        {
            if (signal is IReadOnlySignal<T> typed)
                _target.Set(typed.Value);
        }
    }

    /// <summary>
    /// When: сигнал, который становится true когда условие выполнено (однократно).
    /// </summary>
    public static SgComputed<bool> When<T>(IReadOnlySignal<T> source,
        Func<T, bool> predicate,
        string? debugName = null)
    {
        var triggered = false;

        return new SgComputed<bool>(() =>
        {
            if (triggered) return true;
            if (predicate(source.Value))
                triggered = true;
            return triggered;
        }, null, debugName);
    }
}
