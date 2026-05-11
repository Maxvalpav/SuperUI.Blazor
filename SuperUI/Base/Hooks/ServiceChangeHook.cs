using SuperUI.Base;
using SuperUI.Base.Hooks;

namespace SuperUI.Base.Hooks;

/// <summary>
/// Интерфейс сервиса, уведомляющего об изменениях.
/// </summary>
public interface INotifyChanged
{
    event Action Changed;
}

/// <summary>
/// Хук для авто-обновления компонента при изменении сервиса.
/// </summary>
public sealed class ServiceChangeHook<TService> : IAsyncComponentHook, IDisposable
    where TService : INotifyChanged
{
    private readonly TService _service;
    private SgComponentBase? _component;

    public ServiceChangeHook(TService service) => _service = service;

    // IComponentHook
    public void OnInitialized(SgComponentBase c)
    {
        _component = c;
        _service.Changed += OnServiceChanged;
    }

    public void OnParametersSet(SgComponentBase c) { }
    public void OnAfterRender(SgComponentBase c, bool firstRender) { }

    // IAsyncComponentHook
    public Task OnInitializedAsync(SgComponentBase c) => Task.CompletedTask;
    public Task OnParametersSetAsync(SgComponentBase c) => Task.CompletedTask;
    public Task OnAfterRenderAsync(SgComponentBase c, bool firstRender) => Task.CompletedTask;

    private void OnServiceChanged()
        => _component?.RefreshAsync();

    public void Dispose() => _service.Changed -= OnServiceChanged;
}
