// SuperUI/Base/SgResizeObserverBase.cs
// ✅ UX-6: встроенный debounce — не нужно делать в каждом компоненте
// ✅ SSR: ничего не делает при IsStaticSSR / IsPrerendering
// ✅ Обратная совместимость: SgElementSize, ElementWidth/Height, OnElementResizedAsync сохранены

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace SuperUI.Base;

/// <summary>Данные изменения размера элемента.</summary>
public sealed record SgSizeChangedEventArgs(
    double Width,
    double Height,
    double Left,
    double Top);

/// <summary>
/// Базовый класс для компонентов, наблюдающих за изменением размеров через ResizeObserver.
/// Встроенный debounce предотвращает лавину обновлений при плавном ресайзе.
/// </summary>
public abstract class SgResizeObserverBase : SgJsComponentBase
{
    [Parameter] public EventCallback<SgSizeChangedEventArgs> OnSizeChanged { get; set; }
    [Parameter] public int ResizeDebounceMs { get; set; } = 100;
    [Parameter] public bool ObserveOnFirstRender { get; set; } = true;

    // ── Состояние ────────────────────────────────────────────────────────────

    protected double CurrentWidth { get; private set; }
    protected double CurrentHeight { get; private set; }
    protected bool IsObserving { get; private set; }

    // Обратная совместимость
    protected ElementReference? ObservedElement { get; set; }
    protected SgElementSize? ElementSize { get; private set; }
    protected double ElementWidth => ElementSize?.Width ?? 0;
    protected double ElementHeight => ElementSize?.Height ?? 0;
    protected double AspectRatio => ElementSize?.AspectRatio ?? double.NaN;
    protected bool IsLandscape => ElementSize?.IsLandscape ?? false;

    private DotNetObjectReference<SgResizeObserverBase>? _selfRef;
    private CancellationTokenSource? _resizeCts;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    protected override async Task OnFirstRenderAsync()
    {
        await base.OnFirstRenderAsync();
        if (IsStaticSSR || IsPrerendering) return;

        _selfRef = DotNetObjectReference.Create(this);

        if (ObserveOnFirstRender)
        {
            // Новый путь: по ComponentId
            await StartObservingAsync(ComponentId);
        }
        else if (ObservedElement.HasValue)
        {
            // Обратная совместимость: по ElementReference
            await StartObservingAsync();
        }
    }

    // ── Публичные методы ──────────────────────────────────────────────────────

    /// <summary>Начать наблюдение по elementId (новый API).</summary>
    protected async Task StartObservingAsync(string elementId)
    {
        if (IsStaticSSR || IsDisposed) return;
        await SafeInvokeVoidAsync("superui.observeResizeById", elementId, _selfRef, ResizeDebounceMs);
        IsObserving = true;
    }

    /// <summary>Начать наблюдение по ElementReference (обратная совместимость).</summary>
    protected async Task StartObservingAsync()
    {
        if (IsObserving || ObservedElement is null || IsPrerendering) return;
        _selfRef ??= DotNetObjectReference.Create(this);
        await SafeInvokeVoidAsync("superui.observeResize", ObservedElement.Value, _selfRef, nameof(OnResizeCallback));
        IsObserving = true;
    }

    /// <summary>Остановить наблюдение по elementId.</summary>
    protected async Task StopObservingAsync(string elementId)
    {
        if (IsDisposed) return;
        await SafeInvokeVoidAsync("superui.unobserveResizeById", elementId);
        IsObserving = false;
    }

    /// <summary>Остановить наблюдение по ElementReference (обратная совместимость).</summary>
    protected async Task StopObservingAsync()
    {
        if (!IsObserving || ObservedElement is null) return;
        await SafeInvokeVoidAsync("superui.unobserveResize", ObservedElement.Value);
        IsObserving = false;
    }

    // ── JS Callbacks ──────────────────────────────────────────────────────────

    /// <summary>Вызывается из JS (новый API: width, height, left, top).</summary>
    [JSInvokable]
    public async Task OnResizedAsync(double width, double height, double left, double top)
    {
        if (IsDisposed) return;

        CurrentWidth = width;
        CurrentHeight = height;

        _resizeCts?.Cancel();
        _resizeCts?.Dispose();
        _resizeCts = CancellationTokenSource.CreateLinkedTokenSource(ComponentToken);
        var ct = _resizeCts.Token;

        try
        {
            // Дополнительный debounce на .NET стороне (JS уже дебаунсит, это страховка)
            if (ResizeDebounceMs > 0)
                await Task.Delay(ResizeDebounceMs / 2, ct);

            if (ct.IsCancellationRequested || IsDisposed) return;

            var args = new SgSizeChangedEventArgs(width, height, left, top);
            await InvokeAsync(async () =>
            {
                OnResized(args);
                await OnSizeChanged.InvokeAsync(args);
                StateHasChanged();
            });
        }
        catch (OperationCanceledException) { }
    }

    /// <summary>Вызывается из JS (обратная совместимость: только width, height).</summary>
    [JSInvokable]
    public async Task OnResizeCallback(double width, double height)
    {
        if (IsDisposed) return;

        var newSize = new SgElementSize(width, height);
        if (ElementSize == newSize) return;

        ElementSize = newSize;
        CurrentWidth = width;
        CurrentHeight = height;

        await OnElementResizedAsync(newSize);
        await InvokeAsync(StateHasChanged);
    }

    /// <summary>Переопределите для обработки изменения размера (новый API).</summary>
    protected virtual void OnResized(SgSizeChangedEventArgs args) { }

    /// <summary>Переопределите для обработки изменения размера (обратная совместимость).</summary>
    protected virtual Task OnElementResizedAsync(SgElementSize newSize) => Task.CompletedTask;

    // ── Dispose ───────────────────────────────────────────────────────────────

    protected override async ValueTask DisposeComponentAsync()
    {
        _resizeCts?.Cancel();
        _resizeCts?.Dispose();

        if (_selfRef is not null)
        {
            if (IsObserving)
            {
                if (ObservedElement.HasValue)
                    await StopObservingAsync();
                else
                    await StopObservingAsync(ComponentId);
            }
            _selfRef.Dispose();
            _selfRef = null;
        }

        await base.DisposeComponentAsync();
    }
}

/// <summary>Размеры HTML-элемента (обратная совместимость).</summary>
public readonly record struct SgElementSize(double Width, double Height)
{
    public double AspectRatio => Height == 0 ? double.NaN : Width / Height;
    public bool IsLandscape => Width > Height;
    public bool IsPortrait => Width <= Height;
    public bool IsSquare => Math.Abs(Width - Height) < 1;
    public double Area => Width * Height;
    public double Perimeter => 2 * (Width + Height);
    public double Diagonal => Math.Sqrt(Width * Width + Height * Height);
    public override string ToString() => $"{Width:F1}×{Height:F1}px";
}
