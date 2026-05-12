// SuperUI/Base/Hooks/IAsyncComponentHook.cs
namespace SuperUI.Base.Hooks;

/// <summary>
/// Асинхронный хук жизненного цикла компонента.
/// </summary>
public interface IAsyncComponentHook : IComponentHook
{
    Task OnInitializedAsync(SgComponentBase component) => Task.CompletedTask;
    Task OnParametersSetAsync(SgComponentBase component) => Task.CompletedTask;
    Task OnAfterRenderAsync(SgComponentBase component, bool firstRender) => Task.CompletedTask;
    Task OnFirstRenderAsync(SgComponentBase component) => Task.CompletedTask;
}
