// SuperUI/Services/SgNetworkService.cs
// Сервис статуса сети (online/offline) с событием изменения.

using Microsoft.JSInterop;

namespace SuperUI.Services;

/// <summary>
/// Сервис сетевого статуса (navigator.onLine + events).
/// </summary>
/// <remarks>
/// <para>SSR-safe: в prerender <see cref="IsOnline"/> = true.</para>
/// </remarks>
public sealed class SgNetworkService : IAsyncDisposable
{
    private readonly IJSRuntime _js;
    private int _disposed;
    private DotNetObjectReference<SgNetworkService>? _selfRef;

    public SgNetworkService(IJSRuntime js)
    {
        _js = js;
    }

    /// <summary>True, если браузер считает, что есть сеть.</summary>
    public bool IsOnline { get; private set; } = true;

    /// <summary>Событие изменения статуса сети.</summary>
    public event Action<bool>? Changed;

    /// <summary>Запускает подписку на online/offline-события. Idempotent.</summary>
    public async ValueTask StartAsync()
    {
        if (Volatile.Read(ref _disposed) == 1) return;
        _selfRef ??= DotNetObjectReference.Create(this);
        try
        {
            IsOnline = await _js.InvokeAsync<bool>("SuperUI.readOnline").ConfigureAwait(false);
            await _js.InvokeVoidAsync("SuperUI.startNetwork", _selfRef).ConfigureAwait(false);
        }
        catch (JSDisconnectedException) { }
        catch (TaskCanceledException)   { }
        catch (JSException)             { }
    }

    [Microsoft.JSInterop.JSInvokable]
    public void OnChanged(bool online)
    {
        if (online == IsOnline) return;
        IsOnline = online;
        Changed?.Invoke(online);
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1) return;
        try
        {
            await _js.InvokeVoidAsync("SuperUI.stopNetwork").ConfigureAwait(false);
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
