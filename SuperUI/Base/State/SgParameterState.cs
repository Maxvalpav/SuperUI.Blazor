// SuperUI/Base/State/SgParameterState.cs
// NEW: Полноценный ParameterState<T> — 
// Добавляет: реактивность, history, reset, async onChange

using Microsoft.AspNetCore.Components;
using SuperUI.Base.Reactive;

namespace SuperUI.Base.State;

/// <summary>
/// Расширенное отслеживание параметра компонента с реактивностью.
/// Аналог MudBlazor v7 <c>ParameterState&lt;T&gt;</c>.
/// </summary>
/// <typeparam name="T">Тип параметра.</typeparam>
/// <example>
/// // В компоненте:
/// private SgParameterState&lt;bool&gt; _openState = null!;
///
/// [Parameter] public bool Open { get; set; }
/// [Parameter] public EventCallback&lt;bool&gt; OpenChanged { get; set; }
///
/// protected override void OnInitialized()
/// {
///     _openState = SgParameterState&lt;bool&gt;.Register(
///         getter: () => Open,
///         eventCallback: () => OpenChanged,
///         onChange: async (prev, next) =>
///         {
///             if (next) await OnOpenAsync();
///         }
///     );
/// }
///
/// protected override void OnParametersSet()
/// {
///     _openState.Sync();
/// }
/// </example>
public sealed class SgParameterState<T> : IDisposable
{
    private T _value;
    private T _previousValue;
    private T _initialValue;
    private bool _hasChanged;
    private bool _disposed;
    private readonly IEqualityComparer<T> _comparer;
    private readonly Func<T> _getter;
    private readonly Func<EventCallback<T>>? _callbackGetter;
    private readonly Func<T, T, Task>? _onChangeAsync;
    private readonly Action<T, T>? _onChange;

    // Реактивный сигнал — можно использовать в computed/effects
    private readonly SgSignal<T> _signal;

    private SgParameterState(
        Func<T> getter,
        Func<EventCallback<T>>? callbackGetter,
        IEqualityComparer<T>? comparer,
        Func<T, T, Task>? onChangeAsync,
        Action<T, T>? onChange)
    {
        _getter = getter ?? throw new ArgumentNullException(nameof(getter));
        _callbackGetter = callbackGetter;
        _comparer = comparer ?? EqualityComparer<T>.Default;
        _onChangeAsync = onChangeAsync;
        _onChange = onChange;
        _value = _initialValue = _previousValue = getter();
        _signal = new SgSignal<T>(_value, _comparer);
    }

    // ── Свойства ──────────────────────────────────────────────────────────────

    /// <summary>Текущее значение.</summary>
    public T Value => _value;

    /// <summary>Предыдущее значение (до последнего Sync()).</summary>
    public T PreviousValue => _previousValue;

    /// <summary>Начальное значение (при инициализации).</summary>
    public T InitialValue => _initialValue;

    /// <summary>true — параметр изменился в последнем Sync().</summary>
    public bool HasChanged => _hasChanged;

    /// <summary>true — значение отличается от начального.</summary>
    public bool IsDirty => !_comparer.Equals(_value, _initialValue);

    /// <summary>Реактивный сигнал для использования в computed/effects.</summary>
    public ReadOnlySignal<T> AsSignal() => _signal.AsReadOnly();

    // ── Фабрики ───────────────────────────────────────────────────────────────

    /// <summary>Простая регистрация без EventCallback.</summary>
    public static SgParameterState<T> Attach(
        Func<T> getter,
        IEqualityComparer<T>? comparer = null,
        Action<T, T>? onChange = null)
        => new(getter, null, comparer, null, onChange);

    /// <summary>
    /// Регистрация с поддержкой двусторонней привязки (EventCallback) и async onChange.
    /// </summary>
    public static SgParameterState<T> Register(
        Func<T> getter,
        Func<EventCallback<T>>? eventCallback = null,
        IEqualityComparer<T>? comparer = null,
        Func<T, T, Task>? onChange = null)
        => new(getter, eventCallback, comparer, onChange, null);

    // ── Sync ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Синхронизировать значение с параметром.
    /// Вызывается из OnParametersSet().
    /// </summary>
    /// <returns>true если значение изменилось.</returns>
    public bool Sync()
    {
        if (_disposed) return false;
        var newValue = _getter();
        _hasChanged = !_comparer.Equals(_value, newValue);
        if (_hasChanged)
        {
            _previousValue = _value;
            _value = newValue;
            _signal.Set(newValue);
            _onChange?.Invoke(_previousValue, _value);
        }
        return _hasChanged;
    }

    /// <summary>
    /// Синхронизировать и вызвать async onChange если значение изменилось.
    /// Вызывается из OnParametersSetAsync().
    /// </summary>
    public async Task<bool> SyncAsync()
    {
        if (_disposed) return false;
        var changed = Sync();
        if (changed && _onChangeAsync is not null)
            await _onChangeAsync(_previousValue, _value);
        return changed;
    }

    // ── Two-way binding ───────────────────────────────────────────────────────

    /// <summary>
    /// Установить значение программно (например из дочернего компонента).
    /// Вызывает EventCallback если зарегистрирован.
    /// </summary>
    public async Task SetValueAsync(T newValue)
    {
        if (_disposed) return;
        if (_comparer.Equals(_value, newValue)) return;

        var prev = _value;
        _value = newValue;
        _signal.Set(newValue);
        _hasChanged = true;
        _onChange?.Invoke(prev, newValue);

        if (_onChangeAsync is not null)
            await _onChangeAsync(prev, newValue);

        if (_callbackGetter is not null)
        {
            var cb = _callbackGetter();
            if (cb.HasDelegate)
                await cb.InvokeAsync(newValue);
        }
    }

    // ── Reset ─────────────────────────────────────────────────────────────────

    /// <summary>Сбросить к начальному значению.</summary>
    public void Reset()
    {
        _previousValue = _value;
        _value = _initialValue;
        _hasChanged = true;
        _signal.Set(_initialValue);
    }

    // ── Операторы ─────────────────────────────────────────────────────────────

    public static implicit operator T(SgParameterState<T> state) => state._value;
    public override string ToString()
        => $"SgParameterState<{typeof(T).Name}>(Value={_value}, Changed={_hasChanged})";

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _signal.Dispose();
    }
}
