// SuperUI/Base/Reactive/SignalTracker.cs
// ИСПРАВЛЕНО:
// 1. ОДИН файл — ОДИН тип SignalTracker (устраняет CS0101)
// 2. Track<T>(SgSignal<T>) и Track<T>(Signal<T>) — оба метода в одном классе
// 3. Добавлен EnterScopeForObserver для SgComputed и SgEffect
// 4. [ThreadStatic] корректен для Server (per-thread) и WASM (один поток)

using SuperUI.Base;

namespace SuperUI.Base.Reactive;

/// <summary>
/// Статический трекер для автоматической подписки на сигналы при рендере.
/// </summary>
/// <remarks>
/// ThreadStatic: на Blazor Server каждый запрос имеет свой поток → безопасно.
/// На WASM: один поток → ThreadStatic работает как обычная статика.
///
/// ВАЖНО: CS0101 Fix — этот файл является ЕДИНСТВЕННЫМ определением SignalTracker.
/// Удалите все дублирующие определения SignalTracker из ComponentSignalGraph.cs.
/// </remarks>
public static class SignalTracker
{
    [ThreadStatic]
    private static SgComponentBase? _currentComponent;

    [ThreadStatic]
    private static ISignalObserver? _currentObserver;

    // ── Component scope ──────────────────────────────────────────────────────

    /// <summary>
    /// Открыть scope отслеживания для компонента.
    /// Все сигналы, прочитанные в scope, автоматически подписывают компонент.
    /// </summary>
    public static IDisposable EnterScope(SgComponentBase component)
    {
        var prevComponent = _currentComponent;
        var prevObserver = _currentObserver;
        _currentComponent = component;
        _currentObserver = null;
        return new ScopeHandle(prevComponent, prevObserver);
    }

    // ── Observer scope (для SgComputed, SgEffect) ────────────────────────────

    internal static IDisposable EnterScopeForObserver(ISignalObserver observer)
    {
        var prevComponent = _currentComponent;
        var prevObserver = _currentObserver;
        _currentComponent = null;
        _currentObserver = observer;
        return new ScopeHandle(prevComponent, prevObserver);
    }

    // ── Текущий контекст ─────────────────────────────────────────────────────

    internal static SgComponentBase? Current => _currentComponent;
    internal static ISignalObserver? CurrentObserver => _currentObserver;

    // ── Track методы ─────────────────────────────────────────────────────────

    /// <summary>Автоподписка для SgSignal&lt;T&gt;.</summary>
    internal static void Track<T>(SgSignal<T> signal)
    {
        if (_currentComponent is not null)
            signal.Subscribe(_currentComponent);
        else if (_currentObserver is not null)
            _currentObserver.OnSignalRead(signal);
    }

    /// <summary>Автоподписка для Signal&lt;T&gt; (legacy, обратная совместимость).</summary>
    internal static void Track<T>(Signal<T> signal)
    {
        if (_currentComponent is not null)
            signal.Subscribe(_currentComponent);
    }

    /// <summary>Автоподписка SgComputed&lt;T&gt; (для вложенных computed).</summary>
    internal static void TrackComputed<T>(SgComputed<T> computed)
    {
        if (_currentComponent is not null)
            computed.Subscribe(_currentComponent);
        else if (_currentObserver is not null)
            _currentObserver.OnComputedRead(computed);
    }

    // ── ScopeHandle ──────────────────────────────────────────────────────────

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
}