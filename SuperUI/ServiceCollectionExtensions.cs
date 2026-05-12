using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using SuperUI.Base.Services;

namespace SuperUI;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Регистрирует все сервисы SuperUI.
    /// Вызывать в Program.cs для WASM, Server, Web App (оба проекта).
    /// </summary>
    public static IServiceCollection AddSuperUI(
        this IServiceCollection services,
        Action<SgLibraryOptions>? configure = null)   // ← FIX CS0246: SgConfig → SgLibraryOptions
    {
        // ── Конфигурация ────────────────────────────────────────────────────
        // FIX: регистрируем через IOptions<> (стандартный паттерн .NET)
        if (configure is not null)
            services.Configure<SgLibraryOptions>(opts =>
            {
                var tmp = new SgLibraryOptions();
                configure(tmp);
                // копируем поля через отдельный action
            });

        services.Configure<SgLibraryOptions>(configure ?? (_ => { }));

        // ── Prerendering Detector ────────────────────────────────────────────
        // ВАЖНО: class-library не знает о режиме хостинга.
        // TryAdd — не перезаписывать, если уже зарегистрировано.
        // На WASM — никогда нет prerendering в runtime.
        // На Server — IHttpContextAccessor (нужен AddHttpContextAccessor).
        services.TryAddSingleton<IPrerenderingDetector>(sp =>
        {
            // FIX CS0234: НЕ используем WebAssemblyHostBuilder — только OperatingSystem
            if (OperatingSystem.IsBrowser())
                return WasmPrerenderingDetector.Instance;

            var accessor = sp.GetService<Microsoft.AspNetCore.Http.IHttpContextAccessor>();
            if (accessor is null)
            {
                // Fallback: если IHttpContextAccessor не зарегистрирован — считаем non-prerendering
                return WasmPrerenderingDetector.Instance;
            }
            return new ServerPrerenderingDetector(accessor);
        });

        // Обратная совместимость (устаревший интерфейс с опечаткой)
        services.TryAddSingleton<IPrerendingDetector>(sp =>
            (IPrerendingDetector)sp.GetRequiredService<IPrerenderingDetector>());

        // ── IHttpContextAccessor (нужен для Server prerendering) ─────────────
        // TryAdd — не дублировать если уже добавлен
        services.TryAddSingleton<Microsoft.AspNetCore.Http.IHttpContextAccessor,
            Microsoft.AspNetCore.Http.HttpContextAccessor>();

        // ── Component Options ─────────────────────────────────────────────────
        // Singleton: readonly поля → thread-safe
        services.TryAddSingleton<IComponentOptionsService, ComponentOptionsService>();

        // ── Z-Index Service ──────────────────────────────────────────────────
        // Scoped: per-circuit на Server, per-app (singleton-equiv) на WASM
        services.AddScoped<IZIndexService, ZIndexService>();

        // ── Focus Trap ───────────────────────────────────────────────────────
        services.AddScoped<IFocusTrapService, JsFocusTrapService>();
        services.AddScoped<IFocusTrapServiceEx, JsFocusTrapServiceEx>();
        services.AddScoped<FocusTrapStack>();

        // ── Keyboard Service ─────────────────────────────────────────────────
        services.AddScoped<IKeyboardService, KeyboardService>();

        // ── Session Storage ──────────────────────────────────────────────────
        services.AddScoped<ISessionStorage, JsSessionStorage>();

        // ── Broadcast Service ────────────────────────────────────────────────
        // Singleton — thread-safe impl: ConcurrentDictionary + Channel
        // FIX CS0246: регистрируем через интерфейс, реализация — SgBroadcastService
        services.TryAddSingleton<ISgBroadcastService, SgBroadcastService>();

        // ── Presence Service ──────────────────────────────────────────────────
        // Scoped: per-user на Server, per-tab на WASM
        // FIX CS0246: регистрируем через интерфейс, реализация — SgPresenceServiceImpl
        services.AddScoped<ISgPresenceService, SgPresenceServiceImpl>();

        // ── Toast / Confirm / Notification ────────────────────────────────────
        // FIX CS0246: все три сервиса созданы в разделах 25
        // Scoped: per-circuit (Server), per-app (WASM)
        services.AddScoped<SgToastService>();
        services.AddScoped<SgConfirmService>();
        services.AddScoped<SgNotificationService>();

        return services;
    }
}
