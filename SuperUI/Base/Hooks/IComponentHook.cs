namespace SuperUI.Base.Hooks;

/// <summary>
/// Интерфейс хука жизненного цикла компонента.
/// Позволяет внедрять кросс-катting логику (логирование, телеметрия, отладка).
/// </summary>
public interface IComponentHook
{
    ValueTask OnInitializedAsync(object component, string componentName);
    ValueTask OnParametersSetAsync(object component, string componentName, int changedCount);
    ValueTask OnRenderAsync(object component, string componentName, bool firstRender);
    ValueTask OnDisposedAsync(object component, string componentName);
}
