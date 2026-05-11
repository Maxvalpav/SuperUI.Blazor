// SuperUI/Base/Hooks/IComponentHook.cs
namespace SuperUI.Base.Hooks;

/// <summary>
/// Синхронный хук жизненного цикла компонента.
/// Предоставляет точки входа во все синхронные lifecycle-методы Blazor.
/// </summary>
/// <remarks>
/// Все методы имеют default-реализацию — реализовывать нужно только нужные.
/// </remarks>
public interface IComponentHook
{
    /// <summary>Вызывается после OnInitialized компонента.</summary>
    void OnInitialized(SgComponentBase component) { }

    /// <summary>Вызывается после OnParametersSet компонента.</summary>
    void OnParametersSet(SgComponentBase component) { }

    /// <summary>Вызывается после OnAfterRender компонента.</summary>
    void OnAfterRender(SgComponentBase component, bool firstRender) { }
}