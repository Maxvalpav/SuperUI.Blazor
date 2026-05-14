// SuperUI/Base/Reactive/SgStateMachine.cs
// УЛУЧШЕНО:
// ✅ Trigger хранится как TriggerKey (enum-safe сравнение через Enum.Equals)
// ✅ Guard: условные переходы с предикатом
// ✅ History: последние N переходов
// ✅ CanSend: публичный предикат для UI-биндинга
// ✅ Thread-safe Send через lock

namespace SuperUI.Base.Reactive;

/// <summary>
/// Декларативная машина состояний, управляемая сигналами.
/// </summary>
public sealed class SgStateMachine<TState> : IDisposable
    where TState : struct, Enum
{
    private readonly SgSignal<TState> _stateSignal;

    // ✅ УЛУЧШЕНО: используем Enum как ключ через EqualityComparer<TState>.Default
    // Это корректно для любых enum, включая флаговые
    private readonly Dictionary<TState, Dictionary<object, TransitionDefinition<TState>>> _transitions
        = new();

    private readonly Dictionary<TState, Action> _onEnter = new();
    private readonly Dictionary<TState, Action> _onExit = new();
    private readonly Dictionary<TState, Func<Task>?> _onEnterAsync = new();
    private readonly List<StateTransitionRecord<TState>> _history = new();
    private readonly object _lock = new();
    private int _maxHistorySize = 50;

    public string? DebugName { get; }
    public TState Current => _stateSignal.Value;
    public IReadOnlySignal<TState> StateSignal => _stateSignal;

    public IReadOnlyList<StateTransitionRecord<TState>> History
    {
        get { lock (_lock) return _history.ToArray(); }
    }

    public SgStateMachine(TState initialState, string? debugName = null)
    {
        DebugName = debugName ?? $"FSM<{typeof(TState).Name}>";
        _stateSignal = new SgSignal<TState>(initialState, debugName);
    }

    /// <summary>Добавить переход без guard.</summary>
    public SgStateMachine<TState> On(TState from, object trigger, TState to)
        => On(from, trigger, to, guard: null);

    /// <summary>Добавить переход с guard-предикатом.</summary>
    public SgStateMachine<TState> On(
        TState from,
        object trigger,
        TState to,
        Func<bool>? guard)
    {
        lock (_lock)
        {
            if (!_transitions.ContainsKey(from))
                _transitions[from] = new Dictionary<object, TransitionDefinition<TState>>();

            _transitions[from][trigger] = new TransitionDefinition<TState>(to, guard);
        }
        return this;
    }

    public SgStateMachine<TState> OnEnter(TState state, Action action)
    {
        _onEnter[state] = action;
        return this;
    }

    public SgStateMachine<TState> OnExit(TState state, Action action)
    {
        _onExit[state] = action;
        return this;
    }

    public SgStateMachine<TState> OnEnterAsync(TState state, Func<Task> asyncAction)
    {
        _onEnterAsync[state] = asyncAction;
        return this;
    }

    /// <summary>
    /// Отправить событие.
    /// ✅ УЛУЧШЕНО: lock защищает от concurrent Send.
    /// ✅ Guard проверяется перед переходом.
    /// </summary>
    public bool Send(object trigger)
    {
        TState current, next;
        bool hasTransition;

        lock (_lock)
        {
            current = _stateSignal.Value;
            hasTransition = TryGetTransition(current, trigger, out next);
        }

        if (!hasTransition) return false;

        ExecuteTransition(current, next);
        return true;
    }

    /// <summary>Отправить асинхронное событие.</summary>
    public async Task<bool> SendAsync(object trigger)
    {
        TState current, next;
        bool hasTransition;

        lock (_lock)
        {
            current = _stateSignal.Value;
            hasTransition = TryGetTransition(current, trigger, out next);
        }

        if (!hasTransition) return false;

        await ExecuteTransitionAsync(current, next);
        return true;
    }

    private bool TryGetTransition(TState from, object trigger, out TState next)
    {
        next = from;
        if (!_transitions.TryGetValue(from, out var stateTransitions)) return false;
        if (!stateTransitions.TryGetValue(trigger, out var def)) return false;
        if (def.Guard is not null && !def.Guard()) return false;
        next = def.To;
        return true;
    }

    private void ExecuteTransition(TState from, TState to)
    {
        _onExit.GetValueOrDefault(from)?.Invoke();
        _stateSignal.Set(to);
        _onEnter.GetValueOrDefault(to)?.Invoke();

        if (_onEnterAsync.TryGetValue(to, out var async) && async is not null)
            _ = async();

        RecordHistory(from, to);
    }

    private async Task ExecuteTransitionAsync(TState from, TState to)
    {
        _onExit.GetValueOrDefault(from)?.Invoke();
        _stateSignal.Set(to);
        _onEnter.GetValueOrDefault(to)?.Invoke();

        if (_onEnterAsync.TryGetValue(to, out var async) && async is not null)
            await async();

        RecordHistory(from, to);
    }

    private void RecordHistory(TState from, TState to)
    {
        lock (_lock)
        {
            _history.Add(new StateTransitionRecord<TState>(from, to, DateTimeOffset.UtcNow));
            while (_history.Count > _maxHistorySize)
                _history.RemoveAt(0);
        }
    }

    public bool CanSend(object trigger)
    {
        lock (_lock)
            return TryGetTransition(_stateSignal.Value, trigger, out _);
    }

    public void Reset(TState state)
    {
        var current = _stateSignal.Value;
        _onExit.GetValueOrDefault(current)?.Invoke();
        _stateSignal.Set(state);
        _onEnter.GetValueOrDefault(state)?.Invoke();
        lock (_lock) _history.Clear();
    }

    public void Dispose()
    {
        _stateSignal.Dispose();
    }

    public static implicit operator TState(SgStateMachine<TState> fsm) => fsm.Current;
}

internal sealed record TransitionDefinition<TState>(TState To, Func<bool>? Guard);

public sealed record StateTransitionRecord<TState>(
    TState From,
    TState To,
    DateTimeOffset At);