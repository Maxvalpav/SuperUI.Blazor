namespace SuperUI.Base.Hooks;

/// <summary>
/// Реестр хуков жизненного цикла компонентов.
/// Собирает все зарегистрированные IComponentHook и вызывает их последовательно.
/// </summary>
public interface IComponentHookRegistry
{
    IReadOnlyList<IComponentHook> Hooks { get; }
    ValueTask InvokeInitializedAsync(object component, string componentName);
    ValueTask InvokeParametersSetAsync(object component, string componentName, int changedCount);
    ValueTask InvokeRenderAsync(object component, string componentName, bool firstRender);
    ValueTask InvokeDisposedAsync(object component, string componentName);
}

/// <summary>
/// Реализация реестра хуков.
/// </summary>
public sealed class ComponentHookRegistry : IComponentHookRegistry
{
    private readonly IEnumerable<IComponentHook> _hooks;
    private IReadOnlyList<IComponentHook>? _cachedHooks;

    public ComponentHookRegistry(IEnumerable<IComponentHook> hooks)
    {
        _hooks = hooks;
    }

    public IReadOnlyList<IComponentHook> Hooks => _cachedHooks ??= _hooks.ToList().AsReadOnly();

    public async ValueTask InvokeInitializedAsync(object component, string componentName)
    {
        foreach (var hook in Hooks)
        {
            await hook.OnInitializedAsync(component, componentName);
        }
    }

    public async ValueTask InvokeParametersSetAsync(object component, string componentName, int changedCount)
    {
        foreach (var hook in Hooks)
        {
            await hook.OnParametersSetAsync(component, componentName, changedCount);
        }
    }

    public async ValueTask InvokeRenderAsync(object component, string componentName, bool firstRender)
    {
        foreach (var hook in Hooks)
        {
            await hook.OnRenderAsync(component, componentName, firstRender);
        }
    }

    public async ValueTask InvokeDisposedAsync(object component, string componentName)
    {
        foreach (var hook in Hooks)
        {
            await hook.OnDisposedAsync(component, componentName);
        }
    }
}
