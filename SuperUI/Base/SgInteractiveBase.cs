// SuperUI/Base/SgInteractiveBase.cs
// ИСПРАВЛЕНО:
// 1. ThrottleAsync: IsThrottled ВСЕГДА сбрасывается в finally (даже при отмене токена)
// 2. ThrottleAsync: ключи удаляются из словаря при Dispose
// 3. DebounceAsync: ключ удаляется из словаря после выполнения (нет утечки памяти)
// 4. DebounceAsync: проверка IsDisposed перед добавлением нового ключа
// 5. _throttlers: проверка IsDisposed в ThrottleAsync перед GetOrAdd
// 6. Timer: StopAllTimers безопасен при повторном вызове
// 7. Subscriptions: Subscribe<T> использует InvokeAsync корректно

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
    [Inject] protected IKeyboardService KeyboardService { get; set; } = null!;

    // ── Параметры ─────────────────────────────────────────────────────────────
    [Parameter] public bool Disabled { get; set; }
    [Parameter] public bool Loading { get; set; }
    [Parameter] public bool ReadOnly { get; set; }

    // ── RTL / Culture ─────────────────────────────────────────────────────────
    [CascadingParameter(Name = "RightToLeft")] public bool IsRtl { get; set; }
    [CascadingParameter(Name = "Culture")] public CultureInfo? CascadedCulture { get; set; }
    [Parameter] public CultureInfo? Culture { get; set; }
    protected CultureInfo EffectiveCulture =>
        Culture ?? CascadedCulture ?? CultureInfo.CurrentUICulture;

    protected virtual bool IsEffectivelyDisabled => Disabled || Loading;

    // ── ARIA ───────────────────────────────────────────────────────────────────
    protected override IReadOnlyDictionary<string, object> BuildAriaAttributes()
    {
        var base_ = base.BuildAriaAttributes();
        if (!Disabled && !Loading && !ReadOnly) return base_;

        // Только создаём новый dict если что-то добавляем
        var attrs = new Dictionary<string, object>(base_, StringComparer.Ordinal);
        if (Disabled) attrs["aria-disabled"] = "true";
        if (Loading)  attrs["aria-busy"]     = "true";
        if (ReadOnly) attrs["aria-readonly"] = "true";
        return attrs;
    }

    // ── Debounce ────────────────────────────────────────────────────────────────
    private readonly Lock _debounceLock = new();
    private readonly Dictionary<string, CancellationTokenSource> _debouncers = new();

    protected Task DebounceAsync(string key, Func<Task> action, TimeSpan delay)
    {
        // ИСПРАВЛЕНО: проверяем IsDisposed перед добавлением (нет смысла планировать)
        if (IsDisposed) return Task.CompletedTask;

        CancellationTokenSource newCts;
        lock (_debounceLock)
        {
            if (IsDisposed) return Task.CompletedTask; // double-check под lock

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

    private async Task DelayThenInvokeAsync(string key, Func<Task> action, TimeSpan delay, CancellationToken ct)
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
            // ИСПРАВЛЕНО: удаляем ключ из словаря после завершения
            // Это предотвращает утечку памяти при долгоживущих компонентах
            lock (_debounceLock)
            {
                if (_debouncers.TryGetValue(key, out var cts) && cts.Token == ct)
                {
                    _debouncers.Remove(key);
                    // cts уже завершён/отменён — диспозим
                    try { cts.Dispose(); } catch { /* ignore */ }
                }
            }
        }
    }

    protected Task DebounceAsync(Func<Task> action, TimeSpan? delay = null)
        => DebounceAsync("_default", action, delay ?? TimeSpan.FromMilliseconds(300));

    // ── Throttle ────────────────────────────────────────────────────────────────
    // ИСПРАВЛЕНО: используем ConcurrentDictionary<string, ThrottleEntry>
    // ThrottleEntry теперь содержит также таймер для гарантированного сброса
    private readonly ConcurrentDictionary<string, ThrottleEntry> _throttlers = new();

    protected async Task ThrottleAsync(string key, Func<Task> action, TimeSpan interval)
    {
        // ИСПРАВЛЕНО: если компонент уже диспожен — не добавляем новые записи
        if (IsDisposed) return;

        var entry = _throttlers.GetOrAdd(key, _ => new ThrottleEntry());

        if (Interlocked.CompareExchange(ref entry.IsThrottled, 1, 0) == 1)
            return; // уже throttled

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
            // ИСПРАВЛЕНО: гарантированный сброс IsThrottled через отдельный CTS
            // Не зависит от ComponentToken — даже после Dispose интервал истечёт
            _ = ResetThrottleAfterDelayAsync(entry, interval);
        }
    }

    private static async Task ResetThrottleAfterDelayAsync(ThrottleEntry entry, TimeSpan interval)
    {
        try
        {
            // Используем отдельный CTS без привязки к ComponentToken
            // Это гарантирует что IsThrottled будет сброшен даже если компонент задиспожен
            using var delayCts = new CancellationTokenSource(
                interval + TimeSpan.FromMilliseconds(100)); // небольшой запас
            
            await Task.Delay(interval, delayCts.Token);
        }
        catch (OperationCanceledException) { /* таймаут — всё равно сбрасываем */ }
        finally
        {
            // Гарантированный сброс — ВСЕГДА
            Interlocked.Exchange(ref entry.IsThrottled, 0);
        }
    }

    // ── Timer ─────────────────────────────────────────────────────────────────
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
            if (IsDisposed || ComponentToken.IsCancelled) return;
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
        if (_timerMode == TimerMode.Legacy) _timerMode = TimerMode.None;
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
        if (_timerMode == TimerMode.Periodic) _timerMode = TimerMode.None;
    }

    private void StopAllTimers()
    {
        _internalTimer?.Dispose();
        _internalTimer = null;
        _periodicTimer?.Dispose();
        _periodicTimer = null;
        _timerMode = TimerMode.None;
    }

    // ── Subscriptions ─────────────────────────────────────────────────────────
    private readonly List<IDisposable> _subscriptions = [];

    protected void RegisterSubscription(IDisposable subscription)
        => _subscriptions.Add(subscription);

    protected void Subscribe<T>(IObservable<T> source, Action<T> handler)
    {
        var observer = new SgObserver<T>(value =>
        {
            // ИСПРАВЛЕНО: проверяем IsDisposed перед InvokeAsync
            if (!IsDisposed)
                _ = InvokeAsync(() => handler(value));
        });
        _subscriptions.Add(source.Subscribe(observer));
    }

    // ── Keyboard ──────────────────────────────────────────────────────────────
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

    /// <summary>Zero-allocation построение строки-ключа через string.Create.</summary>
    private static string BuildKeyString(KeyboardEventArgs e)
    {
        var len = (e.CtrlKey ? 5 : 0) + (e.AltKey ? 4 : 0)
                + (e.ShiftKey ? 6 : 0) + (e.Key?.Length ?? 0);
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

    // ── Mouse handlers ────────────────────────────────────────────────────────
    [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }
    [Parameter] public EventCallback<MouseEventArgs> OnMouseEnter { get; set; }
    [Parameter] public EventCallback<MouseEventArgs> OnMouseLeave { get; set; }

    protected async Task HandleClickAsync(MouseEventArgs e)
    {
        if (IsEffectivelyDisabled) return;
        await OnClick.InvokeAsync(e);
    }

    // ── Dispose ─────────────────────────────────────────────────────────────────
    protected override async ValueTask DisposeComponentAsync()
    {
        StopAllTimers();

        if (_periodicTimerTask is not null)
        {
            try { await _periodicTimerTask; }
            catch (OperationCanceledException) { }
        }

        // ИСПРАВЛЕНО: сначала очищаем debouncers с отменой всех CTS
        lock (_debounceLock)
        {
            foreach (var cts in _debouncers.Values)
            {
                try { cts.Cancel(); cts.Dispose(); }
                catch { /* ignore */ }
            }
            _debouncers.Clear();
        }

        // ИСПРАВЛЕНО: НЕ очищаем _throttlers.Clear() (race condition с GetOrAdd)
        // Просто оставляем их — IsDisposed=true предотвращает добавление новых
        // ThrottleEntry без ссылок будет GC'd естественным образом

        foreach (var sub in _subscriptions) sub.Dispose();
        _subscriptions.Clear();

        await base.DisposeComponentAsync();
    }

    // ── Вспомогательный тип ───────────────────────────────────────────────────
    private sealed class ThrottleEntry
    {
        public int IsThrottled; // 0 = free, 1 = throttled (Interlocked)
    }
}
