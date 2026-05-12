// SuperUI/Base/SgInteractiveBase.cs
// ИСПРАВЛЕНИЯ:
// 1. HandleClickAsync — try/catch для защиты от исключений в OnClick
// 2. BuildKeyString — добавлен MetaKey (Cmd на Mac)
// 3. _keyHandlers — ConcurrentDictionary для thread-safety на Server
// 4. KeyboardService — документирован как API для глобальных hotkeys
// 5. ThrottleAsync — возврат void async→Task для корректной обработки исключений

using System.Collections.Concurrent;
using System.Globalization;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;
using SuperUI.Base.Reactive;
using SuperUI.Base.Services;

namespace SuperUI.Base;

/// <summary>
/// Базовый класс для интерактивных компонентов SuperUI.
/// Уровень 3: ComponentBase → SgComponentBase → SgJsComponentBase → SgInteractiveBase
/// </summary>
public abstract class SgInteractiveBase : SgJsComponentBase
{
    /// <summary>
    /// Сервис для регистрации глобальных горячих клавиш (window-level).
    /// Для локальных клавиш компонента используйте OnKey().
    /// </summary>
    [Inject] protected IKeyboardService KeyboardService { get; set; } = null!;

    // ── Параметры ───────────────────────────────────────────────────────────────────
    [Parameter] public bool Disabled { get; set; }
    [Parameter] public bool Loading { get; set; }
    [Parameter] public bool ReadOnly { get; set; }

    // ── RTL / Culture ───────────────────────────────────────────────────────────────
    [CascadingParameter(Name = "RightToLeft")] public bool IsRtl { get; set; }
    [CascadingParameter(Name = "Culture")] public CultureInfo? CascadedCulture { get; set; }
    [Parameter] public CultureInfo? Culture { get; set; }

    protected CultureInfo EffectiveCulture => Culture ?? CascadedCulture ?? CultureInfo.CurrentUICulture;
    protected virtual bool IsEffectivelyDisabled => Disabled || Loading;

    // ── ARIA ────────────────────────────────────────────────────────────────────────
    protected override IReadOnlyDictionary<string, object> BuildAriaAttributes()
    {
        var base_ = base.BuildAriaAttributes();
        if (!Disabled && !Loading && !ReadOnly) return base_;

        var attrs = new Dictionary<string, object>(base_, StringComparer.Ordinal);
        if (Disabled) attrs["aria-disabled"] = "true";
        if (Loading) attrs["aria-busy"] = "true";
        if (ReadOnly) attrs["aria-readonly"] = "true";
        return attrs;
    }

    // ── Debounce ────────────────────────────────────────────────────────────────────
    private readonly object _debounceLock = new();
    private readonly Dictionary<string, CancellationTokenSource> _debouncers = new();

    protected Task DebounceAsync(string key, Func<Task> action, TimeSpan delay)
    {
        if (IsDisposed) return Task.CompletedTask;

        CancellationTokenSource newCts;
        lock (_debounceLock)
        {
            if (IsDisposed) return Task.CompletedTask;
            if (_debouncers.TryGetValue(key, out var old))
            {
                old.Cancel();
                old.Dispose();
            }
            newCts = CancellationTokenSource.CreateLinkedTokenSource(ComponentToken);
            _debouncers[key] = newCts;
        }

        _ = DelayThenInvokeAsync(key, action, delay, newCts.Token);
        return Task.CompletedTask;
    }

    protected Task DebounceAsync(Func<Task> action, TimeSpan? delay = null)
        => DebounceAsync("_default", action, delay ?? TimeSpan.FromMilliseconds(300));

    protected Task DebounceAsync(string key, Action action, TimeSpan delay)
        => DebounceAsync(key, () => { action(); return Task.CompletedTask; }, delay);

    private async Task DelayThenInvokeAsync(
        string key, Func<Task> action, TimeSpan delay, CancellationToken ct)
    {
        try
        {
            await Task.Delay(delay, ct);
            if (!ct.IsCancellationRequested && !IsDisposed)
                await InvokeAsync(action);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[{Id}] Debounce callback error", ComponentId);
        }
        finally
        {
            lock (_debounceLock)
            {
                if (_debouncers.TryGetValue(key, out var cts) && cts.Token == ct)
                {
                    _debouncers.Remove(key);
                    try { cts.Dispose(); } catch { }
                }
            }
        }
    }

    // ── Throttle ────────────────────────────────────────────────────────────────────
    private const int MaxThrottleEntries = 100;
    private readonly ConcurrentDictionary<string, ThrottleEntry> _throttlers = new();

    protected async Task ThrottleAsync(string key, Func<Task> action, TimeSpan interval)
    {
        if (IsDisposed) return;
        if (_throttlers.Count >= MaxThrottleEntries && !_throttlers.ContainsKey(key))
        {
            Logger.LogWarning("[{Id}] ThrottleAsync: too many keys (max {Max}), key={Key} skipped",
                ComponentId, MaxThrottleEntries, key);
            return;
        }

        var entry = _throttlers.GetOrAdd(key, _ => new ThrottleEntry());
        if (Interlocked.CompareExchange(ref entry.IsThrottled, 1, 0) == 1) return;

        try
        {
            await action();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[{Id}] Throttle action error key={Key}", ComponentId, key);
        }
        finally
        {
            _ = ResetThrottleAfterDelayAsync(entry, interval, ComponentToken);
        }
    }

    private static async Task ResetThrottleAfterDelayAsync(
        ThrottleEntry entry, TimeSpan interval, CancellationToken ct)
    {
        try { await Task.Delay(interval, ct); }
        catch (OperationCanceledException) { }
        finally { Interlocked.Exchange(ref entry.IsThrottled, 0); }
        // Не обращаемся к _throttlers — только к entry
    }

    // ── Timer ───────────────────────────────────────────────────────────────────────
    private Timer? _internalTimer;
    private PeriodicTimer? _periodicTimer;
    private Task? _periodicTimerTask;

    protected void StartTimer(Func<Task> callback, TimeSpan period, TimeSpan? dueTime = null)
    {
        StopAllTimers();
        _internalTimer = new Timer(async _ =>
        {
            try
            {
                if (IsDisposed || ComponentToken.IsCancellationRequested) return;
                await InvokeAsync(callback);
            }
            catch (OperationCanceledException) { }
            catch (ObjectDisposedException) { }
            catch (Exception ex)
            {
                try { Logger.LogError(ex, "[{Id}] Timer error", ComponentId); } catch { }
            }
        }, null, dueTime ?? period, period);
    }

    protected void StopTimer()
    {
        _internalTimer?.Dispose();
        _internalTimer = null;
    }

    protected void StartPeriodicTimer(Func<Task> callback, TimeSpan period)
    {
        StopAllTimers();
        _periodicTimer = new PeriodicTimer(period);
        _periodicTimerTask = RunPeriodicTimerAsync(callback);
    }

    private async Task RunPeriodicTimerAsync(Func<Task> callback)
    {
        try
        {
            while (await _periodicTimer!.WaitForNextTickAsync(ComponentToken))
            {
                if (IsDisposed) break;
                try { await InvokeAsync(callback); }
                catch (Exception ex) { Logger.LogError(ex, "[{Id}] PeriodicTimer error", ComponentId); }
            }
        }
        catch (OperationCanceledException) { }
    }

    protected void StopPeriodicTimer()
    {
        _periodicTimer?.Dispose();
        _periodicTimer = null;
    }

    private void StopAllTimers()
    {
        _internalTimer?.Dispose();
        _internalTimer = null;
        _periodicTimer?.Dispose();
        _periodicTimer = null;
    }

    // ── Subscriptions ───────────────────────────────────────────────────────────────
    private readonly List<IDisposable> _subscriptions = [];

    protected void RegisterSubscription(IDisposable subscription)
        => _subscriptions.Add(subscription);

    protected void Subscribe<T>(IObservable<T> source, Action<T> handler)
    {
        var observer = new SgObserver<T>(
            onNext: value => { if (!IsDisposed) _ = InvokeAsync(() => handler(value)); },
            onError: ex => Logger.LogWarning(ex, "[{Id}] Observable error", ComponentId),
            onCompleted: () => { });
        _subscriptions.Add(source.Subscribe(observer));
    }

    // ── Keyboard ────────────────────────────────────────────────────────────────────
    // ИСПРАВЛЕНО: ConcurrentDictionary для thread-safety на Server
    private readonly ConcurrentDictionary<string, Func<KeyboardEventArgs, Task>> _keyHandlers = new();

    protected void OnKey(string key, Func<Task> handler)
        => _keyHandlers[key] = _ => handler();

    protected void OnKey(string key, Action handler)
        => _keyHandlers[key] = _ => { handler(); return Task.CompletedTask; };

    protected void OnKey(string key, Func<KeyboardEventArgs, Task> handler)
        => _keyHandlers[key] = handler;

    protected async Task HandleKeyDownAsync(KeyboardEventArgs e)
    {
        if (IsEffectivelyDisabled) return;
        var key = BuildKeyString(e);
        if (_keyHandlers.TryGetValue(key, out var handler))
            await handler(e);
    }

    /// <summary>
    /// ИСПРАВЛЕНО: добавлен MetaKey (Cmd на Mac).
    /// Zero-allocation через string.Create.
    /// </summary>
    private static string BuildKeyString(KeyboardEventArgs e)
    {
        var len = (e.CtrlKey ? 5 : 0)   // "Ctrl+"
                + (e.AltKey ? 4 : 0)    // "Alt+"
                + (e.ShiftKey ? 6 : 0)  // "Shift+"
                + (e.MetaKey ? 5 : 0)   // "Meta+"
                + (e.Key?.Length ?? 0);

        if (len == 0) return string.Empty;

        return string.Create(len, e, static (span, ev) =>
        {
            var pos = 0;
            if (ev.CtrlKey)  { "Ctrl+".AsSpan().CopyTo(span[pos..]); pos += 5; }
            if (ev.AltKey)   { "Alt+".AsSpan().CopyTo(span[pos..]); pos += 4; }
            if (ev.ShiftKey) { "Shift+".AsSpan().CopyTo(span[pos..]); pos += 6; }
            if (ev.MetaKey)  { "Meta+".AsSpan().CopyTo(span[pos..]); pos += 5; }
            if (ev.Key is not null) ev.Key.AsSpan().CopyTo(span[pos..]);
        });
    }

    // ── Mouse handlers ──────────────────────────────────────────────────────────────
    [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }
    [Parameter] public EventCallback<MouseEventArgs> OnMouseEnter { get; set; }
    [Parameter] public EventCallback<MouseEventArgs> OnMouseLeave { get; set; }

    // ИСПРАВЛЕНО: try/catch — OnClick handler не должен ронять circuit
    protected async Task HandleClickAsync(MouseEventArgs e)
    {
        if (IsEffectivelyDisabled || IsDisposed) return;
        try
        {
            await OnClick.InvokeAsync(e);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Logger.LogError(ex, "[{Id}] OnClick handler error", ComponentId);
        }
    }

    protected async Task HandleMouseEnterAsync(MouseEventArgs e)
    {
        if (IsDisposed) return;
        await OnMouseEnter.InvokeAsync(e);
    }

    protected async Task HandleMouseLeaveAsync(MouseEventArgs e)
    {
        if (IsDisposed) return;
        await OnMouseLeave.InvokeAsync(e);
    }

    // ── Dispose ─────────────────────────────────────────────────────────────────────
    protected override async ValueTask DisposeComponentAsync()
    {
        StopAllTimers();

        if (_periodicTimerTask is not null)
        {
            try { await _periodicTimerTask; }
            catch (OperationCanceledException) { }
        }

        lock (_debounceLock)
        {
            foreach (var cts in _debouncers.Values)
            {
                try { cts.Cancel(); cts.Dispose(); } catch { }
            }
            _debouncers.Clear();
        }

        // Отменяем все pending throttle через ComponentToken (уже реализовано через Cancel)
        _throttlers.Clear();

        foreach (var sub in _subscriptions) sub.Dispose();
        _subscriptions.Clear();

        await base.DisposeComponentAsync();
    }

    // ── Вспомогательные типы ────────────────────────────────────────────────────────
    private sealed class ThrottleEntry
    {
        internal int IsThrottled; // 0 = free, 1 = throttled
    }
}
