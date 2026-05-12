// SuperUI/Base/Reactive/OffscreenOptimizer.cs
//
// УЛУЧШЕНИЯ:
//   1. IsVisible — публичное свойство
//   2. MarkInitialized() — явная инициализация из компонента
//   3. OnVisibilityChanged — потокобезопасный (volatile)
//   4. Документирован JSInvokable паттерн

using SuperUI.Base.Hooks;

namespace SuperUI.Base.Reactive;

/// <summary>
/// Hook для оптимизации рендеров компонентов вне viewport.
/// Использует IntersectionObserver через JS interop.
/// </summary>
/// <remarks>
/// Пример использования в компоненте:
/// <code>
/// private OffscreenOptimizer _offscreen = null!;
///
/// protected override void OnInitialized()
/// {
///     _offscreen = new OffscreenOptimizer(this);
///     AddHook(_offscreen);
/// }
///
/// protected override async Task OnFirstRenderAsync()
/// {
///     await JS.InvokeVoidAsync("superui.observeVisibility",
///         DotNetObjectReference.Create(this), ComponentId);
///     _offscreen.MarkInitialized();
/// }
///
/// [JSInvokable]
/// public void OnVisibilityChanged(bool isVisible)
///     => _offscreen.OnVisibilityChanged(isVisible);
/// </code>
/// </remarks>
public sealed class OffscreenOptimizer : IComponentHook, IRenderHook, IAsyncDisposable
{
    private readonly SgComponentBase _component;
    private volatile bool _isVisible = true;    // default: visible (до инициализации)
    private volatile bool _initialized;

    public OffscreenOptimizer(SgComponentBase component)
    {
        _component = component ?? throw new ArgumentNullException(nameof(component));
    }

    /// <summary>true — компонент сейчас в viewport.</summary>
    public bool IsVisible => _isVisible;

    /// <summary>true — IntersectionObserver инициализирован.</summary>
    public bool IsInitialized => _initialized;

    // ── IComponentHook ────────────────────────────────────────────────────────

    public void OnInitialized(SgComponentBase component) { }
    public void OnParametersSet(SgComponentBase component) { }
    public void OnAfterRender(SgComponentBase component, bool firstRender) { }

    // ── IRenderHook ───────────────────────────────────────────────────────────

    public bool ShouldRender(SgComponentBase component)
    {
        // До инициализации Observer — рендерим всегда (не блокируем первый рендер)
        if (!_initialized) return true;
        return _isVisible;
    }

    // ── JS Callback ───────────────────────────────────────────────────────────

    /// <summary>
    /// Вызывается из JavaScript при изменении видимости элемента.
    /// Делегируется от [JSInvokable] метода компонента.
    /// </summary>
    public void OnVisibilityChanged(bool isVisible)
    {
        if (_isVisible == isVisible) return;
        _isVisible = isVisible;

        // При появлении в viewport — форсируем рендер (показываем актуальные данные)
        if (isVisible && !_component.IsDisposed)
            _ = _component.RefreshAsync();
    }

    /// <summary>Отметить что IntersectionObserver инициализирован.</summary>
    public void MarkInitialized() => _initialized = true;

    /// <summary>Принудительно пометить невидимым (без JS).</summary>
    public void SetVisible(bool visible) => _isVisible = visible;

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
