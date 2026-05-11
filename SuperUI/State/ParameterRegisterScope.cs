// Файл: State/ParameterRegisterScope.cs
// Зависимости: ParameterState<T>

namespace SuperUI.State;

/// <summary>
/// Scope для регистрации ParameterState в конструкторе компонента.
/// Паттерн: using var scope = CreateRegisterScope(); ... scope.RegisterParameter(...)
/// 
/// Использует IDisposable для предотвращения регистрации после завершения конструктора.
/// </summary>
public sealed class ParameterRegisterScope : IDisposable
{
    private readonly List<IParameterState> _states;
    private bool _disposed;

    internal ParameterRegisterScope(List<IParameterState> states)
    {
        _states = states;
    }

    /// <summary>
    /// Зарегистрировать параметр и получить fluent builder для его настройки.
    /// </summary>
    public ParameterStateBuilder<T> RegisterParameter<T>(string parameterName)
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(ParameterRegisterScope));
        return new ParameterStateBuilder<T>(parameterName, _states);
    }

    public void Dispose() => _disposed = true;
}

/// <summary>
/// Fluent builder для настройки ParameterState.
/// </summary>
public sealed class ParameterStateBuilder<T>
{
    private readonly string _parameterName;
    private readonly List<IParameterState> _states;
    private Func<T>? _parameterAccessor;
    private Func<EventCallback<T>>? _callbackAccessor;
    private Func<ValueTask>? _changeHandler;
    private IEqualityComparer<T>? _comparer;

    internal ParameterStateBuilder(string parameterName, List<IParameterState> states)
    {
        _parameterName = parameterName;
        _states = states;
    }

    /// <summary>Привязать к параметру компонента.</summary>
    public ParameterStateBuilder<T> WithParameter(Func<T> accessor)
    {
        _parameterAccessor = accessor;
        return this;
    }

    /// <summary>Привязать к EventCallback для двустороннего binding.</summary>
    public ParameterStateBuilder<T> WithEventCallback(Func<EventCallback<T>> callbackAccessor)
    {
        _callbackAccessor = callbackAccessor;
        return this;
    }

    /// <summary>Обработчик изменения. ValueTask для zero-allocation если нет await.</summary>
    public ParameterStateBuilder<T> WithChangeHandler(Func<ValueTask> handler)
    {
        _changeHandler = handler;
        return this;
    }

    /// <summary>Sync обработчик (преобразуется в ValueTask автоматически).</summary>
    public ParameterStateBuilder<T> WithChangeHandler(Action handler)
    {
        _changeHandler = () => { handler(); return ValueTask.CompletedTask; };
        return this;
    }

    /// <summary>Кастомный компаратор для сложных типов.</summary>
    public ParameterStateBuilder<T> WithComparer(IEqualityComparer<T> comparer)
    {
        _comparer = comparer;
        return this;
    }

    /// <summary>Завершить настройку и зарегистрировать состояние.</summary>
    public ParameterState<T> Build()
    {
        if (_parameterAccessor is null)
            throw new InvalidOperationException($"Parameter '{_parameterName}': WithParameter() обязателен.");

        var state = new ParameterState<T>(
            _parameterName,
            _parameterAccessor,
            _callbackAccessor,
            _changeHandler,
            _comparer);

        _states.Add(state);
        return state;
    }

    // Implicit conversion: не нужно вызывать Build() явно
    public static implicit operator ParameterState<T>(ParameterStateBuilder<T> builder) => builder.Build();
}
