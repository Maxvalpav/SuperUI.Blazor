// SuperUI/ServiceCollectionExtensions.cs
// ИСПРАВЛЕНО:
// ✅ CS0311: SgComponentRegistry регистрируется как ISgComponentTypeRegistry (не IComponentRegistry)
// ✅ CS0104: нет конфликта имён — используем полные имена там, где нужно
// ✅ Добавлена регистрация SgSignalPersistence
// ✅ Добавлена регистрация ISgComponentLifetimeRegistry
// ✅ Поддержка .NET 8/9/10

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
    public static IServiceCollection AddSuperUI(
        this IServiceCollection services,
        Action<SgLibraryOptions>? configure = null)
    {
        // Конфигурация
        services.Configure<SgLibraryOptions>(configure ?? (_ => { }));

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

        // Backward compat alias
        services.TryAddSingleton<IPrerendingDetector>(sp =>
            (IPrerendingDetector)sp.GetRequiredService<IPrerenderingDetector>());

        // HttpContextAccessor (только Server)
        if (!OperatingSystem.IsBrowser())
            services.AddHttpContextAccessor();

        // Streaming Rendering Service
        services.TryAddScoped<ISgStreamingRenderingService>(sp =>
        {
            if (OperatingSystem.IsBrowser())
                return WasmStreamingRenderingService.Instance;

            return new SgStreamingRenderingService(sp.GetRequiredService<IHttpContextAccessor>());
        });

        services.TryAddSingleton<ISuperUILocalizer, SuperUILocalizer>();

        services.TryAddSingleton<SgRenderScheduler>();

        // Component Options
        services.TryAddSingleton<IComponentOptionsService, ComponentOptionsService>();

        // Z-Index
        services.AddScoped<IZIndexService, ZIndexService>();
        services.TryAddSingleton<ZIndexService>();

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

        // ✅ FIX CS0311/CS0104: регистрируем как ISgComponentTypeRegistry, не IComponentRegistry
        services.AddScoped<Base.Services.ISgComponentTypeRegistry, SgComponentRegistry>();
        services.AddScoped<SgComponentRegistry>(); // для прямого доступа если нужен

        // ✅ НОВОЕ: lifecycle registry
        services.TryAddScoped<Base.ISgComponentLifetimeRegistry, DefaultComponentLifetimeRegistry>();

        // Component Factory
        services.AddScoped<SgComponentFactory>();

        // WASM Crypto Optimizer
        services.AddScoped<SgWasmCryptoOptimizer>();

        // Circuit Awareness
        if (OperatingSystem.IsBrowser())
            services.TryAddScoped<ISgCircuitAwareness>(_ => WasmCircuitAwareness.Instance);
        else
            services.TryAddScoped<ISgCircuitAwareness, SgCircuitAwareness>();

        // Render Budget Service
        services.TryAddScoped<IRenderBudgetService, AdaptiveRenderBudgetService>();

        // Form Name Generator (Static SSR)
        services.AddScoped<SgFormNameGenerator>();

        // Memory Pressure Monitor (Server only)
        if (!OperatingSystem.IsBrowser())
            services.TryAddSingleton<ISgMemoryPressureMonitor, SgMemoryPressureMonitor>();

        // Theme Service
        services.TryAddTransient<ISgThemeService, SgThemeService>();

        // ✅ НОВОЕ: Signal Persistence
        services.AddScoped<SgSignalPersistence>();

        // ✅ НОВОЕ: Mediator
        services.TryAddSingleton<ISgMediatorService, SgMediatorService>();

        // ✅ НОВОЕ: Render Mode Detector
        services.TryAddScoped<ISgRenderModeDetector, SgRenderModeDetector>();

        // ✅ НОВОЕ: WASM Optimizer
        services.AddScoped<ISgWasmOptimizer, SgWasmOptimizer>();

        // ✅ НОВОЕ: Web Worker Render Service
        services.AddScoped<ISgWebWorkerRenderService, SgWebWorkerRenderService>();

        // ✅ НОВОЕ: Prerendering Detector (уже выше, но добавляем интерфейсы)
        // services.TryAddSingleton<IPrerenderingDetector>(...) — уже зарегистрирован выше

        return services;
    }

    public static IServiceCollection AddSuperUIServer(
        this IServiceCollection services,
        Action<SgLibraryOptions>? configure = null)
    {
        services.AddHttpContextAccessor();
        return services.AddSuperUI(configure);
    }

    public static IServiceCollection AddSuperUIWasm(
        this IServiceCollection services,
        Action<SgLibraryOptions>? configure = null)
        => services.AddSuperUI(configure);
}

/// <summary>
/// Реализация по умолчанию для ISgComponentLifetimeRegistry.
/// Хранит ссылки на все активные компоненты (для диагностики и DI).
/// </summary>
internal sealed class DefaultComponentLifetimeRegistry : Base.ISgComponentLifetimeRegistry
{
    private readonly HashSet<ISgComponent> _components = [];
    private readonly object _lock = new();

    public void Register(ISgComponent component)
    {
        lock (_lock) _components.Add(component);
    }

    public void Unregister(ISgComponent component)
    {
        lock (_lock) _components.Remove(component);
    }

    public IReadOnlyCollection<ISgComponent> GetAll()
    {
        lock (_lock) return _components.ToList().AsReadOnly();
    }
}
