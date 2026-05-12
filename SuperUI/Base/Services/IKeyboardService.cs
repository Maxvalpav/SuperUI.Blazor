// SuperUI/Base/Services/IKeyboardService.cs
//
// Сервис регистрации глобальных горячих клавиш (window-level keyboard shortcuts).
// Используется в SgInteractiveBase.KeyboardService.
//
// Отличие от OnKey() в SgInteractiveBase:
// - OnKey() — обрабатывает клавиши внутри конкретного элемента компонента.
// - IKeyboardService — регистрирует обработчики на уровне window (глобальные).
//
// Thread safety:
// - Scoped DI → per-circuit на Server → нет конкуренции.
// - Если Singleton — использовать ConcurrentDictionary.

using Microsoft.AspNetCore.Components.Web;

namespace SuperUI.Base.Services;

/// <summary>
/// Сервис регистрации глобальных горячих клавиш (window-level).
/// </summary>
public interface IKeyboardService
{
    /// <summary>
    /// Зарегистрировать глобальный обработчик клавиши.
    /// </summary>
    /// <param name="key">Строка клавиши в формате "Ctrl+S", "Alt+F4", "Escape" и т.д.</param>
    /// <param name="handler">Обработчик. Возвращает true если событие обработано (preventDefault).</param>
    /// <returns>Disposable для отмены регистрации.</returns>
    IDisposable Register(string key, Func<KeyboardEventArgs, Task<bool>> handler);

    /// <summary>Зарегистрировать обработчик без возврата результата.</summary>
    IDisposable Register(string key, Func<Task> handler);

    /// <summary>Зарегистрировать синхронный обработчик.</summary>
    IDisposable Register(string key, Action handler);

    /// <summary>Вызвать обработчики для события (вызывается из JS via [JSInvokable]).</summary>
    Task<bool> HandleKeyAsync(KeyboardEventArgs e);
}

/// <summary>
/// Реализация <see cref="IKeyboardService"/>.
/// </summary>
public sealed class KeyboardService : IKeyboardService
{
    // key → ordered list of handlers (последний зарегистрированный — первый вызываемый)
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
        List<Func<KeyboardEventArgs, Task<bool>>>? handlers;

        lock (_lock)
        {
            if (!_handlers.TryGetValue(keyString, out handlers) || handlers.Count == 0)
                return false;
            // Копируем для вызова вне lock
            handlers = new List<Func<KeyboardEventArgs, Task<bool>>>(handlers);
        }

        // Вызываем в обратном порядке (последний зарегистрированный — приоритетный)
        for (int i = handlers.Count - 1; i >= 0; i--)
        {
            try
            {
                if (await handlers[i](e))
                    return true; // Обработан — останавливаем цепочку
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[KeyboardService] Handler error: {ex}");
            }
        }

        return false;
    }

    private static string BuildKeyString(KeyboardEventArgs e)
    {
        if (string.IsNullOrEmpty(e.Key)) return string.Empty;

        var parts = new List<string>(5);
        if (e.CtrlKey)  parts.Add("Ctrl");
        if (e.AltKey)   parts.Add("Alt");
        if (e.ShiftKey) parts.Add("Shift");
        if (e.MetaKey)  parts.Add("Meta");
        parts.Add(e.Key);

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
    public IDisposable Register(string key, Func<Task> handler) => _noop;
    public IDisposable Register(string key, Action handler) => _noop;
    public Task<bool> HandleKeyAsync(KeyboardEventArgs e) => Task.FromResult(false);

    private sealed class NoopDisposable : IDisposable { public void Dispose() { } }
}
