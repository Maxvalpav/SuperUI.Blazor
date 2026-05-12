// SuperUI/Base/Hooks/IComponentHook.cs
namespace SuperUI.Base.Hooks;

/// <summary>
/// Синхронный хук жизненного цикла компонента.
/// Все методы имеют default-реализацию — реализовывать нужно только нужные.
/// </summary>
public interface IComponentHook
{
    void OnInitialized(SgComponentBase component) { }
    void OnParametersSet(SgComponentBase component) { }
    void OnAfterRender(SgComponentBase component, bool firstRender) { }
}
