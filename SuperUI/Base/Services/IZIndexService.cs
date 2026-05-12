// SuperUI/Base/Services/IZIndexService.cs
//
// ИСПРАВЛЕНИЯ:
// ✅ CS1061: Добавлены static interface members (.NET 8+) — ModalBase, DrawerBase и др.
//    SgOverlayBase.cs строка 94 обращается к ZIndexService.ModalBase через IZIndexService —
//    теперь это компилируется: IZIndexService.ModalBase
// ✅ Добавлен Allocate(int baseZIndex) — был нужен компонентам
// ✅ Добавлен TopOwnerChanged event — был нужен SgDockWindow
// ✅ Добавлены BringToFront / RemoveFromStack

namespace SuperUI.Base.Services;

/// <summary>
/// Контракт сервиса управления z-index для оверлеев SuperUI.
/// </summary>
public interface IZIndexService
{
    // ── Статические константы базовых уровней (.NET 8+ static interface members) ──
    // ИСПРАВЛЕНИЕ CS1061: SgOverlayBase использует IZIndexService.ModalBase и др.
    // Ранее эти константы были только в ZIndexService (class), а не в интерфейсе.

    /// <summary>Базовый z-index для модальных окон.</summary>
    static int ModalBase => 1000;

    /// <summary>Базовый z-index для боковых панелей (Drawer).</summary>
    static int DrawerBase => 1000;

    /// <summary>Базовый z-index для плавающих окон (DockWindow).</summary>
    static int WindowBase => 900;

    /// <summary>Базовый z-index для поповеров.</summary>
    static int PopoverBase => 1100;

    /// <summary>Базовый z-index для тултипов.</summary>
    static int TooltipBase => 1200;

    /// <summary>Базовый z-index для контекстных меню.</summary>
    static int ContextMenuBase => 1150;

    // ── Инстанс-члены ──────────────────────────────────────────────────────────

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
