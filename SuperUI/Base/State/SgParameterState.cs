// SuperUI/Base/State/SgParameterState.cs
// ИСПРАВЛЕНО:
// ✅ CS0101: удалён дублирующий SgParameterAwareBase (строки 192-212 оригинала)
// ✅ CS0111: удалены дублирующие CreateRegisterScope и SetParametersAsync
// ✅ ЛОГИКА: SetParameterAsync больше не принимает бесполезный ParameterView
// ✅ ЛОГИКА: чтение значения из ParameterView напрямую, а не через геттер

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
    private readonly string _parameterName;
    private readonly Func<T> _parameterGetter;
    private readonly Func<EventCallback<T>>? _callbackGetter;
    private readonly Func<T, Task>? _changeHandler;
    private readonly IEqualityComparer<T> _comparer;
    private bool _initialized;

    /// <summary>Текущее значение параметра.</summary>
    public T Value => _value;

    internal SgParameterState(
        string parameterName,
        Func<T> parameterGetter,
        Func<EventCallback<T>>? callbackGetter,
        Func<T, Task>? changeHandler,
        IEqualityComparer<T>? comparer)
    {
        _parameterName = parameterName;
        _parameterGetter = parameterGetter;
        _callbackGetter = callbackGetter;
        _changeHandler = changeHandler;
        _comparer = comparer ?? EqualityComparer<T>.Default;
    }

    /// <summary>
    /// Вызывается из SetParametersAsync компонента.
    /// Обнаруживает изменение и вызывает change handler.
    /// ✅ FIX: читаем из ParameterView напрямую через TryGetValue
    /// </summary>
    internal async Task SetParameterAsync(ParameterView parameters)
    {
        // Пробуем прочитать из ParameterView напрямую — это безопаснее,
        // так как SetParameterProperties ещё не вызывался
        T newValue;
        if (parameters.TryGetValue(_parameterName, out T? fromView))
            newValue = fromView!;
        else
            newValue = _parameterGetter(); // фолбэк на геттер если параметра нет в view

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
/// Scope для регистрации параметров.
/// </summary>
public sealed class SgParameterRegisterScope
{
    private readonly List<Func<ParameterView, Task>> _handlers = new();

    internal SgParameterRegisterScope() { }

    public SgParameterStateBuilder<T> RegisterParameter<T>(string parameterName)
        => new(parameterName, this);

    internal void AddHandler(Func<ParameterView, Task> handler)
        => _handlers.Add(handler);

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

    public SgParameterStateBuilder<T> WithParameter(Func<T> getter)
    {
        _getter = getter;
        return this;
    }

    public SgParameterStateBuilder<T> WithEventCallback(Func<EventCallback<T>> callbackGetter)
    {
        _callbackGetter = callbackGetter;
        return this;
    }

    public SgParameterStateBuilder<T> WithChangeHandler(Func<T, Task> handler)
    {
        _changeHandler = handler;
        return this;
    }

    public SgParameterStateBuilder<T> WithChangeHandler(Action<T> handler)
    {
        _changeHandler = v => { handler(v); return Task.CompletedTask; };
        return this;
    }

    public SgParameterStateBuilder<T> WithComparer(IEqualityComparer<T> comparer)
    {
        _comparer = comparer;
        return this;
    }

    public SgParameterState<T> Build()
    {
        if (_getter == null)
            throw new InvalidOperationException($"WithParameter() must be called before Build() for parameter '{_parameterName}'");

        var state = new SgParameterState<T>(_parameterName, _getter, _callbackGetter, _changeHandler, _comparer);
        _scope.AddHandler(parameters => state.SetParameterAsync(parameters));
        return state;
    }
}

// ✅ КЛАСС SgParameterAwareBase УДАЛЁН ИЗ ЭТОГО ФАЙЛА.
// Он объявлен в SuperUI/Base/State/SgParameterAwareBase.cs
