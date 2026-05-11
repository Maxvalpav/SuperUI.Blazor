using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SuperUI.Base.Hooks;
using SuperUI.Base.RenderBudget;
using SuperUI.Components;
using SuperUI.Localization;
using SuperUI.Options;
using SuperUI.Services;
using SuperUI.Utilities;

namespace SuperUI;

/// <summary>
/// DI registration helpers for SuperUI.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the SuperUI services with the dependency injection container.
    /// </summary>
    public static IServiceCollection AddSuperUI(this IServiceCollection services)
        => services.AddSuperUI(_ => { });

    /// <summary>
    /// Registers the SuperUI services and applies the supplied configuration delegate.
    /// </summary>
    public static IServiceCollection AddSuperUI(this IServiceCollection services, Action<SuperUIOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new SuperUIOptions();
        configure?.Invoke(options);

        // ── Core services (Singleton) ─────────────────────────────
        services.TryAddSingleton<IZIndexService>(_ => new ZIndexService(options.BaseZIndex));
        services.TryAddSingleton<IComponentHookRegistry, ComponentHookRegistry>();
        services.TryAddSingleton<IAdditionalAttributesMerger>(_ => AdditionalAttributesMerger.Instance);
        services.TryAddSingleton<IRenderBudgetService, RenderBudgetService>();

        // ── Scoped services ───────────────────────────────────────
        services.TryAddScoped<IThemeTokenBinder, ThemeTokenBinder>();
        services.TryAddScoped<IFocusTrapService, FocusTrapService>();
        services.TryAddScoped<IComponentContext, ComponentContext>();
        services.TryAddScoped<IKeyboardHandlerService, KeyboardHandlerService>();
        services.TryAddScoped<IMouseHandlerService, MouseHandlerService>();
        services.TryAddScoped<IComponentBus, ComponentBus>();

        // ── Component Options Provider ────────────────────────────
        services.TryAddScoped<IComponentOptionsProvider>(sp =>
        {
            var provider = new ComponentOptionsProvider();
            options.ComponentOptions?.Invoke(provider);
            return provider;
        });

        // ── Legacy services (backward compatibility) ──────────────
        services.TryAddScoped<SgToastService>();
        services.TryAddScoped<SgConfirmService>();
        services.TryAddScoped<SgNotificationService>();
        services.TryAddSingleton<ISuperUILocalizer, SuperUILocalizer>();
        services.TryAddScoped<SgThemeService>();
        services.TryAddScoped<SgRagService>();

        // ── Lifecycle Hooks ───────────────────────────────────────
        services.TryAddSingleton<IComponentHook, LifecycleLoggingHook>();
        services.TryAddSingleton<IComponentHook, TelemetryHook>();

        // Опциональные сервисы
        services.AddHttpContextAccessor(); // для prerendering detection
        services.AddLogging();

        return services;
    }

    /// <summary>Добавляет кастомный хук lifecycle.</summary>
    public static IServiceCollection AddComponentHook<THook>(this IServiceCollection services)
        where THook : class, IComponentHook
    {
        services.TryAddSingleton<IComponentHook, THook>();
        return services;
    }
}

/// <summary>Опции конфигурации SuperUI.</summary>
public sealed class SuperUIOptions
{
    public string DefaultTheme { get; set; } = "light";
    public string DefaultCulture { get; set; } = "en-US";
    public int DefaultToastDurationMs { get; set; } = 3000;
    public int BaseZIndex { get; set; } = 1000;
    public Action<ComponentOptionsProvider>? ComponentOptions { get; set; }
}
