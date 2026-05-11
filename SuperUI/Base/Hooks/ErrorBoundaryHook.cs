using SuperUI.Base;
using SuperUI.Base.Hooks;

namespace SuperUI.Base.Hooks;

/// <summary>
/// Хук для обработки ошибок в жизненном цикле компонента.
/// </summary>
public sealed class ErrorBoundaryHook : IAsyncComponentHook
{
    private readonly Action<Exception> _onError;

    public ErrorBoundaryHook(Action<Exception> onError) => _onError = onError;

    // IComponentHook
    public void OnInitialized(SgComponentBase c) { }
    public void OnParametersSet(SgComponentBase c) { }
    public void OnAfterRender(SgComponentBase c, bool firstRender) { }

    // IAsyncComponentHook
    public Task OnInitializedAsync(SgComponentBase c) => SafeExecute(() => Task.CompletedTask);
    public Task OnParametersSetAsync(SgComponentBase c) => Task.CompletedTask;
    public Task OnAfterRenderAsync(SgComponentBase c, bool firstRender) => Task.CompletedTask;

    private async Task SafeExecute(Func<Task> action)
    {
        try { await action(); }
        catch (Exception ex) { _onError(ex); }
    }
}
