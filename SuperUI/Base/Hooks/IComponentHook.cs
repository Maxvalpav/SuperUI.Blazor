// ================================================================
// Файл: SuperUI/Base/Hooks/IComponentHook.cs
// ЕДИНСТВЕННЫЙ файл с определениями всех хуковых интерфейсов.
// Удалить: SuperUI/Base/Hooks/IAsyncComponentHook.cs (дубликат)
// ================================================================

namespace SuperUI.Base.Hooks;

/// <summary>
/// Базовый sync-интерфейс хука жизненного цикла компонента SuperUI.
/// Все методы принимают SgComponentBase для доступа к контексту компонента.
/// </summary>
public interface IComponentHook
{
    void OnInitialized(SgComponentBase component);
    void OnParametersSet(SgComponentBase component);
    void OnAfterRender(SgComponentBase component, bool firstRender);
    
    // Async methods with default implementations
    Task OnInitializedAsync(SgComponentBase component) => Task.CompletedTask;
    Task OnParametersSetAsync(SgComponentBase component) => Task.CompletedTask;
    Task OnAfterRenderAsync(SgComponentBase component, bool firstRender) => Task.CompletedTask;
}

/// <summary>
/// Хук для управления решением о рендере (ShouldRender).
/// Расширяет IComponentHook.
/// </summary>
public interface IRenderHook : IComponentHook
{
    /// <summary>
    /// Вернуть false чтобы пропустить рендер.
    /// Вызывается из ShouldRender() компонента.
    /// </summary>
    bool ShouldRender(SgComponentBase component);
}
