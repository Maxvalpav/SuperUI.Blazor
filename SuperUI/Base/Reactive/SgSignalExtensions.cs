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
        this ISgSignal<T> source,
        Func<T, TResult> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);
        return new SgComputed<TResult>(() => selector(source.Value));
    }

    /// <summary>
    /// Toggle bool-сигнала.
    /// </summary>
    public static void Toggle(this ISgSignal<bool> signal)
        => signal.Set(!signal.Value);

    /// <summary>
    /// Increment int-сигнала.
    /// </summary>
    public static void Increment(this ISgSignal<int> signal, int step = 1)
        => signal.Set(signal.Value + step);

    /// <summary>
    /// Decrement int-сигнала.
    /// </summary>
    public static void Decrement(this ISgSignal<int> signal, int step = 1)
        => signal.Set(signal.Value - step);

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
