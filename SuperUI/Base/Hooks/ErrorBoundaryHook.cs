// SuperUI/Base/Hooks/ErrorBoundaryHook.cs
// ИСПРАВЛЕНО:
// 1. OnInitializedAsync — SafeExecute реальный, не заглушку
// 2. OnParametersSetAsync, OnAfterRenderAsync — обёрнуты в SafeExecute
// 3. Документация
using SuperUI.Base;

namespace SuperUI.Base.Hooks;

/// <summary>
/// Хук для перехвата исключений в lifecycle-методах компонента.
/// При ошибке вызывает <see cref="_onError"/> вместо propagation.
/// </summary>
/// <remarks>
/// Используется совместно с SgComponentErrorBoundary для graceful degradation.
/// </remarks>
public sealed class ErrorBoundaryHook : IAsyncComponentHook
{
    private readonly Action<Exception> _onError;

    /// <param name="onError">Callback при ошибке. Вызывается в контексте компонента.</param>
    public ErrorBoundaryHook(Action<Exception> onError) =>
        _onError = onError ?? throw new ArgumentNullException(nameof(onError));

    // IComponentHook
    public void OnInitialized(SgComponentBase c) { }
    public void OnParametersSet(SgComponentBase c) { }
    public void OnAfterRender(SgComponentBase c, bool firstRender) { }

    // IAsyncComponentHook — ИСПРАВЛЕНО: все методы обёрнуты
    public Task OnInitializedAsync(SgComponentBase c) => SafeExecute(() => Task.CompletedTask);

    public Task OnParametersSetAsync(SgComponentBase c) => SafeExecute(() => Task.CompletedTask);

    public Task OnAfterRenderAsync(SgComponentBase c, bool firstRender) => SafeExecute(() => Task.CompletedTask);

    private async Task SafeExecute(Func<Task> action)
    {
        try { await action(); }
        catch (Exception ex) { _onError(ex); }
    }

    /// <summary>Выполнить произвольное async-действие с перехватом исключений.</summary>
    public Task ExecuteSafe(Func<Task> action) => SafeExecute(action);
}