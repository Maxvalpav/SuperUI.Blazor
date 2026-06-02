// SuperUI/Services/SgViewportService.cs
// Сервис размеров окна и devicePixelRatio с реактивной подпиской.

using Microsoft.JSInterop;

namespace SuperUI.Services;

/// <summary>
/// Сервис viewport: текущие размеры окна, scroll-позиция, devicePixelRatio.
/// </summary>
/// <remarks>
/// <para>Подписывается на <c>resize</c> и <c>scroll</c> события браузера, дебаунсит
/// обновления (через JS-throttle), уведомляет подписчиков на <see cref="Changed"/>.</para>
/// <para>SSR-safe: в prerender <see cref="Width"/>/<see cref="Height"/> — 0.</para>
/// </remarks>
public sealed class SgViewportService : IAsyncDisposable
{
    private readonly IJSRuntime _js;
    private int _disposed;
    private DotNetObjectReference<SgViewportService>? _selfRef;

    public SgViewportService(IJSRuntime js)
    {
        _js = js;
    }

    /// <summary>Ширина окна (px). Обновляется автоматически после <see cref="StartAsync"/>.</summary>
    public int Width { get; private set; }
    /// <summary>Высота окна (px).</summary>
    public int Height { get; private set; }
    /// <summary>devicePixelRatio (1.0 для обычных мониторов, 2.0+ для Retina/4K).</summary>
    public double DevicePixelRatio { get; private set; } = 1.0;
    /// <summary>Горизонтальный скролл (px).</summary>
    public int ScrollX { get; private set; }
    /// <summary>Вертикальный скролл (px).</summary>
    public int ScrollY { get; private set; }
    /// <summary>Событие изменения любого из свойств.</summary>
    public event Action? Changed;

    /// <summary>Запускает подписку на события браузера и читает начальные значения.</summary>
    public async ValueTask StartAsync()
    {
        if (Volatile.Read(ref _disposed) == 1) return;
        _selfRef ??= DotNetObjectReference.Create(this);
        try
        {
            await _js.InvokeVoidAsync("SuperUI.startViewport", _selfRef).ConfigureAwait(false);
        }
        catch (JSDisconnectedException) { }
        catch (TaskCanceledException)   { }
        catch (JSException)             { }
    }

    /// <summary>Один раз синхронно считывает текущие размеры (без подписки).</summary>
    public async ValueTask ReadAsync()
    {
        if (Volatile.Read(ref _disposed) == 1) return;
        try
        {
            var data = await _js.InvokeAsync<SgViewportData>("SuperUI.readViewport").ConfigureAwait(false);
            if (data is not null) ApplyData(data);
        }
        catch (JSDisconnectedException) { }
        catch (TaskCanceledException)   { }
        catch (JSException)             { }
    }

    [Microsoft.JSInterop.JSInvokable]
    public void OnChanged(SgViewportData data)
    {
        if (data is null) return;
        ApplyData(data);
        Changed?.Invoke();
    }

    private void ApplyData(SgViewportData data)
    {
        Width = data.Width;
        Height = data.Height;
        DevicePixelRatio = data.DevicePixelRatio > 0 ? data.DevicePixelRatio : 1.0;
        ScrollX = data.ScrollX;
        ScrollY = data.ScrollY;
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1) return;
        try
        {
            await _js.InvokeVoidAsync("SuperUI.stopViewport").ConfigureAwait(false);
        }
        catch (JSDisconnectedException) { }
        catch (TaskCanceledException)   { }
        catch (JSException)             { }
        catch (ObjectDisposedException) { }

        var self = _selfRef;
        _selfRef = null;
        self?.Dispose();
    }
}

/// <summary>DTO, передаваемый из JS-модуля viewport.</summary>
public sealed class SgViewportData
{
    public int Width { get; set; }
    public int Height { get; set; }
    public double DevicePixelRatio { get; set; }
    public int ScrollX { get; set; }
    public int ScrollY { get; set; }
}
