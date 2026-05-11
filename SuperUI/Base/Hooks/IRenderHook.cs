namespace SuperUI.Base.Hooks;

/// <summary>
/// Хук управления рендерингом.
/// </summary>
public interface IRenderHook : IComponentHook
{
    /// <summary>
    /// Возврат false — подавить рендер компонента.
    /// </summary>
    bool ShouldRender(SgComponentBase component);
}
