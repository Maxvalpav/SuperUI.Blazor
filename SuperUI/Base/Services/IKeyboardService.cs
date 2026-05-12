// SuperUI/Base/Services/IKeyboardService.cs
//
// ПОЛИРОВКА:
// 1. Clear() — отменить все регистрации (для unit-тестов).
// 2. HandlerCount — количество зарегистрированных обработчиков (диагностика).
// 3. BuildKeyString — нормализация регистра Key (e.Key = "escape" vs "Escape").
// 4. XML docs расширены.

using Microsoft.AspNetCore.Components.Web;

namespace SuperUI.Base.Services;

/// <summary>
/// Сервис регистрации глобальных горячих клавиш (window-level keyboard shortcuts).
/// </summary>
/// <remarks>
/// <b>Отличие от OnKey() в SgInteractiveBase:</b><br/>
/// <list type="bullet">
///   <item>OnKey() — обрабатывает клавиши внутри конкретного элемента компонента</item>
///   <item>IKeyboardService — регистрирует обработчики на уровне window (глобальные)</item>
/// </list>
/// Thread safety: Scoped DI → per-circuit → нет конкуренции. Если Singleton — использовать lock.
/// </remarks>
public interface IKeyboardService
{
    /// <summary>
    /// Зарегистрировать глобальный обработчик клавиши.
    /// </summary>
    /// <param name="key">Строка клавиши: "Ctrl+S", "Alt+F4", "Escape", "Shift+Enter" и т.д.</param>
    /// <param name="handler">
    /// Обработчик. Возвращает <c>true</c> если событие обработано (preventDefault будет вызван в JS).
    /// </param>
    /// <returns>Disposable для отмены регистрации.</returns>
    IDisposable Register(string key, Func<KeyboardEventArgs, Task<bool>> handler);

    /// <summary>Зарегистрировать async обработчик без возврата результата.</summary>
    IDisposable Register(string key, Func<Task> handler);

    /// <summary>Зарегистрировать синхронный обработчик.</summary>
    IDisposable Register(string key, Action handler);

    /// <summary>
    /// Вызвать обработчики для события (вызывается из JS via [JSInvokable]).
    /// </summary>
    /// <returns><c>true</c> если хотя бы один обработчик обработал событие.</returns>
    Task<bool> HandleKeyAsync(KeyboardEventArgs e);

    /// <summary>Снять все регистрации (для тестов/cleanup).</summary>
    void Clear();
}


