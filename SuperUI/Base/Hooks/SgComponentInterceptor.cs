// SuperUI/Base/Hooks/SgComponentInterceptor.cs
using System.Threading.Tasks;
using SuperUI.Base;

/// <summary>
/// Хук-перехватчик для сквозной функциональности:
/// - Логирование производительности
/// - A/B тестирование
/// - Feature flags
/// - Аудит действий пользователя
/// </summary>
public abstract class SgComponentInterceptor : IComponentHook, IAsyncComponentHook
{
    public virtual void OnInitialized(SgComponentBase component) { }
    public virtual Task OnInitializedAsync(SgComponentBase component) => Task.CompletedTask;
    public virtual void OnParametersSet(SgComponentBase component) { }
    public virtual Task OnParametersSetAsync(SgComponentBase component) => Task.CompletedTask;
    public virtual void OnAfterRender(SgComponentBase component, bool firstRender) { }
    public virtual Task OnAfterRenderAsync(SgComponentBase component, bool firstRender) => Task.CompletedTask;
}
