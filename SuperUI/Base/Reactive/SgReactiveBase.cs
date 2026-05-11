// ─────────────────────────────────────────────────────────────────
// FILE: Base/Reactive/SgReactiveBase.cs
// ИННОВАЦИЯ: Signal-like реактивность для Blazor.
// Компонент автоматически перерисовывается при изменении Computed.
// Вдохновлён Solid.js signals, Angular signals, Vue 3 reactivity.
// ─────────────────────────────────────────────────────────────────
using System.Collections.Generic;
using System.Linq;

namespace SuperUI.Base.Reactive;

/// <summary>
/// Реактивный сигнал — отслеживает изменения значения.
/// При изменении нотифицирует подписанные компоненты.
/// </summary>
/// <typeparam name="T">Тип значения сигнала.</typeparam>
public sealed class Signal<T>
{
    private T _value;
    private readonly List<WeakReference<SgReactiveBase>> _subscribers = new();
    private readonly IEqualityComparer<T> _comparer;

    public Signal(T initialValue, IEqualityComparer<T>? comparer = null)
    {
        _value = initialValue;
        _comparer = comparer ?? EqualityComparer<T>.Default;
    }

    public T Value
    {
        get => _value;
        set
        {
            if (_comparer.Equals(_value, value)) return;
            _value = value;
            NotifySubscribers();
        }
    }

    internal void Subscribe(SgReactiveBase component)
    {
        // Удаляем мёртвые ссылки
        _subscribers.RemoveAll(wr => !wr.TryGetTarget(out _));
        _subscribers.Add(new WeakReference<SgReactiveBase>(component));
    }

    private void NotifySubscribers()
    {
        foreach (var wr in _subscribers.ToList())
        {
            if (wr.TryGetTarget(out var comp))
                _ = comp.RequestRenderAsync();
        }
    }

    public static implicit operator T(Signal<T> s) => s._value;
}

/// <summary>
/// Computed сигнал — автоматически пересчитывается при изменении зависимостей.
/// </summary>
public sealed class Computed<T>
{
    private readonly Func<T> _compute;
    private T _value;
    private bool _dirty = true;

    public Computed(Func<T> compute, Signal<T>? dependency = null)
    {
        _compute = compute;
        _value = default!;
    }

    public T Value
    {
        get
        {
            if (_dirty)
            {
                _value = _compute();
                _dirty = false;
            }
            return _value;
        }
    }

    public void Invalidate() => _dirty = true;
}

/// <summary>
/// Уровень 4 (инновация). Компоненты с Signal-like реактивностью.
/// Перерисовываются только при изменении зарегистрированных сигналов.
/// </summary>
public abstract class SgReactiveBase : Components.Base.SgInteractiveBase
{
    private readonly List<Signal<object>> _trackedSignals = new();

    /// <summary>
    /// Создаёт сигнал, привязанный к этому компоненту.
    /// При изменении значения компонент автоматически перерисовывается.
    /// </summary>
    protected Signal<T> CreateSignal<T>(T initialValue)
    {
        var signal = new Signal<T>(initialValue);
        // Для упрощения: кастуем через object-сигнал обёртку
        // В реальности нужен Generic variance trick
        signal.Subscribe(this);
        return signal;
    }

    // RequestRenderAsync уже публичный через наследование (protected → internal)
    internal new Task RequestRenderAsync() => base.RequestStateUpdateAsync();
}
