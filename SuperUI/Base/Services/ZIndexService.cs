// SuperUI/Base/Services/ZIndexService.cs
//
// ИСПРАВЛЕНИЯ:
// ✅ Константы теперь дублируются в IZIndexService как static interface members
// ✅ Добавлен Allocate(int baseZIndex)
// ✅ Событие TopOwnerChanged для SgDockWindow
// ✅ Release(int) — исправлена логика рекурсивного уменьшения _current
// ✅ WeakReference<T> для window stack — предотвращает утечки памяти
// ✅ Thread-safety: lock(_lock) везде, событие вне lock

namespace SuperUI.Base.Services;

/// <summary>
/// Сервис управления z-index для оверлеев (Modal, Drawer, Tooltip, Popover, DockWindow).
/// Регистрируется как Scoped (Server: per-circuit, WASM: singleton-equivalent).
/// </summary>
public sealed class ZIndexService : IZIndexService
{
    // ── Базовые уровни ─────────────────────────────────────────────────────────
    // Дублируются здесь для обратной совместимости (прямое использование ZIndexService.ModalBase)
    public const int ModalBase       = 1000;
    public const int DrawerBase      = 1000;
    public const int WindowBase      = 900;
    public const int PopoverBase     = 1100;
    public const int TooltipBase     = 1200;
    public const int ContextMenuBase = 1150;

    // ── Приватные поля ─────────────────────────────────────────────────────────
    private const int BaseZIndex = 800;
    private const int Step       = 10;
    private readonly object _lock = new();
    private int _current = BaseZIndex;
    private readonly SortedSet<int> _released = [];

    // ── Стек активных окон (для TopOwnerChanged) ───────────────────────────────
    private readonly List<WeakReference<object>> _windowStack = [];

    // ── События ────────────────────────────────────────────────────────────────
    /// <inheritdoc/>
    public event Action<object?>? TopOwnerChanged;

    // ── Публичные свойства ─────────────────────────────────────────────────────
    /// <inheritdoc/>
    public int Current { get { lock (_lock) return _current; } }

    // ── Методы ─────────────────────────────────────────────────────────────────

    /// <inheritdoc/>
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

    /// <inheritdoc/>
    public int Allocate(int baseZIndex)
    {
        lock (_lock)
        {
            if (_current < baseZIndex)
                _current = baseZIndex - Step; // GetNext() добавит Step

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

    /// <inheritdoc/>
    public void Release(int zIndex)
    {
        if (zIndex <= BaseZIndex) return;
        lock (_lock)
        {
            _released.Add(zIndex);
            // Сжимаем хвост: если топ released == _current — уменьшаем
            while (_released.Count > 0 && _released.Max == _current)
            {
                _released.Remove(_current);
                _current -= Step;
            }
        }
    }

    /// <inheritdoc/>
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

    // ── Window stack (для DockWindow focus management) ──────────────────────────

    /// <inheritdoc/>
    public void BringToFront(object window)
    {
        ArgumentNullException.ThrowIfNull(window);
        object? newTop;
        lock (_lock)
        {
            _windowStack.RemoveAll(w =>
                !w.TryGetTarget(out var t) || ReferenceEquals(t, window));
            _windowStack.Add(new WeakReference<object>(window));
            newTop = window;
        }
        TopOwnerChanged?.Invoke(newTop);
    }

    /// <inheritdoc/>
    public void RemoveFromStack(object window)
    {
        ArgumentNullException.ThrowIfNull(window);
        object? newTop = null;
        lock (_lock)
        {
            _windowStack.RemoveAll(w =>
                !w.TryGetTarget(out var t) || ReferenceEquals(t, window));
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
