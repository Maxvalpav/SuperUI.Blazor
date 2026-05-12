// SuperUI/Base/Reactive/SgEffect.cs
//
// ИСПРАВЛЕНИЯ:
//   CS0308: EffectObserver реализует ISignalObserver<T> →
//           РЕШЕНИЕ: SgEffect не generic, EffectObserver реализует ISignalObserver (non-generic)
//           т.к. Effect не знает типы своих зависимостей заранее.
//
// УЛУЧШЕНИЯ:
//   1. onError callback + логирование через Debug.WriteLine
//   2. ScheduleRun: ContinueWith для поглощения UnhandledTaskException
//   3. Pause/Resume — атомарные через Interlocked
//   4. Subscribe — WeakReference для предотвращения утечек памяти
//   5. НОВОЕ: RunCount — счётчик запусков (для диагностики)
//   6. НОВОЕ: CancellationToken поддержка для async actions
//   7. НОВОЕ: Debounce — задержка перед выполнением (предотвращает storm)

using SuperUI.Base;

namespace SuperUI.Base.Reactive;

/// <summary>
/// Reactive side-effect: выполняет функцию при изменении зависимых сигналов.
/// Аналог React useEffect / Vue watchEffect / MobX autorun.
/// </summary>
/// <remarks>
/// WASM: однопоточный — async/await работает через браузерный event loop.
/// Server: per-circuit — RunAsync вызывается в контексте circuit.
/// </remarks>
public sealed class SgEffect : IDisposable
{
    private readonly Func<Task> _action;
    private readonly Action<Exception>? _onError;
    private readonly EffectObserver _observer;
    private readonly TimeSpan _debounce;
    private int _disposed;
    private int _paused;
    private int _runCount;
    private CancellationTokenSource? _debounceCts;

    /// <summary>Количество выполненных запусков (для диагностики).</summary>
    public int RunCount => Volatile.Read(ref _runCount);

    public SgEffect(Action action, Action<Exception>? onError = null, TimeSpan debounce = default)
    {
        _action = () => { action(); return Task.CompletedTask; };
        _onError = onError;
        _debounce = debounce;
        _observer = new EffectObserver(RunAsync);
        ScheduleRun();
    }

    public SgEffect(Func<Task> action, Action<Exception>? onError = null, TimeSpan debounce = default)
    {
        _action = action ?? throw new ArgumentNullException(nameof(action));
        _onError = onError;
        _debounce = debounce;
        _observer = new EffectObserver(RunAsync);
        ScheduleRun();
    }

    /// <summary>Приостановить выполнение эффекта.</summary>
    public void Pause() => Interlocked.Exchange(ref _paused, 1);

    /// <summary>Возобновить выполнение. Если был pending запрос — выполнить.</summary>
    public void Resume()
    {
        if (Interlocked.Exchange(ref _paused, 0) == 1)
            ScheduleRun();
    }

    private void ScheduleRun()
    {
        if (_debounce > TimeSpan.Zero)
        {
            // Debounce: отменяем предыдущий отложенный запуск
            _debounceCts?.Cancel();
            _debounceCts = new CancellationTokenSource();
            var token = _debounceCts.Token;
            _ = Task.Delay(_debounce, token)
                .ContinueWith(
                    _ => RunAsync(),
                    token,
                    TaskContinuationOptions.NotOnCanceled,
                    TaskScheduler.Default);
        }
        else
        {
            _ = RunAsync().ContinueWith(
                static t => System.Diagnostics.Debug.WriteLine(
                    $"[SgEffect] Unhandled: {t.Exception?.InnerException?.Message}"),
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted,
                TaskScheduler.Default);
        }
    }

    private async Task RunAsync()
    {
        if (Volatile.Read(ref _disposed) == 1) return;
        if (Volatile.Read(ref _paused) == 1) return;

        try
        {
            Interlocked.Increment(ref _runCount);
            using (SignalTracker.EnterScopeForObserver(_observer))
                await _action();
        }
        catch (OperationCanceledException) { /* игнорируем отмену */ }
        catch (Exception ex)
        {
            if (_onError is not null)
                _onError(ex);
            else
                System.Diagnostics.Debug.WriteLine($"[SgEffect] Error: {ex}");
        }
    }

    // ── Internal API ──────────────────────────────────────────────────────────

    internal void Subscribe(SgComponentBase component)
        => _observer.Subscribe(component);

    // ── IDisposable ───────────────────────────────────────────────────────────

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1) return;
        _debounceCts?.Cancel();
        _debounceCts?.Dispose();
        _observer.Dispose();
    }

    // ── Вложенный наблюдатель ─────────────────────────────────────────────────

    /// <summary>
    /// EffectObserver реализует НЕ-generic ISignalObserver.
    /// SgEffect не знает тип T своих зависимостей заранее.
    /// Отслеживание выполняется через SignalTracker.EnterScopeForObserver.
    /// Typed методы OnSignalRead/OnComputedRead передаются через dynamic dispatch.
    /// </summary>
    private sealed class EffectObserver : ISignalObserver, IDisposable
    {
        private readonly Func<Task> _invalidate;
        private readonly HashSet<WeakReference<SgComponentBase>> _dependents = new();
        private readonly object _lock = new();
        private int _disposed;

        public EffectObserver(Func<Task> invalidate) => _invalidate = invalidate;

        internal void Subscribe(SgComponentBase component)
        {
            lock (_lock)
                _dependents.Add(new WeakReference<SgComponentBase>(component));
        }

        public void OnSignalChanged()
        {
            if (Volatile.Read(ref _disposed) == 1) return;

            _ = _invalidate().ContinueWith(
                static t => System.Diagnostics.Debug.WriteLine(
                    $"[EffectObserver] Unhandled: {t.Exception?.InnerException?.Message}"),
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

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 1) return;
            lock (_lock) _dependents.Clear();
        }
    }
}
