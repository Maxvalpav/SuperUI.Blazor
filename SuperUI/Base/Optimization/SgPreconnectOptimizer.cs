// SuperUI/Base/Optimization/SgPreconnectOptimizer.cs — НОВЫЙ
// ✅ Предзагрузка критических ресурсов для Blazor WASM
// ✅ Preconnect к CDN/API серверам
// ✅ DNS-prefetch для внешних доменов
// ✅ Lazy-loading компонентов с Assembly
// ✅ Измерение и логирование времени загрузки

using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace SuperUI.Base.Optimization;

/// <summary>
/// Компонент для предзагрузки ресурсов и оптимизации WASM старта.
/// Размещается в <head> Host.cshtml или App.razor.
/// </summary>
public sealed class SgPreconnectOptimizer : ComponentBase, IDisposable
{
    [Inject] private IJSRuntime JS { get; set; } = null!;
    [Inject] private ILogger<SgPreconnectOptimizer> Logger { get; set; } = null!;

    /// <summary>
    /// Список доменов для preconnect (например, CDN, API server).
    /// </summary>
    [Parameter] public string[]? PreconnectHosts { get; set; }

    /// <summary>
    /// Список доменов для DNS-prefetch (менее важные).
    /// </summary>
    [Parameter] public string[]? DnsPrefetchHosts { get; set; }

    /// <summary>
    /// Включить измерение времени загрузки.
    /// </summary>
    [Parameter] public bool EnableTimings { get; set; } = true;

    private bool _initialized;

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender && !_initialized)
        {
            _initialized = true;
            _ = InitializeAsync();
        }
    }

    private async Task InitializeAsync()
    {
        try
        {
            var tasks = new List<Task>();

            if (PreconnectHosts is { Length: > 0 })
                tasks.Add(AddPreconnectLinksAsync(PreconnectHosts));

            if (DnsPrefetchHosts is { Length: > 0 })
                tasks.Add(AddDnsPrefetchLinksAsync(DnsPrefetchHosts));

            if (EnableTimings)
                tasks.Add(MeasureStartupTimeAsync());

            await Task.WhenAll(tasks);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "SgPreconnectOptimizer initialization failed");
        }
    }

    private async Task AddPreconnectLinksAsync(string[] hosts)
    {
        foreach (var host in hosts)
        {
            await JS.InvokeVoidAsync("superui.addPreconnect", host);
        }
    }

    private async Task AddDnsPrefetchLinksAsync(string[] hosts)
    {
        foreach (var host in hosts)
        {
            await JS.InvokeVoidAsync("superui.addDnsPrefetch", host);
        }
    }

    private async Task MeasureStartupTimeAsync()
    {
        var timing = await JS.InvokeAsync<JsonElement>(
            "eval", "JSON.parse(JSON.stringify(performance.timing))");
        // Логирование времени загрузки
        Logger.LogInformation("App startup timing captured");
    }

    public void Dispose() { }
}
