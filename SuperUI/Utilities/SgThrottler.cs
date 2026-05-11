// Файл: Utilities/SgThrottler.cs
// Зависимости: NONE
// ИННОВАЦИЯ: Throttle в базовом классе — нет ни у одной Blazor библиотеки!

namespace SuperUI.Utilities;

/// <summary>
/// Throttle utility: ограничивает частоту вызовов — не чаще чем раз в Interval.
/// В отличие от Debounce, первый вызов выполняется немедленно.
/// 
/// ПРИМЕНЕНИЕ: scroll events, resize events, mouse move.
/// </summary>
public sealed class SgThrottler : IDisposable
{
    private readonly TimeSpan _interval;
    private DateTime _lastExecution = DateTime.MinValue;
    private readonly object _lock = new();
    private bool _disposed;

    public SgThrottler(TimeSpan interval) => _interval = interval;
    public SgThrottler(int intervalMs) : this(TimeSpan.FromMilliseconds(intervalMs)) { }

    /// <summary>
    /// Выполнить action если прошёл Interval с последнего выполнения.
    /// Возвращает true если action был выполнен.
    /// </summary>
    public bool Throttle(Action action)
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(SgThrottler));

        var now = DateTime.UtcNow;
        lock (_lock)
        {
            if (now - _lastExecution < _interval)
                return false;
            _lastExecution = now;
        }
        action();
        return true;
    }

    /// <summary>Async throttle.</summary>
    public async ValueTask<bool> ThrottleAsync(Func<ValueTask> action)
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(SgThrottler));

        var now = DateTime.UtcNow;
        bool shouldExecute;
        lock (_lock)
        {
            shouldExecute = now - _lastExecution >= _interval;
            if (shouldExecute) _lastExecution = now;
        }

        if (!shouldExecute) return false;
        await action();
        return true;
    }

    public void Dispose() => _disposed = true;
}
