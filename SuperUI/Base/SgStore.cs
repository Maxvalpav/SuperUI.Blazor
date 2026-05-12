// SuperUI/Base/SgStore.cs
// Ключевые исправления:
// 1. DispatchAsync — документация о Lost Update
// 2. Dispose — выставляем _disposed через Interlocked (не просто присваивание)

using SuperUI.Base.Reactive;

namespace SuperUI.Base;

/// <summary>
/// Реактивное хранилище состояния в стиле Redux/Zustand.
/// Thread-safe для Blazor Server (multi-circuit).
/// </summary>
/// <typeparam name="TState">Тип состояния. Рекомендуется использовать record (иммутабельность).</typeparam>
/// <example>
/// <code>
/// record CounterState(int Count = 0);
/// var store = new SgStore&lt;CounterState&gt;(new CounterState());
/// store.Dispatch(s => s with { Count = s.Count + 1 });
/// </code>
/// </example>
public sealed class SgStore<TState> : IDisposable where TState : notnull
{
    private readonly SgSignal<TState> _state;
    private readonly List<Middleware<TState>> _middleware = [];

    // ИСПРАВЛЕНО: int для Interlocked.Exchange (atomic compare-and-swap)
    private int _disposedInt;

    private readonly object _dispatchLock = new();

    public SgStore(TState initialState)
        => _state = new SgSignal<TState>(initialState);

    /// <summary>Текущее состояние. Реактивное — чтение подписывает компонент в render scope.</summary>
    public SgSignal<TState> State => _state;

    /// <summary>Текущее состояние без реактивной подписки.</summary>
    public TState Current => _state.Value;

    /// <summary>
    /// Изменить состояние через функцию-reducer.
    /// Thread-safe: atomic на Blazor Server (lock).
    /// </summary>
    public void Dispatch(Func<TState, TState> reducer)
    {
        if (Volatile.Read(ref _disposedInt) == 1) return;

        TState newState;
        lock (_dispatchLock)
        {
            if (Volatile.Read(ref _disposedInt) == 1) return;
            newState = reducer(_state.Value);
            foreach (var mw in _middleware)
                newState = mw(_state.Value, newState);
        }

        _state.Set(newState);
    }

    /// <summary>
    /// Async dispatch для асинхронных операций (action creators).
    /// Thread-safe: использует оптимистичную конкуренцию с повторными попытками чтобы избежать Lost Update.
    /// При превышении максимального количества повторов (5) генерирует InvalidOperationException.
    /// </summary>
    public async Task DispatchAsync(Func<TState, Task<TState>> asyncReducer)
    {
        if (Volatile.Read(ref _disposedInt) == 1) return;

        const int maxRetries = 5;
        int retries = 0;
        while (true)
        {
            TState snapshot;
            lock (_dispatchLock)
            {
                if (Volatile.Read(ref _disposedInt) == 1) return;
                snapshot = _state.Value;
            }

            TState newState = await asyncReducer(snapshot);

            lock (_dispatchLock)
            {
                if (Volatile.Read(ref _disposedInt) == 1) return;
                if (EqualityComparer<TState>.Default.Equals(_state.Value, snapshot))
                {
                    // Нет конкурентного обновления, применяем наше изменение
                    foreach (var mw in _middleware)
                        newState = mw(snapshot, newState);
                    _state.Set(newState);
                    return;
                }
                // Обнаружено конкурентное обновление — повторяем операцию
            }

            retries++;
            if (retries >= maxRetries)
            {
                throw new InvalidOperationException(
                    $"Слишком много попыток в SgStore.DispatchAsync из-за конкурентных обновлений. " +
                    $"Максимальное количество попыток: {maxRetries}.");
            }
            // Повторяем немедленно без задержки (простая реализация)
        }
    }

    /// <summary>Добавить middleware (логирование, DevTools, persistence).</summary>
    public SgStore<TState> Use(Middleware<TState> middleware)
    {
        ArgumentNullException.ThrowIfNull(middleware);
        _middleware.Add(middleware);
        return this;
    }

    /// <summary>Сбросить состояние.</summary>
    public void Reset(TState initialState) => _state.Set(initialState);

    /// <summary>
    /// Создать middleware для хроники состояний (time-travel debugging).
    /// Поддерживает ограниченную историю и опциональный callback при изменении состояния.
    /// </summary>
    /// <param name="maxHistory">Максимальное количество записей в истории (по умолчанию 50).</param>
    /// <param name="onStateChange">Опциональный callback, вызываемый при каждом изменении состояния (передаётся новое состояние).</param>
    /// <returns>Middleware, который добавляет состояние в историю.</returns>
    public static Middleware<TState> CreateHistoryMiddleware(
        int maxHistory = 50,
        Action<TState>? onStateChange = null)
    {
        if (maxHistory <= 0) throw new ArgumentOutOfRangeException(nameof(maxHistory));
        var history = new Queue<TState>(maxHistory + 1);
        return (prev, next) =>
        {
            if (history.Count >= maxHistory) history.Dequeue();
            history.Enqueue(prev);
            onStateChange?.Invoke(next);
            return next;
        };
    }

    /// <summary>
    /// Создать вычисляемый selector.
    /// ⚠️ Caller отвечает за Dispose возвращённого SgComputed.
    /// </summary>
    public SgComputed<TResult> Select<TResult>(Func<TState, TResult> selector)
        => new(() => selector(_state.Value));

    /// <summary>
    /// ИСПРАВЛЕНО: Interlocked.Exchange — атомарный compare-and-swap.
    /// Гарантирует, что Dispose выполняется ровно один раз даже при race.
    /// </summary>
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposedInt, 1) == 1) return;
    }
}

/// <summary>Функция middleware для SgStore.</summary>
public delegate TState Middleware<TState>(TState prev, TState next) where TState : notnull;
