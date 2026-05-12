// SuperUI/Base/Services/ZIndexService.cs
//
// ИСПРАВЛЕНИЯ:
// 1. Добавлены константы базовых уровней (были в SgZIndexService, теперь в самом сервисе)
// 2. Добавлен Allocate(int baseZIndex) — возвращает Max(baseZIndex, GetNext())
// 3. Добавлено событие TopOwnerChanged для SgDockWindow (stack of focused windows)
// 4. Release(int) — исправлена логика рекурсивного уменьшения _current
// 5. Все публичные члены задокументированы
// 6. Thread-safety: lock(_lock) везде, событие вне lock

namespace SuperUI.Base.Services;

/// <summary>
/// Сервис управления z-index для оверлеев (Modal, Drawer, Tooltip, Popover, DockWindow).
/// Регистрируется как Scoped (Server: per-circuit, WASM: singleton-equivalent).
/// </summary>
public sealed class ZIndexService : IZIndexService
{
    // ── Базовые уровни ───────────────────────────────────────────────────────────
    /// <summary>Базовый z-index для модальных окон.</summary>
    public const int ModalBase       = 1000;

    /// <summary>Базовый z-index для боковых панелей (Drawer).</summary>
    public const int DrawerBase      = 1000;

    /// <summary>Базовый z-index для плавающих окон (DockWindow).</summary>
    public const int WindowBase      = 900;

    /// <summary>Базовый z-index для поповеров.</summary>
    public const int PopoverBase     = 1100;

    /// <summary>Базовый z-index для тултипов.</summary>
    public const int TooltipBase     = 1200;

    /// <summary>Базовый z-index для контекстных меню.</summary>
    public const int ContextMenuBase = 1150;

    // ── Приватные поля ───────────────────────────────────────────────────────────
    private const int BaseZIndex = 800;
    private const int Step       = 10;

    private readonly object _lock = new();
    private int _current = BaseZIndex;
    private readonly SortedSet<int> _released = [];

    // ── Стек активных окон (для TopOwnerChanged) ─────────────────────────────────
    private readonly List<WeakReference<object>> _windowStack = [];

    // ── События ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Вызывается когда меняется "верхнее" активное окно (DockWindow focus).
    /// Аргумент — текущий верхний владелец (или null если стек пуст).
    /// </summary>
    public event Action<object?>? TopOwnerChanged;

    // ── Публичные свойства ───────────────────────────────────────────────────────

    /// <summary>Текущий максимальный выданный z-index.</summary>
    public int Current
    {
        get { lock (_lock) return _current; }
    }

    // ── Методы ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// Получить следующий z-index (автоинкремент с шагом <see cref="Step"/>).
    /// </summary>
    public int GetNext()
    {
        lock (_lock)
        {
            if (_released.Count > 0)
            {
                var reused = _released.Max;
                _released.Remove(reused);
                if (reused > _current) _current = reused;
                return reused;
            }
            _current += Step;
            return _current;
        }
    }

    /// <summary>
    /// Выделить z-index не ниже <paramref name="baseZIndex"/>.
    /// Используется компонентами: <c>ZIndex.Allocate(ZIndexService.ModalBase)</c>.
    /// </summary>
    /// <param name="baseZIndex">Минимальный желаемый уровень.</param>
    /// <returns>z-index ≥ baseZIndex.</returns>
    public int Allocate(int baseZIndex)
    {
        lock (_lock)
        {
            // Поднять _current до минимального уровня если нужно
            if (_current < baseZIndex)
                _current = baseZIndex;

            if (_released.Count > 0)
            {
                var reused = _released.Max;
                if (reused >= baseZIndex)
                {
                    _released.Remove(reused);
                    if (reused > _current) _current = reused;
                    return reused;
                }
            }
            _current += Step;
            return _current;
        }
    }

    /// <summary>
    /// Освободить z-index (вернуть в пул для повторного использования).
    /// </summary>
    public void Release(int zIndex)
    {
        if (zIndex <= BaseZIndex) return;

        lock (_lock)
        {
            _released.Add(zIndex);
            // Убираем "хвост" освобождённых значений с конца
            while (_released.Count > 0 && _released.Max == _current)
            {
                _released.Remove(_current);
                _current -= Step;
                if (_current < BaseZIndex) _current = BaseZIndex;
            }
        }
    }

    /// <summary>Сбросить в начальное состояние (используется в тестах).</summary>
    public void Reset()
    {
        lock (_lock)
        {
            _current = BaseZIndex;
            _released.Clear();
            _windowStack.Clear();
        }
        TopOwnerChanged?.Invoke(null);
    }

    // ── Window stack (для DockWindow focus management) ────────────────────────────

    /// <summary>
    /// Зарегистрировать окно как активное (поднять на верх стека).
    /// </summary>
    public void BringToFront(object window)
    {
        ArgumentNullException.ThrowIfNull(window);
        object? newTop;
        lock (_lock)
        {
            // Удаляем мёртвые WeakRef и уже существующую запись для window
            _windowStack.RemoveAll(w => !w.TryGetTarget(out var t) || ReferenceEquals(t, window));
            _windowStack.Add(new WeakReference<object>(window));
            newTop = window;
        }
        TopOwnerChanged?.Invoke(newTop);
    }

    /// <summary>
    /// Удалить окно из стека (вызывается при закрытии/dispose).
    /// </summary>
    public void RemoveFromStack(object window)
    {
        ArgumentNullException.ThrowIfNull(window);
        object? newTop = null;
        lock (_lock)
        {
            _windowStack.RemoveAll(w => !w.TryGetTarget(out var t) || ReferenceEquals(t, window));
            // Новый топ — последний живой
            for (int i = _windowStack.Count - 1; i >= 0; i--)
            {
                if (_windowStack[i].TryGetTarget(out var t))
                {
                    newTop = t;
                    break;
                }
            }
        }
        TopOwnerChanged?.Invoke(newTop);
    }
}
