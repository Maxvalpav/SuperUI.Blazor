// SuperUI/Base/Reactive/SignalTracker.cs
//
// Статический трекер реактивных зависимостей.
// При чтении сигнала/computed в активном scope — автоматически регистрирует подписку.

using System.Runtime.CompilerServices;

namespace SuperUI.Base.Reactive;

/// <summary>
/// Статический трекер реактивных зависимостей.
/// </summary>
/// <remarks>
/// [ThreadStatic] обеспечивает изоляцию:
///   Server: каждый circuit имеет свой поток → per-thread = per-circuit.
///   WASM:   однопоточный WebAssembly thread → все операции в одном потоке.
/// </remarks>
public static class SignalTracker
{
    [ThreadStatic] private static SgComponentBase? _currentComponent;
    [ThreadStatic] private static ISignalObserver? _currentObserver;

    // Список сигналов, прочитанных в текущем scope (для SubscribeToTracked).
    [ThreadStatic] private static List<object>? _trackedSignals;

    // ── Component scope ──────────────────────────────────────────────────────
    public static IDisposable EnterScope(SgComponentBase component)
    {
        var prev = (_currentComponent, _currentObserver, _trackedSignals);
        _currentComponent = component;
        _currentObserver = null;
        _trackedSignals = new();
        return new ScopeHandle(prev.Item1, prev.Item2, prev.Item3);
    }

    internal static IDisposable EnterScopeForObserver(ISignalObserver observer)
    {
        var prev = (_currentComponent, _currentObserver, _trackedSignals);
        _currentComponent = null;
        _currentObserver = observer;
        _trackedSignals = new();
        return new ScopeHandle(prev.Item1, prev.Item2, prev.Item3);
    }

    internal static SgComponentBase? Current => _currentComponent;
    internal static ISignalObserver? CurrentObserver => _currentObserver;

    /// <summary>true — есть активный scope (компонент или наблюдатель).</summary>
    public static bool IsTracking => _currentComponent is not null || _currentObserver is not null;

    // ── Track методы ─────────────────────────────────────────────────────────

    /// <summary>Вызывается при чтении SgSignal&lt;T&gt;.Value.</summary>
    internal static void Track<T>(SgSignal<T> signal)
    {
        if (_currentComponent is not null)
        {
            signal.Subscribe(_currentComponent);
        }
        else if (_currentObserver is ISignalObserver<T> typedObs)
        {
            typedObs.OnSignalRead(signal);
            _trackedSignals?.Add(signal);
        }
        else if (_currentObserver is not null)
        {
            _trackedSignals?.Add(signal);
        }
    }

    /// <summary>Вызывается при чтении SgComputed&lt;T&gt;.Value.</summary>
    internal static void TrackComputed<T>(SgComputed<T> computed)
    {
        if (_currentComponent is not null)
        {
            computed.Subscribe(_currentComponent);
        }
        else if (_currentObserver is ISignalObserver<T> typedObs)
        {
            typedObs.OnComputedRead(computed);
            _trackedSignals?.Add(computed);
        }
        else if (_currentObserver is not null)
        {
            _trackedSignals?.Add(computed);
        }
    }

    /// <summary>
    /// Подписать наблюдателя на все сигналы, прочитанные в текущем scope.
    /// Вызывается из SgComputed после первого ComputeInternal.
    /// </summary>
    internal static void SubscribeToTracked(ISignalObserver observer)
    {
        if (_trackedSignals is null) return;
        foreach (var s in _trackedSignals)
        {
            SubscribeUntyped(s, observer);
        }
    }

    private static void SubscribeUntyped(object signal, ISignalObserver observer)
    {
        // Используем reflection-free подход через интерфейс ISignalSubscribable, если есть.
        if (signal is ISignalSubscribable sub)
            sub.SubscribeObserverUntyped(observer);
    }

    // ── ScopeHandle ──────────────────────────────────────────────────────────
    private sealed class ScopeHandle : IDisposable
    {
        private readonly SgComponentBase? _prevComponent;
        private readonly ISignalObserver? _prevObserver;
        private readonly List<object>? _prevTracked;

        public ScopeHandle(
            SgComponentBase? prevComponent,
            ISignalObserver? prevObserver,
            List<object>? prevTracked)
        {
            _prevComponent = prevComponent;
            _prevObserver = prevObserver;
            _prevTracked = prevTracked;
        }

        public void Dispose()
        {
            _currentComponent = _prevComponent;
            _currentObserver = _prevObserver;
            _trackedSignals = _prevTracked;
        }
    }
}

/// <summary>
/// Маркер для сигналов/computed, поддерживающих non-generic подписку наблюдателя.
/// </summary>
internal interface ISignalSubscribable
{
    void SubscribeObserverUntyped(ISignalObserver observer);
}
