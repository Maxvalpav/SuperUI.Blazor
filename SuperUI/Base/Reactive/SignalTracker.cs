// SuperUI/Base/Reactive/SignalTracker.cs
//
// ДОРАБОТКИ:
// 1. Track<T>(SgSignal<T>) generic-метод (не object)
// 2. EnterScopeForObserver — корректно принимает ISignalObserver (без generic)
// 3. ТОЛЬКО ОДИН файл — удалите все дублирующие определения SignalTracker

using SuperUI.Base;

namespace SuperUI.Base.Reactive;

/// <summary>
/// Статический трекер для автоматической подписки на сигналы при рендере.
/// [ThreadStatic] — Server: per-thread (per-circuit), WASM: один поток.
/// </summary>
public static class SignalTracker
{
    [ThreadStatic] private static SgComponentBase? _currentComponent;
    [ThreadStatic] private static object?          _currentObserver; // ISignalObserver без generic

    // ── Component scope ───────────────────────────────────────────────────────────
    public static IDisposable EnterScope(SgComponentBase component)
    {
        var prevComponent = _currentComponent;
        var prevObserver  = _currentObserver;
        _currentComponent = component;
        _currentObserver  = null;
        return new ScopeHandle(prevComponent, prevObserver);
    }

    // ── Observer scope ────────────────────────────────────────────────────────────
    internal static IDisposable EnterScopeForObserver(object observer)
    {
        var prevComponent = _currentComponent;
        var prevObserver  = _currentObserver;
        _currentComponent = null;
        _currentObserver  = observer;
        return new ScopeHandle(prevComponent, prevObserver);
    }

    internal static SgComponentBase? Current         => _currentComponent;
    internal static object?          CurrentObserver => _currentObserver;

    // ── Track методы ──────────────────────────────────────────────────────────────
    internal static void Track<T>(SgSignal<T> signal)
    {
        if (_currentComponent is not null)
            signal.Subscribe(_currentComponent);
        else if (_currentObserver is ISignalObserver<T> obs)
            obs.OnSignalRead(signal);
    }

    internal static void TrackComputed<T>(SgComputed<T> computed)
    {
        if (_currentComponent is not null)
            computed.Subscribe(_currentComponent);
        else if (_currentObserver is ISignalObserver<T> obs)
            obs.OnComputedRead(computed);
    }

    // ── ScopeHandle ───────────────────────────────────────────────────────────────
    private sealed class ScopeHandle : IDisposable
    {
        private readonly SgComponentBase? _prevComponent;
        private readonly object?          _prevObserver;

        public ScopeHandle(SgComponentBase? prevComponent, object? prevObserver)
        {
            _prevComponent = prevComponent;
            _prevObserver  = prevObserver;
        }

        public void Dispose()
        {
            _currentComponent = _prevComponent;
            _currentObserver  = _prevObserver;
        }
    }
}
