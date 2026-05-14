// SuperUI/Base/Configuration/SgComponentBuilder.cs
// ИСПРАВЛЕНО:
// ✅ ЛОГИКА: IComponentRegistry регистрируется явно (без него все компоненты получают null)
// ✅ ЛОГИКА: IComponentFactory регистрируется с правильными зависимостями
// ✅ NET8: TimeProvider регистрируется через TryAddSingleton
// ✅ SSR: PrerendingDetector для Server и WASM

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SuperUI.Base.Services;
using SuperUI.Base.State;
using SuperUI.Base.Diagnostics;
using SuperUI.Base.Localization;

namespace SuperUI.Base.Configuration;

public sealed class SgComponentBuilder
{
    public IServiceCollection Services { get; }

    internal SgComponentBuilder(IServiceCollection services) { Services = services; }
}

public static class SgComponentBuilderExtensions
{
    /// <summary>
    /// Добавляет все базовые сервисы SuperUI.
    /// Автоматически определяет среду (Server vs WASM) и регистрирует правильные реализации.
    /// </summary>
    public static SgComponentBuilder AddSuperUI(this IServiceCollection services,
        Action<SgLibraryOptions>? configure = null)
    {
        var builder = new SgComponentBuilder(services);

        // Конфигурация
        if (configure != null)
            services.Configure(configure);
        else
            services.Configure<SgLibraryOptions>(_ => { });

        // TimeProvider (.NET 8) — используется в рендер-планировщике
        services.TryAddSingleton<TimeProvider>(TimeProvider.System);

        // ✅ FIX: регистрируем ISgComponentTypeRegistry — без этого [Inject] в SgComponentBase = null
        services.TryAddSingleton<Services.ISgComponentTypeRegistry, SgComponentRegistry>();

        // ✅ FIX: IComponentFactory зависит от ISgComponentTypeRegistry
        services.TryAddSingleton<IComponentFactory, SgComponentFactory>();

        // Render mode detector
        services.TryAddScoped<SgRenderModeDetector>();
        services.TryAddScoped<SgRenderModeResolver>();

        // Z-index service
        services.TryAddScoped<IZIndexService, ZIndexService>();

        // Focus trap
        services.TryAddScoped<IFocusTrapService, FocusTrapService>();
        services.TryAddSingleton<FocusTrapStack>();

        // Toast / Notification / Confirm
        services.TryAddScoped<ISgToastService, SgToastService>();
        services.TryAddScoped<ISgNotificationService, SgNotificationService>();
        services.TryAddScoped<ISgConfirmService, SgConfirmService>();

        // Theme
        services.TryAddScoped<ISgThemeService, SgThemeService>();

        // Localization
        services.TryAddScoped<ISuperUILocalizer, SuperUILocalizer>();

        // Diagnostics
        services.TryAddScoped<ISgDiagnosticsCollector, SgDiagnosticsCollector>();
        services.TryAddSingleton<ISgMemoryPressureMonitor, SgMemoryPressureMonitor>();

        // Broadcast
        services.TryAddScoped<ISgBroadcastService, SgBroadcastService>();

        // Mediator
        services.TryAddScoped<ISgMediatorService, SgMediatorService>();

        // Component Options Service
        services.TryAddScoped<IComponentOptionsService, ComponentOptionsService>();

        // Keyboard
        services.TryAddScoped<IKeyboardService, KeyboardService>();

        return builder;
    }

    /// <summary>
    /// Добавляет Server-specific сервисы (только для Blazor Server / InteractiveAuto).
    /// Вызывать ТОЛЬКО в серверном проекте.
    /// </summary>
    public static SgComponentBuilder AddSuperUIServer(this SgComponentBuilder builder)
    {
        builder.Services.TryAddScoped<IPrerenderingDetector, ServerPrerenderingDetector>();
        builder.Services.TryAddScoped<SgCircuitAwareness>();
        return builder;
    }

    /// <summary>
    /// Добавляет WASM-specific сервисы.
    /// Вызывать ТОЛЬКО в клиентском проекте.
    /// </summary>
    public static SgComponentBuilder AddSuperUIWebAssembly(this SgComponentBuilder builder)
    {
        builder.Services.TryAddScoped<IPrerenderingDetector, WasmPrerendingDetector>();
        builder.Services.TryAddSingleton<SgWasmOptimizer>();
        return builder;
    }

    /// <summary>
    /// Добавляет диагностические сервисы (только в Development).
    /// </summary>
    public static SgComponentBuilder AddSuperUIDiagnostics(this SgComponentBuilder builder)
    {
        builder.Services.TryAddScoped<SgErrorBoundary>();
        return builder;
    }
}
