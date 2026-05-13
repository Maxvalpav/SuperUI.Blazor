// SuperUI/Base/Reactive/SgThrottleSignal.cs — НОВЫЙ
//
// Что это: сигнал, который уведомляет подписчиков не чаще чем раз в интервал.
// В отличие от debounce, первое изменение проходит сразу.
//
// Аналог: Rx ThrottleFirst, Lodash throttle

namespace SuperUI.Base.Reactive;

/// <summary>
/// Сигнал с throttle: уведомления проходят не чаще чем раз в заданный интервал.
/// Первое изменение проходит сразу, последующие игнорируются до истечения интервала,
/// затем проходит последнее накопленное значение.
/// 
/// Пример: resize observer, scroll position — обновлять UI не чаще 60fps.
/// </summary>
public sealed class SgThrottleSignal<T> : ISgSignal<T>, IDisposable
{
    private readonly SgSignal<T> _inner;
    private readonly int _intervalMs;
    private T _pendingValue;
    private bool _hasPending;
    private DateTime _lastEmit = DateTime.MinValue;
    private Timer? _timer;
    private readonly object _lock = new();
    private volatile bool _isDisposed;

    public string? DebugName => _inner.DebugName;
    public int SubscriberCount => _inner.SubscriberCount;

    public T Value
    {
        get
        {
            lock (_lock)
                return _hasPending ? _pendingValue : _inner.Value;
        }
    }

    public SgThrottleSignal(T initialValue, int intervalMs = 16, string? debugName = null)
    {
        if (intervalMs < 0) throw new ArgumentOutOfRangeException(nameof(intervalMs));

        _intervalMs = intervalMs;
        _inner = new SgSignal<T>(initialValue, debugName is not null ? $"Throttle({debugName})" : null);
    }

    public void Set(T value)
    {
        if (_isDisposed) return;

        lock (_lock)
        {
            var now = DateTime.UtcNow;
            var elapsed = (now - _lastEmit).TotalMilliseconds;

            if (elapsed >= _intervalMs)
            {
                // Интервал прошёл — эмитим сразу
                _lastEmit = now;
                _hasPending = false;
                _inner.Set(value);
            }
            else
            {
                // Сохраняем для отправки позже
                _pendingValue = value;
                _hasPending = true;

                if (_timer == null)
                {
                    var remaining = (int)(_intervalMs - elapsed);
                    _timer = new Timer(OnTimerTick, null, remaining, Timeout.Infinite);
                }
            }
        }
    }

    private void OnTimerTick(object? state)
    {
        lock (_lock)
        {
            _timer?.Dispose();
            _timer = null;

            if (_hasPending)
            {
                _lastEmit = DateTime.UtcNow;
                _hasPending = false;
                _inner.Set(_pendingValue);
            }
        }
    }

    public void Subscribe(ISignalObserver observer) => _inner.Subscribe(observer);

    public void Unsubscribe(ISignalObserver observer) => _inner.Unsubscribe(observer);

    public void Dispose()
    {
        _isDisposed = true;

        lock (_lock)
        {
            _timer?.Dispose();
            _timer = null;
        }
    }
}
