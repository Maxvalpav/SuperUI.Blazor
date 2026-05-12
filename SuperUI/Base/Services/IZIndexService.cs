// SuperUI/Base/Services/IZIndexService.cs
//
// ИСПРАВЛЕНИЯ:
// 1. Добавлен Allocate(int baseZIndex) — был нужен компонентам
// 2. Добавлен TopOwnerChanged event — был нужен SgDockWindow
// 3. Добавлены BringToFront / RemoveFromStack

namespace SuperUI.Base.Services;

/// <summary>
/// Контракт сервиса управления z-index для оверлеев SuperUI.
/// </summary>
public interface IZIndexService
{
    // ── Константы баз ────────────────────────────────────────────────────────────
    // Вынесены в ZIndexService как публичные const.

    /// <summary>Текущий максимальный выданный z-index.</summary>
    int Current { get; }

    /// <summary>Получить следующий z-index (автоинкремент).</summary>
    int GetNext();

    /// <summary>Выделить z-index не ниже указанной базы.</summary>
    int Allocate(int baseZIndex);

    /// <summary>Вернуть z-index в пул.</summary>
    void Release(int zIndex);

    /// <summary>Сбросить сервис (тесты, горячая перезагрузка).</summary>
    void Reset();

    /// <summary>Поднять окно на верх стека фокуса.</summary>
    void BringToFront(object window);

    /// <summary>Удалить окно из стека фокуса.</summary>
    void RemoveFromStack(object window);

    /// <summary>
    /// Вызывается при смене верхнего окна в стеке фокуса.
    /// Нужно для SgDockWindow визуальной индикации активности.
    /// </summary>
    event Action<object?>? TopOwnerChanged;
}
