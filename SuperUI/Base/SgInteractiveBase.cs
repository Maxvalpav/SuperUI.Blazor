using Microsoft.AspNetCore.Components.Web;
using SuperUI.Services;

namespace SuperUI.Base;

/// <summary>
/// Базовый класс для интерактивных компонентов.
/// 
/// Обеспечивает:
/// - Управление Disabled + aria-disabled
/// - Управление Loading + aria-busy
/// - Keyboard handler регистрация
/// - Mouse handler регистрация
/// - Auto-unsubscribe события
/// - RTL поддержка
/// - Культура каскадная
/// - Встроенные Debounce / Throttle / Timer
/// - PeriodicTimer с авто-dispose
/// </summary>
public abstract class SgInteractiveBase : SgJsComponentBase
{
    // ── Инъекции ──────────────────────────────────────────────────────────────
    [Inject] protected IKeyboardService KeyboardService { get; set; } = null!;

    // ── Параметры ─────────────────────────────────────────────────────────────

    /// <summary>Отключить компонент. Устанавливает aria-disabled и блокирует события.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Состояние загрузки. Устанавливает aria-busy.</summary>
    [Parameter] public bool Loading { get; set; }

    /// <summary>Только для чтения (для input компонентов).</summary>
    [Parameter] public bool ReadOnly { get; set; }

    // ── RTL ───────────────────────────────────────────────────────────────────

    [CascadingParameter(Name = "RightToLeft")]
    public bool IsRtl { get; set; }

    // ── Культура ──────────────────────────────────────────────────────────────

    [CascadingParameter(Name = "Culture")]
    public CultureInfo? CascadedCulture { get; set; }

    [Parameter]
    public CultureInfo? Culture { get; set; }

    protected CultureInfo EffectiveCulture
        => Culture ?? CascadedCulture ?? CultureInfo.CurrentUICulture;

    // ── Вычисляемые свойства ──────────────────────────────────────────────────

    /// <summary>Компонент недоступен (Disabled или Loading).</summary>
    protected virtual bool IsEffectivelyDisabled => Disabled || Loading;

    // ── ARIA ──────────────────────────────────────────────────────────────────

    protected override IReadOnlyDictionary<string, object?> BuildAriaAttributes()
    {
        var attrs = base.BuildAriaAttributes() as Dictionary<string, object?> ?? [];

        if (Disabled)
            attrs["aria-disabled"] = "true";

        if (Loading)
            attrs["aria-busy"] = "true";

        if (ReadOnly)
            attrs["aria-readonly"] = "true";

        return attrs;
    }

    // ── Debounce встроенный ────────────────────────────────────────────────────

    private readonly Dictionary<string, DebounceEntry> _debouncers = new();

    /// <summary>
    /// Выполнить action с debounce.
    /// Каждый вызов с тем же key откладывает выполнение на delay.
    /// Race-safe: использует LifecycleToken.
    /// </summary>
    protected Task DebounceAsync(string key, Func<Task> action, TimeSpan delay)
    {
        if (_debouncers.TryGetValue(key, out var entry))
        {
            entry.Cancel();
            _debouncers.Remove(key);
        }

        var cts = CancellationTokenSource.CreateLinkedTokenSource(ComponentToken);
        _debouncers[key] = new DebounceEntry(cts);

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(delay, cts.Token);
                if (!cts.Token.IsCancellationRequested && !IsDisposed)
                {
                    await InvokeAsync(action);
                }
            }
            catch (TaskCanceledException) { /* нормально */ }
        }, cts.Token);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Упрощённый debounce для обработчиков событий.
    /// </summary>
    protected Task DebounceAsync(Func<Task> action, TimeSpan? delay = null)
        => DebounceAsync("_default", action, delay ?? TimeSpan.FromMilliseconds(300));

    // ── Throttle встроенный ────────────────────────────────────────────────────

    private readonly Dictionary<string, ThrottleEntry> _throttlers = new();

    /// <summary>
    /// Выполнить action с throttle — не чаще чем раз в interval.
    /// </summary>
    protected async Task ThrottleAsync(string key, Func<Task> action, TimeSpan interval)
    {
        if (_throttlers.TryGetValue(key, out var entry) && entry.IsThrottled)
            return;

        _throttlers[key] = new ThrottleEntry { IsThrottled = true };

        await action();

        _ = Task.Delay(interval, ComponentToken).ContinueWith(_ =>
        {
            if (_throttlers.TryGetValue(key, out var e))
                e.IsThrottled = false;
        }, TaskScheduler.Default);
    }

    // ── Timer встроенный ──────────────────────────────────────────────────────

    private Timer? _internalTimer;

    /// <summary>
    /// Запустить повторяющийся таймер.
    /// Автоматически останавливается при Dispose.
    /// </summary>
    protected void StartTimer(Func<Task> callback, TimeSpan period, TimeSpan? dueTime = null)
    {
        StopTimer();
        _internalTimer = new Timer(async _ =>
        {
            if (IsDisposed || ComponentToken.IsCancellationRequested) return;
            try { await InvokeAsync(callback); }
            catch (Exception ex) { Logger.LogError(ex, "[{Id}] Timer callback error", ComponentId); }
        }, null, dueTime ?? period, period);
    }

    protected void StopTimer()
    {
        _internalTimer?.Dispose();
        _internalTimer = null;
    }

    // ── PeriodicTimer с авто-dispose ──────────────────────────────────────────

    private PeriodicTimer? _periodicTimer;
    private Task? _periodicTimerTask;

    /// <summary>
    /// Запустить PeriodicTimer (.NET 6+).
    /// Предпочтительнее Timer: не накапливает вызовы, не создаёт гонок.
    /// Автоматически останавливается при Dispose.
    /// </summary>
    protected void StartPeriodicTimer(Func<Task> callback, TimeSpan period)
    {
        StopPeriodicTimer();
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
    }

    // ── Auto-unsubscribe события ───────────────────────────────────────────────

    private readonly List<IDisposable> _subscriptions = [];

    /// <summary>
    /// Зарегистрировать подписку с авто-отпиской при Dispose.
    /// </summary>
    protected void RegisterSubscription(IDisposable subscription)
        => _subscriptions.Add(subscription);

    /// <summary>
    /// Подписаться на событие .NET с авто-отпиской.
    /// </summary>
    protected void Subscribe<T>(IObservable<T> source, Action<T> handler)
    {
        var sub = source.Subscribe(value =>
        {
            if (!IsDisposed)
                InvokeAsync(() => handler(value));
        });
        _subscriptions.Add(sub);
    }

    // ── Keyboard handler ──────────────────────────────────────────────────────

    private readonly Dictionary<string, Func<KeyboardEventArgs, Task>> _keyHandlers = new();

    /// <summary>
    /// Зарегистрировать обработчик клавиши.
    /// Key format: "Enter", "Escape", "ArrowUp", "Ctrl+Enter"
    /// </summary>
    protected void OnKey(string key, Func<KeyboardEventArgs, Task> handler)
        => _keyHandlers[key] = handler;

    protected void OnKey(string key, Action handler)
        => _keyHandlers[key] = _ => { handler(); return Task.CompletedTask; };

    /// <summary>Обработать KeyboardEvent — вызвать из razor @onkeydown.</summary>
    protected async Task HandleKeyDownAsync(KeyboardEventArgs e)
    {
        if (IsEffectivelyDisabled) return;

        var key = BuildKeyString(e);
        if (_keyHandlers.TryGetValue(key, out var handler))
        {
            await handler(e);
        }
    }

    private static string BuildKeyString(KeyboardEventArgs e)
    {
        var parts = new List<string>();
        if (e.CtrlKey) parts.Add("Ctrl");
        if (e.AltKey) parts.Add("Alt");
        if (e.ShiftKey) parts.Add("Shift");
        parts.Add(e.Key);
        return string.Join('+', parts);
    }

    // ── Mouse handler ─────────────────────────────────────────────────────────

    [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }
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
        // Остановить таймеры
        StopTimer();
        StopPeriodicTimer();

        // Отменить debounce
        foreach (var d in _debouncers.Values) d.Cancel();
        _debouncers.Clear();

        // Отписаться от событий
        foreach (var sub in _subscriptions) sub.Dispose();
        _subscriptions.Clear();

        await base.DisposeComponentAsync();
    }

    // ── Вспомогательные записи ────────────────────────────────────────────────
    private record DebounceEntry(CancellationTokenSource Cts)
    {
        public void Cancel() { Cts.Cancel(); Cts.Dispose(); }
    }

    private class ThrottleEntry { public bool IsThrottled { get; set; } }
}
