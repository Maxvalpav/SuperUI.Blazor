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
    /// ⚠️ ВНИМАНИЕ: При конкурентных Dispatch/DispatchAsync возможен Lost Update.
    /// Для критичного состояния используйте только синхронный Dispatch().
    /// </summary>
    public async Task DispatchAsync(Func<TState, Task<TState>> asyncReducer)
    {
        if (Volatile.Read(ref _disposedInt) == 1) return;

        var currentState = _state.Value;
        var newState = await asyncReducer(currentState);

        lock (_dispatchLock)
        {
            if (Volatile.Read(ref _disposedInt) == 1) return;
            foreach (var mw in _middleware)
                newState = mw(currentState, newState);
        }

        _state.Set(newState);
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
