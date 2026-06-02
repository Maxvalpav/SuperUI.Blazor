// SuperUI/Services/SgVisibilityService.cs
// Сервис видимости страницы (Page Visibility API).

using Microsoft.JSInterop;

namespace SuperUI.Services;

/// <summary>
/// Сервис видимости страницы (<c>document.visibilityState</c>).
/// </summary>
/// <remarks>
/// <para>Полезен для паузы анимаций, опросов и тяжёлых фоновых задач,
/// когда вкладка свёрнута или неактивна.</para>
/// </remarks>
public sealed class SgVisibilityService : IAsyncDisposable
{
    private readonly IJSRuntime _js;
    private int _disposed;
    private DotNetObjectReference<SgVisibilityService>? _selfRef;

    public SgVisibilityService(IJSRuntime js)
    {
        _js = js;
    }

    /// <summary>True, если страница в данный момент видна.</summary>
    public bool IsVisible { get; private set; } = true;

    /// <summary>"visible" / "hidden" / "prerender" (значение от браузера).</summary>
    public string VisibilityState { get; private set; } = "visible";

    /// <summary>Событие изменения видимости.</summary>
    public event Action<bool>? Changed;

    /// <summary>Запускает подписку на visibilitychange.</summary>
    public async ValueTask StartAsync()
    {
        if (Volatile.Read(ref _disposed) == 1) return;
        _selfRef ??= DotNetObjectReference.Create(this);
        try
        {
            var state = await _js.InvokeAsync<string?>("SuperUI.readVisibility").ConfigureAwait(false);
            VisibilityState = state ?? "visible";
            IsVisible = VisibilityState == "visible";
            await _js.InvokeVoidAsync("SuperUI.startVisibility", _selfRef).ConfigureAwait(false);
        }
        catch (JSDisconnectedException) { }
        catch (TaskCanceledException)   { }
        catch (JSException)             { }
    }

    [Microsoft.JSInterop.JSInvokable]
    public void OnChanged(string state)
    {
        VisibilityState = state ?? "visible";
        var visible = VisibilityState == "visible";
        if (visible == IsVisible) return;
        IsVisible = visible;
        Changed?.Invoke(visible);
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1) return;
        try
        {
            await _js.InvokeVoidAsync("SuperUI.stopVisibility").ConfigureAwait(false);
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
