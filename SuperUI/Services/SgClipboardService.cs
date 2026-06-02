// SuperUI/Services/SgClipboardService.cs
// Обёртка над navigator.clipboard API с fallback на document.execCommand.

using Microsoft.JSInterop;

namespace SuperUI.Services;

/// <summary>
/// Сервис копирования в буфер обмена.
/// </summary>
/// <remarks>
/// <para>Использует <c>navigator.clipboard.writeText/readText</c>, при недоступности —
/// fallback на <c>document.execCommand('copy')</c> через JS-модуль.</para>
/// <para>SSR-safe: в prerender возвращает <c>false</c>.</para>
/// </remarks>
public sealed class SgClipboardService : IAsyncDisposable
{
    private readonly IJSRuntime _js;
    private int _disposed;

    public SgClipboardService(IJSRuntime js)
    {
        _js = js;
    }

    /// <summary>Копирует текст в буфер обмена. Возвращает <c>true</c> при успехе.</summary>
    public async ValueTask<bool> CopyTextAsync(string text, CancellationToken ct = default)
    {
        if (Volatile.Read(ref _disposed) == 1 || string.IsNullOrEmpty(text)) return false;
        try
        {
            await _js.InvokeVoidAsync("SuperUI.copyText", text).ConfigureAwait(false);
            return true;
        }
        catch (JSDisconnectedException) { return false; }
        catch (TaskCanceledException)   { return false; }
        catch (JSException)             { return false; }
        catch (InvalidOperationException) { return false; }
    }

    /// <summary>Читает текст из буфера обмена. Возвращает <c>null</c> при ошибке/недоступности.</summary>
    public async ValueTask<string?> ReadTextAsync(CancellationToken ct = default)
    {
        if (Volatile.Read(ref _disposed) == 1) return null;
        try
        {
            return await _js.InvokeAsync<string?>("SuperUI.readClipboardText").ConfigureAwait(false);
        }
        catch (JSDisconnectedException) { return null; }
        catch (TaskCanceledException)   { return null; }
        catch (JSException)             { return null; }
        catch (InvalidOperationException) { return null; }
    }

    /// <inheritdoc/>
    public ValueTask DisposeAsync()
    {
        Volatile.Write(ref _disposed, 1);
        return ValueTask.CompletedTask;
    }
}
