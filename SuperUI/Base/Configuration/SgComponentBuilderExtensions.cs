// SuperUI/Base/Configuration/SgComponentBuilderExtensions.cs
// Регистрация всех новых сервисов SuperUI в DI

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using System;
using SuperUI.Base;
using SuperUI.Base.Converters;
using SuperUI.Base.Services;

namespace SuperUI.Base.Configuration;

public static class SgComponentBuilderExtensions
{
    /// <summary>
    /// Зарегистрировать все сервисы SuperUI.
    /// </summary>
    /// <param name="services">IServiceCollection.</param>
    /// <param name="configure">Опциональная конфигурация SgLibraryOptions.</param>
    /// <returns>IServiceCollection для fluent-цепочки.</returns>
    public static IServiceCollection AddSuperUI(
        this IServiceCollection services,
        Action<SgLibraryOptions>? configure = null)
    {
        // Options — используем существующий extension (поддерживает IOptionsMonitor)
        // Всегда вызываем Configure (даже с no-op) чтобы зарегистрировать IOptions<T>
        services.AddSuperUIOptions(configure ?? (_ => { }));

        // Render mode resolver — выбираем по среде
        if (OperatingSystem.IsBrowser())
            services.AddSingleton<ISgRenderModeResolver, WasmRenderModeResolver>();
        else
            services.AddSingleton<ISgRenderModeResolver, ServerRenderModeResolver>();

        // Converters — типизированные конвертеры значений
        services.AddSingleton<ISgValueConverter<int>, IntConverter>();
        services.AddSingleton<ISgValueConverter<decimal>, DecimalConverter>();
        services.AddSingleton<ISgValueConverter<DateTime?>, DateTimeConverter>();

        // Другие сервисы (раскомментируйте по мере необходимости):
        // services.AddSingleton<ISgToastService, SgToastService>();
        // services.AddSingleton<IZIndexService, ZIndexService>();
        // services.AddSingleton<ISgResponsiveService, SgResponsiveService>();
        // services.AddSingleton<ISgFocusTrapService, SgFocusTrapService>();
        // services.AddSingleton<ISgA11yService, SgA11yService>();
        // services.AddSingleton<ISgEventBus, SgEventBus>();

        return services;
    }

    /// <summary>
    /// Зарегистрировать SuperUI для InteractiveAuto режима.
    /// </summary>
    public static IServiceCollection AddSuperUIAuto(
        this IServiceCollection services,
        Action<SgLibraryOptions>? configure = null)
    {
        services.AddSuperUI(configure);
        // Перезаписываем resolver для Auto режима
        services.AddSingleton<ISgRenderModeResolver, AutoRenderModeResolver>();
        return services;
    }

    /// <summary>
    /// Зарегистрировать SuperUI с конфигурацией из IConfiguration (appsettings.json).
    /// </summary>
    public static IServiceCollection AddSuperUI(
        this IServiceCollection services,
        Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        services.AddSuperUIOptions(configuration);

        if (OperatingSystem.IsBrowser())
            services.AddSingleton<ISgRenderModeResolver, WasmRenderModeResolver>();
        else
            services.AddSingleton<ISgRenderModeResolver, ServerRenderModeResolver>();

        services.AddSingleton<ISgValueConverter<int>, IntConverter>();
        services.AddSingleton<ISgValueConverter<decimal>, DecimalConverter>();
        services.AddSingleton<ISgValueConverter<DateTime?>, DateTimeConverter>();

        return services;
    }
}
