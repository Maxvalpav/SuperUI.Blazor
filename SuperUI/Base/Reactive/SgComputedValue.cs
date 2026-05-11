// SuperUI/Base/Reactive/SgComputedValue.cs

/// <summary>
/// Мемоизированное вычисление, зависящее от параметров.
/// Пересчитывается только если зависимости изменились.
/// Аналог Vue computed / React useMemo.
/// </summary>
public sealed class SgComputedValue<TDep, TResult>
    where TDep : IEquatable<TDep>
{
    private readonly Func<TDep, TResult> _compute;
    private TDep? _lastDep;
    private TResult? _cached;
    private bool _hasValue;

    public SgComputedValue(Func<TDep, TResult> compute) => _compute = compute;

    public TResult Get(TDep dependency)
    {
        if (_hasValue && _lastDep is not null &&
            EqualityComparer<TDep>.Default.Equals(dependency, _lastDep))
            return _cached!;

        _cached = _compute(dependency);
        _lastDep = dependency;
        _hasValue = true;
        return _cached;
    }

    public void Invalidate() => _hasValue = false;
}
