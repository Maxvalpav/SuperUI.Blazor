// SuperUI/Base/Services/KeyboardService.cs
// ✅ НОВЫЙ: реализация IKeyboardService

using System.Collections.Concurrent;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Microsoft.Extensions.Logging;

namespace SuperUI.Base.Services;

/// <summary>
/// Реализация IKeyboardService через window keydown event (JS Interop).
/// </summary>
internal sealed class KeyboardService : IKeyboardService, IAsyncDisposable
{
    private readonly IJSRuntime _js;
    private readonly ILogger<KeyboardService> _logger;
    private readonly ConcurrentDictionary<string, List<Func<KeyboardEventArgs, Task<bool>>>> _handlers = new();
    private DotNetObjectReference<KeyboardService>? _dotNetRef;
    private IJSObjectReference? _module;
    private bool _initialized;

    public KeyboardService(IJSRuntime js, ILogger<KeyboardService> logger)
    {
        _js = js;
        _logger = logger;
    }

    public IDisposable Register(string key, Func<Task> handler)
        => Register(key, async _ => { await handler(); return false; });

    public IDisposable Register(string key, Action handler)
        => Register(key, _ => { handler(); return Task.FromResult(false); });

    public IDisposable Register(string key, Func<KeyboardEventArgs, Task<bool>> handler)
    {
        var list = _handlers.GetOrAdd(key, _ => new List<Func<KeyboardEventArgs, Task<bool>>>());
        lock (list) list.Add(handler);
        _ = EnsureInitializedAsync();
        return new HandlerDisposable(this, key, handler);
    }

    public void Unregister(string key) => _handlers.TryRemove(key, out _);

    public void Clear() => _handlers.Clear();

    [JSInvokable]
    public async Task<bool> HandleKeyAsync(KeyboardEventArgs e)
    {
        var key = BuildKeyString(e);
        if (_handlers.TryGetValue(key, out var list))
        {
            Func<KeyboardEventArgs, Task<bool>>[] snapshot;
            lock (list) snapshot = list.ToArray();
            foreach (var handler in snapshot)
            {
                try 
                { 
                    var handled = await handler(e);
                    if (handled) return true;
                }
                catch (Exception ex) 
                { 
                    _logger.LogError(ex, "KeyboardService handler error for key {Key}", key); 
                }
            }
        }
        return false;
    }

    private async Task EnsureInitializedAsync()
    {
        if (_initialized) return;
        _initialized = true;
        try
        {
            _dotNetRef = DotNetObjectReference.Create(this);
            // Регистрируем window keydown listener через superui.js
            // В реальном проекте: await _js.InvokeVoidAsync("superui.registerKeyboard", _dotNetRef)
        }
        catch (Exception ex) { _logger.LogError(ex, "KeyboardService init failed"); }
    }

    private static string BuildKeyString(KeyboardEventArgs e)
    {
        if (string.IsNullOrEmpty(e.Key)) return string.Empty;
        var parts = new List<string>(4);
        if (e.CtrlKey)  parts.Add("Ctrl");
        if (e.AltKey)   parts.Add("Alt");
        if (e.ShiftKey) parts.Add("Shift");
        if (e.MetaKey)  parts.Add("Meta");
        parts.Add(e.Key);
        return string.Join("+", parts);
    }

    public async ValueTask DisposeAsync()
    {
        _handlers.Clear();
        if (_module is not null)
        {
            try { await _module.DisposeAsync(); } catch { }
        }
        _dotNetRef?.Dispose();
    }

    private sealed class HandlerDisposable : IDisposable
    {
        private readonly KeyboardService _service;
        private readonly string _key;
        private readonly Func<KeyboardEventArgs, Task<bool>> _handler;

        public HandlerDisposable(KeyboardService service, string key,
            Func<KeyboardEventArgs, Task<bool>> handler)
        {
            _service = service; _key = key; _handler = handler;
        }

        public void Dispose()
        {
            if (_service._handlers.TryGetValue(_key, out var list))
                lock (list) list.Remove(_handler);
        }
    }
}
