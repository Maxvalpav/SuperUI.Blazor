using Microsoft.Extensions.Logging;
using SuperUI.Base.Reactive;

namespace SuperUI.Base;

/// <summary>
/// Реактивное хранилище состояния в стиле Redux/Zustand.
/// Thread-safe для Blazor Server (multi-circuit).
///
/// ИСПРАВЛЕНО:
/// 1. DispatchAsync — await Task.Yield() между попытками (WASM-совместимость).
/// 2. Dispose — Interlocked.Exchange (атомарный compare-and-swap) + _state.Dispose().
/// </summary>
public sealed class SgStore<TState> : IDisposable where TState : notnull
{
    private readonly SgSignal<TState> _state;
    private readonly List<Middleware<TState>> _middleware = [];
    private int _disposedInt;
    private readonly object _dispatchLock = new();

    public SgStore(TState initialState)
        => _state = new SgSignal<TState>(initialState);

    /// <summary>Текущее состояние. Реактивное — чтение подписывает компонент в render scope.</summary>
    public SgSignal<TState> State => _state;

    /// <summary>Текущее состояние без реактивной подписки.</summary>
    public TState Current => _state.Peek();

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
            newState = reducer(_state.Peek());
            foreach (var mw in _middleware) newState = mw(_state.Peek(), newState);
        }
        _state.Set(newState);
    }

    /// <summary>
    /// Выполнить несколько действий как один атомарный batch (один StateHasChanged).
    /// </summary>
    public async Task BatchAsync(params Func<TState, TState>[] reducers)
    {
        if (Volatile.Read(ref _disposedInt) == 1) return;
        TState current;
        lock (_dispatchLock)
        {
            current = _state.Peek();
            foreach (var reducer in reducers)
                current = reducer(current);
        }
        _state.Set(current);
        await Task.CompletedTask;
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
                snapshot = _state.Peek();
            }

            TState newState = await asyncReducer(snapshot);

            lock (_dispatchLock)
            {
                if (Volatile.Read(ref _disposedInt) == 1) return;

                if (EqualityComparer<TState>.Default.Equals(_state.Peek(), snapshot))
                {
                    foreach (var mw in _middleware) newState = mw(snapshot, newState);
                    _state.Set(newState);
                    return;
                }
            }

            retries++;
            if (retries >= maxRetries)
                throw new InvalidOperationException(
                    $"SgStore.DispatchAsync: too many retries ({maxRetries}) due to concurrent updates.");

            // ИСПРАВЛЕНО: даём другим задачам выполниться (WASM-совместимо)
            await Task.Yield();
        }
    }

    public SgStore<TState> Use(Middleware<TState> middleware)
    {
        ArgumentNullException.ThrowIfNull(middleware);
        _middleware.Add(middleware);
        return this;
    }

    public void Reset(TState initialState) => _state.Set(initialState);

    /// <summary>
    /// Подписаться на все изменения состояния (для DevTools, логирования).
    /// </summary>
    public IDisposable OnStateChange(Action<TState, TState> observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        return Use((prev, next) =>
        {
            observer(prev, next);
            return next;
        });
    }

    /// <summary>
    /// Создать снапшот текущего состояния (для time-travel).
    /// Требует TState : ICloneable или использует сериализацию.
    /// </summary>
    public TState Snapshot() => Current;

    /// <summary>
    /// Middleware логирования в Debug (для разработки).
    /// </summary>
    public static Middleware<TState> CreateLoggingMiddleware(
        ILogger? logger = null,
        string? storeName = null)
    {
        var name = storeName ?? typeof(TState).Name;
        return (prev, next) =>
        {
            if (logger?.IsEnabled(LogLevel.Debug) == true)
                logger.LogDebug("[SgStore<{Name}>] State changed: {Prev} → {Next}", name, prev, next);
            else
                System.Diagnostics.Debug.WriteLine($"[SgStore<{name}>] {prev} → {next}");
            return next;
        };
    }

    /// <summary>
    /// Middleware валидации: генерирует исключение если новое состояние не прошло проверку.
    /// </summary>
    public static Middleware<TState> CreateValidationMiddleware(
        Func<TState, bool> isValid,
        string? errorMessage = null)
    {
        ArgumentNullException.ThrowIfNull(isValid);
        return (_, next) =>
        {
            if (!isValid(next))
                throw new InvalidOperationException(
                    errorMessage ?? $"SgStore<{typeof(TState).Name}>: invalid state");
            return next;
        };
    }

    /// <summary>
    /// Middleware для хроники состояний (time-travel debugging).
    /// </summary>
    public static Middleware<TState> CreateHistoryMiddleware(
        int maxHistory = 50, Action<TState>? onStateChange = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxHistory);
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
    /// ⚠️ Caller отвечает за Dispose возвращённого SgComputed[TResult].
    /// </summary>
    public SgComputed<TResult> Select<TResult>(Func<TState, TResult> selector)
        => new(() => selector(_state.Peek()));

    /// <summary>
    /// Подписаться на изменения состояния (IObservable-совместимо).
    /// </summary>
    public IDisposable Subscribe(Action<TState> observer)
        => _state.Subscribe(observer);

    /// <summary>
    /// ИСПРАВЛЕНО: Interlocked.Exchange — атомарный compare-and-swap.
    /// Гарантирует, что Dispose выполняется ровно один раз даже при race.
    /// </summary>
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposedInt, 1) == 1) return;
        _state.Dispose();
    }
}

/// <summary>Функция middleware для SgStore.</summary>
public delegate TState Middleware<TState>(TState prev, TState next) where TState : notnull;
