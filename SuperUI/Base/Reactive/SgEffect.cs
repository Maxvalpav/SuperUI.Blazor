// SuperUI/Base/Reactive/SgEffect.cs
using System;
using System.Collections.Generic;

namespace SuperUI.Base.Reactive;

/// <summary>
/// Represents a reactive side effect. Automatically tracks
/// signal dependencies and re-executes when they change.
/// </summary>
public sealed class SgEffect : IDisposable
{
    private readonly Action _effect;
    private readonly HashSet<ISgSignal> _dependencies = new();
    private bool _isDisposed;
    private bool _isRunning;

    public bool IsDisposed => _isDisposed;
    public bool IsRunning => _isRunning;
    public int DependencyCount => _dependencies.Count;

    public SgEffect(Action effect)
    {
        _effect = effect ?? throw new ArgumentNullException(nameof(effect));
    }

    /// <summary>Execute the effect and track dependencies.</summary>
    public void Run()
    {
        if (_isDisposed) return;
        _isRunning = true;
        try
        {
            SignalTracker.BeginTracking(this);
            _effect();
        }
        finally
        {
            SignalTracker.EndTracking();
            _isRunning = false;
        }
    }

    /// <summary>Add a signal dependency.</summary>
    internal void AddDependency(ISgSignal signal)
    {
        if (!_isDisposed)
            _dependencies.Add(signal);
    }

    /// <summary>Subscribe the effect to a signal, returning a disposable.</summary>
    public IDisposable Subscribe(ISgSignal signal)
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(SgEffect));

        signal.Subscribe(this);
        _dependencies.Add(signal);
        return new Subscription(() =>
        {
            signal.Unsubscribe(this);
            _dependencies.Remove(signal);
        });
    }

    /// <summary>Subscribe to multiple signals.</summary>
    public IDisposable SubscribeAll(params ISgSignal[] signals)
    {
        var disposables = new List<IDisposable>();
        foreach (var signal in signals)
            disposables.Add(Subscribe(signal));
        return new CompositeSubscription(disposables);
    }

    /// <summary>Notify that a dependency changed.</summary>
    internal void OnDependencyChanged()
    {
        Run();
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;
        foreach (var dep in _dependencies)
            dep.Unsubscribe(this);
        _dependencies.Clear();
        GC.SuppressFinalize(this);
    }
}

/// <summary>Simple subscription that calls an action on dispose.</summary>
internal sealed class Subscription : IDisposable
{
    private Action? _onDispose;

    public Subscription(Action onDispose) => _onDispose = onDispose;

    public void Dispose()
    {
        var action = _onDispose;
        _onDispose = null;
        action?.Invoke();
    }
}

internal sealed class CompositeSubscription : IDisposable
{
    private readonly List<IDisposable> _subscriptions;

    public CompositeSubscription(List<IDisposable> subscriptions) => _subscriptions = subscriptions;

    public void Dispose()
    {
        foreach (var sub in _subscriptions)
            sub.Dispose();
        _subscriptions.Clear();
    }
}
