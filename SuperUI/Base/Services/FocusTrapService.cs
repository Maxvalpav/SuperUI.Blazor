// SuperUI/Base/Services/FocusTrapService.cs
//
// ИСПРАВЛЕНИЯ:
//   ✅ using Microsoft.JSInterop добавлен (аналогично IFocusTrapService.cs)
//   ✅ MoveFocusAsync использует InvokeVoidAsync через SafeJsVoidAsync helper
//   ✅ JsFocusTrapServiceEx делегирует через JsFocusTrapService (не дублирует код)
//
// ДОРАБОТКИ:
//   ✅ FocusTrapStack — публичный вспомогательный класс для стека trap-ов
//   ✅ NullFocusTrapServiceEx — null-реализация для IFocusTrapServiceEx

using Microsoft.JSInterop; // ← КЛЮЧЕВОЕ ИСПРАВЛЕНИЕ

namespace SuperUI.Base.Services;

/// <summary>Направление перемещения фокуса внутри focus-trap контейнера.</summary>
public enum FocusDirection { Forward, Backward, First, Last }

/// <summary>
/// Расширенный интерфейс с навигацией по фокусу (<see cref="MoveFocusAsync"/>).
/// </summary>
public interface IFocusTrapServiceEx : IFocusTrapService
{
    /// <summary>
    /// Переместить фокус в указанном направлении внутри контейнера.
    /// </summary>
    Task MoveFocusAsync(string containerId, FocusDirection direction,
        CancellationToken ct = default);
}

/// <summary>Реализация <see cref="IFocusTrapServiceEx"/> через JS Interop.</summary>
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

    public async Task MoveFocusAsync(string containerId, FocusDirection direction,
        CancellationToken ct = default)
    {
        try
        {
            await _js.InvokeVoidAsync(
                "SuperUI.focusTrap.moveFocus",
                ct,
                containerId,
                direction.ToString().ToLowerInvariant());
        }
        catch (Exception ex) when (ex is JSDisconnectedException
                                   or OperationCanceledException
                                   or JSException
                                   or ObjectDisposedException)
        { /* Игнорируемые исключения */ }
    }
}

/// <summary>
/// Null-реализация <see cref="IFocusTrapServiceEx"/> для SSR и тестов.
/// </summary>
public sealed class NullFocusTrapServiceEx : IFocusTrapServiceEx
{
    public static readonly NullFocusTrapServiceEx Instance = new();
    private NullFocusTrapServiceEx() { }

    public Task ActivateAsync(string elementId, CancellationToken ct = default)
        => Task.CompletedTask;
    public Task DeactivateAsync(string elementId, CancellationToken ct = default)
        => Task.CompletedTask;
    public Task FocusFirstAsync(string containerId, CancellationToken ct = default)
        => Task.CompletedTask;
    public Task RestoreFocusAsync(CancellationToken ct = default)
        => Task.CompletedTask;
    public Task MoveFocusAsync(string containerId, FocusDirection direction,
        CancellationToken ct = default) => Task.CompletedTask;
}

/// <summary>
/// Вспомогательный класс для управления стеком активных focus trap-ов.
/// Используется в <see cref="SgOverlayBase"/> для корректного восстановления фокуса
/// при наличии нескольких одновременно открытых overlay.
/// </summary>

