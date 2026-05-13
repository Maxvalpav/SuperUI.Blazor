using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace SuperUI.Base.Reactive;

/// <summary>
/// Базовый класс для реактивных компонентов SuperUI.
/// Автоматически отслеживает сигналы, прочитанные во время рендеринга.
/// </summary>
public abstract class SgReactiveComponentBase : SgComponentBase, ISignalObserver
{
    [ThreadStatic]
    private static ISignalObserver? _currentObserver;

    /// <summary>
    /// Регистрация зависимости текущего активного наблюдателя от сигнала.
    /// Вызывается автоматически при чтении SgSignal.Value.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void TrackSignalImplicitly(ISgSignal signal)
    {
        _currentObserver?.OnSignalRead(signal);
    }

    /// <summary>
    /// Внутренний метод для регистрации зависимости. 
    /// Можно расширить ISignalObserver если нужно, но пока используем виртуальный метод.
    /// </summary>
    internal virtual void OnSignalRead(ISgSignal signal)
    {
        signal.Subscribe(this);
    }

    /// <summary>
    /// Вызывается при изменении любого отслеживаемого сигнала.
    /// </summary>
    public virtual void OnSignalChanged(ISgSignal signal)
    {
        RequestRender();
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var prev = _currentObserver;
        _currentObserver = this;
        try
        {
            BuildReactiveRenderTree(builder);
        }
        finally
        {
            _currentObserver = prev;
        }
    }

    /// <summary>
    /// Переопределите этот метод вместо BuildRenderTree для использования реактивности.
    /// </summary>
    protected abstract void BuildReactiveRenderTree(RenderTreeBuilder builder);

    // ── Фабрики сигналов ───────────────────────────────────────────────────────

    protected SgSignal<T> Signal<T>(T initial, string? debugName = null)
    {
        var signal = new SgSignal<T>(initial, debugName);
        (_reactiveDisposables ??= new()).Add(signal);
        return signal;
    }

    protected SgSignal<T> Signal<T>(T initial, IEqualityComparer<T> comparer, string? debugName = null)
    {
        var signal = new SgSignal<T>(initial, comparer, debugName);
        (_reactiveDisposables ??= new()).Add(signal);
        return signal;
    }

    protected SgComputed<T> Computed<T>(Func<T> compute, string? debugName = null)
    {
        var computed = new SgComputed<T>(compute, null, debugName);
        (_reactiveDisposables ??= new()).Add(computed);
        return computed;
    }

    protected SgEffect Effect(Action action, Action<Exception>? onError = null)
    {
        var effect = new SgEffect(action, onError);
        (_reactiveDisposables ??= new()).Add(effect);
        return effect;
    }

    protected SgEffect Effect(Func<Task> action, Action<Exception>? onError = null)
    {
        var effect = new SgEffect(action, onError);
        (_reactiveDisposables ??= new()).Add(effect);
        return effect;
    }

    // Вспомогательный статический метод для установки текущего наблюдателя (для Computed/Effect)
    internal static IDisposable EnterScope(ISignalObserver observer)
    {
        return new ObserverScope(observer);
    }

    private sealed class ObserverScope : IDisposable
    {
        private readonly ISignalObserver? _prev;
        public ObserverScope(ISignalObserver observer)
        {
            _prev = _currentObserver;
            _currentObserver = observer;
        }
        public void Dispose()
        {
            _currentObserver = _prev;
        }
    }
}

/// <summary>
/// Расширение для поддержки трекинга (внутреннее).
/// </summary>
internal static class SignalObserverExtensions
{
    public static void OnSignalRead(this ISignalObserver observer, ISgSignal signal)
    {
        if (observer is SgReactiveComponentBase reactiveComponent)
        {
            reactiveComponent.OnSignalRead(signal);
        }
        else if (observer is ISignalTrackingObserver trackingObserver)
        {
            trackingObserver.OnSignalRead(signal);
        }
        else
        {
            // Дефолтное поведение — просто подписываемся
            signal.Subscribe(observer);
        }
    }
}

/// <summary>
/// Интерфейс для наблюдателей, которые хотят знать о факте чтения сигнала (например, Computed).
/// </summary>
internal interface ISignalTrackingObserver : ISignalObserver
{
    void OnSignalRead(ISgSignal signal);
}
