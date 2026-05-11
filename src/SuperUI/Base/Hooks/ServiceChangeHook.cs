using SuperUI.Base;

namespace SuperUI.Hooks;

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

    public void OnInitialized(SgComponentBase c)
    {
        _component = c;
        _service.Changed += OnServiceChanged;
    }

    private void OnServiceChanged()
        => _component?.InvokeAsync(_component.StateHasChanged);

    public void Dispose() => _service.Changed -= OnServiceChanged;
}
