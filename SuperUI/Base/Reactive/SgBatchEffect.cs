// SuperUI/Base/Reactive/SgBatchEffect.cs
using System;
using System.Collections.Generic;

namespace SuperUI.Base.Reactive;

/// <summary>
/// Batched version of SgEffect that defers execution
/// to the end of the current synchronization context frame.
/// Reduces redundant re-executions for rapid signal changes.
/// </summary>
public sealed class SgBatchEffect : ISignalObserver, IDisposable
{
    private readonly Action _effect;
    private readonly List<IDisposable> _subscriptions = new();
    private bool _isDirty;
    private bool _isDisposed;
    private bool _isScheduled;

    public SgBatchEffect(Action effect)
    {
        _effect = effect ?? throw new ArgumentNullException(nameof(effect));
    }

    /// <summary>Mark the effect as dirty and schedule execution.</summary>
    public void MarkDirty()
    {
        if (_isDisposed) return;
        _isDirty = true;
        Schedule();
    }

    /// <summary>Called when a signal changes.</summary>
    public void OnSignalChanged(ISgSignal signal)
    {
        MarkDirty();
    }

    private void Schedule()
    {
        if (_isScheduled || _isDisposed) return;
        _isScheduled = true;

        // Schedule via timer to batch rapid changes
        _ = System.Threading.Tasks.Task.Delay(16) // ~60fps
            .ContinueWith(_ =>
            {
                _isScheduled = false;
                if (_isDirty && !_isDisposed)
                {
                    _isDirty = false;
                    try { _effect(); }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"SgBatchEffect error: {ex.Message}");
                    }
                }
            }, System.Threading.Tasks.TaskScheduler.Default);
    }

    /// <summary>Subscribe to a signal with batching.</summary>
    public IDisposable Subscribe(ISgSignal signal)
    {
        var sub = new Subscription(() => signal.Unsubscribe(this));
        _subscriptions.Add(sub);
        signal.Subscribe(this);
        return sub;
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;
        foreach (var sub in _subscriptions)
            sub.Dispose();
        _subscriptions.Clear();
    }
}
