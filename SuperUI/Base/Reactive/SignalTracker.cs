// SuperUI/Base/Reactive/SignalTracker.cs
// ИСПРАВЛЕНО:
// 1. Один файл — один тип SignalTracker (устранён CS0101)
// 2. Track<T>(SgSignal<T>) и Track<T>(Signal<T>) — оба метода в одном классе
// 3. [ThreadStatic] корректен для WASM (один поток) и Server (per-thread контекст)
using SuperUI.Base;

namespace SuperUI.Base.Reactive;

/// <summary>
/// Статический трекер для автоматической подписки на сигналы при рендере.
/// Используется в RefreshAsync для scope-based signal tracking.
///
/// ThreadStatic: на Blazor Server каждый запрос имеет свой поток → безопасно.
/// На WASM: один поток → ThreadStatic работает как обычная статика.
/// </summary>
public static class SignalTracker
{
    [ThreadStatic]
    private static SgComponentBase? _currentComponent;

    /// <summary>
    /// Открыть scope отслеживания сигналов для компонента.
    /// Все сигналы, прочитанные в scope, автоматически подписывают компонент.
    /// </summary>
    public static IDisposable EnterScope(SgComponentBase component)
    {
        var previous = _currentComponent;
        _currentComponent = component;
        return new ScopeHandle(previous);
    }

    /// <summary>Текущий компонент в scope (null если вне scope).</summary>
    internal static SgComponentBase? Current => _currentComponent;

    /// <summary>Автоподписка для SgSignal<T>.</summary>
    internal static void Track<T>(SgSignal<T> signal)
    {
        if (_currentComponent is not null)
            signal.Subscribe(_currentComponent);
    }

    /// <summary>Автоподписка для Signal<T> (legacy).</summary>
    internal static void Track<T>(Signal<T> signal)
    {
        if (_currentComponent is not null)
            signal.Subscribe(_currentComponent);
    }

    private sealed class ScopeHandle : IDisposable
    {
        private readonly SgComponentBase? _previous;
        public ScopeHandle(SgComponentBase? previous) { _previous = previous; }
        public void Dispose() { _currentComponent = _previous; }
    }
}