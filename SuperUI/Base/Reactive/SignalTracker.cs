using SuperUI.Base;

namespace SuperUI.Reactive;

/// <summary>
/// Трекер сигналов для текущего рендера.
/// Хранит текущий компонент для автоматической подписки на сигналы.
/// </summary>
public static class SignalTracker
{
    private static readonly AsyncLocal<SgComponentBase?> _currentComponent = new();

    internal static void Track<T>(Signal<T> signal)
    {
        var component = _currentComponent.Value;
        if (component != null)
            signal.Subscribe(component);
    }

    internal static IDisposable EnterScope(SgComponentBase component)
    {
        _currentComponent.Value = component;
        return new ScopeDisposable(() => _currentComponent.Value = null);
    }
}

internal sealed class ScopeDisposable : IDisposable
{
    private readonly Action _onDispose;
    public ScopeDisposable(Action onDispose) => _onDispose = onDispose;
    public void Dispose() => _onDispose();
}