using SuperUI.Base;

namespace SuperUI.Hooks;

/// <summary>
/// Хук для обработки ошибок в жизненном цикле компонента.
/// </summary>
public sealed class ErrorBoundaryHook : IAsyncComponentHook
{
    private readonly Action<Exception> _onError;

    public ErrorBoundaryHook(Action<Exception> onError) => _onError = onError;

    public Task OnInitializedAsync(SgComponentBase c)
        => SafeExecute(() => Task.CompletedTask);

    private async Task SafeExecute(Func<Task> action)
    {
        try { await action(); }
        catch (Exception ex) { _onError(ex); }
    }
}
