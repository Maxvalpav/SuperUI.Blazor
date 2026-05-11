using Microsoft.AspNetCore.Components.Web;

namespace SuperUI.Base.Services;

/// <summary>
/// Сервис глобальных горячих клавиш.
/// Регистрируется как Scoped через AddSuperUI().
/// </summary>
public interface IKeyboardService
{
    /// <summary>Подписаться на глобальное нажатие клавиши.</summary>
    IDisposable Subscribe(string keyCombo, Func<KeyboardEventArgs, Task> handler);

    /// <summary>Подписаться на глобальное нажатие клавиши (синхронный хендлер).</summary>
    IDisposable Subscribe(string keyCombo, Action<KeyboardEventArgs> handler);

    /// <summary>Отписаться по ключу.</summary>
    void Unsubscribe(string keyCombo);
}
