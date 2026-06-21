// SuperUI/Services/SgResizeService.cs
// Сервис ResizeObserver: уведомление об изменении размеров DOM-элемента.

using Microsoft.JSInterop;

namespace SuperUI.Services;

/// <summary>
/// Сервис ResizeObserver — реактивные размеры элементов.
/// </summary>
public sealed class SgResizeService : IAsyncDisposable
{
    private readonly IJSRuntime _js;
    private int _disposed;
    private readonly Dictionary<string, Action<SgElementSize>> _callbacks = new();
    private DotNetObjectReference<SgResizeService>? _selfRef;

    public SgResizeService(IJSRuntime js)
    {
        _js = js;
    }

    private DotNetObjectReference<SgResizeService> SelfRef =>
        _selfRef ??= DotNetObjectReference.Create(this);

    /// <summary>Начинает наблюдение за элементом <paramref name="elementId"/>.</summary>
    public async ValueTask ObserveAsync(string elementId, Action<SgElementSize> callback)
    {
        if (Volatile.Read(ref _disposed) == 1) return;
        if (string.IsNullOrWhiteSpace(elementId)) throw new ArgumentException("ElementId is empty.", nameof(elementId));
        ArgumentNullException.ThrowIfNull(callback);

        _callbacks[elementId] = callback;
        try
        {
            await _js.InvokeVoidAsync("SuperUI.observeResize", elementId, SelfRef).ConfigureAwait(false);
        }
        catch (JSDisconnectedException) { _callbacks.Remove(elementId); }
        catch (TaskCanceledException)   { _callbacks.Remove(elementId); }
        catch (JSException)             { _callbacks.Remove(elementId); }
        catch (InvalidOperationException) { _callbacks.Remove(elementId); }
    }

    /// <summary>Прекращает наблюдение.</summary>
    public async ValueTask UnobserveAsync(string elementId)
    {
        if (Volatile.Read(ref _disposed) == 1) return;
        if (string.IsNullOrWhiteSpace(elementId)) return;
        _callbacks.Remove(elementId);
        try
        {
            await _js.InvokeVoidAsync("SuperUI.unobserveResize", elementId).ConfigureAwait(false);
        }
        catch (JSDisconnectedException) { }
        catch (TaskCanceledException)   { }
        catch (JSException)             { }
    }

    [Microsoft.JSInterop.JSInvokable]
    public void OnResize(string elementId, int width, int height, double devicePixelRatio)
    {
        if (_callbacks.TryGetValue(elementId, out var cb))
        {
            try { cb(new SgElementSize(width, height, devicePixelRatio)); }
            catch { /* swallow */ }
        }
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1) return;
        _callbacks.Clear();
        try
        {
            await _js.InvokeVoidAsync("SuperUI.clearResizes").ConfigureAwait(false);
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

/// <summary>Размеры DOM-элемента.</summary>
public readonly record struct SgElementSize(int Width, int Height, double DevicePixelRatio);
