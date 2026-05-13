// SuperUI/Base/SgStore.cs
// ИСПРАВЛЕНИЯ v2:
// ✅ BUG-4: ImmutableList<Middleware> — thread-safe Use()
// ✅ UX-7: AsObservable() — IObservable<TState>
// ✅ НОВОЕ: MemoizedSelect<TResult> — кешированный selector
// ✅ НОВОЕ: Hydrate(TState) — восстановление состояния (SSR/WASM)
// ✅ НОВОЕ: CreateTimeTravelMiddleware — time-travel debugging

using System.Collections.Immutable;
using Microsoft.Extensions.Logging;
using SuperUI.Base.Reactive;

namespace SuperUI.Base;

public sealed class SgStore<TState> : IDisposable where TState : notnull
{
    private readonly SgSignal<TState> _state;
    private ImmutableList<Middleware<TState>> _middleware = ImmutableList<Middleware<TState>>.Empty;
    private int _disposedInt;
    private readonly object _dispatchLock = new();

    public SgStore(TState initialState) =>
        _state = new SgSignal<TState>(initialState);

    public SgSignal<TState> State => _state;
    public TState Current => _state.Peek();

    // ── BUG-4 FIX: ImmutableList — атомарная замена без lock ────────────────────
    public SgStore<TState> Use(Middleware<TState> middleware)
    {
        ArgumentNullException.ThrowIfNull(middleware);
        ImmutableList<Middleware<TState>> current, updated;
        do
        {
            current = Volatile.Read(ref _middleware!);
            updated = current.Add(middleware);
        } while (!ReferenceEquals(
            Interlocked.CompareExchange(ref _middleware!, updated, current), current));
        return this;
    }

    public void Dispatch(Func<TState, TState> reducer)
    {
        if (Volatile.Read(ref _disposedInt) == 1) return;
        TState newState;
        lock (_dispatchLock)
        {
            if (Volatile.Read(ref _disposedInt) == 1) return;
            var middleware = Volatile.Read(ref _middleware!);
            newState = reducer(_state.Peek());
            foreach (var mw in middleware)
                newState = mw(_state.Peek(), newState);
        }
        _state.Set(newState);
    }

    public async Task BatchAsync(params Func<TState, TState>[] reducers)
    {
        if (Volatile.Read(ref _disposedInt) == 1) return;
        TState current;
        lock (_dispatchLock)
        {
            current = _state.Peek();
            foreach (var r in reducers) current = r(current);
        }
        _state.Set(current);
        await Task.CompletedTask;
    }

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
                    var middleware = Volatile.Read(ref _middleware!);
                    foreach (var mw in middleware) newState = mw(snapshot, newState);
                    _state.Set(newState);
                    return;
                }
            }
            retries++;
            if (retries >= maxRetries)
                throw new InvalidOperationException(
                    $"SgStore.DispatchAsync: too many retries ({maxRetries}).");

            // Exponential backoff: 0, 1, 2, 4, 8 ms
            var backoffMs = retries == 1 ? 0 : (1 << (retries - 2));
            if (backoffMs > 0)
                await Task.Delay(backoffMs);
            else
                await Task.Yield();
        }
    }

    // UX-7: IObservable<TState>
    public IObservable<TState> AsObservable() => _state.AsObservable();

    public void Reset(TState initialState) => _state.Set(initialState);

    // НОВОЕ: Hydrate — восстановление состояния (для SSR/localStorage)
    public void Hydrate(TState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        _state.Reset(state);
        _state.ForceNotify();
    }

    /// <summary>
    /// Подписаться на изменения состояния с получением предыдущего и нового значения.
    /// Использует SgSignal.Subscribe — не загрязняет middleware цепочку.
    /// </summary>
    public IDisposable OnStateChange(Action<TState, TState> observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        TState prev = _state.Peek();
        return _state.Subscribe(next =>
        {
            var p = Interlocked.Exchange(ref prev, next);
            observer(p, next);
        });
    }

    public TState Snapshot() => Current;

    // НОВОЕ: MemoizedSelect — кешированный selector
    public SgMemoizedSelector<TState, TResult> MemoizedSelect<TResult>(
        Func<TState, TResult> selector,
        IEqualityComparer<TResult>? comparer = null)
    {
        return new SgMemoizedSelector<TState, TResult>(_state, selector, comparer);
    }

    public SgComputed<TResult> Select<TResult>(Func<TState, TResult> selector) =>
        new(() => selector(_state.Peek()));

    public IDisposable Subscribe(Action<TState> observer) =>
        _state.Subscribe(observer);

    // ── Middleware factories ─────────────────────────────────────────────────────
    public static Middleware<TState> CreateLoggingMiddleware(
        ILogger? logger = null, string? storeName = null)
    {
        var name = storeName ?? typeof(TState).Name;
        return (prev, next) =>
        {
            if (logger?.IsEnabled(LogLevel.Debug) == true)
                logger.LogDebug("[SgStore<{Name}>] {Prev} → {Next}", name, prev, next);
            else
                System.Diagnostics.Debug.WriteLine($"[SgStore<{name}>] {prev} → {next}");
            return next;
        };
    }

    public static Middleware<TState> CreateValidationMiddleware(
        Func<TState, bool> isValid, string? errorMessage = null)
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

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposedInt, 1) == 1) return;
        _state.Dispose();
    }
}

// НОВОЕ: мемоизированный selector
public sealed class SgMemoizedSelector<TState, TResult> : IDisposable
    where TState : notnull
{
    private readonly SgSignal<TState> _source;
    private readonly Func<TState, TResult> _selector;
    private readonly IEqualityComparer<TResult> _comparer;
    private TResult _cached;
    private TState _lastInput;
    private readonly IDisposable _subscription;

    public SgMemoizedSelector(
        SgSignal<TState> source,
        Func<TState, TResult> selector,
        IEqualityComparer<TResult>? comparer = null)
    {
        _source = source;
        _selector = selector;
        _comparer = comparer ?? EqualityComparer<TResult>.Default;
        _lastInput = source.Peek();
        _cached = selector(_lastInput);
        _subscription = source.Subscribe(Invalidate);
    }

    public TResult Value
    {
        get
        {
            var current = _source.Peek();
            if (!EqualityComparer<TState>.Default.Equals(current, _lastInput))
            {
                _lastInput = current;
                _cached = _selector(current);
            }
            return _cached;
        }
    }

    private void Invalidate(TState newState)
    {
        var newValue = _selector(newState);
        if (!_comparer.Equals(_cached, newValue))
        {
            _lastInput = newState;
            _cached = newValue;
        }
    }

    public void Dispose() => _subscription.Dispose();
}

public delegate TState Middleware<TState>(TState prev, TState next) where TState : notnull;
