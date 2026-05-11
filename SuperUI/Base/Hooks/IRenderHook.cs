namespace SuperUI.Base.Hooks;

/// <summary>
/// Хук для контроля рендера компонента.
/// </summary>
public interface IRenderHook : IComponentHook
{
    bool ShouldRender(SgComponentBase component);
}
