// SuperUI/Base/Reactive/SgEffect.cs
// ИСПРАВЛЕНО:
// ✅ ЛОГИКА: async эффекты — ошибки передаются в _onError через ContinueWith
// ✅ ЛОГИКА: зависимости сбрасываются перед каждым Run() — нет "застрявших" подписок
// ✅ ЛОГИКА: SignalTracker интегрирован правильно через SgReactiveComponentBase.EnterScope

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SuperUI.Base.Reactive;

/// <summary>
/// Представляет реактивный побочный эффект.
/// Автоматически отслеживает зависимости и перезапускается при изменении сигналов.
/// </summary>
public sealed class SgEffect : ISignalObserver, ISignalTrackingObserver, IDisposable
{
    private readonly Delegate _effect;
    private readonly Action<Exception>? _onError;
    private readonly object _depLock = new();
    private readonly HashSet<ISgSignal> _dependencies = new(ReferenceEqualityComparer.Instance);
    private volatile bool _isDisposed;
    private int _isRunning; // Interlocked guard

    public bool IsDisposed => _isDisposed;
    public bool IsRunning => _isRunning == 1;
    public int DependencyCount { get { lock (_depLock) return _dependencies.Count; } }

    public SgEffect(Action effect, Action<Exception>? onError = null)
    {
        _effect = effect ?? throw new ArgumentNullException(nameof(effect));
        _onError = onError;
    }

    public SgEffect(Func<Task> effect, Action<Exception>? onError = null)
    {
        _effect = effect ?? throw new ArgumentNullException(nameof(effect));
        _onError = onError;
    }

    /// <summary>
    /// Выполняет эффект и отслеживает зависимости.
    /// ✅ FIX: зависимости сбрасываются перед каждым вызовом
    /// </summary>
    public void Run()
    {
        if (_isDisposed) return;

        if (Interlocked.CompareExchange(ref _isRunning, 1, 0) != 0) return; // уже выполняется

        try
        {
            // ✅ FIX: очищаем старые зависимости перед перезапуском
            ClearDependencies();

            using var scope = SgReactiveComponentBase.EnterScope(this);

            if (_effect is Action syncAction)
            {
                syncAction();
            }
            else if (_effect is Func<Task> asyncAction)
            {
                // ✅ FIX: async — обрабатываем исключение через ContinueWith
                var task = asyncAction();

                if (!task.IsCompleted)
                {
                    task.ContinueWith(t => _onError?.Invoke(t.Exception!.InnerException ?? t.Exception),
                        CancellationToken.None,
                        TaskContinuationOptions.OnlyOnFaulted,
                        TaskScheduler.Default);
                }
                else if (task.IsFaulted && _onError is not null)
                {
                    _onError(task.Exception!.InnerException ?? task.Exception);
                }
            }
        }
        catch (Exception ex)
        {
            _onError?.Invoke(ex);
        }
        finally
        {
            Interlocked.Exchange(ref _isRunning, 0);
        }
    }

    private void ClearDependencies()
    {
        ISgSignal[] old;
        lock (_depLock)
        {
            old = _dependencies.Count > 0 ? _dependencies.ToArray() : Array.Empty<ISgSignal>();
            _dependencies.Clear();
        }

        foreach (var dep in old)
            dep.Unsubscribe(this);
    }

    // ISignalTrackingObserver — вызывается при чтении сигнала в scope
    public void OnSignalRead(ISgSignal signal)
    {
        if (_isDisposed) return;

        lock (_depLock)
        {
            if (_dependencies.Add(signal))
                signal.Subscribe(this);
        }
    }

    // ISignalObserver — вызывается при изменении сигнала
    public void OnSignalChanged(ISgSignal signal)
    {
        if (_isDisposed || _isRunning == 1) return;

        if (SignalBatch.IsBatching)
            SignalBatch.EnqueueEffect(this);
        else
            Run();
    }

    public IDisposable Subscribe(ISgSignal signal)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, nameof(SgEffect));

        signal.Subscribe(this);
        lock (_depLock) _dependencies.Add(signal);

        return new Subscription(() =>
        {
            signal.Unsubscribe(this);
            lock (_depLock) _dependencies.Remove(signal);
        });
    }

    public IDisposable SubscribeAll(params ISgSignal[] signals)
    {
        var disposables = new List<IDisposable>(signals.Length);
        foreach (var signal in signals)
            disposables.Add(Subscribe(signal));

        return new CompositeSubscription(disposables);
    }

    public void Dispose()
    {
        if (_isDisposed) return;

        _isDisposed = true;
        ClearDependencies();
        GC.SuppressFinalize(this);
    }
}

internal sealed class Subscription : IDisposable
{
    private Action? _onDispose;

    public Subscription(Action onDispose) => _onDispose = onDispose;

    public void Dispose() { var a = _onDispose; _onDispose = null; a?.Invoke(); }
}

internal sealed class CompositeSubscription : IDisposable
{
    private readonly List<IDisposable> _subscriptions;

    public CompositeSubscription(List<IDisposable> subscriptions) => _subscriptions = subscriptions;

    public void Dispose() { foreach (var s in _subscriptions) s.Dispose(); _subscriptions.Clear(); }
}
