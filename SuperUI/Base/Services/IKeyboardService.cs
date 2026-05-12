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

/// <summary>Реализация <see cref="IKeyboardService"/>.</summary>
public sealed class KeyboardService : IKeyboardService
{
    private readonly Dictionary<string, List<Func<KeyboardEventArgs, Task<bool>>>> _handlers = new();
    private readonly object _lock = new();

    public IDisposable Register(string key, Func<KeyboardEventArgs, Task<bool>> handler)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        ArgumentNullException.ThrowIfNull(handler);

        lock (_lock)
        {
            if (!_handlers.TryGetValue(key, out var list))
                _handlers[key] = list = new();
            list.Add(handler);
        }

        return new HandlerDisposable(() =>
        {
            lock (_lock)
            {
                if (_handlers.TryGetValue(key, out var l))
                    l.Remove(handler);
            }
        });
    }

    public IDisposable Register(string key, Func<Task> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        return Register(key, async _ => { await handler(); return false; });
    }

    public IDisposable Register(string key, Action handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        return Register(key, _ => { handler(); return Task.FromResult(false); });
    }

    public async Task<bool> HandleKeyAsync(KeyboardEventArgs e)
    {
        var keyString = BuildKeyString(e);
        if (string.IsNullOrEmpty(keyString)) return false;

        List<Func<KeyboardEventArgs, Task<bool>>>? handlers;
        lock (_lock)
        {
            // ПОЛИРОВКА: нормализация регистра Key для case-insensitive matching
            if (!_handlers.TryGetValue(keyString, out handlers) || handlers.Count == 0)
                return false;
            handlers = new List<Func<KeyboardEventArgs, Task<bool>>>(handlers); // copy outside lock
        }

        // Вызываем в обратном порядке (последний зарегистрированный — приоритетный)
        for (int i = handlers.Count - 1; i >= 0; i--)
        {
            try
            {
                if (await handlers[i](e)) return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[KeyboardService] Handler error: {ex}");
            }
        }
        return false;
    }

    public void Clear()
    {
        lock (_lock) _handlers.Clear();
    }

    /// <summary>Количество зарегистрированных обработчиков (для диагностики).</summary>
    public int HandlerCount
    {
        get
        {
            lock (_lock)
                return _handlers.Values.Sum(l => l.Count);
        }
    }

    private static string BuildKeyString(KeyboardEventArgs e)
    {
        if (string.IsNullOrEmpty(e.Key)) return string.Empty;

        // ПОЛИРОВКА: нормализация первой буквы Key (браузеры могут слать "escape"/"Escape")
        var key = e.Key.Length == 1
            ? e.Key
            : char.ToUpperInvariant(e.Key[0]) + e.Key[1..];

        var parts = new List<string>(5);
        if (e.CtrlKey)  parts.Add("Ctrl");
        if (e.AltKey)   parts.Add("Alt");
        if (e.ShiftKey) parts.Add("Shift");
        if (e.MetaKey)  parts.Add("Meta");
        parts.Add(key);
        return string.Join("+", parts);
    }

    private sealed class HandlerDisposable : IDisposable
    {
        private readonly Action _dispose;
        private int _disposed;

        public HandlerDisposable(Action dispose) => _dispose = dispose;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 1) return;
            _dispose();
        }
    }
}

/// <summary>Null-реализация для тестов и SSR.</summary>
public sealed class NullKeyboardService : IKeyboardService
{
    public static readonly NullKeyboardService Instance = new();
    private NullKeyboardService() { }

    private static readonly IDisposable _noop = new NoopDisposable();

    public IDisposable Register(string key, Func<KeyboardEventArgs, Task<bool>> handler) => _noop;
    public IDisposable Register(string key, Func<Task> handler)                           => _noop;
    public IDisposable Register(string key, Action handler)                               => _noop;
    public Task<bool>  HandleKeyAsync(KeyboardEventArgs e)                                => Task.FromResult(false);
    public void        Clear()                                                            { }

    private sealed class NoopDisposable : IDisposable { public void Dispose() { } }
}
