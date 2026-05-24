using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SuperUI.Base.Utilities;
using SuperUI.Components;
using SuperUI.Localization;
using SuperUI.Services;
using SuperUI.Themes;

namespace SuperUI;

/// <summary>
/// DI registration helpers for SuperUI.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the SuperUI services with default options.
    /// </summary>
    public static IServiceCollection AddSuperUI(this IServiceCollection services)
        => services.AddSuperUI(null, null);

    /// <summary>
    /// Registers the SuperUI services and applies the supplied configuration delegate.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Delegate that mutates the <see cref="SuperUiOptions"/>.</param>
    /// <param name="configureThemes">Optional delegate to register custom themes in <see cref="ThemeRegistry"/>.</param>
    /// <returns>The same service collection so calls can be chained.</returns>
    /// <remarks>
    /// Services are registered as Scoped, which is appropriate for both Blazor Server and Blazor WebAssembly.
    /// In Blazor Server, each circuit gets its own instance, ensuring proper isolation.
    /// In Blazor WebAssembly, each user session gets its own instance.
    /// </remarks>
    public static IServiceCollection AddSuperUI(
        this IServiceCollection services, 
        Action<SuperUiOptions>? configure = null,
        Action<ThemeRegistry>? configureThemes = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        
        if (configure != null)
        {
            services.Configure(configure);
        }

        // Toast, Confirm, and Notification services are Scoped to ensure proper isolation
        // in Blazor Server (per-circuit) and Blazor WebAssembly (per-session)
        services.TryAddScoped<SgToastService>();
        services.TryAddScoped<SgConfirmService>();
        services.TryAddScoped<SgNotificationService>();
        services.TryAddSingleton<ISuperUILocalizer, SuperUILocalizer>();
        services.TryAddScoped<SgZIndexService>();
        
        services.TryAddSingleton<ThemeRegistry>(sp =>
        {
            var registry = new ThemeRegistry();
            configureThemes?.Invoke(registry);
            return registry;
        });
        
        services.TryAddScoped<SgThemeService>();
        services.TryAddScoped<SgSettingsService>();
        services.TryAddScoped<SgPageTabsService>();
        services.TryAddScoped<SgRagService>();
        services.TryAddScoped<Services.AI.SgLangGraphService>();
        services.TryAddScoped<Services.Data.SgDexieService>();
        services.TryAddScoped<Services.Data.SgCbrService>();
        services.TryAddScoped<Services.Analytics.SgHeatmapService>();
        services.TryAddScoped<Services.Llm.ILlmService, Services.Llm.SgLlmService>();
        services.TryAddScoped<Services.Llm.IOpenRouterService, Services.Llm.SgOpenRouterService>();
        services.TryAddScoped<Services.Llm.SgChatHistoryService>();
        services.TryAddScoped<Services.Llm.SgPuterService>();
        services.TryAddScoped<Services.Llm.SgLlmProxyForwarder>();
        services.TryAddScoped<SgJsModuleCache>();
        services.TryAddScoped<SgWeatherService>();

        Components.DocumentExtractor.Services.DocumentExtractorServiceCollectionExtensions.AddSgDocumentExtractor(services);

        return services;
    }
}
