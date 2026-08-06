// SuperUI/Base/Utilities/SgFocusManager.cs
// Централизованное управление фокусом: focus trap, focus first/last, focus restore.
// Решает паттерн "каждый оверлей ловит Tab сам, не делясь логикой".

using System.Collections.Generic;
using System.Threading;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace SuperUI.Base.Utilities;

/// <summary>
/// Централизованный focus manager: focus trap, focus first/last, focus restore
/// при открытии/закрытии оверлея.
/// </summary>
/// <remarks>
/// <para>Scoped lifetime: на circuit/сессию. Хранит стек "предыдущих фокусов"
/// чтобы <see cref="RestoreAsync"/> мог вернуть фокус туда, откуда он был
/// перехвачен.</para>
/// <para>Использование в SgModal/SgDrawer/SgDialog (через SgOverlayComponentBase).</para>
/// </remarks>
public sealed class SgFocusManager : IAsyncDisposable
{
    private readonly IJSRuntime _js;
    private readonly Stack<FocusSnapshot> _stack = new();
    private readonly object _stackLock = new();
    private int _disposed;

    /// <summary>Creates a new focus manager.</summary>
    public SgFocusManager(IJSRuntime js)
    {
        _js = js;
    }

    /// <summary>
    /// Captures the currently focused element (id + selector) for later restore.
    /// Call before opening an overlay.
    /// </summary>
    public async ValueTask CaptureAsync()
    {
        if (Volatile.Read(ref _disposed) == 1) return;
        try
        {
            var snapshot = await _js.InvokeAsync<FocusSnapshot?>(
                "eval",
                "(() => { const el = document.activeElement; if (!el || el === document.body) return null; return { id: el.id || null, selector: el.tagName.toLowerCase() + (el.id?'#'+el.id:'') }; })()");
            if (snapshot is not null)
            {
                lock (_stackLock) _stack.Push(snapshot);
            }
        }
        catch (JSDisconnectedException) { }
        catch (TaskCanceledException)   { }
        catch (JSException)             { }
        catch (InvalidOperationException) { }
    }

    /// <summary>
    /// Restores focus to the most recently captured element. Idempotent.
    /// </summary>
    public async ValueTask RestoreAsync()
    {
        if (Volatile.Read(ref _disposed) == 1) return;
        FocusSnapshot? snap;
        lock (_stackLock)
        {
            if (_stack.Count == 0) return;
            snap = _stack.Pop();
        }
        try
        {
            if (!string.IsNullOrEmpty(snap.Id))
            {
                await _js.InvokeVoidAsync("eval", $"document.getElementById({ToJsString(snap.Id)})?.focus()");
            }
        }
        catch (JSDisconnectedException) { }
        catch (TaskCanceledException)   { }
        catch (JSException)             { }
        catch (InvalidOperationException) { }
    }

    /// <summary>
    /// Focuses the first focusable element inside <paramref name="root"/>.
    /// </summary>
    public async ValueTask FocusFirstAsync(ElementReference root)
    {
        if (Volatile.Read(ref _disposed) == 1) return;
        try
        {
            await _js.InvokeVoidAsync("SuperUI.focusFirst", root);
        }
        catch (JSDisconnectedException) { }
        catch (TaskCanceledException)   { }
        catch (JSException)             { }
        catch (InvalidOperationException) { }
    }

    /// <summary>
    /// Traps Tab/Shift+Tab inside <paramref name="root"/>. Returns an IDisposable
    /// that removes the trap.
    /// </summary>
    public async ValueTask<IAsyncDisposable> TrapAsync(ElementReference root)
    {
        if (Volatile.Read(ref _disposed) == 1) return new NoopDisposable();
        try
        {
            var handle = await _js.InvokeAsync<IJSObjectReference?>(
                "SuperUI.trapFocus", root);
            return new TrapHandle(handle, _js);
        }
        catch (JSDisconnectedException) { return new NoopDisposable(); }
        catch (TaskCanceledException)   { return new NoopDisposable(); }
        catch (JSException)             { return new NoopDisposable(); }
        catch (InvalidOperationException) { return new NoopDisposable(); }
    }

    private static string ToJsString(string s) =>
        "'" + s.Replace("\\", "\\\\").Replace("'", "\\'") + "'";

    /// <inheritdoc/>
    public ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1) return ValueTask.CompletedTask;
        lock (_stackLock) _stack.Clear();
        return ValueTask.CompletedTask;
    }

    private sealed class FocusSnapshot
    {
        public string? Id { get; set; }
        public string? Selector { get; set; }
    }

    private sealed class NoopDisposable : IAsyncDisposable
    {
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class TrapHandle : IAsyncDisposable
    {
        private IJSObjectReference? _handle;
        private readonly IJSRuntime _js;
        private int _disposed;

        public TrapHandle(IJSObjectReference? handle, IJSRuntime js)
        {
            _handle = handle;
            _js = js;
        }

        public async ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 1) return;
            var h = Interlocked.Exchange(ref _handle, null);
            if (h is null) return;
            try
            {
                await h.InvokeVoidAsync("release");
            }
            catch (JSDisconnectedException) { }
            catch (TaskCanceledException)   { }
            catch (JSException)             { }
            catch (ObjectDisposedException) { }
            finally
            {
                try { await h.DisposeAsync(); }
                catch (JSDisconnectedException) { }
                catch (TaskCanceledException)   { }
                catch (ObjectDisposedException) { }
            }
        }
    }
}
