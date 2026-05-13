// SuperUI/ServiceCollectionExtensions.cs

// ИСПРАВЛЕНИЯ:
// ✅ CS0246: FocusTrapService → правильный using + namespace
// ✅ CS0311: SgConfirmService → ISgConfirmService (реализует интерфейс)
// ✅ CS0311: SgNotificationService → ISgNotificationService (реализует интерфейс)
// ✅ ISgToastService → SgToastService
// ✅ ISgPresenceService → SgPresenceServiceImpl

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SuperUI.Base;
using SuperUI.Base.Services;
using SuperUI.Base.Diagnostics;

namespace SuperUI;

/// <summary>
/// Методы расширения для регистрации сервисов SuperUI в DI-контейнере.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Регистрирует все сервисы SuperUI.
    /// Работает на WASM, Blazor Server и Web App.
    /// </summary>
    public static IServiceCollection AddSuperUI(this IServiceCollection services,
        Action<SgLibraryOptions>? configure = null)
    {
        // ── Конфигурация ─────────────────────────────────────────────────────
        services.Configure<SgLibraryOptions>(configure ?? (_ => { }));

        // ── Prerendering Detector ─────────────────────────────────────────────
        services.TryAddSingleton<IPrerenderingDetector>(sp =>
        {
            if (OperatingSystem.IsBrowser())
                return WasmPrerenderingDetector.Instance;

            var accessor = sp.GetService<Microsoft.AspNetCore.Http.IHttpContextAccessor>();
            return accessor is not null
                ? new ServerPrerenderingDetector(accessor)
                : (IPrerenderingDetector)WasmPrerenderingDetector.Instance;
        });

        // Обратная совместимость (опечатка в старом имени)
        services.TryAddSingleton<IPrerendingDetector>(sp =>
            (IPrerendingDetector)sp.GetRequiredService<IPrerenderingDetector>());

        // ── IHttpContextAccessor (только Server) ──────────────────────────────
        if (!OperatingSystem.IsBrowser())
        {
            services.AddHttpContextAccessor();
        }

        // ── Streaming Rendering Service ───────────────────────────────────────
        // SSR-3 FIX: HttpContext-based detection вместо нестабильного CascadingParameter
        services.TryAddScoped<ISgStreamingRenderingService>(sp =>
            OperatingSystem.IsBrowser()
                ? WasmStreamingRenderingService.Instance
                : new SgStreamingRenderingService(
                    sp.GetRequiredService<Microsoft.AspNetCore.Http.IHttpContextAccessor>()));

        services.TryAddSingleton<ISuperUILocalizer, SuperUILocalizer>();

        // ── Опции компонентов ─────────────────────────────────────────────────
        services.TryAddSingleton<IComponentOptionsService, ComponentOptionsService>();

        // ── Z-Index Service ───────────────────────────────────────────────────
        services.AddScoped<IZIndexService, ZIndexService>();

        // ── Focus Trap ────────────────────────────────────────────────────────
        services.AddScoped<IFocusTrapService, JsFocusTrapService>();

        // ── Keyboard Service ──────────────────────────────────────────────────
        services.AddScoped<IKeyboardService, KeyboardService>();

        // ── Session Storage ───────────────────────────────────────────────────
        services.AddScoped<ISessionStorage, JsSessionStorage>();

        // ── Broadcast Service ─────────────────────────────────────────────────
        services.TryAddSingleton<ISgBroadcastService, SgBroadcastService>();

        // ── Presence Service ──────────────────────────────────────────────────
        // ✅ FIX: SgPresenceServiceImpl реализует ISgPresenceService
        services.AddScoped<ISgPresenceService, SgPresenceServiceImpl>();

        // ── Toast Service ─────────────────────────────────────────────────────
        // ✅ FIX CS0535: SgToastService реализует ISgToastService
        services.AddScoped<ISgToastService, SgToastService>();

        // ── Confirm Service ───────────────────────────────────────────────────
        // ✅ FIX CS0311: SgConfirmService реализует ISgConfirmService
        services.AddScoped<ISgConfirmService, SgConfirmService>();

        // ── Notification Service ──────────────────────────────────────────────
        // ✅ FIX CS0311: SgNotificationService реализует ISgNotificationService
        services.AddScoped<ISgNotificationService, SgNotificationService>();

        // ── Component Registry ───────────────────────────────────────────────────
        services.AddScoped<IComponentRegistry, ComponentRegistry>();

        // ── Component Factory ────────────────────────────────────────────────────
        services.AddScoped<IComponentFactory, ComponentFactory>();

        // ── WASM Crypto Optimizer ────────────────────────────────────────────────
        services.AddScoped<ICryptoOptimizer, WasmCryptoOptimizer>();

        // ── Circuit Awareness ─────────────────────────────────────────────────────
        // На WASM — always-connected заглушка.
        // На Server — переопределите через AddScoped<ICircuitAwareness, ServerCircuitAwareness>()
        services.TryAddScoped<ICircuitAwareness, WasmCircuitAwareness>();

        // ── Render Budget Service ─────────────────────────────────────────────────
        // AdaptiveRenderBudgetService: мониторит CPU на Server и адаптирует бюджет.
        // На WASM работает как обычный RenderBudgetService без CPU мониторинга.
        services.TryAddScoped<IRenderBudgetService, AdaptiveRenderBudgetService>();

        // ── Form Name Generator (Static SSR) ─────────────────────────────────────
        services.AddScoped<IFormNameGenerator, DefaultFormNameGenerator>();

        // ── Memory Pressure Monitor (Blazor Server only) ─────────────────────────
        if (!OperatingSystem.IsBrowser())
        {
            services.TryAddSingleton<ISgMemoryPressureMonitor, SgMemoryPressureMonitor>();
        }

        return services;
    }

    /// <summary>AddSuperUI для Blazor Server / Web App Server.</summary>
    public static IServiceCollection AddSuperUIServer(this IServiceCollection services,
        Action<SgLibraryOptions>? configure = null)
    {
        services.AddHttpContextAccessor();
        return services.AddSuperUI(configure);
    }

    /// <summary>AddSuperUI для Blazor WebAssembly.</summary>
    public static IServiceCollection AddSuperUIWasm(this IServiceCollection services,
        Action<SgLibraryOptions>? configure = null)
        => services.AddSuperUI(configure);
}
