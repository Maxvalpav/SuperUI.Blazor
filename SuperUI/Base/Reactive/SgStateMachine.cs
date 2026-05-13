// SuperUI/Base/Reactive/SgStateMachine.cs
// УНИКАЛЬНЫЙ КЛАСС — аналог XState, но на сигналах Blazor.

namespace SuperUI.Base.Reactive;

/// <summary>
/// Декларативная машина состояний, управляемая сигналами.
/// Аналог XState / Robot, но с нативной интеграцией в Blazor RenderTree.
/// 
/// Использование:
/// <code>
/// var fsm = new SgStateMachine&lt;MyState&gt;(MyState.Idle, "my-fsm")
///     .On(MyState.Idle, MyEvent.Start, MyState.Loading)
///     .On(MyState.Loading, MyEvent.Success, MyState.Loaded)
///     .On(MyState.Loading, MyEvent.Error, MyState.Error)
///     .On(MyState.Error, MyEvent.Retry, MyState.Loading)
///     .OnEnter(MyState.Loading, () => _ = LoadDataAsync())
///     .OnExit(MyState.Loading, () => cts.Cancel());
/// 
/// // В рендере:
/// @switch (fsm.Current)
/// { ... }
/// </code>
/// </summary>
public sealed class SgStateMachine<TState> : IDisposable
    where TState : struct, Enum
{
    private readonly SgSignal<TState> _stateSignal;
    private readonly Dictionary<TState, Dictionary<object, TState>> _transitions = new();
    private readonly Dictionary<TState, Action?> _onEnter = new();
    private readonly Dictionary<TState, Action?> _onExit = new();
    private readonly Dictionary<TState, Func<Task>?> _onEnterAsync = new();
    private readonly List<IDisposable> _disposables = new();

    public string? DebugName { get; }
    public TState Current => _stateSignal.Value;
    public IReadOnlySignal<TState> StateSignal => _stateSignal;

    public SgStateMachine(TState initialState, string? debugName = null)
    {
        DebugName = debugName ?? $"FSM<{typeof(TState).Name}>";
        _stateSignal = new SgSignal<TState>(initialState, debugName);
    }

    /// <summary>Добавить синхронный переход.</summary>
    public SgStateMachine<TState> On(TState from, object trigger, TState to)
    {
        if (!_transitions.ContainsKey(from))
            _transitions[from] = new Dictionary<object, TState>();
        _transitions[from][trigger] = to;
        return this;
    }

    /// <summary>Действие при входе в состояние.</summary>
    public SgStateMachine<TState> OnEnter(TState state, Action action)
    {
        _onEnter[state] = action;
        return this;
    }

    /// <summary>Действие при выходе из состояния.</summary>
    public SgStateMachine<TState> OnExit(TState state, Action action)
    {
        _onExit[state] = action;
        return this;
    }

    /// <summary>Асинхронное действие при входе.</summary>
    public SgStateMachine<TState> OnEnterAsync(TState state, Func<Task> asyncAction)
    {
        _onEnterAsync[state] = asyncAction;
        return this;
    }

    /// <summary>Отправить событие (триггер).</summary>
    public bool Send(object trigger)
    {
        var current = _stateSignal.Value;
        if (!_transitions.TryGetValue(current, out var stateTransitions))
            return false;
        if (!stateTransitions.TryGetValue(trigger, out var next))
            return false;

        // Выход из текущего
        if (_onExit.TryGetValue(current, out var exitAction))
            exitAction?.Invoke();

        _stateSignal.Set(next);

        // Вход в новое
        if (_onEnter.TryGetValue(next, out var enterAction))
            enterAction?.Invoke();

        if (_onEnterAsync.TryGetValue(next, out var asyncAction) && asyncAction is not null)
            _ = asyncAction();

        return true;
    }

    /// <summary>Отправить асинхронное событие.</summary>
    public async Task<bool> SendAsync(object trigger)
    {
        var current = _stateSignal.Value;
        if (!_transitions.TryGetValue(current, out var stateTransitions))
            return false;
        if (!stateTransitions.TryGetValue(trigger, out var next))
            return false;

        if (_onExit.TryGetValue(current, out var exitAction))
            exitAction?.Invoke();

        _stateSignal.Set(next);

        if (_onEnter.TryGetValue(next, out var enterAction))
            enterAction?.Invoke();

        if (_onEnterAsync.TryGetValue(next, out var asyncAction) && asyncAction is not null)
            await asyncAction();

        return true;
    }

    /// <summary>Можно ли отправить событие из текущего состояния?</summary>
    public bool CanSend(object trigger)
    {
        return _transitions.TryGetValue(_stateSignal.Value, out var stateTransitions)
            && stateTransitions.ContainsKey(trigger);
    }

    /// <summary>Сбросить в начальное состояние.</summary>
    public void Reset(TState state)
    {
        var current = _stateSignal.Value;
        if (_onExit.TryGetValue(current, out var exitAction))
            exitAction?.Invoke();

        _stateSignal.Set(state);

        if (_onEnter.TryGetValue(state, out var enterAction))
            enterAction?.Invoke();
    }

    public void Dispose()
    {
        _stateSignal.Dispose();
        foreach (var d in _disposables) d.Dispose();
        _disposables.Clear();
    }

    public static implicit operator TState(SgStateMachine<TState> fsm) => fsm.Current;
}
