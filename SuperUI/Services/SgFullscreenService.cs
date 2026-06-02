// SuperUI/Services/SgFullscreenService.cs
// Утилита для Fullscreen API: запрос, выход, события изменения состояния.

using Microsoft.JSInterop;

namespace SuperUI.Services;

/// <summary>
/// Сервис Fullscreen API: запрос, выход, отслеживание изменений.
/// </summary>
/// <remarks>
/// <para>Использует нативный <c>element.requestFullscreen()</c> / <c>document.exitFullscreen()</c>.</para>
/// <para>SSR-safe: в prerender все методы возвращают <c>false</c>.</para>
/// </remarks>
public sealed class SgFullscreenService : IAsyncDisposable
{
    private readonly IJSRuntime _js;
    private int _disposed;
    private DotNetObjectReference<SgFullscreenService>? _selfRef;

    public SgFullscreenService(IJSRuntime js)
    {
        _js = js;
    }

    /// <summary>True, если документ сейчас в fullscreen-режиме.</summary>
    public async ValueTask<bool> IsFullscreenAsync()
    {
        if (Volatile.Read(ref _disposed) == 1) return false;
        try
        {
            return await _js.InvokeAsync<bool>("SuperUI.isFullscreen").ConfigureAwait(false);
        }
        catch (JSDisconnectedException) { return false; }
        catch (TaskCanceledException)   { return false; }
        catch (JSException)             { return false; }
        catch (InvalidOperationException) { return false; }
    }

    /// <summary>Запрашивает fullscreen для всего документа.</summary>
    public async ValueTask<bool> RequestAsync()
    {
        if (Volatile.Read(ref _disposed) == 1) return false;
        try
        {
            return await _js.InvokeAsync<bool>("SuperUI.requestFullscreen").ConfigureAwait(false);
        }
        catch (JSDisconnectedException) { return false; }
        catch (TaskCanceledException)   { return false; }
        catch (JSException)             { return false; }
        catch (InvalidOperationException) { return false; }
    }

    /// <summary>Выходит из fullscreen.</summary>
    public async ValueTask<bool> ExitAsync()
    {
        if (Volatile.Read(ref _disposed) == 1) return false;
        try
        {
            return await _js.InvokeAsync<bool>("SuperUI.exitFullscreen").ConfigureAwait(false);
        }
        catch (JSDisconnectedException) { return false; }
        catch (TaskCanceledException)   { return false; }
        catch (JSException)             { return false; }
        catch (InvalidOperationException) { return false; }
    }

    /// <summary>Toggle: вход/выход из fullscreen.</summary>
    public async ValueTask<bool> ToggleAsync()
    {
        var isFs = await IsFullscreenAsync().ConfigureAwait(false);
        return isFs
            ? await ExitAsync().ConfigureAwait(false)
            : await RequestAsync().ConfigureAwait(false);
    }

    /// <summary>Событие изменения fullscreen-состояния браузера.</summary>
    public event Action<bool>? FullscreenChanged;

    /// <summary>Подписывает сервис на fullscreenchange-событие браузера. Idempotent.</summary>
    public async ValueTask EnsureListeningAsync()
    {
        if (Volatile.Read(ref _disposed) == 1) return;
        _selfRef ??= DotNetObjectReference.Create(this);
        try
        {
            await _js.InvokeVoidAsync("SuperUI.subscribeFullscreen", _selfRef).ConfigureAwait(false);
        }
        catch (JSDisconnectedException) { }
        catch (TaskCanceledException)   { }
        catch (JSException)             { }
    }

    [Microsoft.JSInterop.JSInvokable]
    public void OnFullscreenChanged(bool isFullscreen)
    {
        FullscreenChanged?.Invoke(isFullscreen);
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1) return;
        try
        {
            await _js.InvokeVoidAsync("SuperUI.unsubscribeFullscreen").ConfigureAwait(false);
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
