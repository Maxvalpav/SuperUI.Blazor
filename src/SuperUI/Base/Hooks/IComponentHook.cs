namespace SuperUI.Hooks;

using SuperUI.Base;

/// <summary>
/// Интерфейс хука жизненного цикла компонента.
/// Хуки регистрируются в AddHook() и вызываются автоматически.
/// </summary>
public interface IComponentHook
{
    void OnInitialized(SgComponentBase component) { }
    void OnParametersSet(SgComponentBase component) { }
    void OnAfterRender(SgComponentBase component, bool firstRender) { }
}

public interface IAsyncComponentHook : IComponentHook
{
    Task OnInitializedAsync(SgComponentBase component) => Task.CompletedTask;
    Task OnParametersSetAsync(SgComponentBase component) => Task.CompletedTask;
    Task OnAfterRenderAsync(SgComponentBase component, bool firstRender) => Task.CompletedTask;
}

public interface IRenderHook : IComponentHook
{
    bool ShouldRender(SgComponentBase component);
}
