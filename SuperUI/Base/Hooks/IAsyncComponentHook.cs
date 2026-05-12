// SuperUI/Base/Hooks/IAsyncComponentHook.cs
namespace SuperUI.Base.Hooks;

/// <summary>
/// Асинхронный хук жизненного цикла компонента.
/// Предоставляет async-версии всех lifecycle-методов Blazor.
/// </summary>
/// <remarks>
/// Все методы имеют default-реализацию, возвращающую <see cref="Task.CompletedTask"/>.
/// Переопределяйте только те методы, которые нужны.
/// 
/// Thread safety:
/// - WASM: однопоточный, Task — синхронный overhead.
/// - Server: вызывается в контексте SignalR circuit — не используйте глобальные mutable состояния без lock.
/// </remarks>
public interface IAsyncComponentHook : IComponentHook
{
    /// <summary>Вызывается после OnInitializedAsync компонента.</summary>
    Task OnInitializedAsync(SgComponentBase component) => Task.CompletedTask;

    /// <summary>Вызывается после OnParametersSetAsync компонента.</summary>
    Task OnParametersSetAsync(SgComponentBase component) => Task.CompletedTask;

    /// <summary>Вызывается после первого вызова OnAfterRenderAsync (firstRender=true).</summary>
    Task OnFirstRenderAsync(SgComponentBase component) => Task.CompletedTask;

    /// <summary>Вызывается после OnAfterRenderAsync компонента.</summary>
    Task OnAfterRenderAsync(SgComponentBase component, bool firstRender) => Task.CompletedTask;
}