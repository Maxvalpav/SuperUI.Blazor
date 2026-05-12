// SuperUI/Base/Hooks/ServiceChangeHook.cs
using SuperUI.Base;
using System.Diagnostics;

namespace SuperUI.Base.Hooks;

/// <summary>
/// Интерфейс сервиса, уведомляющего об изменениях.
/// </summary>
public interface INotifyChanged
{
    event Action Changed;
}

/// <summary>
/// Хук авто-обновления компонента при изменении сервиса.
/// </summary>
/// <remarks>
/// - Подписка идемпотентна: повторный <see cref="OnInitialized"/> не создаёт дубликат.
/// - <see cref="Dispose"/> отписывается. Реализуйте <see cref="IDisposable"/>/<see cref="IAsyncDisposable"/>
///   в компоненте и вызывайте Dispose хука, чтобы избежать утечки event handler'ов
///   (особенно критично на Server: сервис singleton/scoped живёт дольше компонента).
/// - <see cref="RefreshAsync"/> вызывается через dispatcher компонента — корректно на Server,
///   где Changed может прийти с произвольного потока.
/// </remarks>
public sealed class ServiceChangeHook<TService> : IAsyncComponentHook, IDisposable
    where TService : INotifyChanged
{
    private readonly TService _service;
    private SgComponentBase? _component;
    private int _subscribed; // 0/1, atomic flag

    public ServiceChangeHook(TService service)
        => _service = service ?? throw new ArgumentNullException(nameof(service));

    public void OnInitialized(SgComponentBase c)
    {
        _component = c ?? throw new ArgumentNullException(nameof(c));
        if (Interlocked.Exchange(ref _subscribed, 1) == 0)
            _service.Changed += OnServiceChanged;
    }

    public void OnParametersSet(SgComponentBase c) { }
    public void OnAfterRender(SgComponentBase c, bool firstRender) { }

    public Task OnInitializedAsync(SgComponentBase c) => Task.CompletedTask;
    public Task OnParametersSetAsync(SgComponentBase c) => Task.CompletedTask;
    public Task OnAfterRenderAsync(SgComponentBase c, bool firstRender) => Task.CompletedTask;

    private void OnServiceChanged()
    {
        var component = _component;
        if (component is null) return;
        // RefreshAsync ставит StateHasChanged через dispatcher компонента —
        // безопасно вызывать из любого потока (Server-сценарий).
        _ = component.RefreshAsync();
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _subscribed, 0) == 1)
            _service.Changed -= OnServiceChanged;
        _component = null;
    }

#if DEBUG
    ~ServiceChangeHook()
    {
        if (Volatile.Read(ref _subscribed) == 1)
            System.Diagnostics.Debug.WriteLine(
                $"[LEAK] ServiceChangeHook<{typeof(TService).Name}> was not disposed!");
    }
#endif
}
