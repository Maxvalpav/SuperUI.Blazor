// SuperUI/Base/Services/SgIntersectionObserver.cs
// УНИКАЛЬНЫЙ КЛАСС — IntersectionObserver API как сигнал.

using Microsoft.JSInterop;
using SuperUI.Base.Reactive;

namespace SuperUI.Base.Services;

/// <summary>
/// Обёртка над IntersectionObserver API как сигнал.
/// Автоматически обновляет сигнал при изменении видимости элемента.
/// 
/// Использование:
/// <code>
/// var observer = new SgIntersectionObserver(jsRuntime, elementId);
/// // В рендере: @if (observer.IsVisible.Value) { ... }
/// </code>
/// </summary>
public sealed class SgIntersectionObserver : IAsyncDisposable
{
    private readonly IJSRuntime _js;
    private readonly string _elementId;
    private readonly SgSignal<bool> _isVisible;
    private readonly SgSignal<double> _intersectionRatio;
    private DotNetObjectReference<SgIntersectionObserver>? _dotNetRef;
    private bool _initialized;

    public IReadOnlySignal<bool> IsVisible => _isVisible;
    public IReadOnlySignal<double> IntersectionRatio => _intersectionRatio;

    public SgIntersectionObserver(
        IJSRuntime js,
        string elementId,
        double threshold = 0.1,
        string? rootMargin = null)
    {
        _js = js ?? throw new ArgumentNullException(nameof(js));
        _elementId = elementId ?? throw new ArgumentNullException(nameof(elementId));
        _isVisible = new SgSignal<bool>(false, $"intersect-visible-{elementId}");
        _intersectionRatio = new SgSignal<double>(0, $"intersect-ratio-{elementId}");
    }

    /// <summary>Инициализировать observer (вызвать в OnAfterRenderAsync первого рендера).</summary>
    public async Task InitializeAsync()
    {
        if (_initialized) return;
        _dotNetRef = DotNetObjectReference.Create(this);

        try
        {
            await _js.InvokeVoidAsync("SuperUI.intersectionObserver.observe",
                _elementId, _dotNetRef);
            _initialized = true;
        }
        catch (JSException)
        {
            // SSR — не инициализируем
        }
    }

    /// <summary>Вызывается из JS.</summary>
    [JSInvokable]
    public void OnIntersectionChanged(bool isIntersecting, double ratio)
    {
        _isVisible.Set(isIntersecting);
        _intersectionRatio.Set(ratio);
    }

    /// <summary>Отключить observer.</summary>
    public async Task DisconnectAsync()
    {
        if (!_initialized) return;

        try
        {
            await _js.InvokeVoidAsync("SuperUI.intersectionObserver.disconnect", _elementId);
        }
        catch (JSException) { }
    }

    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync();
        _dotNetRef?.Dispose();
        _isVisible.Dispose();
        _intersectionRatio.Dispose();
    }
}
