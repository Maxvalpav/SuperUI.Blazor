// SuperUI/Base/Utilities/SgRenderGate.cs
namespace SuperUI.Base.Utilities;

/// <summary>
/// Управление стратегией рендера для fine-grained оптимизации.
/// Аналог React.memo + useMemo, но на уровне компонента.
/// </summary>
/// <remarks>
/// Per-instance состояние — безопасно на WASM и Server (per-circuit изоляция).
/// Не thread-safe сам по себе: предполагается вызов из Blazor dispatcher.
/// </remarks>
public sealed class SgRenderGate<T>
{
    private T? _lastValue;
    private bool _initialized;
    private bool _manualDirty = true;
    private readonly IEqualityComparer<T> _comparer;

    public enum Strategy { Always, WhenChanged, OnceOnly, Manual }

    public Strategy RenderStrategy { get; init; } = Strategy.WhenChanged;

    public SgRenderGate(IEqualityComparer<T>? comparer = null)
        => _comparer = comparer ?? EqualityComparer<T>.Default;

    public bool ShouldRender(T currentValue) => RenderStrategy switch
    {
        Strategy.Always => true,
        Strategy.OnceOnly => !_initialized && MarkInitialized(currentValue),
        Strategy.Manual => ConsumeManualDirty(),
        Strategy.WhenChanged => CheckChanged(currentValue),
        _ => true
    };

    private bool MarkInitialized(T current)
    {
        _initialized = true;
        _lastValue = current;
        return true;
    }

    private bool ConsumeManualDirty()
    {
        if (!_manualDirty) return false;
        _manualDirty = false;
        return true;
    }

    private bool CheckChanged(T current)
    {
        if (!_initialized)
        {
            _initialized = true;
            _lastValue = current;
            return true;
        }
        var changed = !_comparer.Equals(current, _lastValue!);
        if (changed) _lastValue = current;
        return changed;
    }

    public void MarkDirty() => _manualDirty = true;
    public void MarkClean() => _manualDirty = false;
}
