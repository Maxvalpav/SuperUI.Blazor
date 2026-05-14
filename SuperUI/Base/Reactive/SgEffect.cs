// SuperUI/Base/Reactive/SgEffect.cs
// ИСПРАВЛЕНИЯ v2:
// ✅ L6: _cts пересоздаётся при каждом Run(), старый Dispose-ится
// ✅ C3: CanExecute проверяет disposed перед Run
// ✅ Async fire-and-forget: ошибки всегда передаются в _onError

using System.Runtime.CompilerServices;

namespace SuperUI.Base.Reactive;

/// <summary>
/// Реактивный побочный эффект.
/// Автоматически отслеживает зависимости от сигналов и перезапускается при их изменении.
/// </summary>
public sealed class SgEffect : ISignalObserver, IDisposable
{
    private readonly Delegate _effect;
    private readonly Action<Exception>? _onError;
    private readonly object _depLock = new();
    private readonly HashSet<ISgSignal> _dependencies = new(ReferenceEqualityComparer.Instance);
    private volatile int _disposed;
    private volatile int _isRunning;
    private volatile int _pendingRerun;

    // ✅ FIX L6: CTS создаётся/пересоздаётся при каждом Run, глобальный — только для Dispose
    private readonly CancellationTokenSource _globalCts = new();

    public bool IsDisposed => Volatile.Read(ref _disposed) == 1;
    public bool IsRunning => Volatile.Read(ref _isRunning) == 1;

    public int DependencyCount
    {
        get { lock (_depLock) return _dependencies.Count; }
    }

    /// <summary>Создать синхронный эффект.</summary>
    public SgEffect(Action effect, Action<Exception>? onError = null)
    {
        _effect = effect ?? throw new ArgumentNullException(nameof(effect));
        _onError = onError;
    }

    /// <summary>Создать асинхронный эффект.</summary>
    public SgEffect(Func<Task> effect, Action<Exception>? onError = null)
    {
        _effect = effect ?? throw new ArgumentNullException(nameof(effect));
        _onError = onError;
    }

    /// <summary>Создать асинхронный эффект с CancellationToken.</summary>
    public SgEffect(Func<CancellationToken, Task> effect, Action<Exception>? onError = null)
    {
        _effect = effect ?? throw new ArgumentNullException(nameof(effect));
        _onError = onError;
    }

    /// <summary>
    /// Выполнить эффект и отследить зависимости.
    /// Thread-safe. При рекурсивном вызове помечает pending-rerun.
    /// </summary>
    public void Run()
    {
        if (IsDisposed) return;
        if (_globalCts.IsCancellationRequested) return;

        if (Interlocked.CompareExchange(ref _isRunning, 1, 0) == 1)
        {
            Volatile.Write(ref _pendingRerun, 1);
            return;
        }

        try
        {
            do
            {
                Volatile.Write(ref _pendingRerun, 0);
                ClearDependencies();

                // ✅ FIX L6: создаём локальный CTS для этого запуска,
                // linked с глобальным (для отмены при Dispose)
                using var runCts = CancellationTokenSource.CreateLinkedTokenSource(_globalCts.Token);

                using (SgReactiveComponentBase.EnterScope(new EffectSignalObserver(this)))
                {
                    if (_effect is Action syncAction)
                    {
                        syncAction();
                    }
                    else if (_effect is Func<Task> asyncAction)
                    {
                        // ✅ FIX: fire-and-forget с явной обработкой ошибок
                        _ = RunAsyncSafe(() => asyncAction(), runCts.Token);
                    }
                    else if (_effect is Func<CancellationToken, Task> asyncWithToken)
                    {
                        _ = RunAsyncSafe(() => asyncWithToken(runCts.Token), runCts.Token);
                    }
                }
            }
            while (Volatile.Read(ref _pendingRerun) == 1 && !IsDisposed);
        }
        catch (Exception ex) when (!IsDisposed)
        {
            if (_onError is not null)
                _onError(ex);
            else
                System.Diagnostics.Debug.WriteLine($"[SgEffect] Unhandled error: {ex}");
        }
        finally
        {
            Interlocked.Exchange(ref _isRunning, 0);
        }
    }

    private async Task RunAsyncSafe(Func<Task> asyncAction, CancellationToken ct)
    {
        try
        {
            await asyncAction().ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // Нормальное завершение при dispose
        }
        catch (Exception ex) when (!IsDisposed)
        {
            if (_onError is not null)
                _onError(ex);
            else
                System.Diagnostics.Debug.WriteLine($"[SgEffect] Async error: {ex}");
        }
    }

    private void ClearDependencies()
    {
        ISgSignal[] old;
        lock (_depLock)
        {
            old = _dependencies.ToArray();
            _dependencies.Clear();
        }
        foreach (var dep in old)
            dep.Unsubscribe(this);
    }

    internal void AddDependency(ISgSignal signal)
    {
        if (IsDisposed) return;
        lock (_depLock)
        {
            if (_dependencies.Add(signal))
                signal.Subscribe(this);
        }
    }

    public void OnSignalChanged(ISgSignal signal)
    {
        if (!IsDisposed && !IsRunning)
            Run();
    }

    public IDisposable Subscribe(ISgSignal signal)
    {
        ObjectDisposedException.ThrowIf(IsDisposed, nameof(SgEffect));
        signal.Subscribe(this);
        lock (_depLock) _dependencies.Add(signal);
        return new Subscription(() =>
        {
            signal.Unsubscribe(this);
            lock (_depLock) _dependencies.Remove(signal);
        });
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1) return;
        _globalCts.Cancel();
        _globalCts.Dispose();
        ClearDependencies();
        GC.SuppressFinalize(this);
    }

    private sealed class EffectSignalObserver : ISignalObserver, ISignalTrackingObserver
    {
        private readonly SgEffect _effect;
        public EffectSignalObserver(SgEffect effect) => _effect = effect;
        public void OnSignalChanged(ISgSignal signal) => _effect.OnSignalChanged(signal);
        public void OnSignalRead(ISgSignal signal) => _effect.AddDependency(signal);
    }
}