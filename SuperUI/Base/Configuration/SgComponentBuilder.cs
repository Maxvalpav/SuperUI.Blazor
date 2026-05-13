// SuperUI/Base/Configuration/SgComponentBuilder.cs 
// Улучшения: 
// - Правильная регистрация для WASM vs Server 
// - TimeProvider регистрация (.NET 8) 
// - PersistentComponentState поддержка 
// - Опциональные сервисы 
 
using System; 
using Microsoft.Extensions.DependencyInjection; 
using Microsoft.Extensions.DependencyInjection.Extensions; 
using SuperUI.Base.Services; 
using SuperUI.Base.State; 
using SuperUI.Base.Utilities; 
 
namespace SuperUI.Base.Configuration; 
 
 public sealed class SgComponentBuilder 
 { 
     public IServiceCollection Services { get; } 
 
     internal SgComponentBuilder(IServiceCollection services) 
     { 
         Services = services; 
     } 
 } 
 
 public static class SgComponentBuilderExtensions 
 { 
     /// <summary> 
     /// Добавляет все базовые сервисы SuperUI. 
     /// Автоматически определяет среду (Server vs WASM) и регистрирует 
     /// правильные реализации. 
     /// </summary> 
     public static SgComponentBuilder AddSuperUI( 
         this IServiceCollection services, 
         Action<SgLibraryOptions>? configure = null) 
     { 
         var builder = new SgComponentBuilder(services); 
 
         // Конфигурация 
         if (configure != null) 
             services.Configure(configure); 
         else 
             services.Configure<SgLibraryOptions>(_ => { }); 
 
         // TimeProvider (.NET 8) — используется в рендер планировщике 
         services.TryAddSingleton(TimeProvider.System); 
 
         // Render mode detector 
         services.TryAddScoped<ISgRenderModeDetector, SgRenderModeDetector>(); 
 
         // Z-index service 
         services.TryAddScoped<IZIndexService, ZIndexService>(); 
 
         // Focus trap 
         services.TryAddScoped<IFocusTrapService, FocusTrapService>(); 
         services.TryAddScoped<FocusTrapStack>(); 
 
         // Toast / Notification / Confirm 
         services.TryAddScoped<ISgToastService, SgToastService>(); 
         services.TryAddScoped<ISgNotificationService, SgNotificationService>(); 
         services.TryAddScoped<ISgConfirmService, SgConfirmService>(); 
 
         // Theme 
         services.TryAddScoped<SgThemeService>(); 
 
         // Broadcast (для Server — используем SgBroadcastService с Channel<T>) 
         services.TryAddScoped<ISgBroadcastService, SgBroadcastService>(); 
 
         // Batch renderer — Singleton на WASM, Scoped на Server 
         // (на Server каждый circuit должен иметь свой batch renderer) 
         if (OperatingSystem.IsBrowser()) 
             services.TryAddSingleton<SgThrottledBatchRenderer>(); 
         else 
             services.TryAddScoped<SgThrottledBatchRenderer>(); 
 
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
         builder.Services.TryAddScoped<Diagnostics.ISgDiagnosticsCollector, 
             Diagnostics.ComponentDiagnostics>(); 
 
         return builder; 
     } 
 } 
