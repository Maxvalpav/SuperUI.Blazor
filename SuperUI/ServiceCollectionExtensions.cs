using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SuperUI.Base.Utilities;
using SuperUI.Components;
using SuperUI.Localization;
using SuperUI.Services;

namespace SuperUI;

/// <summary>
/// DI registration helpers for SuperUI.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the SuperUI services (<see cref="SgToastService"/>, <see cref="SgConfirmService"/>, 
    /// <see cref="ISuperUILocalizer"/>, <see cref="SgZIndexService"/>)
    /// with the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same service collection so calls can be chained.</returns>
    public static IServiceCollection AddSuperUI(this IServiceCollection services)
        => services.AddSuperUI(_ => { });

    /// <summary>
    /// Registers the SuperUI services and applies the supplied configuration delegate.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Delegate that mutates the <see cref="SuperUiOptions"/>.</param>
    /// <returns>The same service collection so calls can be chained.</returns>
    /// <remarks>
    /// Services are registered as Scoped, which is appropriate for both Blazor Server and Blazor WebAssembly.
    /// In Blazor Server, each circuit gets its own instance, ensuring proper isolation.
    /// In Blazor WebAssembly, each user session gets its own instance.
    /// </remarks>
    public static IServiceCollection AddSuperUI(this IServiceCollection services, Action<SuperUiOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.Configure(configure);
        // Toast, Confirm, and Notification services are Scoped to ensure proper isolation
        // in Blazor Server (per-circuit) and Blazor WebAssembly (per-session)
        services.TryAddScoped<SgToastService>();
        services.TryAddScoped<SgConfirmService>();
        services.TryAddScoped<SgNotificationService>();
        services.TryAddSingleton<ISuperUILocalizer, SuperUILocalizer>();
        services.TryAddScoped<SgZIndexService>();
        services.TryAddScoped<SgThemeService>();
        services.TryAddScoped<SgRagService>();
        services.TryAddScoped<SgJsModuleCache>();

        return services;
    }
}
