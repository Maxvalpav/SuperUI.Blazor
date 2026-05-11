// SuperUI/Base/Utilities/SgRenderGate.cs

/// <summary>
/// Управление стратегией рендера для fine-grained оптимизации.
/// Аналог React.memo + useMemo, но на уровне компонента.
/// </summary>
public sealed class SgRenderGate<T>
{
    private T? _lastValue;
    private bool _initialized;

    public enum Strategy { Always, WhenChanged, OnceOnly, Manual }

    public Strategy RenderStrategy { get; set; } = Strategy.WhenChanged;

    private bool _manualDirty = true;

    public bool ShouldRender(T currentValue)
    {
        return RenderStrategy switch
        {
            Strategy.Always => true,
            Strategy.OnceOnly => !_initialized,
            Strategy.Manual => _manualDirty,
            Strategy.WhenChanged => CheckChanged(currentValue),
            _ => true
        };
    }

    private bool CheckChanged(T current)
    {
        if (!_initialized)
        {
            _initialized = true;
            _lastValue = current;
            return true;
        }
        var changed = !EqualityComparer<T>.Default.Equals(current, _lastValue);
        if (changed) _lastValue = current;
        return changed;
    }

    public void MarkDirty() => _manualDirty = true;
    public void MarkClean() => _manualDirty = false;
}
