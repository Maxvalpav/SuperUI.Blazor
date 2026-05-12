// SuperUI/ServiceCollectionExtensions.cs
//
// Extension-метод для регистрации всех сервисов SuperUI в DI.
// Поддерживает WASM, Server, Web App (Auto), Hybrid.
//
// Использование:
//   builder.Services.AddSuperUI();
//   builder.Services.AddSuperUI(opts => opts.DefaultTheme = "light");

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using SuperUI.Base.Services;
using System;

namespace SuperUI;

/// <summary>
/// Расширения для регистрации SuperUI в DI-контейнере.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Зарегистрировать все сервисы SuperUI.
    /// </summary>
    /// <param name="services">Коллекция сервисов.</param>
    /// <param name="configure">Опциональная конфигурация библиотеки.</param>
    /// <returns>Коллекция сервисов (для чейнинга).</returns>
    /// <example>
    /// <code>
    /// // Минимальная регистрация
    /// builder.Services.AddSuperUI();
    ///
    /// // С конфигурацией
    /// builder.Services.AddSuperUI(opts =>
    /// {
    ///     opts.DefaultTheme          = "light";
    ///     opts.DefaultCulture        = "ru-RU";
    ///     opts.DefaultToastDurationMs = 5000;
    ///
    ///     opts.Button  = new() { ShowRipple = false };
    ///     opts.DataGrid = new() { DefaultPageSize = 50 };
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection AddSuperUI(
        this IServiceCollection services,
        Action<SgLibraryOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Опции библиотеки
        if (configure is not null)
            services.Configure<SgLibraryOptions>(configure);
        else
            services.TryAddSingleton(
                Microsoft.Extensions.Options.Options.Create(new SgLibraryOptions()));

        // Основной сервис настроек компонентов
        services.TryAddSingleton<IComponentOptionsService, ComponentOptionsService>();

        // Z-index менеджер
        // Scoped для Server (per-circuit), Singleton для WASM
        services.TryAddScoped<IZIndexService, ZIndexService>();

        // Focus trap
        services.TryAddScoped<IFocusTrapService, JsFocusTrapService>();

        // Keyboard service
        services.TryAddScoped<IKeyboardService, KeyboardService>();

        // Prerendering detector
        // На Server — через IHttpContextAccessor
        // На WASM — всегда false
        RegisterPrerendingDetector(services);

        return services;
    }

    private static void RegisterPrerendingDetector(IServiceCollection services)
    {
        // Проверяем доступность IHttpContextAccessor (только Server)
        services.TryAddSingleton<IPrerenderingDetector>(sp =>
        {
            // На WASM OperatingSystem.IsBrowser() == true
            if (OperatingSystem.IsBrowser())
                return WasmPrerenderingDetector.Instance;

            // На Server пробуем получить IHttpContextAccessor
            var accessor = sp.GetService<Microsoft.AspNetCore.Http.IHttpContextAccessor>();
            return accessor is not null
                ? new ServerPrerenderingDetector(accessor)
                : WasmPrerenderingDetector.Instance;
        });
    }
}


