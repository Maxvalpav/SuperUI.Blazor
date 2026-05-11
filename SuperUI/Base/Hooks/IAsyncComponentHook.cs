namespace SuperUI.Base.Hooks;

/// <summary>
/// Асинхронный хук жизненного цикла компонента.
/// </summary>
public interface IAsyncComponentHook : IComponentHook
{
    Task OnInitializedAsync(SgComponentBase component);
    Task OnParametersSetAsync(SgComponentBase component);
    Task OnAfterRenderAsync(SgComponentBase component, bool firstRender);
}
