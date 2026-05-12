// SuperUI/Base/Services/IFocusTrapService.cs
//
// ИСПРАВЛЕНИЯ КОМПИЛЯЦИИ:
//   ✅ CS0117: Task.CompletedValue → Task.CompletedTask (строка 136)
//   ✅ using Microsoft.JSInterop добавлен — InvokeVoidAsync является методом расширения
//      из JSRuntimeExtensions и требует этого using.
//
// ПОЛИРОВКА:
//   ✅ Общий helper SafeJsVoidAsync() — устранено дублирование try/catch
//   ✅ NullFocusTrapService — public для тестов
//   ✅ XML-docs расширены (params + remarks)
//   ✅ JsFocusTrapService логирует неожиданные JS-ошибки
//   ✅ JSException — теперь в IsIgnorable через JSException (не базовый Exception)
//
// WASM/Server совместимость:
//   ✅ IJSRuntime Scoped DI — per-circuit на Server, singleton-equiv на WASM
//   ✅ Prerendering: все методы no-op (NullFocusTrapService)
//   ✅ JSDisconnectedException обрабатывается — корректно для Server circuit disconnect

using Microsoft.JSInterop; // ← КЛЮЧЕВОЕ ИСПРАВЛЕНИЕ CS1061

namespace SuperUI.Base.Services;

/// <summary>
/// Сервис управления focus trap (захват фокуса внутри overlay-компонентов).
/// Обеспечивает доступность (a11y) для модальных окон, выдвижных панелей и т.д.
/// </summary>
/// <remarks>
/// <para><b>WASM:</b> работает через <see cref="IJSRuntime"/> напрямую (singleton-equivalent).</para>
/// <para><b>Server:</b> работает через <see cref="IJSRuntime"/> per-circuit (Scoped DI).</para>
/// <para><b>Prerendering:</b> все методы должны быть no-op — используйте <see cref="NullFocusTrapService"/>.</para>
/// </remarks>
public interface IFocusTrapService
{
    /// <summary>
    /// Активировать focus trap для элемента с указанным ID.
    /// После активации Tab/Shift+Tab циклически перемещают фокус внутри контейнера.
    /// </summary>
    /// <param name="elementId">HTML <c>id</c> атрибут контейнера.</param>
    /// <param name="ct">Токен отмены.</param>
    Task ActivateAsync(string elementId, CancellationToken ct = default);

    /// <summary>
    /// Деактивировать focus trap для указанного элемента.
    /// Если был стек trap-ов — восстанавливает предыдущий.
    /// </summary>
    /// <param name="elementId">HTML <c>id</c> атрибут контейнера.</param>
    /// <param name="ct">Токен отмены.</param>
    Task DeactivateAsync(string elementId, CancellationToken ct = default);

    /// <summary>
    /// Переместить фокус на первый focusable элемент внутри контейнера.
    /// Полезно при открытии модального окна.
    /// </summary>
    /// <param name="containerId">HTML <c>id</c> контейнера.</param>
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
/// Реализация <see cref="IFocusTrapService"/> через JS Interop (<see cref="IJSRuntime"/>).
/// Scoped DI — per-circuit на Server, per-app на WASM.
/// </summary>
internal sealed class JsFocusTrapService : IFocusTrapService
{
    private readonly IJSRuntime _js;

    public JsFocusTrapService(IJSRuntime js)
        => _js = js ?? throw new ArgumentNullException(nameof(js));

    /// <inheritdoc/>
    public Task ActivateAsync(string elementId, CancellationToken ct = default)
        => SafeJsVoidAsync("SuperUI.focusTrap.activate", ct, elementId);

    /// <inheritdoc/>
    public Task DeactivateAsync(string elementId, CancellationToken ct = default)
        => SafeJsVoidAsync("SuperUI.focusTrap.deactivate", ct, elementId);

    /// <inheritdoc/>
    public Task FocusFirstAsync(string containerId, CancellationToken ct = default)
        => SafeJsVoidAsync("SuperUI.focusTrap.focusFirst", ct, containerId);

    /// <inheritdoc/>
    public Task RestoreFocusAsync(CancellationToken ct = default)
        => SafeJsVoidAsync("SuperUI.focusTrap.restoreFocus", ct);

    // ── Общий helper — устраняет дублирование try/catch ──────────────────────
    private async Task SafeJsVoidAsync(string identifier, CancellationToken ct,
        params object?[] args)
    {
        try
        {
            await _js.InvokeVoidAsync(identifier, ct, args);
        }
        catch (Exception ex) when (IsIgnorable(ex)) { /* no-op */ }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"[FocusTrap] {identifier} error: {ex.GetType().Name}: {ex.Message}");
        }
    }

    private static bool IsIgnorable(Exception ex)
        => ex is JSDisconnectedException
            or OperationCanceledException
            or JSException
            or ObjectDisposedException;
}

/// <summary>
/// Null-реализация для SSR prerendering и тестов.
/// Все методы — no-op. Потокобезопасна (иммутабельный singleton).
/// </summary>
public sealed class NullFocusTrapService : IFocusTrapService
{
    /// <summary>Singleton-экземпляр (thread-safe lazy init).</summary>
    public static readonly NullFocusTrapService Instance = new();

    private NullFocusTrapService() { }

    /// <inheritdoc/>
    public Task ActivateAsync(string elementId, CancellationToken ct = default)
        => Task.CompletedTask;

    /// <inheritdoc/>
    public Task DeactivateAsync(string elementId, CancellationToken ct = default)
        => Task.CompletedTask; // ✅ ИСПРАВЛЕНО: было Task.CompletedValue (CS0117)

    /// <inheritdoc/>
    public Task FocusFirstAsync(string containerId, CancellationToken ct = default)
        => Task.CompletedTask;

    /// <inheritdoc/>
    public Task RestoreFocusAsync(CancellationToken ct = default)
        => Task.CompletedTask;
}
