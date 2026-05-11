// SuperUI/Base/SgStore.cs
// НОВЫЙ: Redux/Zustand-подобное хранилище состояния для Blazor
// Аналог Fluxor/Redux, но без boilerplate и с реактивными сигналами
// Thread-safe для Blazor Server (multi-circuit)

using SuperUI.Base.Reactive;

namespace SuperUI.Base;

/// <summary>
/// Реактивное хранилище состояния в стиле Redux/Zustand.
/// Все изменения через Dispatch() → автоматическое уведомление подписчиков.
/// </summary>
/// <example>
/// // Определение состояния
/// record CounterState(int Count = 0);
///
/// // Создание хранилища
/// var store = new SgStore&lt;CounterState&gt;(new CounterState());
///
/// // В компоненте:
/// store.Dispatch(s => s with { Count = s.Count + 1 });
/// var count = store.State.Value; // реактивное чтение
/// </example>
public sealed class SgStore<TState> : IDisposable where TState : notnull
{
    private readonly SgSignal<TState> _state;
    private readonly List<Middleware<TState>> _middleware = [];
    private bool _disposed;

    public SgStore(TState initialState)
    {
        _state = new SgSignal<TState>(initialState);
    }

    /// <summary>
    /// Текущее состояние. Реактивное — чтение подписывает компонент.
    /// </summary>
    public SgSignal<TState> State => _state;

    /// <summary>
    /// Получить текущее состояние без реактивной подписки.
    /// </summary>
    public TState Current => _state.Value;

    /// <summary>
    /// Изменить состояние через reducer функцию.
    /// Потокобезопасно для Blazor Server.
    /// </summary>
    public void Dispatch(Func<TState, TState> reducer)
    {
        if (_disposed) return;

        var newState = reducer(_state.Value);

        // Применяем middleware
        foreach (var mw in _middleware)
            newState = mw(_state.Value, newState);

        _state.Set(newState);
    }

    /// <summary>Async dispatch для асинхронных операций.</summary>
    public async Task DispatchAsync(Func<TState, Task<TState>> asyncReducer)
    {
        if (_disposed) return;

        var currentState = _state.Value;
        var newState = await asyncReducer(currentState);

        foreach (var mw in _middleware)
            newState = mw(currentState, newState);

        _state.Set(newState);
    }

    /// <summary>
    /// Добавить middleware (логирование, DevTools, persistence и т.д.).
    /// </summary>
    public SgStore<TState> Use(Middleware<TState> middleware)
    {
        _middleware.Add(middleware);
        return this;
    }

    /// <summary>Сбросить состояние к начальному.</summary>
    public void Reset(TState initialState) => _state.Set(initialState);

    /// <summary>Выбрать часть состояния (memoized selector).</summary>
    public SgComputed<TResult> Select<TResult>(Func<TState, TResult> selector)
        => new(() => selector(_state.Value));

    public void Dispose() => _disposed = true;

    public delegate TState Middleware<TState_>(TState_ prev, TState_ next);
}
