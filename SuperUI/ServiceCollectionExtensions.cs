// ================================================================
// Файл: SuperUI/ServiceCollectionExtensions.cs
// ИСПРАВЛЕНО:
// - WasmCircuitAwareness доступен (в том же namespace)
// - WasmPrerenderingDetector → WasmPrerendingDetector
// - IComponentRegistry → SgComponentRegistry
// - IComponentFactory → SgComponentFactory
// - ICircuitAwareness → SgCircuitAwareness / WasmCircuitAwareness
// - Добавлена регистрация SgThemeService
// ================================================================

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SuperUI.Base;
using SuperUI.Base.Diagnostics;
using SuperUI.Base.Services;
using SuperUI.Services;

namespace SuperUI;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSuperUI(this IServiceCollection services,
        Action<SgLibraryOptions>? configure = null)
    {
        // Конфигурация
        services.Configure(configure ?? (_ => { }));

        // Prerendering Detector
        services.TryAddSingleton<IPrerenderingDetector>(sp =>
        {
            if (OperatingSystem.IsBrowser())
                return WasmPrerendingDetector.Instance;

            var accessor = sp.GetService<IHttpContextAccessor>();
            return accessor is not null
                ? new ServerPrerenderingDetector(accessor)
                : (IPrerenderingDetector)WasmPrerendingDetector.Instance;
        });

        // Обратная совместимость (опечатка в имени)
        services.TryAddSingleton<IPrerendingDetector>(sp =>
            (IPrerendingDetector)sp.GetRequiredService<IPrerenderingDetector>());

        // HttpContextAccessor (только Server)
        if (!OperatingSystem.IsBrowser())
        {
            services.AddHttpContextAccessor();
        }

        // Streaming Rendering Service
        services.TryAddScoped<ISgStreamingRenderingService>(sp =>
        {
            if (OperatingSystem.IsBrowser())
                return WasmStreamingRenderingService.Instance;
            return new SgStreamingRenderingService(sp.GetRequiredService<IHttpContextAccessor>());
        });

        services.TryAddSingleton<SgRenderModeResolver>();

        // Опции компонентов
        services.TryAddSingleton<IComponentOptionsService, ComponentOptionsService>();

        // Z-Index
        services.AddScoped<IZIndexService, ZIndexService>();

        services.TryAddSingleton<ISuperUILocalizer, SuperUILocalizer>();

        // Focus Trap
        services.AddScoped<IFocusTrapService, FocusTrapService>();

        // Keyboard
        services.AddScoped<IKeyboardService, KeyboardService>();

        // Session Storage
        services.AddScoped<ISessionStorage, JsSessionStorage>();

        // Broadcast
        services.TryAddSingleton<ISgBroadcastService, SgBroadcastService>();

        // Presence
        services.AddScoped<ISgPresenceService, SgPresenceServiceImpl>();

        // Toast
        services.AddScoped<ISgToastService, SgToastService>();

        // Confirm
        services.AddScoped<ISgConfirmService, SgConfirmService>();

        // Notification
        services.AddScoped<ISgNotificationService, SgNotificationService>();

        // Component Registry
        services.AddScoped<IComponentRegistry, SgComponentRegistry>();

        // Component Factory
        services.AddScoped<IComponentFactory, SgComponentFactory>();

        // WASM Crypto Optimizer
        services.AddScoped<ICryptoOptimizer, WasmCryptoOptimizer>();

        // Circuit Awareness (платформо-зависимая)
        if (OperatingSystem.IsBrowser())
        {
            services.TryAddScoped<ICircuitAwareness>(_ => WasmCircuitAwareness.Instance);
        }
        else
        {
            services.TryAddScoped<ICircuitAwareness, SgCircuitAwareness>();
        }

        // Render Budget Service
        services.TryAddScoped<IRenderBudgetService, AdaptiveRenderBudgetService>();

        // Form Name Generator (Static SSR)
        services.AddScoped<IFormNameGenerator, DefaultFormNameGenerator>();

        // Memory Pressure Monitor (Server only)
        if (!OperatingSystem.IsBrowser())
        {
            services.TryAddSingleton<ISgMemoryPressureMonitor, SgMemoryPressureMonitor>();
        }

        // SgThemeService (transient — создаётся per-circuit на Server, per-app на WASM)
        services.TryAddTransient<SgThemeService>();

        return services;
    }

    public static IServiceCollection AddSuperUIServer(this IServiceCollection services,
        Action<SgLibraryOptions>? configure = null)
    {
        services.AddHttpContextAccessor();
        return services.AddSuperUI(configure);
    }

    public static IServiceCollection AddSuperUIWasm(this IServiceCollection services,
        Action<SgLibraryOptions>? configure = null)
        => services.AddSuperUI(configure);
}
