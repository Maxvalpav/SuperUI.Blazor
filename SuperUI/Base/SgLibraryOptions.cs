// SgLibraryOptions.cs — Конфигурация библиотеки SuperUI 
// Поддержка .NET 8+ RenderMode по умолчанию 
 
using System;
using Microsoft.AspNetCore.Components; 
using Microsoft.Extensions.DependencyInjection;
using SuperUI.Base.Diagnostics;
using SuperUI.Localization;
using SuperUI.Base.Services;

namespace SuperUI.Base; 
 
/// <summary> 
/// Опции конфигурации библиотеки SuperUI. 
/// Регистрируется через services.Configure&lt;SgLibraryOptions&gt;(). 
/// </summary> 
public class SgLibraryOptions 
{ 
    /// <summary> 
    /// Режим рендеринга по умолчанию для компонентов SuperUI. 
    /// Unknown = авто-определение. 
    /// </summary> 
    public SgRenderMode DefaultRenderMode { get; set; } = SgRenderMode.Unknown; 
 
    /// <summary> 
    /// Включать ли диагностический оверлей (только в Development). 
    /// </summary> 
    public bool EnableDiagnostics { get; set; } 
 
    /// <summary> 
    /// Включать ли Performance Budget tracking. 
    /// </summary> 
    public bool EnablePerformanceBudget { get; set; } 
 
    /// <summary> 
    /// Минимальный интервал между рендерами (anti-thrashing). 
    /// По умолчанию 16ms (~60fps). 
    /// </summary> 
    public TimeSpan MinRenderInterval { get; set; } = TimeSpan.FromMilliseconds(16); 
 
    /// <summary> 
    /// Максимальное количество рендеров в секунду. 
    /// 0 = без ограничений. 
    /// </summary> 
    public int MaxRendersPerSecond { get; set; } 
 
    /// <summary> 
    /// Стратегия throttling для реактивных компонентов. 
    /// </summary> 
    public ThrottleStrategy ThrottleStrategy { get; set; } = ThrottleStrategy.Adaptive; 
 
    /// <summary> 
    /// Включать ли автоматическое определение RenderMode. 
    /// </summary> 
    public bool AutoDetectRenderMode { get; set; } = true; 
 
    /// <summary> 
    /// Использовать ли стриминговый рендеринг где возможно. 
    /// </summary> 
    public bool PreferStreamingRendering { get; set; } = true; 
 
    /// <summary> 
    /// Размер пула RenderTreeBuilder. 
    /// </summary> 
    public int RenderTreeBuilderPoolSize { get; set; } = 32; 
 
    /// <summary> 
    /// Включать ли AOT-оптимизации. 
    /// </summary> 
    public bool EnableAotOptimizations { get; set; } 
 
    /// <summary> 
    /// Тема по умолчанию. 
    /// </summary> 
    public string DefaultTheme { get; set; } = "light"; 
 
    /// <summary> 
    /// Локаль по умолчанию. 
    /// </summary> 
    public string DefaultLocale { get; set; } = "en-US"; 
 
    /// <summary> 
    /// Путь к файлам локализации. 
    /// </summary> 
    public string? LocalizationResourcesPath { get; set; } 
 
    /// <summary> 
    /// Максимальный размер хранилища SgStore (в количестве ключей). 
    /// </summary> 
    public int MaxStoreKeys { get; set; } = 1000; 
} 
 
/// <summary> 
/// Стратегии throttling для рендеров. 
/// </summary> 
public enum ThrottleStrategy 
{ 
    /// <summary>Нет throttling.</summary> 
    None = 0, 
 
    /// <summary>Фиксированный интервал.</summary> 
    Fixed = 1, 
 
    /// <summary>Адаптивный: интервал растёт при высокой нагрузке.</summary> 
    Adaptive = 2, 
 
    /// <summary>Только requestAnimationFrame (для WASM).</summary> 
    RafOnly = 3 
} 
 
/// <summary> 
/// Методы расширения для регистрации SuperUI в DI. 
/// </summary> 
public static class SgServiceCollectionExtensions 
{ 
    /// <summary> 
    /// Регистрирует все сервисы SuperUI. 
    /// </summary> 
    public static IServiceCollection AddSuperUI(this IServiceCollection services, Action<SgLibraryOptions>? configure = null) 
    { 
        // Опции 
        if (configure != null) 
            services.Configure(configure); 
        else 
            services.Configure<SgLibraryOptions>(_ => { }); 
 
        // Базовые сервисы 
        services.AddScoped<SgStore>(); 
        services.AddScoped<SgRenderModeDetector>(); 
        services.AddScoped<SgRenderModeResolver>(); 
        services.AddScoped<SgComponentRegistry>(); 
        services.AddScoped<SgComponentFactory>(); 
 
        // Сервисы UI 
        services.AddScoped<SgToastService>(); 
        services.AddScoped<SgNotificationService>(); 
        services.AddScoped<SgConfirmService>(); 
        services.AddScoped<SgBroadcastService>(); 
 
        // Тема 
        services.AddScoped<SgThemeService>(); 
 
        // Фокус/Клавиатура 
        services.AddScoped<IFocusTrapService, FocusTrapService>(); 
        services.AddScoped<IKeyboardService, KeyboardService>(); 
 
        // Пререндеринг 
        services.AddScoped<IPrerenderingDetector, ServerPrerenderingDetector>(); 
 
        // Диагностика (только в Development) 
        services.AddScoped<ComponentDiagnostics>(); 
        services.AddScoped<PerformanceBudget>(); 
 
        // Локализация 
        services.AddScoped<ISuperUILocalizer, SuperUILocalizer>(); 
 
        return services; 
    } 
} 
