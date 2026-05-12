using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using SuperUI.Base.Services;

namespace SuperUI;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Регистрирует все сервисы SuperUI.
    /// Вызывать в Program.cs для обоих хостинг-режимов (WASM и Server).
    /// </summary>
    public static IServiceCollection AddSuperUI(
        this IServiceCollection services,
        Action<SgConfig>? configure = null)
    {
        // ── Конфигурация ──────────────────────────────────────────────────────
        var config = new SgConfig();
        configure?.Invoke(config);
        services.AddSingleton(config);

        // ── Prerendering Detector ─────────────────────────────────────────────
        // ВАЖНО: разные реализации для WASM и Server!
        // Метод вызывается дважды (Server + Client в Web App) — регистрации
        // не должны конфликтовать.
        // Используем TryAdd чтобы не перезаписывать уже зарегистрированный.

        services.TryAddSingleton<IPrerenderingDetector>(sp =>
        {
            // На WASM никогда prerendering в runtime
            if (OperatingSystem.IsBrowser())
                return WasmPrerendingDetector.Instance;

            // На Server — определяем по IHttpContextAccessor
            return new ServerPrerendingDetector(
                sp.GetRequiredService<IHttpContextAccessor>());
        });

        // Обратная совместимость (устаревший интерфейс)
        services.TryAddSingleton<IPrerendingDetector>(sp =>
            (IPrerendingDetector)sp.GetRequiredService<IPrerenderingDetector>());

        // ── Z-Index Service ───────────────────────────────────────────────────
        // Scoped: per-circuit на Server, per-app (singleton-equiv) на WASM
        services.AddScoped<IZIndexService, ZIndexService>();

        // ── Focus Trap Service ────────────────────────────────────────────────
        // Scoped: per-circuit на Server
        services.AddScoped<IFocusTrapService, JsFocusTrapService>();
        services.AddScoped<IFocusTrapServiceEx, JsFocusTrapServiceEx>();

        // ── Keyboard Service ──────────────────────────────────────────────────
        services.AddScoped<IKeyboardService, KeyboardService>();

        // ── Component Options Service ─────────────────────────────────────────
        services.AddSingleton<IComponentOptionsService, ComponentOptionsService>();

        // ── Session Storage ───────────────────────────────────────────────────
        services.AddScoped<ISessionStorage, JsSessionStorage>();

        // ── Broadcast Service ─────────────────────────────────────────────────
        // Singleton — межкомпонентные события (нужен thread-safe impl для Server)
        services.AddSingleton<ISgBroadcastService, SgBroadcastService>();

        // ── Presence Service ──────────────────────────────────────────────────
        services.AddScoped<SgPresenceService>();

        // ── Toast / Confirm / Notification ───────────────────────────────────
        services.AddScoped<SgToastService>();
        services.AddScoped<SgConfirmService>();
        services.AddScoped<SgNotificationService>();

        return services;
    }
}
