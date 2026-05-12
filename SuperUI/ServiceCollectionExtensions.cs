using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;   // ← FIX CS1061
using Microsoft.Extensions.Options;
using SuperUI.Base.Services;

// НЕ добавляем: using Microsoft.AspNetCore.Components.WebAssembly.Hosting  ← FIX CS0234
// НЕ добавляем: using Microsoft.AspNetCore.Http  — условно через reflection

namespace SuperUI;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Регистрирует все сервисы SuperUI.
    /// Вызывать в Program.cs для WASM, Server и Web App (оба проекта).
    /// </summary>
    /// <param name="services">Коллекция сервисов.</param>
    /// <param name="configure">Опциональная конфигурация <see cref="SgLibraryOptions"/>.</param>
    public static IServiceCollection AddSuperUI(
        this IServiceCollection services,
        Action<SgLibraryOptions>? configure = null)
    {
        // ── Конфигурация ──────────────────────────────────────────────────────────
        services.Configure<SgLibraryOptions>(configure ?? (_ => { }));

        // ── Prerendering Detector ─────────────────────────────────────────────────
        // ВАЖНО: class-library не знает о режиме хостинга.
        // Определяем через OperatingSystem.IsBrowser() — это работает и на WASM и на Server.
        // На WASM: всегда false (нет prerendering в runtime).
        // На Server: используем IHttpContextAccessor если он зарегистрирован.
        services.TryAddSingleton<IPrerenderingDetector>(sp =>
        {
            // FIX CS0234: НЕ используем WebAssemblyHostBuilder
            if (OperatingSystem.IsBrowser())
                return WasmPrerenderingDetector.Instance;

            // Server-side: пытаемся получить IHttpContextAccessor
            // Если не зарегистрирован (например, чистый WASM проект) — fallback
            var accessor = sp.GetService<Microsoft.AspNetCore.Http.IHttpContextAccessor>();
            if (accessor is null)
                return WasmPrerenderingDetector.Instance;

            return new ServerPrerenderingDetector(accessor);
        });

        // Обратная совместимость с устаревшим интерфейсом (опечатка в имени)
        services.TryAddSingleton<IPrerendingDetector>(
            sp => (IPrerendingDetector)sp.GetRequiredService<IPrerenderingDetector>());

        // ── IHttpContextAccessor (только Server / Web App Server проект) ──────────
        // TryAdd — не дублировать если уже добавлен.
        // На WASM этот тип недоступен, но TryAdd безопасен.
        if (!OperatingSystem.IsBrowser())
        {
            services.AddHttpContextAccessor();
        }

        // ── Component Options ─────────────────────────────────────────────────────
        services.TryAddSingleton<IComponentOptionsService, ComponentOptionsService>();

        // ── Z-Index Service ───────────────────────────────────────────────────────
        services.AddScoped<IZIndexService, ZIndexService>();

        // ── Focus Trap ────────────────────────────────────────────────────────────
        services.AddScoped<IFocusTrapService, JsFocusTrapService>();
        services.AddScoped<IFocusTrapServiceEx, JsFocusTrapServiceEx>();
        services.AddScoped<FocusTrapStack>();

        // ── Keyboard Service ──────────────────────────────────────────────────────
        services.AddScoped<IKeyboardService, KeyboardService>();

        // ── Session Storage ───────────────────────────────────────────────────────
        services.AddScoped<ISessionStorage, JsSessionStorage>();

        // ── Broadcast Service ─────────────────────────────────────────────────────
        services.TryAddSingleton<ISgBroadcastService, SgBroadcastService>();

        // ── Presence Service ──────────────────────────────────────────────────────
        services.AddScoped<ISgPresenceService, SgPresenceServiceImpl>();

        // ── Toast / Confirm / Notification ────────────────────────────────────────
        services.AddScoped<SgToastService>();
        services.AddScoped<SgConfirmService>();
        services.AddScoped<SgNotificationService>();

        return services;
    }

    /// <summary>
    /// AddSuperUI для Blazor Server / Web App Server.
    /// Автоматически добавляет IHttpContextAccessor.
    /// </summary>
    public static IServiceCollection AddSuperUIServer(
        this IServiceCollection services,
        Action<SgLibraryOptions>? configure = null)
    {
        services.AddHttpContextAccessor(); // нужен для ServerPrerenderingDetector
        return services.AddSuperUI(configure);
    }

    /// <summary>
    /// AddSuperUI для Blazor WebAssembly.
    /// IHttpContextAccessor не нужен.
    /// </summary>
    public static IServiceCollection AddSuperUIWasm(
        this IServiceCollection services,
        Action<SgLibraryOptions>? configure = null)
    {
        return services.AddSuperUI(configure);
    }
}
