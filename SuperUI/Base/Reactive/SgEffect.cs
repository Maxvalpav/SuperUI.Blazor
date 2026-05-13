// SuperUI/Base/Reactive/SgEffect.cs
// ИСПРАВЛЕНИЯ:
// ✅ THREAD SAFETY: HashSet → использование lock для _dependencies
// ✅ _isDisposed → volatile int + Interlocked.Exchange (idempotent)
// ✅ ASYNC: async эффект правильно awaited с обработкой исключений
// ✅ AOT: нет dynamic, нет рефлексии
// ✅ TRACKING: использует SgReactiveComponentBase.EnterScope() как SgComputed
// ✅ NET8: совместим с .NET 8+

using System.Runtime.CompilerServices;

namespace SuperUI.Base.Reactive;

/// <summary>
/// Реактивный побочный эффект.
/// Автоматически отслеживает зависимости от сигналов и перезапускается при их изменении.
///
/// ИСПРАВЛЕНИЯ:
/// - HashSet без lock → HashSet с lock (thread-safety)
/// - bool _isDisposed → volatile int (Interlocked)
/// - async Fire-and-forget без обработки → правильный async с CancellationToken
/// - SignalTracker.BeginTracking → SgReactiveComponentBase.EnterScope() (единая система)
///
/// Использование:
/// <code>
/// var count = Signal&lt;int&gt;(0);
/// var effect = new SgEffect(() =&gt; Console.WriteLine($"Count: {count.Value}"));
/// effect.Run(); // первый запуск + подписка
/// count.Set(1); // автоматически перезапустит эффект
/// </code>
/// </summary>
public sealed class SgEffect : ISignalObserver, IDisposable
{
    private readonly Delegate _effect;         // Action или Func<Task>
    private readonly Action<Exception>? _onError;

    // ✅ FIX: lock вместо незащищённого HashSet
    private readonly object _depLock = new();
    private readonly HashSet<ISgSignal> _dependencies = new(ReferenceEqualityComparer.Instance);

    // ✅ FIX: volatile int для idempotent dispose и thread-visible state
    private volatile int _disposed;
    private volatile int _isRunning;
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
    /// Thread-safe. Идемпотентен (не запускается если уже выполняется).
    /// </summary>
    public void Run()
    {
        if (IsDisposed) return;

        // Предотвращаем рекурсивный запуск
        if (Interlocked.CompareExchange(ref _isRunning, 1, 0) == 1) return;

        try
        {
            // Очищаем старые зависимости и переподписываемся
            ClearDependencies();

            // Запускаем эффект внутри scope отслеживания
            using (SgReactiveComponentBase.EnterScope(new EffectSignalObserver(this)))
            {
                if (_effect is Action syncAction)
                {
                    syncAction();
                }
                else if (_effect is Func<Task> asyncAction)
                {
                    // ✅ FIX: правильный fire-and-forget с обработкой исключений
                    _ = RunAsyncEffect(asyncAction, _cts.Token);
                }
                else if (_effect is Func<CancellationToken, Task> asyncWithToken)
                {
                    _ = RunAsyncEffect(() => asyncWithToken(_cts.Token), _cts.Token);
                }
            }
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

    /// <summary>Добавить зависимость (вызывается из EffectSignalObserver).</summary>
    internal void AddDependency(ISgSignal signal)
    {
        if (IsDisposed) return;

        lock (_depLock)
        {
            if (_dependencies.Add(signal))
                signal.Subscribe(this);
        }
    }

    /// <summary>
    /// Вызывается при изменении отслеживаемого сигнала.
    /// Перезапускает эффект.
    /// </summary>
    public void OnSignalChanged(ISgSignal signal)
    {
        if (!IsDisposed && !IsRunning)
            Run();
    }

    /// <summary>Подписаться на сигнал вручную.</summary>
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

    /// <summary>Подписаться на несколько сигналов вручную.</summary>
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

    // ── Вложенный наблюдатель для scope отслеживания ──────────────────────

    /// <summary>
    /// Адаптер, связывающий SgReactiveComponentBase.EnterScope() с SgEffect.
    /// Реализует ISignalObserver для использования как observer в scope.
    /// </summary>
    private sealed class EffectSignalObserver : ISignalObserver, ISignalTrackingObserver
    {
        private readonly SgEffect _effect;

        public EffectSignalObserver(SgEffect effect) => _effect = effect;

        public void OnSignalChanged(ISgSignal signal) => _effect.OnSignalChanged(signal);

        public void OnSignalRead(ISgSignal signal) => _effect.AddDependency(signal);
    }
}

// ── Вспомогательные типы ──────────────────────────────────────────────────────

/// <summary>Подписка с действием на Dispose.</summary>
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

/// <summary>Композитная подписка — dispose всех при dispose.</summary>
internal sealed class CompositeSubscription : IDisposable
{
    private readonly List<IDisposable> _subscriptions;

    public CompositeSubscription(List<IDisposable> subscriptions)
        => _subscriptions = subscriptions;

    public void Dispose()
    {
        foreach (var sub in _subscriptions)
            sub.Dispose();
        _subscriptions.Clear();
    }
}
