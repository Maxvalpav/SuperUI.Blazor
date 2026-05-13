// SuperUI/Base/Services/SgWasmOptimizer.cs
// УЛУЧШЕНИЯ: полная поддержка WASM-оптимизаций

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;

namespace SuperUI.Base.Services;

/// <summary>
/// Интерфейс WASM-оптимизатора.
/// </summary>
public interface ISgWasmOptimizer
{
    ValueTask PreloadAssemblyAsync(string assemblyName);
    ValueTask PreloadAssembliesAsync(params string[] assemblyNames);
    ValueTask OptimizeMemoryAsync();
    bool IsWasm { get; }
}

/// <summary>
/// Оптимизатор для Blazor WASM: предзагрузка сборок, управление памятью.
/// </summary>
public sealed class SgWasmOptimizer : ISgWasmOptimizer
{
    private readonly IJSRuntime _js;
    private readonly ILogger<SgWasmOptimizer> _logger;
    private readonly HashSet<string> _loaded = new();

    public bool IsWasm => OperatingSystem.IsBrowser();

    public SgWasmOptimizer(
        IJSRuntime js,
        ILogger<SgWasmOptimizer>? logger = null)
    {
        _js = js ?? throw new ArgumentNullException(nameof(js));
        _logger = logger ?? NullLogger<SgWasmOptimizer>.Instance;
    }

    public async ValueTask PreloadAssemblyAsync(string assemblyName)
    {
        if (!IsWasm || !_loaded.Add(assemblyName)) return;

        try
        {
            _logger.LogDebug("Preloading assembly: {Assembly}", assemblyName);
            await _js.InvokeVoidAsync("SuperUI.wasm.preloadAssembly", assemblyName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to preload assembly: {Assembly}", assemblyName);
        }
    }

    public async ValueTask PreloadAssembliesAsync(params string[] assemblyNames)
    {
        foreach (var name in assemblyNames)
            await PreloadAssemblyAsync(name);
    }

    public async ValueTask OptimizeMemoryAsync()
    {
        if (!IsWasm) return;

        try
        {
            await _js.InvokeVoidAsync("SuperUI.wasm.optimizeMemory");
            _logger.LogDebug("WASM memory optimized");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "WASM memory optimization failed");
        }
    }
}
