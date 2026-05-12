// SuperUI/Base/Reactive/SignalTracker.cs
//
// ИСПРАВЛЕНИЯ:
//   1. Track<T> принимает typed ISignalObserver<T> — typed dispatch без boxing
//   2. TrackComputed<T> аналогично
//   3. CurrentObserver — typed dispatch через pattern matching
//   4. EnterScopeForObserver принимает ISignalObserver (non-generic) — корректно
//
// УЛУЧШЕНИЯ:
//   1. IsTracking — публичное свойство (для conditional tracking в компонентах)
//   2. ThreadStatic комментарии: Server = per-thread (per-circuit), WASM = single thread
//   3. Защита от null в Track методах

using SuperUI.Base;

namespace SuperUI.Base.Reactive;

/// <summary>
/// Статический трекер реактивных зависимостей.
/// При чтении сигнала/computed в активном scope — автоматически регистрирует подписку.
/// </summary>
/// <remarks>
/// [ThreadStatic] обеспечивает изоляцию:
///   Server: каждый circuit имеет свой поток → per-thread = per-circuit. ✅
///   WASM:   однопоточный WebAssembly thread → все операции в одном потоке. ✅
/// </remarks>
public static class SignalTracker
{
    [ThreadStatic]
    private static SgComponentBase? _currentComponent;

    [ThreadStatic]
    private static ISignalObserver? _currentObserver;   // non-generic для storage

    // ── Component scope ───────────────────────────────────────────────────────

    /// <summary>
    /// Войти в scope компонента. Все чтения сигналов/computed внутри
    /// автоматически подписывают компонент на уведомления.
    /// </summary>
    public static IDisposable EnterScope(SgComponentBase component)
    {
        var prev = (_currentComponent, _currentObserver);
        _currentComponent = component;
        _currentObserver = null;
        return new ScopeHandle(prev.Item1, prev.Item2);
    }

    // ── Observer scope ────────────────────────────────────────────────────────

    /// <summary>
    /// Войти в scope наблюдателя (computed/effect).
    /// Принимает non-generic ISignalObserver (не знает T на уровне TrackAPI).
    /// </summary>
    internal static IDisposable EnterScopeForObserver(ISignalObserver observer)
    {
        var prev = (_currentComponent, _currentObserver);
        _currentComponent = null;
        _currentObserver = observer;
        return new ScopeHandle(prev.Item1, prev.Item2);
    }

    // ── Свойства ──────────────────────────────────────────────────────────────

    internal static SgComponentBase? Current => _currentComponent;
    internal static ISignalObserver? CurrentObserver => _currentObserver;

    /// <summary>true — есть активный scope (компонент или наблюдатель).</summary>
    public static bool IsTracking => _currentComponent is not null || _currentObserver is not null;

    // ── Track методы ──────────────────────────────────────────────────────────

    /// <summary>
    /// Вызывается при чтении SgSignal&lt;T&gt;.Value.
    /// Регистрирует зависимость в текущем scope.
    /// </summary>
    internal static void Track<T>(SgSignal<T> signal)
    {
        if (_currentComponent is not null)
        {
            // Компонент напрямую подписывается на сигнал
            signal.Subscribe(_currentComponent);
        }
        else if (_currentObserver is ISignalObserver<T> typedObs)
        {
            // Typed dispatch — нет boxing, нет dynamic
            typedObs.OnSignalRead(signal);
        }
        else if (_currentObserver is ISignalObserver obs)
        {
            // Fallback для EffectObserver (non-generic)
            // Подписываем мост через SubscribeObserver (не через компонент)
            signal.SubscribeObserver(new EffectSignalBridge<T>(signal, obs));
        }
    }

    /// <summary>
    /// Вызывается при чтении SgComputed&lt;T&gt;.Value.
    /// Регистрирует зависимость в текущем scope.
    /// </summary>
    internal static void TrackComputed<T>(SgComputed<T> computed)
    {
        if (_currentComponent is not null)
        {
            computed.Subscribe(_currentComponent);
        }
        else if (_currentObserver is ISignalObserver<T> typedObs)
        {
            typedObs.OnComputedRead(computed);
        }
        // EffectObserver: computed изменение придёт через цепочку сигналов
    }

    // ── ScopeHandle ───────────────────────────────────────────────────────────

    private sealed class ScopeHandle : IDisposable
    {
        private readonly SgComponentBase? _prevComponent;
        private readonly ISignalObserver? _prevObserver;

        public ScopeHandle(SgComponentBase? prevComponent, ISignalObserver? prevObserver)
        {
            _prevComponent = prevComponent;
            _prevObserver = prevObserver;
        }

        public void Dispose()
        {
            _currentComponent = _prevComponent;
            _currentObserver = _prevObserver;
        }
    }

    // ── EffectSignalBridge ────────────────────────────────────────────────────

    /// <summary>
    /// Мост для подписки EffectObserver (non-generic) на typed SgSignal&lt;T&gt;.
    /// Оборачивает typed observer в ISignalObserver&lt;T&gt;.
    /// Является WeakRef-безопасным через SgSignal.SubscribeObserver.
    /// </summary>
    private sealed class EffectSignalBridge<T> : ISignalObserver<T>, IDisposable
    {
        private readonly SgSignal<T> _signal;
        private readonly ISignalObserver _effect;
        private int _disposed;

        public EffectSignalBridge(SgSignal<T> signal, ISignalObserver effect)
        {
            _signal = signal;
            _effect = effect;
            _signal.SubscribeObserver(this);
        }

        public void OnSignalChanged()
        {
            if (Volatile.Read(ref _disposed) == 1) return;
            _effect.OnSignalChanged();
        }

        public void OnSignalRead(SgSignal<T> signal) { }
        public void OnComputedRead(SgComputed<T> computed) { }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 1) return;
            _signal.UnsubscribeObserver(this);
        }
    }
}
