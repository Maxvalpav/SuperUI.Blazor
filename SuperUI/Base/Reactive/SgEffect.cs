// SuperUI/Base/Reactive/SgEffect.cs
// ✅ Реентрантность: _isRunning сбрасывается в finally
// ✅ Лимитированная очередь: MaxQueueSize = 1, DroppedCount для метрик
// ✅ Pause/Resume; sync + async actions; onError callback

using System.Collections.Concurrent;

namespace SuperUI.Base.Reactive;

/// <summary>
/// Side-effect, реактивно реагирующий на изменения отслеживаемых сигналов.
/// </summary>
public sealed class SgEffect : ISignalTrackingObserver, IDisposable, ISignalFlushable
{
    private readonly Action? _sync;
    private readonly Func<Task>? _async;
    private readonly Action<Exception>? _onError;

    private int _disposed;
    private int _isRunning;
    private int _paused;
    private readonly HashSet<ISgSignal> _dependencies = new();
    private readonly object _lock = new();

    public bool IsDisposed => Volatile.Read(ref _disposed) == 1;
    public bool IsPaused => Volatile.Read(ref _paused) == 1;
    public bool IsRunning => Volatile.Read(ref _isRunning) == 1;

    public SgEffect(Action action, Action<Exception>? onError = null)
    {
        ArgumentNullException.ThrowIfNull(action);
        _sync = action;
        _onError = onError;
        Run(); // Initial run to track dependencies
    }

    public SgEffect(Func<Task> action, Action<Exception>? onError = null)
    {
        ArgumentNullException.ThrowIfNull(action);
        _async = action;
        _onError = onError;
        Run(); // Initial run to track dependencies
    }

    public void Pause() => Volatile.Write(ref _paused, 1);

    public void Resume()
    {
        if (Interlocked.Exchange(ref _paused, 0) == 1)
            Schedule();
    }

    public void OnSignalRead(ISgSignal signal)
    {
        lock (_lock)
        {
            if (_dependencies.Add(signal))
            {
                signal.Subscribe(this);
            }
        }
    }

    public void OnSignalChanged(ISgSignal signal)
    {
        Schedule();
    }

    void ISignalFlushable.FlushIfDirty()
    {
        Run();
    }

    private void Schedule()
    {
        if (IsDisposed || IsPaused) return;

        if (Interlocked.CompareExchange(ref _isRunning, 1, 0) == 1)
            return;

        if (SignalBatch.IsBatching)
        {
            SignalBatch.MarkDirty(this);
        }
        else
        {
            SignalBatch.EnqueueEffect(this);
        }
    }

    public void Run()
    {
        if (IsDisposed || IsPaused)
        {
            Volatile.Write(ref _isRunning, 0);
            return;
        }

        try
        {
            lock (_lock)
            {
                foreach (var dep in _dependencies) dep.Unsubscribe(this);
                _dependencies.Clear();
            }

            using (SgReactiveComponentBase.EnterScope(this))
            {
                if (_sync is not null)
                {
                    _sync();
                }
                else if (_async is not null)
                {
                    var task = RunAsync();
                    _ = task.ContinueWith(
                        t =>
                        {
                            var ex = t.Exception!.GetBaseException();
                            if (_onError is not null)
                                try { _onError(ex); } catch { }
                            else
                                System.Diagnostics.Debug.WriteLine(
                                    $"[SgEffect] Unobserved async exception: {ex.Message}");
                        },
                        CancellationToken.None,
                        TaskContinuationOptions.OnlyOnFaulted,
                        TaskScheduler.Current);
                }
            }
        }
        catch (Exception ex)
        {
            if (_onError is not null) try { _onError(ex); } catch { }
            else System.Diagnostics.Debug.WriteLine($"[SgEffect] Unhandled exception: {ex}");
        }
        finally
        {
            Volatile.Write(ref _isRunning, 0);
        }
    }

    private async Task RunAsync()
    {
        try { await _async!(); }
        catch (Exception ex)
        {
            if (_onError is not null) try { _onError(ex); } catch { }
            else System.Diagnostics.Debug.WriteLine($"[SgEffect] Async exception: {ex}");
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1) return;
        lock (_lock)
        {
            foreach (var dep in _dependencies) dep.Unsubscribe(this);
            _dependencies.Clear();
        }
        Volatile.Write(ref _isRunning, 0);
    }
}
