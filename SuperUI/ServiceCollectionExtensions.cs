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
using SuperUI.Localization;

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
