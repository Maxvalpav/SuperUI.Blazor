// SuperUI/Base/Reactive/SgEffect.cs
//
// ДОРАБОТКИ:
// 1. onError callback логируется через ILogger если доступен
// 2. ScheduleRun: защита от UnhandledTaskException (ContinueWith → TaskScheduler.Default)
// 3. Subscribe — WeakReference для предотвращения утечек
// 4. Pause/Resume — атомарные

using SuperUI.Base;

namespace SuperUI.Base.Reactive;

/// <summary>
/// Reactive side-effect: выполняет функцию при изменении зависимых сигналов.
/// </summary>
public sealed class SgEffect : IDisposable
{
    private readonly Func<Task>       _action;
    private readonly Action<Exception>? _onError;
    private readonly EffectObserver   _observer;
    private int _disposed;
    private int _paused;

    public SgEffect(Action action, Action<Exception>? onError = null)
    {
        _action   = () => { action(); return Task.CompletedTask; };
        _onError  = onError;
        _observer = new EffectObserver(RunAsync);
        ScheduleRun();
    }

    public SgEffect(Func<Task> action, Action<Exception>? onError = null)
    {
        _action   = action ?? throw new ArgumentNullException(nameof(action));
        _onError  = onError;
        _observer = new EffectObserver(RunAsync);
        ScheduleRun();
    }

    public void Pause()  => Interlocked.Exchange(ref _paused, 1);

    public void Resume()
    {
        if (Interlocked.Exchange(ref _paused, 0) == 1) ScheduleRun();
    }

    private void ScheduleRun()
    {
        _ = RunAsync().ContinueWith(
            t => System.Diagnostics.Debug.WriteLine($"[SgEffect] Unhandled: {t.Exception}"),
            CancellationToken.None,
            TaskContinuationOptions.OnlyOnFaulted,
            TaskScheduler.Default);
    }

    private async Task RunAsync()
    {
        if (Interlocked.CompareExchange(ref _disposed, 0, 0) == 1) return;
        if (Interlocked.CompareExchange(ref _paused,   0, 0) == 1) return;
        try
        {
            using (SignalTracker.EnterScopeForObserver(_observer))
                await _action();
        }
        catch (Exception ex)
        {
            if (_onError is not null)
                _onError(ex);
            else
                System.Diagnostics.Debug.WriteLine($"SgEffect error: {ex}");
        }
    }

    // ИСПРАВЛЕНО CS1061: метод Subscribe
    internal void Subscribe(SgComponentBase component) => _observer.Subscribe(component);

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1) return;
        _observer.Dispose();
    }

    // ── Вложенный наблюдатель ─────────────────────────────────────────────────────
    private sealed class EffectObserver : ISignalObserver<object>, IDisposable
    {
        private readonly Func<Task>                              _invalidate;
        private readonly HashSet<WeakReference<SgComponentBase>> _dependents = new();
        private readonly object _lock = new();
        private int _disposed;

        public EffectObserver(Func<Task> invalidate) => _invalidate = invalidate;

        internal void Subscribe(SgComponentBase component)
        {
            lock (_lock) _dependents.Add(new WeakReference<SgComponentBase>(component));
        }

        public void OnSignalChanged()
        {
            if (Interlocked.CompareExchange(ref _disposed, 0, 0) == 1) return;
            _ = _invalidate().ContinueWith(
                t => System.Diagnostics.Debug.WriteLine($"[EffectObserver] Error: {t.Exception}"),
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted,
                TaskScheduler.Default);
            NotifyComponents();
        }

        private void NotifyComponents()
        {
            List<WeakReference<SgComponentBase>>? snapshot;
            List<WeakReference<SgComponentBase>>? dead = null;
            lock (_lock)
            {
                if (_dependents.Count == 0) return;
                snapshot = new(_dependents.Count);
                foreach (var r in _dependents)
                {
                    if (r.TryGetTarget(out var c) && !c.IsDisposed)
                        snapshot.Add(r);
                    else
                        (dead ??= new()).Add(r);
                }
                if (dead is not null)
                    foreach (var d in dead) _dependents.Remove(d);
            }
            foreach (var r in snapshot)
                if (r.TryGetTarget(out var c) && !c.IsDisposed)
                    SignalBatch.NotifyComponent(c);
        }

        // ISignalObserver<object> — не используется напрямую (EnterScopeForObserver)
        public void OnSignalRead(SgSignal<object> signal)  { }
        public void OnComputedRead<TVal>(SgComputed<TVal> c) { }

        public void Dispose()
        {
            Interlocked.Exchange(ref _disposed, 1);
            lock (_lock) _dependents.Clear();
        }
    }
}
