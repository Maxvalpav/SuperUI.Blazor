namespace SuperUI.Base.Services;

public interface IZIndexService
{
    int GetNext();
    void Release(int zIndex);
    int Current { get; }
}

/// <summary>
/// Thread-safe сервис управления z-index для оверлеев.
/// Singleton: безопасен для Server (multi-circuit) и WASM.
///
/// ИСПРАВЛЕНО: устранён race condition между _current и _released.
/// Весь доступ к _current и _released под единым lock.
/// </summary>
public sealed class ZIndexService : IZIndexService
{
    private const int BaseZIndex = 1000;
    private const int Step = 10;

    // ИСПРАВЛЕНО: единый lock для _current + _released
    private readonly object _lock = new();
    private int _current = BaseZIndex;
    private readonly SortedSet<int> _released = [];

    public int Current
    {
        get { lock (_lock) return _current; }
    }

    public int GetNext()
    {
        lock (_lock)
        {
            if (_released.Count > 0)
            {
                var reused = _released.Max;
                _released.Remove(reused);
                return reused;
            }

            _current += Step;
            return _current;
        }
    }

    public void Release(int zIndex)
    {
        if (zIndex <= BaseZIndex) return;

        lock (_lock)
        {
            _released.Add(zIndex);

            // Оптимизация: если это максимальное значение — уменьшить счётчик
            // ИСПРАВЛЕНО: читаем и изменяем _current под тем же lock
            if (zIndex == _current)
                _current -= Step;
        }
    }
}
