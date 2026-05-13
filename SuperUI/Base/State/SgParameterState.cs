// SuperUI/Base/State/SgParameterState.cs 
// Улучшения: 
// - Полный fluent API регистрации параметров 
// - Async change handlers 
// - Защита от unobserved async discards в сеттерах 
// - EventCallback интеграция 
// - Автоматическая регистрация через SetParametersAsync 
 
using System; 
using System.Collections.Generic; 
using System.Threading.Tasks; 
using Microsoft.AspNetCore.Components; 
 
namespace SuperUI.Base.State; 
 
/// <summary> 
/// Типобезопасное состояние параметра с поддержкой change handlers. 
/// Предотвращает паттерн unobserved async discard в сеттерах параметров. 
/// </summary> 
public sealed class SgParameterState<T> 
{ 
    private T _value = default!; 
    private readonly Func<T> _parameterGetter; 
    private readonly Func<EventCallback<T>>? _callbackGetter; 
    private readonly Func<T, Task>? _changeHandler; 
    private readonly IEqualityComparer<T> _comparer; 
    private bool _initialized; 
 
    internal SgParameterState( 
        Func<T> parameterGetter, 
        Func<EventCallback<T>>? callbackGetter, 
        Func<T, Task>? changeHandler, 
        IEqualityComparer<T>? comparer) 
    { 
        _parameterGetter = parameterGetter; 
        _callbackGetter = callbackGetter; 
        _changeHandler = changeHandler; 
        _comparer = comparer ?? EqualityComparer<T>.Default; 
    } 
 
    /// <summary>Текущее значение параметра.</summary> 
    public T Value => _value; 
 
    /// <summary> 
    /// Вызывается из SetParametersAsync компонента. 
    /// Обнаруживает изменение и вызывает change handler. 
    /// </summary> 
    internal async Task SetParameterAsync(ParameterView parameters, string parameterName) 
    { 
        var newValue = _parameterGetter(); 
        bool changed = !_initialized || !_comparer.Equals(_value, newValue); 
 
        _value = newValue; 
        _initialized = true; 
 
        if (changed && _changeHandler != null) 
            await _changeHandler(newValue); 
    } 
 
    /// <summary> 
    /// Уведомляет родительский компонент через EventCallback (для two-way binding). 
    /// </summary> 
    public async Task SetValueAsync(T newValue) 
    { 
        if (_comparer.Equals(_value, newValue)) return; 
        _value = newValue; 
 
        if (_callbackGetter != null) 
        { 
            var callback = _callbackGetter(); 
            if (callback.HasDelegate) 
                await callback.InvokeAsync(newValue); 
        } 
 
        if (_changeHandler != null) 
            await _changeHandler(newValue); 
    } 
 
    public static implicit operator T(SgParameterState<T> state) => state.Value; 
} 
 
/// <summary> 
/// Scope для регистрации параметров в конструкторе компонента. 
/// </summary> 
public sealed class SgParameterRegisterScope 
{ 
    private readonly List<Func<ParameterView, Task>> _handlers = new(); 
 
    internal SgParameterRegisterScope() { } 
 
    /// <summary> 
    /// Регистрирует параметр с опциональным change handler. 
    /// </summary> 
    public SgParameterStateBuilder<T> RegisterParameter<T>(string parameterName) 
        => new SgParameterStateBuilder<T>(parameterName, this); 
 
    internal void AddHandler(Func<ParameterView, Task> handler) => _handlers.Add(handler); 
 
    internal async Task ApplyAsync(ParameterView parameters) 
    { 
        foreach (var handler in _handlers) 
            await handler(parameters); 
    } 
} 
 
/// <summary> 
/// Fluent builder для регистрации параметра. 
/// </summary> 
public sealed class SgParameterStateBuilder<T> 
{ 
    private readonly string _parameterName; 
    private readonly SgParameterRegisterScope _scope; 
    private Func<T>? _getter; 
    private Func<EventCallback<T>>? _callbackGetter; 
    private Func<T, Task>? _changeHandler; 
    private IEqualityComparer<T>? _comparer; 
 
    internal SgParameterStateBuilder(string parameterName, SgParameterRegisterScope scope) 
    { 
        _parameterName = parameterName; 
        _scope = scope; 
    } 
 
    /// <summary>Геттер значения параметра.</summary> 
    public SgParameterStateBuilder<T> WithParameter(Func<T> getter) 
    { 
        _getter = getter; 
        return this; 
    } 
 
    /// <summary>Геттер EventCallback для two-way binding.</summary> 
    public SgParameterStateBuilder<T> WithEventCallback(Func<EventCallback<T>> callbackGetter) 
    { 
        _callbackGetter = callbackGetter; 
        return this; 
    } 
 
    /// <summary>Async change handler.</summary> 
    public SgParameterStateBuilder<T> WithChangeHandler(Func<T, Task> handler) 
    { 
        _changeHandler = handler; 
        return this; 
    } 
 
    /// <summary>Sync change handler.</summary> 
    public SgParameterStateBuilder<T> WithChangeHandler(Action<T> handler) 
    { 
        _changeHandler = v => { handler(v); return Task.CompletedTask; }; 
        return this; 
    } 
 
    /// <summary>Кастомный компаратор для обнаружения изменений.</summary> 
    public SgParameterStateBuilder<T> WithComparer(IEqualityComparer<T> comparer) 
    { 
        _comparer = comparer; 
        return this; 
    } 
 
    /// <summary>Завершает регистрацию и возвращает SgParameterState.</summary> 
    public SgParameterState<T> Build() 
    { 
        if (_getter == null) 
            throw new InvalidOperationException( 
                $"WithParameter() must be called before Build() for parameter '{_parameterName}'"); 
 
        var state = new SgParameterState<T>(_getter, _callbackGetter, _changeHandler, _comparer); 
        _scope.AddHandler(parameters => state.SetParameterAsync(parameters, _parameterName)); 
        return state; 
    } 
} 
 
/// <summary> 
/// Базовый класс с поддержкой SgParameterState. 
/// Использование: 
/// <code> 
/// public class MyComponent : SgParameterAwareBase 
/// { 
///     private readonly SgParameterState&lt;bool&gt; _expandedState; 
/// 
///     public MyComponent() 
///     { 
///         using var scope = CreateRegisterScope(); 
///         _expandedState = scope.RegisterParameter&lt;bool&gt;(nameof(Expanded)) 
///             .WithParameter(() => Expanded) 
///             .WithEventCallback(() => ExpandedChanged) 
///             .WithChangeHandler(OnExpandedChangedAsync) 
///             .Build(); 
///     } 
/// } 
/// </code> 
/// </summary> 
public abstract class SgParameterAwareBase : SgComponentBase 
{ 
    private readonly List<SgParameterRegisterScope> _scopes = new(); 
 
    protected SgParameterRegisterScope CreateRegisterScope() 
    { 
        var scope = new SgParameterRegisterScope(); 
        _scopes.Add(scope); 
        return scope; 
    } 
 
    public override async Task SetParametersAsync(ParameterView parameters) 
    { 
        parameters.SetParameterProperties(this); 
        foreach (var scope in _scopes) 
            await scope.ApplyAsync(parameters); 
        await base.SetParametersAsync(ParameterView.Empty); 
    } 
}