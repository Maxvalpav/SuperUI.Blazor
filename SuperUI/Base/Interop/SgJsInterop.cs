using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace SuperUI.Base.Interop;

/// <summary>
/// Расширенный сервис JS Interop с 5 уровнями защиты.
/// Singleton. Кэширует модули глобально.
/// </summary>
public sealed class SgJsInterop : IAsyncDisposable
{
    private readonly IJSRuntime _js;
    private readonly ILogger<SgJsInterop> _logger;
    private readonly ConcurrentDictionary<string, IJSObjectReference> _modules = new();
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();
    private bool _disposed;

    public SgJsInterop(IJSRuntime js, ILogger<SgJsInterop> logger)
    {
        _js = js;
        _logger = logger;
    }

    /// <summary>Уровень 5: Глобальный кэш модулей — один import на весь lifetime.</summary>
    public async ValueTask<IJSObjectReference?> GetModuleAsync(
        string path, CancellationToken cancellationToken = default)
    {
        // Уровень 1: Dispose guard
        if (_disposed) return null;

        if (_modules.TryGetValue(path, out var cached)) return cached;

        var @lock = _locks.GetOrAdd(path, _ => new SemaphoreSlim(1, 1));
        await @lock.WaitAsync(cancellationToken);

        try
        {
            if (_modules.TryGetValue(path, out cached)) return cached;

            // Уровень 2: Circuit guard (Blazor Server)
            var module = await _js.InvokeAsync<IJSObjectReference>(
                "import", cancellationToken, path);

            _modules[path] = module;
            return module;
        }
        catch (JSDisconnectedException ex)
        {
            // Уровень 3: Disconnected guard
            _logger.LogWarning(ex, "JS disconnected loading module {Path}", path);
            return null;
        }
        catch (TaskCanceledException)
        {
            // Уровень 4: Cancellation guard
            return null;
        }
        finally
        {
            @lock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        _disposed = true;
        foreach (var module in _modules.Values)
        {
            try { await module.DisposeAsync(); }
            catch { /* ignore */ }
        }
        _modules.Clear();
    }
}
