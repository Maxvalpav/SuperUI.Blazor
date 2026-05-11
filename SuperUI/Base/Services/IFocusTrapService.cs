namespace SuperUI.Services;

/// <summary>
/// Сервис управления ловушкой фокуса (Focus Trap) для модальных окон и оверлеев.
/// </summary>
public interface IFocusTrapService
{
    Task ActivateAsync(string containerId);
    Task DeactivateAsync(string containerId);
    Task MoveFocusAsync(string containerId, FocusDirection direction);
}

/// <summary>
/// Направление перемещения фокуса.
/// </summary>
public enum FocusDirection { Next, Previous, First, Last }
