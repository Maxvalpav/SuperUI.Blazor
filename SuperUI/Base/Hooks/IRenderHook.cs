// SuperUI/Base/Hooks/IRenderHook.cs
namespace SuperUI.Base.Hooks;

/// <summary>
/// Хук для управления решением о рендере (ShouldRender).
/// </summary>
public interface IRenderHook : IComponentHook
{
    /// <summary>
    /// Вернуть false чтобы пропустить рендер.
    /// Вызывается из ShouldRender() компонента.
    /// </summary>
    bool ShouldRender(SgComponentBase component);
}
