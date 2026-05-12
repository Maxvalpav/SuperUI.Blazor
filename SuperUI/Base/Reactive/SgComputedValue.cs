// SuperUI/Base/Reactive/SgComputedValue.cs
//
// УЛУЧШЕНИЯ:
//   1. Multi-dependency версия (2 зависимости)
//   2. HasValue публичное свойство
//   3. TryGet — безопасное чтение
//   4. Reset() — явная инвалидация

namespace SuperUI.Base.Reactive;

/// <summary>
/// Мемоизированное вычисление с одной зависимостью.
/// Пересчитывается только если зависимость изменилась.
/// Аналог React useMemo / Vue computed.
/// </summary>
/// <typeparam name="TDep">Тип зависимости (должен реализовывать IEquatable).</typeparam>
/// <typeparam name="TResult">Тип результата.</typeparam>
public sealed class SgComputedValue<TDep, TResult>
    where TDep : IEquatable<TDep>
{
    private readonly Func<TDep, TResult> _compute;
    private TDep? _lastDep;
    private TResult? _cached;
    private bool _hasValue;

    public SgComputedValue(Func<TDep, TResult> compute)
        => _compute = compute ?? throw new ArgumentNullException(nameof(compute));

    /// <summary>true — есть кэшированное значение.</summary>
    public bool HasValue => _hasValue;

    /// <summary>Получить значение, пересчитав при необходимости.</summary>
    public TResult Get(TDep dependency)
    {
        if (_hasValue && _lastDep is not null
            && EqualityComparer<TDep>.Default.Equals(dependency, _lastDep))
            return _cached!;

        _cached = _compute(dependency);
        _lastDep = dependency;
        _hasValue = true;
        return _cached;
    }

    /// <summary>Попробовать получить кэш без пересчёта.</summary>
    public bool TryGet(TDep dependency, out TResult? value)
    {
        if (_hasValue && _lastDep is not null
            && EqualityComparer<TDep>.Default.Equals(dependency, _lastDep))
        {
            value = _cached;
            return true;
        }
        value = default;
        return false;
    }

    /// <summary>Инвалидировать кэш.</summary>
    public void Invalidate() => _hasValue = false;

    /// <summary>Сбросить в начальное состояние.</summary>
    public void Reset()
    {
        _hasValue = false;
        _lastDep = default;
        _cached = default;
    }
}

/// <summary>
/// Мемоизированное вычисление с двумя зависимостями.
/// </summary>
public sealed class SgComputedValue<TDep1, TDep2, TResult>
    where TDep1 : IEquatable<TDep1>
    where TDep2 : IEquatable<TDep2>
{
    private readonly Func<TDep1, TDep2, TResult> _compute;
    private TDep1? _lastDep1;
    private TDep2? _lastDep2;
    private TResult? _cached;
    private bool _hasValue;

    public SgComputedValue(Func<TDep1, TDep2, TResult> compute)
        => _compute = compute ?? throw new ArgumentNullException(nameof(compute));

    public bool HasValue => _hasValue;

    public TResult Get(TDep1 dep1, TDep2 dep2)
    {
        if (_hasValue
            && EqualityComparer<TDep1>.Default.Equals(dep1, _lastDep1!)
            && EqualityComparer<TDep2>.Default.Equals(dep2, _lastDep2!))
            return _cached!;

        _cached = _compute(dep1, dep2);
        _lastDep1 = dep1;
        _lastDep2 = dep2;
        _hasValue = true;
        return _cached;
    }

    public void Invalidate() => _hasValue = false;

    public void Reset()
    {
        _hasValue = false;
        _lastDep1 = default;
        _lastDep2 = default;
        _cached = default;
    }
}
