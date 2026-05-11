// SuperUI/Base/Hooks/SgComponentInterceptor.cs
// ИСПРАВЛЕНО:
// 1. Добавлен namespace SuperUI.Base.Hooks (отсутствовал — CS0246)
// 2. Добавлен using SuperUI.Base (для SgComponentBase)
// 3. Реализует IAsyncComponentHook корректно
using SuperUI.Base;

namespace SuperUI.Base.Hooks;

/// <summary>
/// Базовый хук-перехватчик для сквозной функциональности:
/// - Логирование производительности
/// - A/B тестирование
/// - Feature flags
/// - Аудит действий пользователя
/// </summary>
public abstract class SgComponentInterceptor : IAsyncComponentHook
{
    // IComponentHook
    public virtual void OnInitialized(SgComponentBase component) { }
    public virtual void OnParametersSet(SgComponentBase component) { }
    public virtual void OnAfterRender(SgComponentBase component, bool firstRender) { }
    public virtual bool ShouldRender(SgComponentBase component) => true;

    // IAsyncComponentHook
    public virtual Task OnInitializedAsync(SgComponentBase component) => Task.CompletedTask;
    public virtual Task OnParametersSetAsync(SgComponentBase component) => Task.CompletedTask;
    public virtual Task OnAfterRenderAsync(SgComponentBase component, bool firstRender) => Task.CompletedTask;
}