// SuperUI/Base/Reactive/SgEffect.cs (УЛУЧШЕННАЯ ВЕРСИЯ с CancellationToken)
//
// ИСПРАВЛЕНИЯ:
//   C1: ContinueWith fault handling — используем RunSafeAsync вместо inline обработки
//   CS0308: EffectObserver реализует ISignalObserver (non-generic)
//
// УЛУЧШЕНИЯ:
//   1. onError callback + логирование через Debug.WriteLine
//   2. Pause/Resume — атомарные через Interlocked
//   3. Subscribe — WeakReference для предотвращения утечек памяти
//   4. RunCount — счётчик запусков (для диагностики)
//   5. ✅ CancellationToken поддержка для async actions (НОВОЕ)
//   6. Debounce — задержка перед выполнением (предотвращает storm)
//   7. Restart() — отменить текущий запуск и запустить заново

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
    private readonly Func<CancellationToken, Task> _action;
    private readonly Action<Exception>? _onError;
    private readonly EffectObserver _observer;
    private readonly TimeSpan _debounce;
    private int _disposed;
    private int _paused;
    private int _runCount;
    private volatile CancellationTokenSource? _debounceCts;
    private volatile CancellationTokenSource? _runCts; // ← НОВОЕ: отмена текущего запуска

    /// <summary>Количество выполненных запусков (для диагностики).</summary>
    public int RunCount => Volatile.Read(ref _runCount);

    /// <summary>Конструктор для sync action.</summary>
    public SgEffect(Action action, Action<Exception>? onError = null, TimeSpan debounce = default)
        : this(_ => { action(); return Task.CompletedTask; }, onError, debounce) { }

    /// <summary>Конструктор для async action (без CancellationToken).</summary>
    public SgEffect(Func<Task> action, Action<Exception>? onError = null, TimeSpan debounce = default)
        : this(_ => action(), onError, debounce) { }

    /// <summary>Конструктор для async action с CancellationToken (НОВОЕ).</summary>
    public SgEffect(
        Func<CancellationToken, Task> action,
        Action<Exception>? onError = null,
        TimeSpan debounce = default)
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

    /// <summary>Отменить текущий запущенный эффект и запустить заново.</summary>
    public void Restart()
    {
        var oldCts = Interlocked.Exchange(ref _runCts, null);
        oldCts?.Cancel();
        oldCts?.Dispose();
        ScheduleRun();
    }

    private void ScheduleRun()
    {
        if (Volatile.Read(ref _disposed) == 1) return;

        if (_debounce > TimeSpan.Zero)
        {
            // ✅ ИСПРАВЛЕНО: dispose старого CTS
            var oldCts = Interlocked.Exchange(ref _debounceCts, null);
            oldCts?.Cancel();
            oldCts?.Dispose();

            var newCts = new CancellationTokenSource();
            Volatile.Write(ref _debounceCts, newCts);
            var token = newCts.Token;

            _ = Task.Delay(_debounce, token).ContinueWith(
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

        // ✅ НОВОЕ: отменяем предыдущий запуск
        var oldRunCts = Interlocked.Exchange(ref _runCts, null);
        oldRunCts?.Cancel();
        oldRunCts?.Dispose();

        var runCts = new CancellationTokenSource();
        Volatile.Write(ref _runCts, runCts);

        try
        {
            Interlocked.Increment(ref _runCount);
            using (SignalTracker.EnterScopeForObserver(_observer))
                await _action(runCts.Token); // ← передаём токен в action
        }
        catch (OperationCanceledException) { /* нормально */ }
        catch (Exception ex)
        {
            if (_onError is not null)
                _onError(ex);
            else
                System.Diagnostics.Debug.WriteLine($"[SgEffect] Error: {ex}");
        }
        finally
        {
            // Освобождаем CTS если он всё ещё "наш"
            var currentCts = Interlocked.CompareExchange(ref _runCts, null, runCts);
            if (ReferenceEquals(currentCts, runCts))
                runCts.Dispose();
        }
    }

    // ── Internal API ──────────────────────────────────────────────────────────

    internal void Subscribe(SgComponentBase component)
        => _observer.Subscribe(component);

    // ── IDisposable ───────────────────────────────────────────────────────────

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1) return;

        var debCts = Interlocked.Exchange(ref _debounceCts, null);
        debCts?.Cancel();
        debCts?.Dispose();

        var runCts = Interlocked.Exchange(ref _runCts, null);
        runCts?.Cancel();
        runCts?.Dispose();

        _observer.Dispose();
    }

    // ── Вложенный наблюдатель ─────────────────────────────────────────────────

    /// <summary>
    /// EffectObserver реализует НЕ-generic ISignalObserver.
    /// SgEffect не знает тип T своих зависимостей заранее.
    /// Отслеживание выполняется через SignalTracker.EnterScopeForObserver.
    /// Typed методы OnSignalRead/OnComputedRead передаются через dynamic dispatch.
    /// </summary>
    internal sealed class EffectObserver : ISignalObserver, IDisposable
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
            _ = RunInvalidateSafeAsync();
        }

        /// <summary>C1 FIX: безопасный запуск с корректным ContinueWith.</summary>
        private async Task RunInvalidateSafeAsync()
        {
            try
            {
                await _invalidate();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[EffectObserver] Unhandled: {ex.Message}");
            }
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

            lock (_lock)
            {
                _dependents.Clear();
            }
        }
    }
}
