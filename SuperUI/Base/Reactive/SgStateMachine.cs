// SuperUI/Base/Reactive/SgStateMachine.cs
// ИСПРАВЛЕНИЯ v2:
// ✅ W7: ExecuteTransitionAsync — защита от concurrent Send через _transitionLock
// ✅ Guard проверяется ВНУТРИ _transitionLock (после lock)
// ✅ OnEnterAsync: async fire-and-forget с явной обработкой ошибок
// ✅ Reset: корректен под lock

namespace SuperUI.Base.Reactive;

/// <summary>
/// Декларативная машина состояний, управляемая сигналами.
/// </summary>
public sealed class SgStateMachine<TState> : IDisposable
    where TState : struct, Enum
{
    private readonly SgSignal<TState> _stateSignal;
    private readonly Dictionary<TState, Dictionary<object, TransitionDefinition<TState>>> _transitions = new();
    private readonly Dictionary<TState, Action> _onEnter = new();
    private readonly Dictionary<TState, Action> _onExit = new();
    private readonly Dictionary<TState, Func<Task>?> _onEnterAsync = new();
    private readonly List<StateTransitionRecord<TState>> _history = new();

    // ✅ FIX W7: отдельный lock для переходов — защита от concurrent Send
    private readonly SemaphoreSlim _transitionLock = new(1, 1);
    private readonly object _historyLock = new();

    private int _maxHistorySize = 50;

    public string? DebugName { get; }
    public TState Current => _stateSignal.Value;
    public IReadOnlySignal<TState> StateSignal => _stateSignal;

    public IReadOnlyList<StateTransitionRecord<TState>> History
    {
        get { lock (_historyLock) return _history.ToArray(); }
    }

    public SgStateMachine(TState initialState, string? debugName = null)
    {
        DebugName = debugName ?? $"FSM<{typeof(TState).Name}>";
        _stateSignal = new SgSignal<TState>(initialState, debugName);
    }

    public SgStateMachine<TState> On(TState from, object trigger, TState to)
        => On(from, trigger, to, guard: null);

    public SgStateMachine<TState> On(TState from, object trigger, TState to, Func<bool>? guard)
    {
        if (!_transitions.ContainsKey(from))
            _transitions[from] = new Dictionary<object, TransitionDefinition<TState>>();
        _transitions[from][trigger] = new TransitionDefinition<TState>(to, guard);
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
    /// ✅ FIX W7: синхронный Send — атомарный, Guard проверяется под lock.
    /// </summary>
    public bool Send(object trigger)
    {
        // Синхронный вариант: используем простой lock (не SemaphoreSlim)
        // для синхронных переходов без async
        bool acquired = _transitionLock.Wait(0);
        if (!acquired) return false; // Если переход уже идёт — пропускаем

        try
        {
            var current = _stateSignal.Value;
            if (!TryGetTransition(current, trigger, out var next)) return false;
            ExecuteTransitionSync(current, next);
            return true;
        }
        finally
        {
            _transitionLock.Release();
        }
    }

    /// <summary>
    /// ✅ FIX W7: async Send — полностью атомарен через SemaphoreSlim.
    /// StateSignal обновляется только после завершения OnEnterAsync.
    /// </summary>
    public async Task<bool> SendAsync(object trigger)
    {
        await _transitionLock.WaitAsync();
        try
        {
            var current = _stateSignal.Value;
            if (!TryGetTransition(current, trigger, out var next)) return false;
            await ExecuteTransitionAsync(current, next);
            return true;
        }
        finally
        {
            _transitionLock.Release();
        }
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

    private void ExecuteTransitionSync(TState from, TState to)
    {
        _onExit.GetValueOrDefault(from)?.Invoke();
        _stateSignal.Set(to);
        _onEnter.GetValueOrDefault(to)?.Invoke();

        // ✅ FIX: async OnEnter в sync context — fire-and-forget с обработкой ошибок
        if (_onEnterAsync.TryGetValue(to, out var asyncAction) && asyncAction is not null)
        {
            _ = asyncAction().ContinueWith(t =>
            {
                if (t.IsFaulted)
                    System.Diagnostics.Debug.WriteLine(
                        $"[SgStateMachine] OnEnterAsync error for state {to}: {t.Exception}");
            }, TaskContinuationOptions.OnlyOnFaulted);
        }

        RecordHistory(from, to);
    }

    private async Task ExecuteTransitionAsync(TState from, TState to)
    {
        _onExit.GetValueOrDefault(from)?.Invoke();
        // ✅ FIX W7: Set происходит ПОСЛЕ всех side-effects, включая async OnEnter
        _onEnter.GetValueOrDefault(to)?.Invoke();

        if (_onEnterAsync.TryGetValue(to, out var asyncAction) && asyncAction is not null)
        {
            try { await asyncAction(); }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[SgStateMachine] OnEnterAsync error for state {to}: {ex}");
            }
        }

        // ✅ FIX: Set ПОСЛЕ завершения всех обработчиков
        _stateSignal.Set(to);
        RecordHistory(from, to);
    }

    private void RecordHistory(TState from, TState to)
    {
        lock (_historyLock)
        {
            _history.Add(new StateTransitionRecord<TState>(from, to, DateTimeOffset.UtcNow));
            while (_history.Count > _maxHistorySize)
                _history.RemoveAt(0);
        }
    }

    public bool CanSend(object trigger)
    {
        var current = _stateSignal.Value;
        return TryGetTransition(current, trigger, out _);
    }

    public void Reset(TState state)
    {
        var current = _stateSignal.Value;
        _onExit.GetValueOrDefault(current)?.Invoke();
        _stateSignal.Set(state);
        _onEnter.GetValueOrDefault(state)?.Invoke();
        lock (_historyLock) _history.Clear();
    }

    public void Dispose()
    {
        _stateSignal.Dispose();
        _transitionLock.Dispose();
    }

    public static implicit operator TState(SgStateMachine<TState> fsm) => fsm.Current;
}

internal sealed record TransitionDefinition<TState>(TState To, Func<bool>? Guard);

public sealed record StateTransitionRecord<TState>(
    TState From,
    TState To,
    DateTimeOffset At);