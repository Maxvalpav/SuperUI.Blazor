// SuperUI/Services/SgIntersectionService.cs
// Сервис IntersectionObserver: уведомление о появлении элементов в viewport.

using Microsoft.JSInterop;

namespace SuperUI.Services;

/// <summary>
/// Сервис IntersectionObserver — ленивая загрузка, infinite scroll, анимации появления.
/// </summary>
public sealed class SgIntersectionService : IAsyncDisposable
{
    private readonly IJSRuntime _js;
    private int _disposed;
    private readonly Dictionary<string, Action<bool, double>> _callbacks = new();
    private DotNetObjectReference<SgIntersectionService>? _selfRef;

    public SgIntersectionService(IJSRuntime js)
    {
        _js = js;
    }

    private DotNetObjectReference<SgIntersectionService> SelfRef =>
        _selfRef ??= DotNetObjectReference.Create(this);

    /// <summary>
    /// Начинает наблюдение за элементом <paramref name="elementId"/>.
    /// <paramref name="callback"/> вызывается при изменении видимости: (isIntersecting, intersectionRatio).
    /// </summary>
    public async ValueTask ObserveAsync(string elementId, Action<bool, double> callback, SgIntersectionOptions? options = null)
    {
        if (Volatile.Read(ref _disposed) == 1) return;
        if (string.IsNullOrWhiteSpace(elementId)) throw new ArgumentException("ElementId is empty.", nameof(elementId));
        ArgumentNullException.ThrowIfNull(callback);

        _callbacks[elementId] = callback;
        try
        {
            await _js.InvokeVoidAsync("SuperUI.observeIntersection", elementId,
                options?.RootSelector, options?.RootMargin ?? "0px", options?.Threshold ?? 0.0,
                options?.Once ?? false, SelfRef).ConfigureAwait(false);
        }
        catch (JSDisconnectedException) { _callbacks.Remove(elementId); }
        catch (TaskCanceledException)   { _callbacks.Remove(elementId); }
        catch (JSException)             { _callbacks.Remove(elementId); }
        catch (InvalidOperationException) { _callbacks.Remove(elementId); }
    }

    /// <summary>Прекращает наблюдение за элементом.</summary>
    public async ValueTask UnobserveAsync(string elementId)
    {
        if (Volatile.Read(ref _disposed) == 1) return;
        if (string.IsNullOrWhiteSpace(elementId)) return;
        _callbacks.Remove(elementId);
        try
        {
            await _js.InvokeVoidAsync("SuperUI.unobserveIntersection", elementId).ConfigureAwait(false);
        }
        catch (JSDisconnectedException) { }
        catch (TaskCanceledException)   { }
        catch (JSException)             { }
    }

    [Microsoft.JSInterop.JSInvokable]
    public void OnIntersect(string elementId, bool isIntersecting, double ratio)
    {
        if (_callbacks.TryGetValue(elementId, out var cb))
        {
            try { cb(isIntersecting, ratio); }
            catch { /* swallow */ }

            // If Once=true, the JS side will unobserve automatically after the first intersection.
        }
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1) return;
        _callbacks.Clear();
        try
        {
            await _js.InvokeVoidAsync("SuperUI.clearIntersections").ConfigureAwait(false);
        }
        catch (JSDisconnectedException) { }
        catch (TaskCanceledException)   { }
        catch (JSException)             { }
        catch (ObjectDisposedException) { }
        finally
        {
            _selfRef?.Dispose();
            _selfRef = null;
        }
    }
}

/// <summary>Опции IntersectionObserver.</summary>
public sealed class SgIntersectionOptions
{
    /// <summary>CSS-селектор корневого элемента (null = viewport).</summary>
    public string? RootSelector { get; init; }
    /// <summary>Margin корня (default "0px").</summary>
    public string? RootMargin { get; init; }
    /// <summary>Порог пересечения (0.0 – 1.0, default 0.0).</summary>
    public double Threshold { get; init; }
    /// <summary>True — отписаться после первого появления.</summary>
    public bool Once { get; init; }
}
