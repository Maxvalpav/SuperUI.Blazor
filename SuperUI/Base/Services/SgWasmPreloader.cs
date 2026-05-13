// SuperUI/Base/Services/SgWasmPreloader.cs — НОВЫЙ
// ✅ PERF: Предзагрузка WASM runtime в InteractiveAuto режиме
// ✅ Показывает прогресс загрузки
// ✅ Кэширует результат между навигациями

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace SuperUI.Base.Services;

/// <summary>
/// Сервис предзагрузки WASM runtime в режиме InteractiveAuto.
/// Сокращает время до интерактивности при переходе с Server на WASM.
/// </summary>
public interface ISgWasmPreloader
{
    /// <summary>Прогресс загрузки WASM (0.0 - 1.0).</summary>
    double Progress { get; }

    /// <summary>true — WASM полностью загружен.</summary>
    bool IsLoaded { get; }

    /// <summary>Событие: прогресс загрузки изменился.</summary>
    event Action<double>? OnProgress;

    /// <summary>Начать предзагрузку WASM.</summary>
    Task PreloadAsync(CancellationToken ct = default);
}

/// <summary>
/// Реализация через JS Interop.
/// </summary>
public sealed class SgWasmPreloader : ISgWasmPreloader, IAsyncDisposable
{
    private readonly IJSRuntime _js;
    private readonly ILogger<SgWasmPreloader> _logger;
    private double _progress;
    private bool _isLoaded;
    private readonly object _lock = new();

    public double Progress
    {
        get { lock (_lock) return _progress; }
        private set
        {
            lock (_lock) _progress = value;
            OnProgress?.Invoke(value);
        }
    }

    public bool IsLoaded
    {
        get { lock (_lock) return _isLoaded; }
        private set { lock (_lock) _isLoaded = value; }
    }

    public event Action<double>? OnProgress;

    private DotNetObjectReference<SgWasmPreloader>? _dotNetRef;

    public SgWasmPreloader(IJSRuntime js, ILogger<SgWasmPreloader> logger)
    {
        _js = js;
        _logger = logger;
    }

    /// <summary>
    /// Начать предзагрузку WASM runtime.
    /// Безопасно вызывать многократно — повторные вызовы игнорируются.
    /// </summary>
    public async Task PreloadAsync(CancellationToken ct = default)
    {
        if (IsLoaded) return;

        if (!OperatingSystem.IsBrowser())
        {
            _logger.LogDebug("WASM preload skipped — not in browser");
            return;
        }

        try
        {
            _dotNetRef = DotNetObjectReference.Create(this);

            await _js.InvokeVoidAsync(
                "SuperUI.preloadWasm",
                ct,
                _dotNetRef,
                nameof(OnProgressCallback));

            IsLoaded = true;
            Progress = 1.0;
            _logger.LogInformation("WASM runtime preloaded successfully");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "WASM preload failed (non-critical)");
        }
    }

    /// <summary>
    /// JS callback для обновления прогресса.
    /// </summary>
    [JSInvokable]
    public void OnProgressCallback(double progress)
    {
        Progress = Math.Clamp(progress, 0.0, 1.0);
    }

    public async ValueTask DisposeAsync()
    {
        _dotNetRef?.Dispose();
        _dotNetRef = null;
        await Task.CompletedTask;
    }
}

/// <summary>
/// Расширения для регистрации WASM preloader.
/// </summary>
public static class SgWasmPreloaderExtensions
{
    public static IServiceCollection AddSgWasmPreloader(this IServiceCollection services)
    {
        services.AddSingleton<ISgWasmPreloader, SgWasmPreloader>();
        return services;
    }
}
