using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;
using SuperUI.Base.Reactive;
using SuperUI.Base.Services;

namespace SuperUI.Base;

/// <summary>
/// Базовый класс для интерактивных компонентов SuperUI.
/// Уровень 3 в иерархии: ComponentBase → SgComponentBase → SgJsComponentBase → SgInteractiveBase
/// </summary>
/// <remarks>
/// ИСПРАВЛЕНИЯ:
/// 1. CS1061: добавлен using Microsoft.Extensions.Logging для LogError/LogWarning
/// 2. CS0246: добавлен using SuperUI.Base.Services для IKeyboardService
/// 3. CS1660: Subscribe использует SgObserver<T> вместо лямбды напрямую
/// 4. CS0208: BuildKeyString использует ValueStringBuilder вместо stackalloc string[]
/// 5. DebounceAsync: Lock для thread-safety, без Task.Run
/// 6. ThrottleAsync: Dispose очистки _throttlers в DisposeComponentAsync
/// 7. Timer + PeriodicTimer: взаимоисключающая активация через TimerMode
/// 8. PeriodicTimer task: правильное await в DisposeComponentAsync
/// </remarks>
public abstract class SgInteractiveBase : SgJsComponentBase
{
    // ── Инъекции ──────────────────────────────────────────────────────────────
    [Inject]
    protected IKeyboardService KeyboardService { get; set; } = null!;

    // ── Параметры ────────────────────────────────────────────────────────────
    [Parameter] public bool Disabled   { get; set; }
    [Parameter] public bool Loading    { get; set; }
    [Parameter] public bool ReadOnly   { get; set; }

    // ── RTL ──────────────────────────────────────────────────────────────────
    [CascadingParameter(Name = "RightToLeft")]
    public bool IsRtl { get; set; }

    // ── Культура ─────────────────────────────────────────────────────────────
    [CascadingParameter(Name = "Culture")]
    public CultureInfo? CascadedCulture { get; set; }

    [Parameter]
    public CultureInfo? Culture { get; set; }

    protected CultureInfo EffectiveCulture =>
        Culture ?? CascadedCulture ?? CultureInfo.CurrentUICulture;

    // ── Вычисляемые свойства ──────────────────────────────────────────────────
    protected virtual bool IsEffectivelyDisabled => Disabled || Loading;

    // ── ARIA ──────────────────────────────────────────────────────────────────
    protected override IReadOnlyDictionary<string, object> BuildAriaAttributes()
    {
        var base_ = base.BuildAriaAttributes();

        // Не создаём новый dict если нет дополнений — экономия GC
        if (!Disabled && !Loading && !ReadOnly) return base_;

        var attrs = new Dictionary<string, object>(base_, StringComparer.Ordinal);
        if (Disabled) attrs["aria-disabled"] = "true";
        if (Loading)  attrs["aria-busy"]     = "true";
        if (ReadOnly) attrs["aria-readonly"] = "true";
        return attrs;
    }

    // ── Debounce ──────────────────────────────────────────────────────────────
    private readonly Lock _debounceLock = new();
    private readonly Dictionary<string, CancellationTokenSource> _debouncers = new();

    /// <summary>
    /// Выполнить action с debounce.
    /// Fire-and-forget без Task.Run — экономия ThreadPool.
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

    // ── Throttle ──────────────────────────────────────────────────────────────
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

        _ = Task.Delay(interval, ComponentToken).ContinueWith(t =>
        {
            if (!t.IsFaulted && !t.IsCanceled)
                lock (_throttlers)
                    if (_throttlers.TryGetValue(key, out var e))
                        e.IsThrottled = false;
        }, TaskScheduler.Default);
    }

    // ── Timer ─────────────────────────────────────────────────────────────────
    private enum TimerMode { None, Legacy, Periodic }
    private TimerMode      _timerMode = TimerMode.None;
    private Timer?         _internalTimer;
    private PeriodicTimer? _periodicTimer;
    private Task?          _periodicTimerTask;

    protected void StartTimer(Func<Task> callback, TimeSpan period, TimeSpan? dueTime = null)
    {
        StopAllTimers();
        _timerMode = TimerMode.Legacy;
        _internalTimer = new Timer(async _ =>
        {
            if (IsDisposed || ComponentToken.IsCancellationRequested) return;
            try
            {
                await InvokeAsync(callback);
            }
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

    /// <summary>
    /// Подписаться на IObservable<T>.
    /// Использует SgObserver<T> чтобы передать лямбду как IObserver<T> без System.Reactive.
    /// </summary>
    protected void Subscribe<T>(IObservable<T> source, Action<T> handler)
    {
        var observer = new SgObserver<T>(value =>
        {
            if (!IsDisposed)
                InvokeAsync(() => handler(value));
        });
        var sub = source.Subscribe(observer);
        _subscriptions.Add(sub);
    }

    // ── Keyboard handler ──────────────────────────────────────────────────────
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

    /// <summary>
    /// Сборка строки сочетания клавиш (Ctrl+Alt+Shift+F12).
    /// Использует Span<char> на стеке — CS0208 исправлен.
    /// </summary>
    [SkipLocalsInit]
    private static string BuildKeyString(KeyboardEventArgs e)
    {
        Span<char> buffer = stackalloc char[64];
        var sb = new SpanStringBuilder(buffer);

        if (e.CtrlKey)  { sb.Append("Ctrl");  sb.Append('+'); }
        if (e.AltKey)   { sb.Append("Alt");   sb.Append('+'); }
        if (e.ShiftKey) { sb.Append("Shift"); sb.Append('+'); }
        sb.Append(e.Key);

        return sb.ToString();
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

    // ── Dispose ───────────────────────────────────────────────────────────────
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
                cts.Cancel();
                cts.Dispose();
            }
            _debouncers.Clear();
        }

        lock (_throttlers) _throttlers.Clear();

        foreach (var sub in _subscriptions)
            sub.Dispose();
        _subscriptions.Clear();

        await base.DisposeComponentAsync();
    }

    // ── Вспомогательные типы ──────────────────────────────────────────────────
    private sealed class ThrottleEntry
    {
        public bool IsThrottled { get; set; }
    }

    private ref struct SpanStringBuilder
    {
        private readonly Span<char> _buffer;
        private int _pos;

        public SpanStringBuilder(Span<char> buffer)
        {
            _buffer = buffer;
            _pos = 0;
        }

        public void Append(string value)
        {
            value.AsSpan().CopyTo(_buffer[_pos..]);
            _pos += value.Length;
        }

        public void Append(char c)
        {
            _buffer[_pos++] = c;
        }

        public readonly override string ToString()
            => new(_buffer[.._pos]);
    }
}
