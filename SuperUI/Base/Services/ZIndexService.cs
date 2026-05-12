// SuperUI/Base/Services/ZIndexService.cs

namespace SuperUI.Base.Services;

public sealed class ZIndexService : IZIndexService
{
    private const int BaseZIndex = 1000;
    private const int Step = 10;

    private readonly object _lock = new();
    private int _current = BaseZIndex;
    private readonly SortedSet<int> _released = [];

    public int Current { get { lock (_lock) return _current; } }

    public int GetNext()
    {
        lock (_lock)
        {
            if (_released.Count > 0)
            {
                var reused = _released.Max;
                _released.Remove(reused);
                if (reused > _current) _current = reused;
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
            while (_current > BaseZIndex && _released.Contains(_current))
            {
                _released.Remove(_current);
                _current -= Step;
            }
        }
    }

    public void Reset()
    {
        lock (_lock)
        {
            _current = BaseZIndex;
            _released.Clear();
        }
    }
}
