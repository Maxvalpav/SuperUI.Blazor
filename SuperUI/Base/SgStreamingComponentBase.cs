// SgStreamingComponentBase.cs — Поддержка Streaming Rendering (.NET 8+) 
// Использует [StreamRendering] атрибут и PersistentComponentState 
 
using Microsoft.AspNetCore.Components; 
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.Logging;
 
namespace SuperUI.Base; 
 
/// <summary> 
/// Базовый класс для компонентов, использующих Streaming Rendering (.NET 8+). 
/// 
/// Ключевые возможности: 
/// - Автоматическое применение [StreamRendering] атрибута 
/// - Интеграция с PersistentComponentState для seamless перехода prerender→interactive 
/// - Placeholder контент во время загрузки 
/// - Отложенная загрузка данных без блокировки первого рендера 
/// 
/// Пример использования: 
/// <code> 
/// @inherits SgStreamingComponentBase&lt;WeatherData&gt; 
/// 
/// @if (HasData) 
/// { 
///     &lt;WeatherDisplay Data="Data" /&gt; 
/// } 
/// else 
/// { 
///     &lt;SgSkeleton /&gt; 
/// } 
/// </code> 
/// </summary> 
[StreamRendering(true)] 
public abstract class SgStreamingComponentBase<TData> : SgComponentBase where TData : class 
{ 
    // ────────────────────────────────────────────── 
    //  Свойства 
    // ────────────────────────────────────────────── 
 
    /// <summary>Загруженные данные.</summary> 
    protected TData? Data { get; set; } 
 
    /// <summary>Загружены ли данные.</summary> 
    protected bool HasData => Data != null; 
 
    /// <summary>Произошла ли ошибка при загрузке.</summary> 
    protected Exception? LoadError { get; set; } 
 
    /// <summary>Идёт ли загрузка.</summary> 
    protected bool IsLoading { get; set; } = true; 
 
    /// <summary>Ключ для PersistentComponentState.</summary> 
    protected virtual string PersistenceKey => $"streaming:{GetType().Name}:{ComponentId}"; 
 
    // ────────────────────────────────────────────── 
    //  Жизненный цикл 
    // ────────────────────────────────────────────── 
 
    protected override async Task OnInitializeAsync() 
    { 
        // Пробуем восстановить данные из PersistentComponentState 
        // (если это второй проход после пререндеринга в InteractiveAuto) 
        if (PersistentState != null) 
        { 
            if (PersistentState.TryTakeFromJson<TData>(PersistenceKey, out var restored)) 
            { 
                Data = restored; 
                IsLoading = false; 
                return; 
            } 
        } 
 
        // Загружаем данные с поддержкой потоковой передачи 
        try 
        { 
            Data = await LoadDataAsync(LifecycleToken); 
        } 
        catch (OperationCanceledException) 
        { 
            // Компонент был уничтожен 
            return; 
        } 
        catch (Exception ex) 
        { 
            LoadError = ex; 
            Logger.LogError(ex, "[{ComponentId}] Streaming data load failed", ComponentId); 
        } 
        finally 
        { 
            IsLoading = false; 
        } 
 
        // Сохраняем для PersistentComponentState 
        if (PersistentState != null && Data != null) 
        { 
            PersistentState.PersistAsJson(PersistenceKey, Data); 
        } 
    } 
 
    // ────────────────────────────────────────────── 
    //  Абстрактный метод загрузки данных 
    // ────────────────────────────────────────────── 
 
    /// <summary> 
    /// Загрузка данных. Вызывается асинхронно без блокировки первого рендера. 
    /// Результат кешируется в PersistentComponentState. 
    /// </summary> 
    protected abstract Task<TData?> LoadDataAsync(CancellationToken cancellationToken); 
 
    // ────────────────────────────────────────────── 
    //  Рендеринг 
    // ────────────────────────────────────────────── 
 
    protected override void BuildRenderTree(RenderTreeBuilder builder) 
    { 
        if (LoadError != null) 
        { 
            RenderError(builder, LoadError); 
            return; 
        } 
 
        if (IsLoading) 
        { 
            RenderLoading(builder); 
            return; 
        } 
 
        if (HasData) 
        { 
            RenderData(builder, Data!); 
        } 
        else 
        { 
            RenderEmpty(builder); 
        } 
    } 
 
    // ────────────────────────────────────────────── 
    //  Виртуальные методы для переопределения 
    // ────────────────────────────────────────────── 
 
    /// <summary>Рендерит состояние загрузки.</summary> 
    protected virtual void RenderLoading(RenderTreeBuilder builder) 
    { 
        builder.OpenElement(0, "div"); 
        builder.AddAttribute(1, "class", "sg-streaming-loading"); 
        builder.AddAttribute(2, "aria-busy", "true"); 
        builder.AddAttribute(3, "aria-label", "Loading..."); 
        builder.OpenElement(4, "div"); 
        builder.AddAttribute(5, "class", "sg-skeleton"); 
        builder.CloseElement(); 
        builder.CloseElement(); 
    } 
 
    /// <summary>Рендерит загруженные данные.</summary> 
    protected abstract void RenderData(RenderTreeBuilder builder, TData data); 
 
    /// <summary>Рендерит состояние "нет данных".</summary> 
    protected virtual void RenderEmpty(RenderTreeBuilder builder) 
    { 
        builder.OpenElement(0, "div"); 
        builder.AddAttribute(1, "class", "sg-streaming-empty"); 
        builder.AddContent(2, "No data available."); 
        builder.CloseElement(); 
    } 
 
    /// <summary>Рендерит состояние ошибки.</summary> 
    protected virtual void RenderError(RenderTreeBuilder builder, Exception error) 
    { 
        builder.OpenElement(0, "div"); 
        builder.AddAttribute(1, "class", "sg-streaming-error"); 
        builder.AddAttribute(2, "role", "alert"); 
        builder.OpenElement(3, "p"); 
        builder.AddContent(4, $"Error loading data: {error.Message}"); 
        builder.CloseElement(); 
        builder.CloseElement(); 
    } 
} 
