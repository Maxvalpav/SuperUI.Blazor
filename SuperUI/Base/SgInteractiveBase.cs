// SuperUI/Base/SgInteractiveBase.cs
using System.Globalization;
using System.Threading;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using SuperUI.Services;

namespace SuperUI.Base;

/// <summary>
/// Базовый класс для интерактивных компонентов.
///
/// ИСПРАВЛЕНИЯ:
/// 1. DebounceAsync: Task.Run → async void паттерн без ThreadPool overhead
/// 2. ThrottleAsync: добавлен Dispose очистки _throttlers
/// 3. Timer + PeriodicTimer: взаимоисключающая активация
/// 4. _debouncers: Lock для thread-safety в многопоточном WASM
/// 5. PeriodicTimer task: правильное await в DisposeComponentAsync
/// </summary>
public abstract class SgInteractiveBase : SgJsComponentBase
{
    // ── Инъекции ──────────────────────────────────────────────────────────────
    [Inject] protected IKeyboardService KeyboardService { get; set; } = null!;

    // ── Параметры ────────────────────────────────────────────────────────────
    [Parameter] public bool Disabled  { get; set; }
    [Parameter] public bool Loading   { get; set; }
    [Parameter] public bool ReadOnly  { get; set; }

    // ── RTL ──────────────────────────────────────────────────────────────────
    [CascadingParameter(Name = "RightToLeft")] public bool IsRtl { get; set; }

    // ── Культура ─────────────────────────────────────────────────────────────
    [CascadingParameter(Name = "Culture")] public CultureInfo? CascadedCulture { get; set; }
    [Parameter] public CultureInfo? Culture { get; set; }
    protected CultureInfo EffectiveCulture => Culture ?? CascadedCulture ?? CultureInfo.CurrentUICulture;

    // ── Вычисляемые свойства ─────────────────────────────────────────────────
    protected virtual bool IsEffectivelyDisabled => Disabled || Loading;

    // ── ARIA ──────────────────────────────────────────────────────────────────
    protected override IReadOnlyDictionary<string, object> BuildAriaAttributes()
    {
        // Начинаем с базовых атрибутов (кэш из SgComponentBase)
        var base_ = base.BuildAriaAttributes();
        // ИСПРАВЛЕНО: не создаём новый dict если нет дополнений
        if (!Disabled && !Loading && !ReadOnly) return base_;

        var attrs = new Dictionary<string, object>(base_, StringComparer.Ordinal);
        if (Disabled)  attrs["aria-disabled"] = "true";
        if (Loading)   attrs["aria-busy"]     = "true";
        if (ReadOnly)  attrs["aria-readonly"] = "true";
        return attrs;
    }

    // ── Debounce — ИСПРАВЛЕНО ─────────────────────────────────────────────────
    // ИСПРАВЛЕНО: Lock для thread-safety, async void → fire-and-forget без Task.Run
    private readonly Lock _debounceLock = new();
    private readonly Dictionary<string, CancellationTokenSource> _debouncers = new();

    /// <summary>
    /// Выполнить action с debounce.
    /// ИСПРАВЛЕНО: не использует Task.Run — экономия ThreadPool
    /// </summary>
    protected Task DebounceAsync(string key, Func<Task> action, TimeSpan delay)
    {
        CancellationTokenSource newCts;
        lock (_debounceLock)
        {
            if (_debouncers.TryGetValue(key, out var old))
            {
                old.Cancel();
                old.Dispose();
            }
            newCts = CancellationTokenSource.CreateLinkedTokenSource(ComponentToken);
            _debouncers[key] = newCts;
        }

        // ИСПРАВЛЕНО: fire-and-forget без Task.Run
        _ = DelayThenInvokeAsync(action, delay, newCts.Token);
        return Task.CompletedTask;
    }

    private async Task DelayThenInvokeAsync(Func<Task> action, TimeSpan delay, CancellationToken ct)
    {
        try
        {
            await Task.Delay(delay, ct);
            if (!ct.IsCancellationRequested && !IsDisposed)
                await InvokeAsync(action);
        }
        catch (OperationCanceledException) { /* нормально */ }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[{Id}] Debounce callback error", ComponentId);
        }
    }

    protected Task DebounceAsync(Func<Task> action, TimeSpan? delay = null)
        => DebounceAsync("_default", action, delay ?? TimeSpan.FromMilliseconds(300));

    // ── Throttle — ИСПРАВЛЕНО ─────────────────────────────────────────────────
    private readonly Dictionary<string, ThrottleEntry> _throttlers = new();

    protected async Task ThrottleAsync(string key, Func<Task> action, TimeSpan interval)
    {
        ThrottleEntry? entry;
        lock (_throttlers)
        {
            if (_throttlers.TryGetValue(key, out entry) && entry.IsThrottled)
                return;
            if (entry is null)
            {
                entry = new ThrottleEntry();
                _throttlers[key] = entry;
            }
            entry.IsThrottled = true;
        }

        await action();

        // ИСПРАВЛЕНО: используем ComponentToken для автоотмены при dispose
        _ = Task.Delay(interval, ComponentToken).ContinueWith(t =>
        {
            if (!t.IsFaulted && !t.IsCanceled)
                lock (_throttlers)
                    if (_throttlers.TryGetValue(key, out var e))
                        e.IsThrottled = false;
        }, TaskScheduler.Default);
    }

    // ── Timer — ИСПРАВЛЕНО ────────────────────────────────────────────────────
    // ИСПРАВЛЕНО: единый enum для типа активного таймера
    private enum TimerMode { None, Legacy, Periodic }
    private TimerMode _timerMode = TimerMode.None;
    private Timer? _internalTimer;
    private PeriodicTimer? _periodicTimer;
    private Task? _periodicTimerTask;

    protected void StartTimer(Func<Task> callback, TimeSpan period, TimeSpan? dueTime = null)
    {
        StopAllTimers(); // ИСПРАВЛЕНО: останавливаем любой активный таймер
        _timerMode = TimerMode.Legacy;
        _internalTimer = new Timer(async _ =>
        {
            if (IsDisposed || ComponentToken.IsCancellationRequested) return;
            try { await InvokeAsync(callback); }
            catch (Exception ex) { Logger.LogError(ex, "[{Id}] Timer error", ComponentId); }
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
        StopAllTimers(); // ИСПРАВЛЕНО: останавливаем любой активный таймер
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
                await InvokeAsync(callback);
            }
        }
        catch (OperationCanceledException) { /* нормально */ }
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
        _internalTimer?.Dispose();
        _internalTimer = null;
        _periodicTimer?.Dispose();
        _periodicTimer = null;
        _timerMode = TimerMode.None;
    }

    // ── Auto-unsubscribe события ──────────────────────────────────────────────
    private readonly List<IDisposable> _subscriptions = [];

    protected void RegisterSubscription(IDisposable subscription)
        => _subscriptions.Add(subscription);

    protected void Subscribe<T>(IObservable<T> source, Action<T> handler)
    {
        var sub = source.Subscribe(value =>
        {
            if (!IsDisposed)
                InvokeAsync(() => handler(value));
        });
        _subscriptions.Add(sub);
    }

    // ── Keyboard handler ─────────────────────────────────────────────────────
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

    private static string BuildKeyString(KeyboardEventArgs e)
    {
        // ИСПРАВЛЕНО: Span-based string building без промежуточных string allocations
        Span<string> parts = stackalloc string[4];
        var count = 0;
        if (e.CtrlKey)  parts[count++] = "Ctrl";
        if (e.AltKey)   parts[count++] = "Alt";
        if (e.ShiftKey) parts[count++] = "Shift";
        parts[count++] = e.Key;
        return string.Join('+', parts[..count].ToArray());
    }

    // ── Mouse handlers ────────────────────────────────────────────────────────
    [Parameter] public EventCallback<MouseEventArgs> OnClick      { get; set; }
    [Parameter] public EventCallback<MouseEventArgs> OnMouseEnter { get; set; }
    [Parameter] public EventCallback<MouseEventArgs> OnMouseLeave { get; set; }

    protected async Task HandleClickAsync(MouseEventArgs e)
    {
        if (IsEffectivelyDisabled) return;
        await OnClick.InvokeAsync(e);
    }

    // ── Dispose — ИСПРАВЛЕНО ─────────────────────────────────────────────────
    protected override async ValueTask DisposeComponentAsync()
    {
        StopAllTimers();

        // ИСПРАВЛЕНО: ждём PeriodicTimer task
        if (_periodicTimerTask is not null)
        {
            try { await _periodicTimerTask; }
            catch (OperationCanceledException) { }
        }

        // Отменить debounce — ИСПРАВЛЕНО: _throttlers тоже очищается
        lock (_debounceLock)
        {
            foreach (var cts in _debouncers.Values)
            {
                cts.Cancel();
                cts.Dispose();
            }
            _debouncers.Clear();
        }

        lock (_throttlers) _throttlers.Clear(); // ИСПРАВЛЕНО: очистка throttlers

        foreach (var sub in _subscriptions)
            sub.Dispose();
        _subscriptions.Clear();

        await base.DisposeComponentAsync();
    }

    // ── Вспомогательные записи ────────────────────────────────────────────────
    private sealed class ThrottleEntry { public bool IsThrottled { get; set; } }
}
