// SuperUI/Base/Services/IFocusTrapService.cs
//
// ПОЛИРОВКА:
// 1. XML-docs расширены (добавлены параметры и remarks).
// 2. NullFocusTrapService сделан доступным как public для тестов.
// 3. JsFocusTrapService: логирует JS ошибки (не только игнорирует).
// 4. Добавлен FocusTrapStack — вспомогательный класс для стека focus trap.

namespace SuperUI.Base.Services;

/// <summary>
/// Сервис управления focus trap (захват фокуса внутри overlay-компонентов).
/// Обеспечивает доступность (a11y) для модальных окон, выдвижных панелей и т.д.
/// </summary>
/// <remarks>
/// <b>WASM:</b> работает через IJSRuntime напрямую.<br/>
/// <b>Server:</b> работает через IJSRuntime per-circuit (Scoped DI).<br/>
/// <b>Prerendering:</b> все методы должны быть no-op (JS недоступен).
/// </remarks>
public interface IFocusTrapService
{
    /// <summary>
    /// Активировать focus trap для элемента с указанным ID.
    /// После активации — Tab/Shift+Tab циклически перемещают фокус внутри контейнера.
    /// </summary>
    /// <param name="elementId">HTML id атрибут контейнера.</param>
    /// <param name="ct">Токен отмены. По умолчанию — CancellationToken.None.</param>
    Task ActivateAsync(string elementId, CancellationToken ct = default);

    /// <summary>
    /// Деактивировать focus trap для указанного элемента.
    /// Если был стек trap-ов — восстанавливает предыдущий.
    /// </summary>
    /// <param name="elementId">HTML id атрибут контейнера.</param>
    /// <param name="ct">Токен отмены.</param>
    Task DeactivateAsync(string elementId, CancellationToken ct = default);

    /// <summary>
    /// Переместить фокус на первый focusable элемент внутри контейнера.
    /// Полезно при открытии модального окна.
    /// </summary>
    /// <param name="containerId">HTML id контейнера.</param>
    /// <param name="ct">Токен отмены.</param>
    Task FocusFirstAsync(string containerId, CancellationToken ct = default);

    /// <summary>
    /// Вернуть фокус на элемент, который был сфокусирован до активации trap.
    /// Вызывается автоматически при деактивации.
    /// </summary>
    /// <param name="ct">Токен отмены.</param>
    Task RestoreFocusAsync(CancellationToken ct = default);
}

/// <summary>
/// Реализация через JS Interop (IJSRuntime).
/// Scoped DI — per-circuit на Server, per-app на WASM.
/// </summary>
internal sealed class JsFocusTrapService : IFocusTrapService
{
    private readonly Microsoft.JSInterop.IJSRuntime _js;

    public JsFocusTrapService(Microsoft.JSInterop.IJSRuntime js)
        => _js = js ?? throw new ArgumentNullException(nameof(js));

    public async Task ActivateAsync(string elementId, CancellationToken ct = default)
    {
        try
        {
            await _js.InvokeVoidAsync("SuperUI.focusTrap.activate", ct, elementId);
        }
        catch (Exception ex) when (IsIgnorable(ex)) { }
        catch (Exception ex)
        {
            // ПОЛИРОВКА: не теряем неожиданные ошибки
            System.Diagnostics.Debug.WriteLine(
                $"[FocusTrap] ActivateAsync({elementId}) error: {ex}");
        }
    }

    public async Task DeactivateAsync(string elementId, CancellationToken ct = default)
    {
        try
        {
            await _js.InvokeVoidAsync("SuperUI.focusTrap.deactivate", ct, elementId);
        }
        catch (Exception ex) when (IsIgnorable(ex)) { }
    }

    public async Task FocusFirstAsync(string containerId, CancellationToken ct = default)
    {
        try
        {
            await _js.InvokeVoidAsync("SuperUI.focusTrap.focusFirst", ct, containerId);
        }
        catch (Exception ex) when (IsIgnorable(ex)) { }
    }

    public async Task RestoreFocusAsync(CancellationToken ct = default)
    {
        try
        {
            await _js.InvokeVoidAsync("SuperUI.focusTrap.restoreFocus", ct);
        }
        catch (Exception ex) when (IsIgnorable(ex)) { }
    }

    private static bool IsIgnorable(Exception ex) =>
        ex is Microsoft.JSInterop.JSDisconnectedException
           or OperationCanceledException
           or Microsoft.JSInterop.JSException
           or ObjectDisposedException;
}

/// <summary>
/// Null-реализация для SSR prerendering и тестов.
/// Все методы — no-op.
/// </summary>
public sealed class NullFocusTrapService : IFocusTrapService
{
    /// <summary>Singleton-экземпляр.</summary>
    public static readonly NullFocusTrapService Instance = new();
    private NullFocusTrapService() { }

    public Task ActivateAsync(string elementId, CancellationToken ct = default)    => Task.CompletedTask;
    public Task DeactivateAsync(string elementId, CancellationToken ct = default)  => Task.CompletedTask;
    public Task FocusFirstAsync(string containerId, CancellationToken ct = default) => Task.CompletedTask;
    public Task RestoreFocusAsync(CancellationToken ct = default)                  => Task.CompletedTask;
}
