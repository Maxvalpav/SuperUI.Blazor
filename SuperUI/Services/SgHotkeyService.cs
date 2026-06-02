// SuperUI/Services/SgHotkeyService.cs
// Сервис глобальных горячих клавиш с scope и ignore-условиями.

using Microsoft.JSInterop;

namespace SuperUI.Services;

/// <summary>
/// Сервис глобальных горячих клавиш.
/// </summary>
/// <remarks>
/// <para>Регистрация комбинации: <see cref="RegisterAsync(string, Func{ValueTask}, SgHotkeyOptions?)"/>.</para>
/// <para>Сервис хранит комбинации в JS, поддерживает <c>Ctrl/Cmd+Shift+S</c>-стиль нотации,
/// <see cref="SgHotkeyOptions"/> задаёт scope (active element), preventDefault, stopPropagation.</para>
/// </remarks>
public sealed class SgHotkeyService : IAsyncDisposable
{
    private readonly IJSRuntime _js;
    private int _disposed;
    private DotNetObjectReference<SgHotkeyService>? _selfRef;
    private readonly Dictionary<string, Func<ValueTask>> _handlers = new();

    public SgHotkeyService(IJSRuntime js)
    {
        _js = js;
    }

    /// <summary>Регистрирует горячую клавишу. <paramref name="combination"/> — например, "Ctrl+Shift+S" или "Cmd+K".</summary>
    public async ValueTask RegisterAsync(string combination, Func<ValueTask> handler, SgHotkeyOptions? options = null)
    {
        if (Volatile.Read(ref _disposed) == 1) return;
        ArgumentException.ThrowIfNullOrWhiteSpace(combination);
        ArgumentNullException.ThrowIfNull(handler);
        _selfRef ??= DotNetObjectReference.Create(this);
        _handlers[combination] = handler;
        try
        {
            await _js.InvokeVoidAsync("SuperUI.registerHotkey",
                combination, options?.ScopeSelector, options?.PreventDefault ?? true,
                options?.StopPropagation ?? true, _selfRef).ConfigureAwait(false);
        }
        catch (JSDisconnectedException) { }
        catch (TaskCanceledException)   { }
        catch (JSException)             { }
    }

    /// <summary>Снимает регистрацию горячей клавиши.</summary>
    public async ValueTask UnregisterAsync(string combination)
    {
        if (Volatile.Read(ref _disposed) == 1) return;
        if (string.IsNullOrWhiteSpace(combination)) return;
        _handlers.Remove(combination);
        try
        {
            await _js.InvokeVoidAsync("SuperUI.unregisterHotkey", combination).ConfigureAwait(false);
        }
        catch (JSDisconnectedException) { }
        catch (TaskCanceledException)   { }
        catch (JSException)             { }
    }

    [Microsoft.JSInterop.JSInvokable]
    public async ValueTask OnHotkeyAsync(string combination)
    {
        if (_handlers.TryGetValue(combination, out var handler))
        {
            try { await handler().ConfigureAwait(false); }
            catch { /* swallow — обработчик сам логирует */ }
        }
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1) return;
        _handlers.Clear();
        try
        {
            await _js.InvokeVoidAsync("SuperUI.clearHotkeys").ConfigureAwait(false);
        }
        catch (JSDisconnectedException) { }
        catch (TaskCanceledException)   { }
        catch (JSException)             { }
        catch (ObjectDisposedException) { }

        var self = _selfRef;
        _selfRef = null;
        self?.Dispose();
    }
}

/// <summary>Опции регистрации горячей клавиши.</summary>
public sealed class SgHotkeyOptions
{
    /// <summary>CSS-селектор области видимости (если null — глобально).</summary>
    public string? ScopeSelector { get; init; }
    /// <summary>Вызвать preventDefault (default true).</summary>
    public bool PreventDefault { get; init; } = true;
    /// <summary>Вызвать stopPropagation (default true).</summary>
    public bool StopPropagation { get; init; } = true;
}
