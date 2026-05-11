using Microsoft.AspNetCore.Components;

namespace SuperUI.State;

/// <summary>
/// Типизированное состояние параметра компонента с change-tracking и fluent API.
/// 
/// Решает проблему: "что изменилось с прошлого рендера?"
/// 
/// Вдохновлено MudBlazor ParameterState, но с:
/// - Fluent API (цепочки)
/// - Async change handlers
/// - Previous value tracking
/// - Equality comparer настраиваемый
/// - SetValueAsync без StateHasChanged (контроль вызывающей стороны)
/// </summary>
public sealed class ParameterState<T>
{
    private T? _value;
    private T? _previousValue;
    private bool _initialized;
    private Func<T?>? _parameterAccessor;
    private Func<EventCallback<T>>? _callbackAccessor;
    private Func<ParameterChangedEventArgs<T>, Task>? _changeHandler;
    private IEqualityComparer<T?> _comparer = EqualityComparer<T?>.Default;

    public T? Value => _value;
    public T? PreviousValue => _previousValue;
    public bool HasChanged => !_comparer.Equals(_value, _previousValue);

    // ── Fluent API ────────────────────────────────────────────────────────────

    public ParameterState<T> WithParameter(Func<T?> accessor)
    {
        _parameterAccessor = accessor;
        return this;
    }

    public ParameterState<T> WithEventCallback(Func<EventCallback<T>> callbackAccessor)
    {
        _callbackAccessor = callbackAccessor;
        return this;
    }

    public ParameterState<T> WithChangeHandler(Func<ParameterChangedEventArgs<T>, Task> handler)
    {
        _changeHandler = handler;
        return this;
    }

    public ParameterState<T> WithChangeHandler(Action handler)
    {
        _changeHandler = _ => { handler(); return Task.CompletedTask; };
        return this;
    }

    public ParameterState<T> WithComparer(IEqualityComparer<T?> comparer)
    {
        _comparer = comparer;
        return this;
    }

    // ── Обновление из SetParametersAsync ─────────────────────────────────────

    /// <summary>
    /// Вызывается при каждом SetParametersAsync.
    /// Обновляет значение и вызывает handler если изменилось.
    /// </summary>
    internal async Task UpdateAsync()
    {
        if (_parameterAccessor is null) return;

        var newValue = _parameterAccessor();
        _previousValue = _value;

        if (!_initialized || !_comparer.Equals(_value, newValue))
        {
            _value = newValue;
            _initialized = true;

            if (_changeHandler != null && _initialized)
            {
                await _changeHandler(new ParameterChangedEventArgs<T>(_previousValue, newValue));
            }
        }
    }

    // ── Программное обновление ────────────────────────────────────────────────

    /// <summary>
    /// Обновить значение программно (не через параметр).
    /// Также вызывает EventCallback и changeHandler.
    /// </summary>
    public async Task SetValueAsync(T? value)
    {
        if (_comparer.Equals(_value, value)) return;

        _previousValue = _value;
        _value = value;

        // Уведомить через EventCallback (для @bind-*)
        if (_callbackAccessor != null)
        {
            var callback = _callbackAccessor();
            if (callback.HasDelegate && value is not null)
                await callback.InvokeAsync(value);
        }

        // Вызвать changeHandler
        if (_changeHandler != null)
        {
            await _changeHandler(new ParameterChangedEventArgs<T>(_previousValue, value));
        }
    }
}

public record ParameterChangedEventArgs<T>(T? OldValue, T? NewValue);

/// <summary>
/// Scope для регистрации параметров. Использовать в конструкторе компонента.
/// </summary>
public sealed class ParameterScope : IDisposable
{
    private readonly List<Func<Task>> _updaters = [];
    private bool _disposed;

    public ParameterState<T> Register<T>(string parameterName)
    {
        var state = new ParameterState<T>();
        _updaters.Add(() => state.UpdateAsync());
        return state;
    }

    /// <summary>Обновить все зарегистрированные параметры.</summary>
    internal async Task UpdateAllAsync()
    {
        foreach (var updater in _updaters)
            await updater();
    }

    public void Dispose() => _disposed = true;
}
