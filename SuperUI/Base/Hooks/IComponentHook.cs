// SuperUI/Base/Hooks/IComponentHook.cs
// УЛУЧШЕНО:
// 1. IRenderHook вынесен в отдельный интерфейс (Single Responsibility)
// 2. IAsyncComponentHook расширен OnAfterRenderAsync
// 3. Документация

namespace SuperUI.Base.Hooks;

/// <summary>
/// Синхронный хук жизненного цикла компонента.
/// </summary>
public interface IComponentHook
{
    void OnInitialized(SgComponentBase component) { }
    void OnParametersSet(SgComponentBase component) { }
    void OnAfterRender(SgComponentBase component, bool firstRender) { }
}

/// <summary>
/// Хук с контролем рендера.
/// Возвращает false → рендер пропускается.
/// </summary>
public interface IRenderHook : IComponentHook
{
    bool ShouldRender(SgComponentBase component);
}

/// <summary>
/// Асинхронный хук жизненного цикла компонента.
/// </summary>
public interface IAsyncComponentHook : IComponentHook
{
    Task OnInitializedAsync(SgComponentBase component) => Task.CompletedTask;
    Task OnParametersSetAsync(SgComponentBase component) => Task.CompletedTask;
    Task OnAfterRenderAsync(SgComponentBase component, bool firstRender) => Task.CompletedTask;
}
