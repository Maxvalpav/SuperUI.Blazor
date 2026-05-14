// ================================================================
// Файл: SuperUI/ServiceCollectionExtensions.cs
// ИСПРАВЛЕНО:
// ✅ CS0246: SgFormNameGenerator → IFormNameGenerator / DefaultFormNameGenerator
// ✅ CS0246: ISgCircuitAwareness → ICircuitAwareness (правильный интерфейс)
// ✅ CS0311: SgCircuitAwareness регистрируется как ICircuitAwareness
// ✅ CS0006: вторичная ошибка — исчезнет после исправления остальных
// ✅ WasmStreamingRenderingService: добавлена заглушка
// ✅ ISgComponentLifetimeRegistry: регистрируется как DefaultComponentLifetimeRegistry
// ✅ ISgComponentTypeRegistry: регистрируется как SgComponentRegistry
// ✅ .NET 8/9/10: совместим (Server + WASM + SSR)
// ================================================================

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SuperUI.Base;
using SuperUI.Base.Diagnostics;
using SuperUI.Base.Reactive;
using SuperUI.Base.Services;

namespace SuperUI;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Добавить все сервисы SuperUI в DI контейнер.
    /// Поддерживает Server-side (SignalR circuit), WASM и Static SSR.
    /// </summary>
    public static IServiceCollection AddSuperUI(this IServiceCollection services,
        Action<SgLibraryOptions>? configure = null)
    {
        // ── Конфигурация ──────────────────────────────────────────────────
        services.Configure<SgLibraryOptions>(configure ?? (_ => { }));

        // ── Prerendering Detector ─────────────────────────────────────────
        // ✅ Поддержка .NET 8/9/10: Server + WASM
        services.TryAddSingleton<TimeProvider>(TimeProvider.System);
        services.TryAddSingleton<IPrerenderingDetector>(sp =>
        {
            if (OperatingSystem.IsBrowser())
                return WasmPrerendingDetector.Instance;

            var accessor = sp.GetService<IHttpContextAccessor>();
            return accessor is not null
                ? new ServerPrerenderingDetector(accessor)
                : (IPrerenderingDetector)WasmPrerendingDetector.Instance;
        });

        // HttpContextAccessor — только Server
        if (!OperatingSystem.IsBrowser())
            services.AddHttpContextAccessor();

        // ── Streaming Rendering ───────────────────────────────────────────
        // ✅ Поддержка .NET 8+ Streaming SSR
        services.TryAddScoped<ISgStreamingRenderingService>(sp =>
        {
            if (OperatingSystem.IsBrowser())
                return WasmStreamingRenderingService.Instance;

            return new SgStreamingRenderingService(sp.GetService<IHttpContextAccessor>());
        });

        services.TryAddSingleton<SgLibraryOptions>();
        services.TryAddSingleton<ISgLibraryOptionsService, SgLibraryOptionsService>();

        // ── Component Options ─────────────────────────────────────────────
        services.TryAddSingleton<IComponentOptionsService, ComponentOptionsService>();

        // ── Z-Index ───────────────────────────────────────────────────────
        services.AddScoped<IZIndexService, ZIndexService>();
        services.TryAddSingleton<ZIndexService>();

        // ── Focus Trap ────────────────────────────────────────────────────
        services.AddScoped<IFocusTrapService, FocusTrapService>();
        services.TryAddSingleton<FocusTrapStack>();

        // ── Keyboard ──────────────────────────────────────────────────────
        services.AddScoped<IKeyboardService, KeyboardService>();

        // ── Session Storage ───────────────────────────────────────────────
        services.AddScoped<ISessionStorage, JsSessionStorage>();

        // ── Broadcast ─────────────────────────────────────────────────────
        services.TryAddSingleton<ISgBroadcastService, SgBroadcastService>();

        // ── Presence ──────────────────────────────────────────────────────
        services.AddScoped<ISgPresenceService, SgPresenceService>();

        // ── Toast ─────────────────────────────────────────────────────────
        services.AddScoped<ISgToastService, SgToastService>();

        // ── Confirm ───────────────────────────────────────────────────────
        services.AddScoped<ISgConfirmService, SgConfirmService>();

        // ── Notification ──────────────────────────────────────────────────
        services.AddScoped<ISgNotificationService, SgNotificationService>();

        services.TryAddSingleton<ISuperUILocalizer, SuperUILocalizer>();

        // ── Component Registry ────────────────────────────────────────────
        // ✅ FIX CS0311: ISgComponentTypeRegistry (не IComponentRegistry)
        services.AddScoped<ISgComponentTypeRegistry, SgComponentRegistry>();
        services.AddScoped<SgComponentRegistry>(); // прямой доступ если нужен

        // ── Component Lifetime Registry ───────────────────────────────────
        // ✅ НОВОЕ: lifecycle registry для диагностики
        services.TryAddScoped<ISgComponentLifetimeRegistry, DefaultComponentLifetimeRegistry>();

        // ── Component Factory ─────────────────────────────────────────────
        services.AddScoped<IComponentFactory, SgComponentFactory>();

        // ── WASM Crypto Optimizer ─────────────────────────────────────────
        services.AddScoped<SgWasmCryptoOptimizer>();

        // ── Circuit Awareness ─────────────────────────────────────────────
        // ✅ FIX CS0246/CS0311: используем ICircuitAwareness (правильный интерфейс)
        if (OperatingSystem.IsBrowser())
            services.TryAddScoped<ICircuitAwareness>(_ => WasmCircuitAwareness.Instance);
        else
            services.TryAddScoped<ICircuitAwareness, SgCircuitAwareness>();

        // ── Render Budget Service ─────────────────────────────────────────
        services.TryAddScoped<IRenderBudgetService, AdaptiveRenderBudgetService>();

        // ── Form Name Generator ───────────────────────────────────────────
        // ✅ FIX CS0246: SgFormNameGenerator не существует!
        // Правильно: регистрируем IFormNameGenerator → DefaultFormNameGenerator
        services.AddScoped<IFormNameGenerator, DefaultFormNameGenerator>();

        // ── Memory Pressure Monitor ───────────────────────────────────────
        // Только Server-side (WASM не имеет доступа к GC.GetTotalMemory подробно)
        if (!OperatingSystem.IsBrowser())
            services.TryAddSingleton<ISgMemoryPressureMonitor, SgMemoryPressureMonitor>();

        // ── Theme Service ─────────────────────────────────────────────────
        services.TryAddTransient<ISgThemeService, SgThemeService>();

        // ── Signal Persistence ────────────────────────────────────────────
        // ✅ НОВОЕ: персистентность сигналов (localStorage/sessionStorage)
        services.AddScoped<SgSignalPersistence>();

        // ── Mediator ──────────────────────────────────────────────────────
        // ✅ НОВОЕ: медиатор для межкомпонентного взаимодействия
        services.TryAddSingleton<ISgMediatorService, SgMediatorService>();

        // ── Render Mode Detector ──────────────────────────────────────────
        // ✅ НОВОЕ: определение режима рендеринга
        services.TryAddScoped<SgRenderModeDetector>();
        services.TryAddScoped<SgRenderModeResolver>();

        // ── Diagnostics ───────────────────────────────────────────────────
        services.TryAddScoped<ISgDiagnosticsCollector, SgDiagnosticsCollector>();

        // ── WASM Optimizer ────────────────────────────────────────────────
        // ✅ НОВОЕ: оптимизации для WASM
        services.AddScoped<SgWasmOptimizer>();

        // ── Web Worker Render Service ─────────────────────────────────────
        // ✅ НОВОЕ: фоновый рендеринг
        services.AddScoped<SgWebWorkerRenderService>();

        return services;
    }

    /// <summary>
    /// Добавить сервисы SuperUI для Server-side Blazor (добавляет HttpContextAccessor).
    /// </summary>
    public static IServiceCollection AddSuperUIServer(this IServiceCollection services,
        Action<SgLibraryOptions>? configure = null)
    {
        services.AddHttpContextAccessor();
        return services.AddSuperUI(configure);
    }

    /// <summary>
    /// Добавить сервисы SuperUI для WebAssembly Blazor.
    /// </summary>
    public static IServiceCollection AddSuperUIWasm(this IServiceCollection services,
        Action<SgLibraryOptions>? configure = null)
        => services.AddSuperUI(configure);
}

// ── Вспомогательные типы ─────────────────────────────────────────────────────

/// <summary>
/// Реализация ISgComponentLifetimeRegistry по умолчанию.
/// Хранит ссылки на все активные компоненты (для диагностики).
/// Thread-safe (lock-based для Server-side concurrent access).
/// </summary>
internal sealed class DefaultComponentLifetimeRegistry : ISgComponentLifetimeRegistry
{
    private readonly HashSet<ISgComponent> _components = [];
    private readonly object _lock = new();

    public void Register(ISgComponent component)
    {
        ArgumentNullException.ThrowIfNull(component);
        lock (_lock) _components.Add(component);
    }

    public void Unregister(ISgComponent component)
    {
        if (component is null) return;
        lock (_lock) _components.Remove(component);
    }

    public IReadOnlyCollection<ISgComponent> GetAll()
    {
        lock (_lock) return _components.ToList().AsReadOnly();
    }
}

/// <summary>
/// WASM-заглушка для ISgStreamingRenderingService.
/// В WASM нет SSR streaming — все операции noop.
/// </summary>
internal sealed class WasmStreamingRenderingService : ISgStreamingRenderingService
{
    public static readonly WasmStreamingRenderingService Instance = new();

    public bool IsSupported => false;
    public bool IsStreaming => false;
    public event Action? StreamingCompleted { add { } remove { } }

    public void NotifyStreamingCompleted() { }
}
