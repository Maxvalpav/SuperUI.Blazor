// SuperUI/Base/Hooks/SgComponentInterceptor.cs
// ИСПРАВЛЕНО:
// 1. Реализует IRenderHook явно (не просто объявляет ShouldRender)
// 2. ShouldRender помечен virtual
// 3. Документация
using SuperUI.Base;

namespace SuperUI.Base.Hooks;

/// <summary>
/// Базовый класс для хуков-перехватчиков (cross-cutting concerns).
/// </summary>
/// <remarks>
/// Используется для: логирования производительности, feature flags, A/B тестов, аудита.
/// Реализует <see cref="IAsyncComponentHook"/> и <see cref="IRenderHook"/>.
/// Переопределяйте только нужные методы.
/// </remarks>
public abstract class SgComponentInterceptor : IComponentHook, IRenderHook
{
    // IComponentHook
    public virtual void OnInitialized(SgComponentBase component) { }
    public virtual void OnParametersSet(SgComponentBase component) { }
    public virtual void OnAfterRender(SgComponentBase component, bool firstRender) { }

    // IRenderHook — ИСПРАВЛЕНО: virtual (переопределяется в SgPerformanceInterceptor и др.)
    public virtual bool ShouldRender(SgComponentBase component) => true;

    // IAsyncComponentHook
    public virtual Task OnInitializedAsync(SgComponentBase component) => Task.CompletedTask;
    public virtual Task OnParametersSetAsync(SgComponentBase component) => Task.CompletedTask;
    public virtual Task OnAfterRenderAsync(SgComponentBase component, bool firstRender) => Task.CompletedTask;
}