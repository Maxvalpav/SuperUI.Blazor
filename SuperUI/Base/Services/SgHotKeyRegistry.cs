// SuperUI/Base/Services/SgHotKeyRegistry.cs
// НОВЫЙ: типобезопасный реестр горячих клавиш
// Аналог FluentUI KeyCode, но с Blazor-интеграцией и автоотпиской

using System.Threading;

namespace SuperUI.Base.Services;

/// <summary>
/// Стандартные клавиши (типобезопасная альтернатива строкам).
/// </summary>
public static class SgKeys
{
    public const string Enter = "Enter";
    public const string Escape = "Escape";
    public const string Space = " ";
    public const string Tab = "Tab";
    public const string ArrowUp = "ArrowUp";
    public const string ArrowDown = "ArrowDown";
    public const string ArrowLeft = "ArrowLeft";
    public const string ArrowRight = "ArrowRight";
    public const string Home = "Home";
    public const string End = "End";
    public const string PageUp = "PageUp";
    public const string PageDown = "PageDown";
    public const string Delete = "Delete";
    public const string Backspace = "Backspace";
    public const string F1 = "F1";
    public const string F2 = "F2";
    public const string F3 = "F3";
    public const string F4 = "F4";
    public const string F5 = "F5";
    public const string F6 = "F6";
    public const string F7 = "F7";
    public const string F8 = "F8";
    public const string F9 = "F9";
    public const string F10 = "F10";
    public const string F11 = "F11";
    public const string F12 = "F12";

    // Composer
    public static string Ctrl(string key) => $"Ctrl+{key}";
    public static string Alt(string key) => $"Alt+{key}";
    public static string Shift(string key) => $"Shift+{key}";
    public static string CtrlShift(string key) => $"Ctrl+Shift+{key}";
    public static string CtrlAlt(string key) => $"Ctrl+Alt+{key}";
}

/// <summary>
/// Глобальный реестр горячих клавиш.
/// Позволяет регистрировать/отменять регистрацию хоткеев с scope и приоритетом.
/// </summary>
public sealed class SgHotKeyRegistry : IDisposable
{
    private sealed record HotkeyEntry(
        string Key,
        Func<Task> Handler,
        string Scope,
        int Priority,
        bool PreventDefault);

    private readonly List<(HotkeyEntry Entry, WeakReference<SgComponentBase>? Owner)> _entries = [];
    private readonly object _lock = new();
    private int _disposed;

    /// <summary>
    /// Зарегистрировать горячую клавишу.
    /// </summary>
    /// <param name="key">Строка клавиши (SgKeys.Ctrl("s"), "Escape", etc.)</param>
    /// <param name="handler">Async обработчик.</param>
    /// <param name="owner">Компонент-владелец (для автоотписки при Dispose).</param>
    /// <param name="scope">Scope для группировки (null = глобальный).</param>
    /// <param name="priority">Приоритет (выше = обрабатывается первым).</param>
    /// <param name="preventDefault">Предотвратить дефолтное поведение браузера.</param>
    public IDisposable Register(
        string key,
        Func<Task> handler,
        SgComponentBase? owner = null,
        string scope = "global",
        int priority = 0,
        bool preventDefault = true)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(handler);
        if (Volatile.Read(ref _disposed) == 1) return NullDisposable.Instance;

        var entry = new HotkeyEntry(key, handler, scope, priority, preventDefault);
        var weakOwner = owner is not null ? new WeakReference<SgComponentBase>(owner) : null;

        lock (_lock)
            _entries.Add((entry, weakOwner));

        return new HotkeyRegistration(this, entry);
    }

    /// <summary>Зарегистрировать synchronous обработчик.</summary>
    public IDisposable Register(
        string key,
        Action handler,
        SgComponentBase? owner = null,
        string scope = "global",
        int priority = 0,
        bool preventDefault = true)
    {
        return Register(
            key,
            () => { handler(); return Task.CompletedTask; },
            owner, scope, priority, preventDefault);
    }

    /// <summary>Выполнить обработчики для данной клавиши.</summary>
    public async Task<bool> InvokeAsync(string key, string? activeScope = null)
    {
        if (Volatile.Read(ref _disposed) == 1) return false;

        List<HotkeyEntry> handlers;
        List<(HotkeyEntry, WeakReference<SgComponentBase>?)>? dead = null;

        lock (_lock)
        {
            handlers = _entries
                .Where(e =>
                {
                    // Очистить мёртвые владельцы
                    if (e.Owner is not null && !e.Owner.TryGetTarget(out _))
                    {
                        (dead ??= []).Add(e);
                        return false;
                    }
                    // Проверить владельца — не Disposed
                    if (e.Owner is not null && e.Owner.TryGetTarget(out var comp) && comp.IsDisposed)
                    {
                        (dead ??= []).Add(e);
                        return false;
                    }
                    return e.Entry.Key.Equals(key, StringComparison.OrdinalIgnoreCase) &&
                           (activeScope is null ||
                            e.Entry.Scope == "global" ||
                            e.Entry.Scope.Equals(activeScope, StringComparison.OrdinalIgnoreCase));
                })
                .OrderByDescending(e => e.Entry.Priority)
                .Select(e => e.Entry)
                .ToList();

            if (dead is not null)
                foreach (var d in dead) _entries.Remove(d);
        }

        if (handlers.Count == 0) return false;

        // Вызываем в порядке приоритета, останавливаемся если handled
        foreach (var entry in handlers)
        {
            try { await entry.Handler(); }
            catch (Exception ex)
            { System.Diagnostics.Debug.WriteLine($"[SgHotKeyRegistry] Handler error for '{key}': {ex.Message}"); }

            if (entry.PreventDefault) return true;
        }
        return handlers.Count > 0;
    }

    /// <summary>Отменить регистрацию всех хоткеев для компонента.</summary>
    public void UnregisterOwner(SgComponentBase owner)
    {
        lock (_lock)
            _entries.RemoveAll(e => e.Owner is not null &&
                e.Owner.TryGetTarget(out var comp) && ReferenceEquals(comp, owner));
    }

    /// <summary>Отменить регистрацию всех хоткеев в scope.</summary>
    public void UnregisterScope(string scope)
    {
        lock (_lock)
            _entries.RemoveAll(e => e.Entry.Scope.Equals(scope, StringComparison.OrdinalIgnoreCase));
    }

    private void Unregister(HotkeyEntry entry)
    {
        lock (_lock)
            _entries.RemoveAll(e => ReferenceEquals(e.Entry, entry));
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1) return;
        lock (_lock) _entries.Clear();
    }

    private sealed class HotkeyRegistration : IDisposable
    {
        private readonly SgHotKeyRegistry _registry;
        private readonly HotkeyEntry _entry;
        private int _disposed;

        public HotkeyRegistration(SgHotKeyRegistry registry, HotkeyEntry entry)
        { _registry = registry; _entry = entry; }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 1) return;
            _registry.Unregister(_entry);
        }
    }

    private sealed class NullDisposable : IDisposable
    {
        public static readonly NullDisposable Instance = new();
        public void Dispose() { }
    }
}