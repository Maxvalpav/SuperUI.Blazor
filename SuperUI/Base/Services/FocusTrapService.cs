// SuperUI/Base/Services/FocusTrapService.cs

using Microsoft.JSInterop;

namespace SuperUI.Base.Services;

/// <summary>
/// Направление перемещения фокуса внутри focus-trap контейнера.
/// </summary>
public enum FocusDirection
{
    Forward,
    Backward,
    First,
    Last
}

/// <summary>
/// Расширенный интерфейс с навигацией по фокусу (MoveFocusAsync).
/// </summary>
public interface IFocusTrapServiceEx : IFocusTrapService
{
    Task MoveFocusAsync(string containerId, FocusDirection direction, CancellationToken ct = default);
}

/// <summary>
/// Реализация IFocusTrapServiceEx через JS Interop.
/// </summary>
internal sealed class JsFocusTrapServiceEx : IFocusTrapServiceEx
{
    private readonly JsFocusTrapService _inner;
    private readonly IJSRuntime _js;

    public JsFocusTrapServiceEx(IJSRuntime js)
    {
        _js = js ?? throw new ArgumentNullException(nameof(js));
        _inner = new JsFocusTrapService(js);
    }

    public Task ActivateAsync(string elementId, CancellationToken ct = default)
        => _inner.ActivateAsync(elementId, ct);

    public Task DeactivateAsync(string elementId, CancellationToken ct = default)
        => _inner.DeactivateAsync(elementId, ct);

    public Task FocusFirstAsync(string containerId, CancellationToken ct = default)
        => _inner.FocusFirstAsync(containerId, ct);

    public Task RestoreFocusAsync(CancellationToken ct = default)
        => _inner.RestoreFocusAsync(ct);

    public async Task MoveFocusAsync(string containerId, FocusDirection direction, CancellationToken ct = default)
    {
        try
        {
            await _js.InvokeVoidAsync(
                "SuperUI.focusTrap.moveFocus", ct,
                containerId, direction.ToString().ToLowerInvariant());
        }
        catch (Exception ex) when (ex is JSDisconnectedException or OperationCanceledException
                                      or JSException or ObjectDisposedException) { }
    }
}
