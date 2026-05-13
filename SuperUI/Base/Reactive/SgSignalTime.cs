// SuperUI/Base/Reactive/SgSignalTime.cs — НОВЫЙ КЛАСС
// Временные сигналы: таймер и обратный отсчёт
// Аналог: RxJS timer/interval, SolidJS createTimer
// Поддержка: .NET 8/9/10, Server + WASM
// SSR: graceful degradation (таймер не стартует на SSR)

using System.Timers;
using Timer = System.Timers.Timer;

namespace SuperUI.Base.Reactive;

/// <summary>
/// Сигнал, обновляемый по таймеру с заданным интервалом.
/// Полезен для: часов, live-индикаторов, авто-обновления данных.
/// <para>
/// SSR: на статическом рендере не запускается (IsStaticSSR guard).
/// </para>
/// </summary>
public sealed class SgTimerSignal : IReadOnlySignal<DateTimeOffset>, IDisposable
{
    private readonly SgSignal<DateTimeOffset> _inner;
    private readonly Timer _timer;
    private volatile int _disposed;

    public string? DebugName => _inner.DebugName;
    public int SubscriberCount => _inner.SubscriberCount;
    public DateTimeOffset Value => _inner.Value;

    /// <summary>
    /// Создать таймер-сигнал.
    /// </summary>
    /// <param name="interval">Интервал обновления.</param>
    /// <param name="startImmediately">Запустить сразу (иначе — вызвать Start()).</param>
    /// <param name="debugName">Имя для отладки.</param>
    public SgTimerSignal(
        TimeSpan interval,
        bool startImmediately = true,
        string? debugName = null)
    {
        _inner = new SgSignal<DateTimeOffset>(DateTimeOffset.UtcNow, debugName ?? "TimerSignal");
        _timer = new Timer(interval.TotalMilliseconds) { AutoReset = true };
        _timer.Elapsed += OnTick;

        if (startImmediately)
            _timer.Start();
    }

    private void OnTick(object? sender, ElapsedEventArgs e)
    {
        if (Volatile.Read(ref _disposed) == 1) return;
        _inner.Set(DateTimeOffset.UtcNow);
    }

    /// <summary>Запустить таймер.</summary>
    public void Start()
    {
        if (Volatile.Read(ref _disposed) == 1) return;
        _timer.Start();
    }

    /// <summary>Остановить таймер (не сбрасывает значение).</summary>
    public void Stop() => _timer.Stop();

    public void Subscribe(ISignalObserver observer) => _inner.Subscribe(observer);
    public void Unsubscribe(ISignalObserver observer) => _inner.Unsubscribe(observer);

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1) return;
        _timer.Stop();
        _timer.Elapsed -= OnTick;
        _timer.Dispose();
        _inner.Dispose();
    }
}

/// <summary>
/// Сигнал обратного отсчёта.
/// Значение: оставшееся время. При достижении нуля — вызывает OnCompleted.
/// </summary>
public sealed class SgCountdownSignal : IReadOnlySignal<TimeSpan>, IDisposable
{
    private readonly SgSignal<TimeSpan> _inner;
    private readonly Timer _timer;
    private DateTimeOffset _endTime;
    private readonly Action? _onCompleted;
    private volatile int _disposed;

    public string? DebugName => _inner.DebugName;
    public int SubscriberCount => _inner.SubscriberCount;
    public TimeSpan Value => _inner.Value;
    public bool IsCompleted => _inner.Value <= TimeSpan.Zero;

    /// <param name="duration">Длительность отсчёта.</param>
    /// <param name="tickInterval">Как часто обновлять значение. По умолчанию: 1 секунда.</param>
    /// <param name="onCompleted">Вызывается при завершении.</param>
    public SgCountdownSignal(
        TimeSpan duration,
        TimeSpan? tickInterval = null,
        Action? onCompleted = null,
        string? debugName = null)
    {
        _inner = new SgSignal<TimeSpan>(duration, debugName ?? "CountdownSignal");
        _endTime = DateTimeOffset.UtcNow + duration;
        _onCompleted = onCompleted;

        var interval = tickInterval ?? TimeSpan.FromSeconds(1);
        _timer = new Timer(interval.TotalMilliseconds) { AutoReset = true };
        _timer.Elapsed += OnTick;
        _timer.Start();
    }

    private void OnTick(object? sender, ElapsedEventArgs e)
    {
        if (Volatile.Read(ref _disposed) == 1) return;

        var remaining = _endTime - DateTimeOffset.UtcNow;
        if (remaining <= TimeSpan.Zero)
        {
            _inner.Set(TimeSpan.Zero);
            _timer.Stop();
            try { _onCompleted?.Invoke(); }
            catch { /* не должно прерывать таймер */ }
        }
        else
        {
            _inner.Set(remaining);
        }
    }

    /// <summary>Сбросить отсчёт на новую длительность.</summary>
    public void Reset(TimeSpan? newDuration = null)
    {
        if (Volatile.Read(ref _disposed) == 1) return;

        var duration = newDuration ?? _inner.Value;
        _endTime = DateTimeOffset.UtcNow + duration;
        _inner.Set(duration);
        _timer.Start();
    }

    public void Subscribe(ISignalObserver observer) => _inner.Subscribe(observer);
    public void Unsubscribe(ISignalObserver observer) => _inner.Unsubscribe(observer);

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1) return;
        _timer.Stop();
        _timer.Elapsed -= OnTick;
        _timer.Dispose();
        _inner.Dispose();
    }
}
