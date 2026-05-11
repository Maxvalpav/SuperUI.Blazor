// SuperUI/Base/SgInteractiveBase.cs
// ИСПРАВЛЕНО:
// 1. _debounceLock: используем object вместо Lock (совместимость с .NET 8)
// 2. _throttlers: ограничение размера + очистка при Dispose
// 3. Subscribe<T>: обработка OnError через logger
// 4. HandleClickAsync: проверка IsDisposed
// 5. HandleMouseEnterAsync / HandleMouseLeaveAsync — реализованы
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
/// </summary>
/// <remarks>
/// Уровень 3: ComponentBase → SgComponentBase → SgJsComponentBase → SgInteractiveBase
///
/// Предоставляет: Debounce, Throttle, Timer, Keyboard handlers, Subscriptions, Mouse events.
/// </remarks>
public abstract class SgInteractiveBase : SgJsComponentBase
{
    [Inject] protected IKeyboardService KeyboardService { get; set; } = null!;

    // ── Параметры ─────────────────────────────────────────────────────────────────
    [Parameter] public bool Disabled { get; set; }
    [Parameter] public bool Loading { get; set; }
    [Parameter] public bool ReadOnly { get; set; }

    // ── RTL / Culture ─────────────────────────────────────────────────────────────
    [CascadingParameter(Name = "RightToLeft")] public bool IsRtl { get; set; }
    [CascadingParameter(Name = "Culture")] public CultureInfo? CascadedCulture { get; set; }
    [Parameter] public CultureInfo? Culture { get; set; }

    protected CultureInfo EffectiveCulture
        => Culture ?? CascadedCulture ?? CultureInfo.CurrentUICulture;

    protected virtual bool IsEffectivelyDisabled => Disabled || Loading;

    // ── ARIA ──────────────────────────────────────────────────────────────────────
    protected override IReadOnlyDictionary<string, object> BuildAriaAttributes()
    {
        var base_ = base.BuildAriaAttributes();
        if (!Disabled && !Loading && !ReadOnly) return base_;

        var attrs = new Dictionary<string, object>(base_, StringComparer.Ordinal);
        if (Disabled) attrs["aria-disabled"] = "true";
        if (Loading)  attrs["aria-busy"]     = "true";
        if (ReadOnly) attrs["aria-readonly"] = "true";
        return attrs;
    }

    // ── Debounce ──────────────────────────────────────────────────────────────────
    // ИСПРАВЛЕНО: object вместо Lock (совместимость с .NET 8/9/10)
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

    // ── Throttle ──────────────────────────────────────────────────────────────────
    // ИСПРАВЛЕНО: ограничение размера словаря (max 100 ключей)
    private const int MaxThrottleEntries = 100;
    private readonly ConcurrentDictionary<string, ThrottleEntry> _throttlers = new();

    protected async Task ThrottleAsync(string key, Func<Task> action, TimeSpan interval)
    {
        if (IsDisposed) return;

        // ИСПРАВЛЕНО: защита от бесконечного роста словаря
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
            _ = ResetThrottleAfterDelayAsync(entry, interval);
        }
    }

    private static async Task ResetThrottleAfterDelayAsync(ThrottleEntry entry, TimeSpan interval)
    {
        try { await Task.Delay(interval); }
        finally { Interlocked.Exchange(ref entry.IsThrottled, 0); }
    }

    // ── Timer ─────────────────────────────────────────────────────────────────────
    private enum TimerMode { None, Legacy, Periodic }
    private TimerMode _timerMode = TimerMode.None;
    private Timer? _internalTimer;
    private PeriodicTimer? _periodicTimer;
    private Task? _periodicTimerTask;

    protected void StartTimer(Func<Task> callback, TimeSpan period, TimeSpan? dueTime = null)
    {
        StopAllTimers();
        _timerMode = TimerMode.Legacy;
        _internalTimer = new Timer(async _ =>
        {
            if (IsDisposed || ComponentToken.IsCancellationRequested) return;
            try { await InvokeAsync(callback); }
            catch (Exception ex)
            {
                Logger.LogError(ex, "[{Id}] Timer error", ComponentId);
            }
        }, null, dueTime ?? period, period);
    }

    protected void StopTimer()
    {
        _internalTimer?.Dispose();
        _internalTimer = null;
        if (_timerMode == TimerMode.Legacy)
            _timerMode = TimerMode.None;
    }

    protected void StartPeriodicTimer(Func<Task> callback, TimeSpan period)
    {
        StopAllTimers();
        _timerMode = TimerMode.Periodic;
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
                catch (Exception ex)
                {
                    Logger.LogError(ex, "[{Id}] PeriodicTimer error", ComponentId);
                }
            }
        }
        catch (OperationCanceledException) { }
    }

    protected void StopPeriodicTimer()
    {
        _periodicTimer?.Dispose();
        _periodicTimer = null;
        if (_timerMode == TimerMode.Periodic)
            _timerMode = TimerMode.None;
    }

    private void StopAllTimers()
    {
        _internalTimer?.Dispose(); _internalTimer = null;
        _periodicTimer?.Dispose(); _periodicTimer = null;
        _timerMode = TimerMode.None;
    }

    // ── Subscriptions ─────────────────────────────────────────────────────────────
    private readonly List<IDisposable> _subscriptions = [];

    protected void RegisterSubscription(IDisposable subscription) =>
        _subscriptions.Add(subscription);

    /// <summary>
    /// Подписаться на Observable с автоматической отпиской при Dispose.
    /// Обработка ошибок — через логгер, OnCompleted игнорируется.
    /// </summary>
    protected void Subscribe<T>(IObservable<T> source, Action<T> handler)
    {
        // ИСПРАВЛЕНО: OnError обрабатывается через логгер (не игнорируется)
        var observer = new SgObserver<T>(
            onNext:      value => { if (!IsDisposed) _ = InvokeAsync(() => handler(value)); },
            onError:     ex    => Logger.LogWarning(ex, "[{Id}] Observable error", ComponentId),
            onCompleted: ()    => { }
        );
        _subscriptions.Add(source.Subscribe(observer));
    }

    // ── Keyboard ──────────────────────────────────────────────────────────────────
    private readonly Dictionary<string, Func<KeyboardEventArgs, Task>> _keyHandlers = new();

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

    /// <summary>Zero-allocation построение строки-ключа.</summary>
    private static string BuildKeyString(KeyboardEventArgs e)
    {
        var len = (e.CtrlKey  ? 5 : 0)
                + (e.AltKey   ? 4 : 0)
                + (e.ShiftKey ? 6 : 0)
                + (e.Key?.Length ?? 0);

        if (len == 0) return string.Empty;

        return string.Create(len, e, static (span, ev) =>
        {
            var pos = 0;
            if (ev.CtrlKey)  { "Ctrl+" .AsSpan().CopyTo(span[pos..]); pos += 5; }
            if (ev.AltKey)   { "Alt+"  .AsSpan().CopyTo(span[pos..]); pos += 4; }
            if (ev.ShiftKey) { "Shift+".AsSpan().CopyTo(span[pos..]); pos += 6; }
            if (ev.Key is not null) ev.Key.AsSpan().CopyTo(span[pos..]);
        });
    }

    // ── Mouse handlers ────────────────────────────────────────────────────────────
    [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }
    [Parameter] public EventCallback<MouseEventArgs> OnMouseEnter { get; set; }
    [Parameter] public EventCallback<MouseEventArgs> OnMouseLeave { get; set; }

    // ИСПРАВЛЕНО: проверка IsDisposed
    protected async Task HandleClickAsync(MouseEventArgs e)
    {
        if (IsEffectivelyDisabled || IsDisposed) return;
        await OnClick.InvokeAsync(e);
    }

    // ИСПРАВЛЕНО: реализованы обработчики для OnMouseEnter / OnMouseLeave
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

    // ── Dispose ───────────────────────────────────────────────────────────────────
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

        // ИСПРАВЛЕНО: очищаем throttlers при Dispose
        _throttlers.Clear();

        foreach (var sub in _subscriptions) sub.Dispose();
        _subscriptions.Clear();

        await base.DisposeComponentAsync();
    }

    // ── Вспомогательные типы ──────────────────────────────────────────────────────
    private sealed class ThrottleEntry
    {
        // 0 = free, 1 = throttled (Interlocked)
        internal int IsThrottled;
    }
}