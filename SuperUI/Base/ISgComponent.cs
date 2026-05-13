// ISgComponent.cs — Улучшенный интерфейс базового компонента SuperUI 
// Поддержка .NET 8+, Render Modes, IAsyncDisposable 
 
using Microsoft.AspNetCore.Components; 
using Microsoft.AspNetCore.Components.Rendering; 
using Microsoft.AspNetCore.Components.RenderTree; 
using Microsoft.AspNetCore.Components.Web; 
using SuperUI.Base.Hooks;
 
namespace SuperUI.Base; 
 
 /// <summary> 
 /// Расширенный интерфейс компонента SuperUI с поддержкой: 
 /// - .NET 8+ Render Modes (Static SSR, InteractiveServer, InteractiveWebAssembly, InteractiveAuto) 
 /// - Асинхронной очистки ресурсов 
 /// - Отслеживания состояния рендеринга 
 /// - Доступа к RendererInfo 
 /// </summary> 
 public interface ISgComponent : IComponent, IHandleEvent, IHandleAfterRender, IDisposable, IAsyncDisposable 
 { 
     /// <summary> 
     /// Уникальный идентификатор компонента в рамках сессии/приложения. 
     /// </summary> 
     string ComponentId { get; } 
 
     /// <summary> 
     /// Текущий режим рендеринга компонента. 
     /// </summary> 
     SgRenderMode CurrentRenderMode { get; } 
 
     /// <summary> 
     /// Находится ли компонент в состоянии пререндеринга. 
     /// </summary> 
     bool IsPrerendering { get; } 
 
     /// <summary> 
     /// Завершена ли инициализация компонента. 
     /// Используется для защиты от callback-ов до завершения OnInitializedAsync. 
     /// </summary> 
     bool IsInitialized { get; } 
 
     /// <summary> 
     /// Был ли компонент уже отрендерен хотя бы раз. 
     /// </summary> 
     bool HasRendered { get; } 
 
     /// <summary> 
     /// Количество рендеров компонента. 
     /// Полезно для отладки и PerformanceBudget. 
     /// </summary> 
     int RenderCount { get; } 
 
     /// <summary> 
     /// Словарь дополнительных атрибутов, передаваемых компоненту. 
     /// Совместим с [Parameter(CaptureUnmatchedValues = true)]. 
     /// </summary> 
     IReadOnlyDictionary<string, object>? AdditionalAttributes { get; } 
 
     /// <summary> 
     /// CSS классы компонента, построенные через SgCssBuilder. 
     /// </summary> 
     string? CssClass { get; } 
 
     /// <summary> 
     /// Inline стили компонента, построенные через StyleBuilder. 
     /// </summary> 
     string? CssStyle { get; } 
 
     /// <summary> 
     /// Принудительный вызов перерисовки с учётом текущего RenderMode. 
     /// В Static SSR — игнорируется. 
     /// </summary> 
     Task RefreshAsync(); 
 
     /// <summary> 
     /// Вызывается при изменении режима рендеринга (например, переход Server → WASM в InteractiveAuto). 
     /// </summary> 
     /// <param name="newMode">Новый режим рендеринга.</param> 
     void OnRenderModeChanged(SgRenderMode newMode); 
 
     /// <summary> 
     /// Подписывает внешний обработчик на события жизненного цикла компонента. 
     /// </summary> 
     /// <param name="hook">Хук для подписки.</param> 
     void Subscribe(IComponentHook hook); 
 
     /// <summary> 
     /// Отписывает внешний обработчик от событий жизненного цикла. 
     /// </summary> 
     /// <param name="hook">Хук для отписки.</param> 
     void Unsubscribe(IComponentHook hook); 
 } 
 
 /// <summary> 
 /// Информация о рендер-окружении компонента. 
 /// Предоставляется Renderer'ом Blazor. 
 /// </summary> 
 public readonly struct SgRendererInfo 
 { 
     public SgRenderMode RenderMode { get; init; } 
     public bool IsPrerendering { get; init; } 
     public bool IsInteractive { get; init; } 
     public string? RendererId { get; init; } 
     public bool SupportsStreamingRendering { get; init; } 
 
     public static SgRendererInfo Unknown => new() 
     { 
         RenderMode = SgRenderMode.Unknown, 
         IsPrerendering = false, 
         IsInteractive = false, 
         RendererId = null, 
         SupportsStreamingRendering = false 
     }; 
 } 
