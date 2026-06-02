// SuperUI/Services/SgErrorService.cs
// Глобальный обработчик JS-ошибок (window.onerror, unhandledrejection).

using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace SuperUI.Services;

/// <summary>
/// Сервис глобального перехвата JS-ошибок: window.error, unhandledrejection.
/// </summary>
/// <remarks>
/// <para>Подписывается на JS-стороны, логирует в <see cref="ILogger"/> и
/// уведомляет подписчиков через <see cref="ErrorOccurred"/>.</para>
/// </remarks>
public sealed class SgErrorService : IAsyncDisposable
{
    private readonly IJSRuntime _js;
    private readonly ILogger<SgErrorService> _logger;
    private int _disposed;
    private DotNetObjectReference<SgErrorService>? _selfRef;

    public SgErrorService(IJSRuntime js, ILogger<SgErrorService> logger)
    {
        _js = js;
        _logger = logger;
    }

    /// <summary>Событие JS-ошибки: (message, source, lineno, colno, error.stack).</summary>
    public event Action<string, string?, int, int, string?>? ErrorOccurred;

    /// <summary>Событие необработанного promise-отклонения: (reason, stack).</summary>
    public event Action<string, string?>? UnhandledRejection;

    /// <summary>Запускает подписку на window.onerror / unhandledrejection.</summary>
    public async ValueTask StartAsync()
    {
        if (Volatile.Read(ref _disposed) == 1) return;
        _selfRef ??= DotNetObjectReference.Create(this);
        try
        {
            await _js.InvokeVoidAsync("SuperUI.startErrorCapture", _selfRef).ConfigureAwait(false);
        }
        catch (JSDisconnectedException) { }
        catch (TaskCanceledException)   { }
        catch (JSException)             { }
    }

    [Microsoft.JSInterop.JSInvokable]
    public void OnError(string message, string? source, int lineno, int colno, string? stack)
    {
        _logger.LogError("JS error: {Message} at {Source}:{Line}:{Col}\n{Stack}", message, source, lineno, colno, stack);
        try { ErrorOccurred?.Invoke(message, source, lineno, colno, stack); }
        catch { /* swallow subscriber errors */ }
    }

    [Microsoft.JSInterop.JSInvokable]
    public void OnUnhandledRejection(string reason, string? stack)
    {
        _logger.LogError("Unhandled promise rejection: {Reason}\n{Stack}", reason, stack);
        try { UnhandledRejection?.Invoke(reason, stack); }
        catch { }
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1) return;
        try
        {
            await _js.InvokeVoidAsync("SuperUI.stopErrorCapture").ConfigureAwait(false);
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
