// SuperUI/Base/Services/IFocusTrapService.cs
//
// Сервис управления focus trap для доступности (a11y).
// При открытии модального окна — фокус захватывается внутри него.
// При закрытии — возвращается на предыдущий элемент.
//
// Реализация делегирует JS-модулю superui.js.

namespace SuperUI.Base.Services;

/// <summary>
/// Сервис управления focus trap (захват фокуса внутри overlay-компонентов).
/// </summary>
public interface IFocusTrapService
{
    /// <summary>Активировать focus trap для элемента с указанным ID.</summary>
    Task ActivateAsync(string elementId, CancellationToken ct = default);

    /// <summary>Деактивировать focus trap.</summary>
    Task DeactivateAsync(string elementId, CancellationToken ct = default);

    /// <summary>Переместить фокус на первый focusable элемент внутри контейнера.</summary>
    Task FocusFirstAsync(string containerId, CancellationToken ct = default);

    /// <summary>Вернуть фокус на ранее сфокусированный элемент.</summary>
    Task RestoreFocusAsync(CancellationToken ct = default);
}

/// <summary>
/// Реализация через JS Interop.
/// </summary>
internal sealed class JsFocusTrapService : IFocusTrapService
{
    private readonly Microsoft.JSInterop.IJSRuntime _js;

    public JsFocusTrapService(Microsoft.JSInterop.IJSRuntime js)
    {
        _js = js;
    }

    public async Task ActivateAsync(string elementId, CancellationToken ct = default)
    {
        try
        {
            await _js.InvokeVoidAsync("SuperUI.focusTrap.activate", ct, elementId);
        }
        catch (Exception ex) when (
            ex is Microsoft.JSInterop.JSDisconnectedException or
                  OperationCanceledException or
                  Microsoft.JSInterop.JSException)
        {
            // Игнорируем при отключении circuit
        }
    }

    public async Task DeactivateAsync(string elementId, CancellationToken ct = default)
    {
        try
        {
            await _js.InvokeVoidAsync("SuperUI.focusTrap.deactivate", ct, elementId);
        }
        catch (Exception ex) when (
            ex is Microsoft.JSInterop.JSDisconnectedException or
                  OperationCanceledException or
                  Microsoft.JSInterop.JSException)
        {
        }
    }

    public async Task FocusFirstAsync(string containerId, CancellationToken ct = default)
    {
        try
        {
            await _js.InvokeVoidAsync("SuperUI.focusTrap.focusFirst", ct, containerId);
        }
        catch { }
    }

    public async Task RestoreFocusAsync(CancellationToken ct = default)
    {
        try
        {
            await _js.InvokeVoidAsync("SuperUI.focusTrap.restoreFocus", ct);
        }
        catch { }
    }
}

/// <summary>
/// Null-реализация для сред без JS (тесты, SSR).
/// </summary>
public sealed class NullFocusTrapService : IFocusTrapService
{
    public static readonly NullFocusTrapService Instance = new();
    private NullFocusTrapService() { }

    public Task ActivateAsync(string elementId, CancellationToken ct = default) => Task.CompletedTask;
    public Task DeactivateAsync(string elementId, CancellationToken ct = default) => Task.CompletedTask;
    public Task FocusFirstAsync(string containerId, CancellationToken ct = default) => Task.CompletedTask;
    public Task RestoreFocusAsync(CancellationToken ct = default) => Task.CompletedTask;
}
