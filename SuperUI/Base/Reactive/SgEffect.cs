// SuperUI/Base/Reactive/SgEffect.cs
// ИСПРАВЛЕНО:
// ✅ CS0101: убраны дублирующие Subscription и CompositeSubscription (теперь в SgSubscription.cs)
// ✅ Добавлена защита от потери pending-rerun при рекурсии
// ✅ Добавлен onError callback с логированием
// ✅ CancellationToken передаётся в async эффект
// ✅ Поддержка .NET 8/9/10

using System.Runtime.CompilerServices;

namespace SuperUI.Base.Reactive;

/// <summary>
/// Реактивный побочный эффект.
/// Автоматически отслеживает зависимости от сигналов и перезапускается при их изменении.
/// <para>
/// Использование:
/// <code>
/// var count = Signal&lt;int&gt;(0);
/// var effect = new SgEffect(() => Console.WriteLine($"Count: {count.Value}"));
/// effect.Run(); // первый запуск + подписка
/// count.Set(1); // автоматически перезапустит эффект
/// </code>
/// </para>
/// </summary>
public sealed class SgEffect : ISignalObserver, IDisposable
{
    private readonly Delegate _effect;
    private readonly Action<Exception>? _onError;
    private readonly object _depLock = new();
    private readonly HashSet<ISgSignal> _dependencies = new(ReferenceEqualityComparer.Instance);
    private volatile int _disposed;
    private volatile int _isRunning;
    private volatile int _pendingRerun; // ✅ NEW: защита от потери pending-rerun
    private readonly CancellationTokenSource _cts = new();

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

        // ✅ FIX: если уже запущен — помечаем pending, не теряем вызов
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

                using (SgReactiveComponentBase.EnterScope(new EffectSignalObserver(this)))
                {
                    if (_effect is Action syncAction)
                    {
                        syncAction();
                    }
                    else if (_effect is Func<Task> asyncAction)
                    {
                        _ = RunAsyncEffect(asyncAction, _cts.Token);
                    }
                    else if (_effect is Func<CancellationToken, Task> asyncWithToken)
                    {
                        _ = RunAsyncEffect(() => asyncWithToken(_cts.Token), _cts.Token);
                    }
                }
            }
            // ✅ FIX: повторяем если было pending-rerun во время выполнения
            while (Volatile.Read(ref _pendingRerun) == 1 && !IsDisposed);
        }
        catch (Exception ex) when (!IsDisposed)
        {
            _onError?.Invoke(ex);
        }
        finally
        {
            Interlocked.Exchange(ref _isRunning, 0);
        }
    }

    private async Task RunAsyncEffect(Func<Task> asyncAction, CancellationToken cancellationToken)
    {
        try
        {
            await asyncAction();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Нормальное завершение при dispose
        }
        catch (Exception ex) when (!IsDisposed)
        {
            _onError?.Invoke(ex);
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

    public IDisposable SubscribeAll(params ISgSignal[] signals)
    {
        var disposables = new List<IDisposable>(signals.Length);
        foreach (var signal in signals)
            disposables.Add(Subscribe(signal));
        return new CompositeSubscription(disposables);
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1) return;

        _cts.Cancel();
        _cts.Dispose();
        ClearDependencies();
        GC.SuppressFinalize(this);
    }

    private sealed class EffectSignalObserver : ISignalObserver, ISignalTrackingObserver
    {
        private readonly SgEffect _effect;

        public EffectSignalObserver(SgEffect effect)
            => _effect = effect;

        public void OnSignalChanged(ISgSignal signal)
            => _effect.OnSignalChanged(signal);

        public void OnSignalRead(ISgSignal signal)
            => _effect.AddDependency(signal);
    }
}
