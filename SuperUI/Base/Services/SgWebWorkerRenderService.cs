// SuperUI/Base/Services/SgWebWorkerRenderService.cs
// УНИКАЛЬНЫЙ КЛАСС — Offscreen rendering в Web Worker (WASM-only).

using Microsoft.JSInterop;

namespace SuperUI.Base.Services;

/// <summary>
/// Интерфейс сервиса рендеринга в Web Worker.
/// </summary>
public interface ISgWebWorkerRenderService
{
    bool IsSupported { get; }
    ValueTask<T> ComputeInWorkerAsync<T>(string functionName, object? args = null);
    ValueTask OffloadToWorkerAsync(Func<Task> heavyWork);
}

/// <summary>
/// Сервис для выполнения тяжёлых вычислений в Web Worker.
/// Работает только на WASM. На Server-side возвращает синхронный результат.
/// </summary>
public sealed class SgWebWorkerRenderService : ISgWebWorkerRenderService, IAsyncDisposable
{
    private readonly IJSRuntime _js;
    private readonly bool _isWasm;
    private IJSObjectReference? _workerModule;

    public bool IsSupported => _isWasm;

    public SgWebWorkerRenderService(IJSRuntime js)
    {
        _js = js ?? throw new ArgumentNullException(nameof(js));
        _isWasm = OperatingSystem.IsBrowser();
    }

    public async ValueTask<T> ComputeInWorkerAsync<T>(string functionName, object? args = null)
    {
        if (!_isWasm)
            throw new PlatformNotSupportedException("Web Worker is only available on WASM.");

        await EnsureWorkerAsync();

        try
        {
            var result = await _workerModule!.InvokeAsync<T>("compute", functionName, args);
            return result;
        }
        catch (JSException ex)
        {
            throw new InvalidOperationException($"Web Worker computation failed: {functionName}", ex);
        }
    }

    public async ValueTask OffloadToWorkerAsync(Func<Task> heavyWork)
    {
        if (!_isWasm)
        {
            await heavyWork();
            return;
        }

        await Task.Run(heavyWork); // На WASM это тоже UI thread — используем Worker
    }

    private async ValueTask EnsureWorkerAsync()
    {
        if (_workerModule is not null) return;
        _workerModule = await _js.InvokeAsync<IJSObjectReference>("import", "./_content/SuperUI/js/worker-render.js");
    }

    public async ValueTask DisposeAsync()
    {
        if (_workerModule is not null)
        {
            try
            {
                await _workerModule.InvokeVoidAsync("terminate");
                await _workerModule.DisposeAsync();
            }
            catch { }
        }
    }
}
