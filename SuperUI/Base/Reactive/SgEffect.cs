// SuperUI/Base/Reactive/SgEffect.cs
// ИСПРАВЛЕНИЯ:
// 1. [CS1061 FIX] Subscribe(SgComponentBase) — метод добавлен
// 2. RunAsync — EnterScopeForObserver для отслеживания зависимостей
// 3. _disposed — Interlocked для Server thread-safety
// 4. EffectObserver теперь уведомляет компоненты-подписчики (RefreshAsync)
// 5. onError callback вместо Console.Error

using SuperUI.Base;

namespace SuperUI.Base.Reactive;

/// <summary>
/// Reactive side-effect: выполняет функцию при изменении зависимых сигналов.
/// Автоматически отслеживает SgSignal, прочитанные во время выполнения.
/// </summary>
public sealed class SgEffect : IDisposable
{
    private readonly Func<Task> _action;
    private readonly Action<Exception>? _onError;
    private readonly EffectObserver _observer;
    private int _disposed;
    private int _paused; // 0 = active, 1 = paused

    public SgEffect(Action action, Action<Exception>? onError = null)
    {
        _action  = () => { action(); return Task.CompletedTask; };
        _onError = onError;
        _observer = new EffectObserver(RunAsync);
        ScheduleRun();
    }

    public SgEffect(Func<Task> action, Action<Exception>? onError = null)
    {
        _action  = action ?? throw new ArgumentNullException(nameof(action));
        _onError = onError;
        _observer = new EffectObserver(RunAsync);
        ScheduleRun();
    }

    /// <summary>Приостановить выполнение эффекта.</summary>
    public void Pause() => Interlocked.Exchange(ref _paused, 1);

    /// <summary>
    /// Возобновить выполнение эффекта после паузы.
    /// Если эффект был приостановлен — перезапускает выполнение.
    /// </summary>
    public void Resume()
    {
        if (Interlocked.Exchange(ref _paused, 0) == 1)
            ScheduleRun();
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
        if (Interlocked.CompareExchange(ref _paused,   0, 0) == 1) return; // пропускаем при паузе
        try
        {
            using (SignalTracker.EnterScopeForObserver(_observer))
            {
                await _action();
            }
        }
        catch (Exception ex)
        {
            if (_onError is not null)
                _onError(ex);
            else
                System.Diagnostics.Debug.WriteLine($"SgEffect error: {ex}");
        }
    }

    // ── FIX CS1061 ───────────────────────────────────────────────────────────
    /// <summary>
    /// Подписать компонент: при изменении зависимых сигналов компонент
    /// автоматически получит RefreshAsync() → StateHasChanged().
    /// </summary>
    internal void Subscribe(SgComponentBase component)
        => _observer.Subscribe(component);
    // ─────────────────────────────────────────────────────────────────────────

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1) return;
        _observer.Dispose();
    }

    // ── Вложенный наблюдатель ─────────────────────────────────────────────────
    private sealed class EffectObserver : ISignalObserver, IDisposable
    {
        private readonly Func<Task> _invalidate;
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
                t => System.Diagnostics.Debug.WriteLine($"[EffectObserver] Invalidate error: {t.Exception}"),
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

        public void OnSignalRead<T>(SgSignal<T> signal)   { /* tracking done via EnterScopeForObserver */ }
        public void OnComputedRead<T>(SgComputed<T> c)    { }

        public void Dispose()
        {
            Interlocked.Exchange(ref _disposed, 1);
            lock (_lock) _dependents.Clear();
        }
    }
}