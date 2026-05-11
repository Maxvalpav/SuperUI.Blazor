namespace SuperUI.Base.Hooks;

/// <summary>
/// Синхронный хук жизненного цикла компонента.
/// Реализуйте этот интерфейс для перехвата lifecycle событий.
/// </summary>
public interface IComponentHook
{
    void OnInitialized(SgComponentBase component);
    void OnParametersSet(SgComponentBase component);
    void OnAfterRender(SgComponentBase component, bool firstRender);
    bool ShouldRender(SgComponentBase component) => true; // default interface implementation
}
