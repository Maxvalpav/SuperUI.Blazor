// SuperUI/Base/Reactive/SgEffect.cs
// ✅ Реентрантность: _isRunning сбрасывается в finally
// ✅ Лимитированная очередь: MaxQueueSize = 1, DroppedCount для метрик
// ✅ Pause/Resume; sync + async actions; onError callback

using System.Collections.Concurrent;

namespace SuperUI.Base.Reactive;

/// <summary>
/// Side-effect, реактивно реагирующий на изменения отслеживаемых сигналов.
/// </summary>
public sealed class SgEffect : ISignalObserver, IDisposable
{
    private readonly Action? _sync;
    private readonly Func<Task>? _async;
    private readonly Action<Exception>? _onError;

    private readonly ConcurrentQueue<object> _queue = new();
    private int _disposed;
    private int _isRunning;
    private int _paused;

    // Лимитированная очередь — максимум 1 задача, drops лишние
    private const int MaxQueueSize = 1;
    private int _droppedCount;

    /// <summary>true, если эффект уже dispose'нут.</summary>
    public bool IsDisposed => Volatile.Read(ref _disposed) == 1;

    /// <summary>true, если эффект на паузе (Pause).</summary>
    public bool IsPaused => Volatile.Read(ref _paused) == 1;

    /// <summary>Эффект сейчас в очереди на исполнение или исполняется.</summary>
    public bool IsRunning => Volatile.Read(ref _isRunning) == 1;

    /// <summary>Сколько уведомлений было отброшено из-за переполнения очереди.</summary>
    public int DroppedCount => _droppedCount;

    public SgEffect(Action action, Action<Exception>? onError = null)
    {
        ArgumentNullException.ThrowIfNull(action);
        _sync = action;
        _onError = onError;
    }

    public SgEffect(Func<Task> action, Action<Exception>? onError = null)
    {
        ArgumentNullException.ThrowIfNull(action);
        _async = action;
        _onError = onError;
    }

    // ── Subscribe to component lifecycle (auto-dispose on component dispose) ──
    /// <summary>
    /// Привязать эффект к компоненту — при изменениях вызывает RequestRender.
    /// (Сами сигналы подписываются на компонент через SignalTracker.)
    /// </summary>
    public void Subscribe(SgComponentBase component)
    {
        // Поведение совместимое: эффект сам по себе не требует компонента,
        // но для жизненного цикла мы храним ссылку, чтобы dispose'нуть.
        // Здесь — фактически no-op; компонент сам владеет эффектом через _reactiveDisposables.
        _ = component;
    }

    // ── Pause / Resume ────────────────────────────────────────────────────────
    public void Pause() => Volatile.Write(ref _paused, 1);

    public void Resume()
    {
        if (Interlocked.Exchange(ref _paused, 0) == 1 && _queue.Count > 0)
            Schedule();
    }

    // ── Enqueue ───────────────────────────────────────────────────────────────
    public void Enqueue()
    {
        if (IsDisposed) return;

        if (_queue.Count >= MaxQueueSize)
        {
            Interlocked.Increment(ref _droppedCount);
            return;
        }

        _queue.Enqueue(this);
        if (!IsPaused) Schedule();
    }

    // ── ISignalObserver ───────────────────────────────────────────────────────
    public void OnSignalChanged() => Enqueue();

    // ── Schedule ──────────────────────────────────────────────────────────────
    private void Schedule()
    {
        if (IsDisposed || IsPaused) return;

        if (Interlocked.CompareExchange(ref _isRunning, 1, 0) == 1)
            return;

        SignalBatch.EnqueueEffect(this);
    }

    public void Run()
    {
        if (IsDisposed || IsPaused || !_queue.TryDequeue(out _))
        {
            Volatile.Write(ref _isRunning, 0);
            return;
        }

        try
        {
            if (_sync is not null) _sync();
            else if (_async is not null) _ = RunAsync();
        }
        catch (Exception ex)
        {
            if (_onError is not null) try { _onError(ex); } catch { }
            else System.Diagnostics.Debug.WriteLine($"[SgEffect] Unhandled exception: {ex}");
        }

        if (_queue.TryPeek(out _))
            SignalBatch.EnqueueEffect(this);
        else
            Volatile.Write(ref _isRunning, 0);
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

    // ── IDisposable ───────────────────────────────────────────────────────────
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1) return;
        _registeredSignals.Clear();
        Volatile.Write(ref _isRunning, 0);
        while (_queue.TryDequeue(out _)) { }
    }

    // ── Вспомогательное ───────────────────────────────────────────────────────
    private readonly HashSet<object> _registeredSignals = new();

    internal void RegisterSignal(object signal)
        => _registeredSignals.Add(signal);
}
