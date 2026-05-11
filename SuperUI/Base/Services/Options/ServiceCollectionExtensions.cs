using Microsoft.Extensions.DependencyInjection;

namespace SuperUI.Services;

/// <summary>
/// Методы расширения для регистрации SuperUI в DI.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Добавляет сервисы SuperUI в контейнер.
    /// </summary>
    public static IServiceCollection AddSuperUI(this IServiceCollection services, Action<SgLibraryOptions> configure)
    {
        services.Configure(configure);
        services.AddSingleton<IComponentOptionsService, ComponentOptionsService>();
        services.AddSingleton<IAdditionalAttributesService, AdditionalAttributesService>();
        return services;
    }

    /// <summary>
    /// Добавляет сервисы SuperUI в контейнер с настройками по умолчанию.
    /// </summary>
    public static IServiceCollection AddSuperUI(this IServiceCollection services)
    {
        services.Configure<SgLibraryOptions>(_ => { });
        services.AddSingleton<IComponentOptionsService, ComponentOptionsService>();
        services.AddSingleton<IAdditionalAttributesService, AdditionalAttributesService>();
        return services;
    }
}