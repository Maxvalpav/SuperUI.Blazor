// SuperUI/Services/SgDownloadService.cs
// Утилита для скачивания файлов из Blazor (text, blob, base64, URL).

using Microsoft.JSInterop;

namespace SuperUI.Services;

/// <summary>
/// Сервис скачивания файлов из Blazor.
/// </summary>
/// <remarks>
/// <para>Поддерживает скачивание из:</para>
/// <list type="bullet">
///   <item>Текста (<see cref="DownloadTextAsync"/>).</item>
///   <item>byte[] (<see cref="DownloadBytesAsync"/>).</item>
///   <item>URL (например, <c>https://...</c>) — браузер сам обработает.</item>
///   <item>Потока Stream.</item>
/// </list>
/// </remarks>
public sealed class SgDownloadService : IAsyncDisposable
{
    private readonly IJSRuntime _js;
    private int _disposed;

    public SgDownloadService(IJSRuntime js)
    {
        _js = js;
    }

    /// <summary>Скачивает текст как файл.</summary>
    public ValueTask DownloadTextAsync(string fileName, string content, string mimeType = "text/plain", CancellationToken ct = default)
    {
        if (Volatile.Read(ref _disposed) == 1) return ValueTask.CompletedTask;
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        return InvokeDownloadAsync(fileName, content, mimeType);
    }

    /// <summary>Скачивает массив байт как файл (base64-обёртка через JS).</summary>
    public async ValueTask DownloadBytesAsync(string fileName, byte[] data, string mimeType = "application/octet-stream", CancellationToken ct = default)
    {
        if (Volatile.Read(ref _disposed) == 1) return;
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentNullException.ThrowIfNull(data);
        try
        {
            await _js.InvokeVoidAsync("SuperUI.downloadBytes", fileName, Convert.ToBase64String(data), mimeType).ConfigureAwait(false);
        }
        catch (JSDisconnectedException) { }
        catch (TaskCanceledException)   { }
        catch (JSException)             { }
    }

    /// <summary>Инициирует скачивание по URL (например, ссылка на API).</summary>
    public async ValueTask DownloadUrlAsync(string fileName, string url, CancellationToken ct = default)
    {
        if (Volatile.Read(ref _disposed) == 1) return;
        try
        {
            await _js.InvokeVoidAsync("SuperUI.downloadUrl", fileName, url).ConfigureAwait(false);
        }
        catch (JSDisconnectedException) { }
        catch (TaskCanceledException)   { }
        catch (JSException)             { }
    }

    /// <summary>Сохраняет поток (Stream) как файл.</summary>
    public async ValueTask DownloadStreamAsync(string fileName, System.IO.Stream stream, string mimeType = "application/octet-stream", CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        using var ms = new System.IO.MemoryStream();
        await stream.CopyToAsync(ms, ct).ConfigureAwait(false);
        await DownloadBytesAsync(fileName, ms.ToArray(), mimeType, ct).ConfigureAwait(false);
    }

    private async ValueTask InvokeDownloadAsync(string fileName, string content, string mimeType)
    {
        try
        {
            await _js.InvokeVoidAsync("SuperUI.downloadText", fileName, content, mimeType).ConfigureAwait(false);
        }
        catch (JSDisconnectedException) { }
        catch (TaskCanceledException)   { }
        catch (JSException)             { }
    }

    /// <inheritdoc/>
    public ValueTask DisposeAsync()
    {
        Volatile.Write(ref _disposed, 1);
        return ValueTask.CompletedTask;
    }
}
