// SuperUI/ServiceCollectionExtensions.cs

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using SuperUI.Base;              // ✅ FIX CS0246: SgLibraryOptions находится здесь
using SuperUI.Base.Services;

namespace SuperUI;

/// <summary>
/// Методы расширения для регистрации сервисов SuperUI в DI-контейнере.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Регистрирует все сервисы SuperUI.
    /// Универсальный метод: работает на WASM, Blazor Server и Web App (auto-detect).
    /// </summary>
    /// <param name="services">Коллекция сервисов.</param>
    /// <param name="configure">Опциональная конфигурация <see cref="SgLibraryOptions"/>.</param>
    public static IServiceCollection AddSuperUI(this IServiceCollection services,
        Action<SgLibraryOptions>? configure = null)
    {
        // ── Конфигурация ─────────────────────────────────────────────────────
        services.Configure<SgLibraryOptions>(configure ?? (_ => { }));

        // ── Prerendering Detector ─────────────────────────────────────────────
        // Определяем хост-среду через OperatingSystem.IsBrowser() — безопасно для AOT.
        // Не используем WebAssemblyHostBuilder (CS0234 на Server).
        services.TryAddSingleton<IPrerenderingDetector>(sp =>
        {
            if (OperatingSystem.IsBrowser())
                return WasmPrerenderingDetector.Instance;

            var accessor = sp.GetService<Microsoft.AspNetCore.Http.IHttpContextAccessor>();
            return accessor is not null
                ? new ServerPrerenderingDetector(accessor)
                : (IPrerenderingDetector)WasmPrerenderingDetector.Instance;
        });

        // Обратная совместимость (опечатка в старом имени интерфейса)
        services.TryAddSingleton<IPrerendingDetector>(sp =>
            (IPrerendingDetector)sp.GetRequiredService<IPrerenderingDetector>());

        // ── IHttpContextAccessor (только Server) ──────────────────────────────
        if (!OperatingSystem.IsBrowser())
        {
            services.AddHttpContextAccessor();
        }

        // ── Опции компонентов ─────────────────────────────────────────────────
        services.TryAddSingleton<IComponentOptionsService, ComponentOptionsService>();

        // ── Z-Index Service ───────────────────────────────────────────────────
        services.AddScoped<IZIndexService, ZIndexService>();

        // ── Focus Trap ────────────────────────────────────────────────────────
        services.AddScoped<IFocusTrapService, FocusTrapService>();
        services.AddScoped<FocusTrapStack>();

        // ── Keyboard Service ──────────────────────────────────────────────────
        services.AddScoped<IKeyboardService, KeyboardService>();

        // ── Session Storage ───────────────────────────────────────────────────
        services.AddScoped<ISessionStorage, JsSessionStorage>();

        // ── Broadcast Service (Singleton: in-process event bus) ───────────────
        // ✅ FIX: зарегистрирован как ISgBroadcastService → SgBroadcastService
        services.TryAddSingleton<ISgBroadcastService, SgBroadcastService>();

        // ── Presence Service (Scoped: per-circuit / per-user) ─────────────────
        // ✅ FIX: ISgPresenceService → SgPresenceServiceImpl (полная реализация)
        services.AddScoped<ISgPresenceService, SgPresenceServiceImpl>();

        // ── Toast Service ─────────────────────────────────────────────────────
        services.AddScoped<ISgToastService, SgToastService>();

        // ── Confirm Service ───────────────────────────────────────────────────
        services.AddScoped<ISgConfirmService, SgConfirmService>();

        // ── Notification Service ──────────────────────────────────────────────
        services.AddScoped<ISgNotificationService, SgNotificationService>();

        return services;
    }

    /// <summary>
    /// AddSuperUI для Blazor Server / Web App Server-проекта.
    /// Явно добавляет IHttpContextAccessor.
    /// </summary>
    public static IServiceCollection AddSuperUIServer(this IServiceCollection services,
        Action<SgLibraryOptions>? configure = null)
    {
        services.AddHttpContextAccessor();
        return services.AddSuperUI(configure);
    }

    /// <summary>
    /// AddSuperUI для Blazor WebAssembly.
    /// IHttpContextAccessor не нужен.
    /// </summary>
    public static IServiceCollection AddSuperUIWasm(this IServiceCollection services,
        Action<SgLibraryOptions>? configure = null)
    {
        return services.AddSuperUI(configure);
    }
}
