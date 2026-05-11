namespace SuperUI.Services;

public interface IZIndexService
{
    int GetNext();
    void Release(int zIndex);
    int Current { get; }
}

/// <summary>
/// Thread-safe сервис управления z-index для оверлеев.
/// Обеспечивает правильный порядок наложения.
/// Базовый z-index: 1000 (над обычным контентом).
/// </summary>
public sealed class ZIndexService : IZIndexService
{
    private const int BaseZIndex = 1000;
    private const int Step = 10;

    private int _current = BaseZIndex;
    private readonly SortedSet<int> _released = []; // для повторного использования

    public int Current => _current;

    public int GetNext()
    {
        lock (_released)
        {
            if (_released.Count > 0)
            {
                var reused = _released.Max;
                _released.Remove(reused);
                return reused;
            }
        }

        return Interlocked.Add(ref _current, Step);
    }

    public void Release(int zIndex)
    {
        if (zIndex <= BaseZIndex) return;
        lock (_released)
        {
            _released.Add(zIndex);
            // Оптимизация: если это максимальный - уменьшить счётчик
            if (zIndex == _current)
                Interlocked.Add(ref _current, -Step);
        }
    }
}
