// SuperUI/Base/Hooks/IComponentHook.cs
using System;
using System.Threading.Tasks;

namespace SuperUI.Base.Hooks;

/// <summary>
/// Lifecycle hook interface for SuperUI components.
/// Mirrors Blazor's component lifecycle with sync methods.
/// </summary>
public interface IComponentHook
{
    void OnInitialized();
    void OnParametersSet();
    void OnAfterRender(bool firstRender);
}

/// <summary>
/// Async version of component lifecycle hooks.
/// </summary>
public interface IAsyncComponentHook
{
    Task OnInitializedAsync();
    Task OnParametersSetAsync();
    Task OnAfterRenderAsync(bool firstRender);
}
