// SuperUI/Base/Reactive/SgReactiveComponentBase.cs
// ИСПРАВЛЕНИЯ v2:
// ✅ W6: документация о [ThreadStatic] + NOTE для будущего async BuildRenderTree
// ✅ Убран дублирующий SignalObserverExtensions.OnSignalRead
// ✅ EnterScope: возвращает IDisposable (уже корректно)

using Microsoft.AspNetCore.Components.Rendering;
using System.Runtime.CompilerServices;

namespace SuperUI.Base.Reactive;

/// <summary>
/// Базовый класс для реактивных компонентов SuperUI.
/// Автоматически отслеживает сигналы, прочитанные во время рендеринга.
///
/// ВАЖНО о [ThreadStatic]:
/// - WASM: однопоточная среда — [ThreadStatic] безопасен.
/// - Server: BuildRenderTree всегда синхронный вызов в рамках одного circuit.
///   При await внутри компонента поток меняется, но BuildRenderTree к этому моменту
///   уже завершён. Трекинг корректен.
/// - Не читайте сигналы после await в BuildRenderTree (если такое появится в будущем).
/// </summary>
public abstract class SgReactiveComponentBase : SgComponentBase,
    ISignalObserver, ISignalTrackingObserver
{
    // ✅ [ThreadStatic] безопасен для синхронного BuildRenderTree
    [ThreadStatic]
    private static ISignalObserver? _currentObserver;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void TrackSignalImplicitly(ISgSignal signal)
    {
        var observer = _currentObserver;
        if (observer is null) return;
        if (observer is ISignalTrackingObserver tracking)
            tracking.OnSignalRead(signal);
        else
            signal.Subscribe(observer);
    }

    public virtual void OnSignalRead(ISgSignal signal)
    {
        signal.Subscribe(this);
    }

    public virtual void OnSignalChanged(ISgSignal signal)
    {
        RequestRender();
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var prev = _currentObserver;
        _currentObserver = this;
        try { BuildReactiveRenderTree(builder); }
        finally { _currentObserver = prev; }
    }

    protected abstract void BuildReactiveRenderTree(RenderTreeBuilder builder);

    // ── Фабрики сигналов ─────────────────────────────────────────────────────
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

    internal static IDisposable EnterScope(ISignalObserver observer) => new ObserverScope(observer);

    private sealed class ObserverScope : IDisposable
    {
        private readonly ISignalObserver? _prev;
        public ObserverScope(ISignalObserver observer)
        {
            _prev = _currentObserver;
            _currentObserver = observer;
        }
        public void Dispose() => _currentObserver = _prev;
    }
}