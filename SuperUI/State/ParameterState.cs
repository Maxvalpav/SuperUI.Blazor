// Файл: State/ParameterState.cs
// Зависимости: NONE (кроме System.*)
// Инспирирован MudBlazor, кардинально улучшен:
// - ValueTask везде вместо Task (меньше аллокаций)
// - Fluent API в ParameterBuilder<T>
// - Поддержка [ParameterState] атрибута для source generators
// - Полная документация

namespace SuperUI.State;

/// <summary>
/// Управляет параметром компонента в соответствии с правилами Blazor:
/// параметры должны быть auto-property, вся логика — через ParameterState.
/// 
/// ПРОБЛЕМА которую решает:
/// Blazor не позволяет писать логику в setter параметра.
/// ParameterState перехватывает изменения через SetParametersAsync
/// и вызывает ChangeHandler только при реальных изменениях значения.
/// 
/// GC ОПТИМИЗАЦИЯ: ValueTask вместо Task, struct-based где возможно.
/// </summary>
/// <typeparam name="T">Тип параметра.</typeparam>
public sealed class ParameterState<T> : IParameterState
{
    private readonly Func<T> _parameterAccessor;
    private readonly Func<EventCallback<T>>? _callbackAccessor;
    private readonly Func<ValueTask>? _changeHandler;
    private readonly IEqualityComparer<T> _comparer;
    private T _value;
    private bool _hasValue;

    internal ParameterState(
        string parameterName,
        Func<T> parameterAccessor,
        Func<EventCallback<T>>? callbackAccessor,
        Func<ValueTask>? changeHandler,
        IEqualityComparer<T>? comparer)
    {
        ParameterName = parameterName;
        _parameterAccessor = parameterAccessor;
        _callbackAccessor = callbackAccessor;
        _changeHandler = changeHandler;
        _comparer = comparer ?? EqualityComparer<T>.Default;
        _value = default!;
    }

    public string ParameterName { get; }

    /// <summary>Текущее значение параметра (после последнего SetParametersAsync).</summary>
    public T Value => _value;

    /// <summary>
    /// Программно установить новое значение. Уведомляет EventCallback и вызывает ChangeHandler.
    /// Используется для двустороннего связывания (two-way binding).
    /// </summary>
    public async ValueTask SetValueAsync(T newValue)
    {
        if (_hasValue && _comparer.Equals(_value, newValue))
            return;

        _value = newValue;
        _hasValue = true;

        if (_callbackAccessor is not null)
        {
            var callback = _callbackAccessor();
            if (callback.HasDelegate)
                await callback.InvokeAsync(newValue);
        }

        if (_changeHandler is not null)
            await _changeHandler();
    }

    /// <summary>
    /// Вызывается из SetParametersAsync базового класса.
    /// Возвращает true если значение изменилось и нужен re-render.
    /// </summary>
    async ValueTask<bool> IParameterState.OnParametersSetAsync()
    {
        var newValue = _parameterAccessor();

        if (_hasValue && _comparer.Equals(_value, newValue))
            return false; // нет изменений — не нужен ChangeHandler

        var oldValue = _value;
        _value = newValue;
        _hasValue = true;

        if (_changeHandler is not null)
            await _changeHandler();

        return true; // значение изменилось
    }

    public static implicit operator T(ParameterState<T> state) => state._value;
}

/// <summary>Внутренний контракт для обхода типизированных состояний.</summary>
internal interface IParameterState
{
    string ParameterName { get; }
    ValueTask<bool> OnParametersSetAsync();
}
