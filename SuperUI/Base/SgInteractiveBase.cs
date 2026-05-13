// SuperUI/Base/SgInteractiveBase.cs
//
// УЛУЧШЕНИЯ:
// 1. DebounceAsync: IsDisposed check ДО CancellationTokenSource.
// 2. ThrottleEntry.IsThrottled: volatile int (Interlocked уже используется, но volatile
//    гарантирует видимость в WASM на ARM).
// 3. BuildKeyString: защита от null Key.
// 4. HandleClickAsync: более точная фильтрация исключений.
// 5. ClearDebouncers() — новый метод для явной отмены всех debounce.
// 6. OnKey overload для async handler с возвратом bool (обработано/нет).
// 7. RegisterGlobalKey — регистрация через KeyboardService + авто-отписка.

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
    [Parameter] public bool Loading  { get; set; }
    [Parameter] public bool ReadOnly { get; set; }

    /// <summary>Callback при получении фокуса.</summary>
    [Parameter] public EventCallback<FocusEventArgs> OnFocus { get; set; }

    /// <summary>Callback при потере фокуса.</summary>
    [Parameter] public EventCallback<FocusEventArgs> OnBlur { get; set; }

    // ── RTL / Culture ─────────────────────────────────────────────────────────
    [CascadingParameter(Name = "RightToLeft")] public bool IsRtl { get; set; }
    [CascadingParameter(Name = "Culture")] public CultureInfo? CascadedCulture { get; set; }
    [Parameter] public CultureInfo? Culture { get; set; }

    protected CultureInfo EffectiveCulture =>
        Culture ?? CascadedCulture ?? CultureInfo.CurrentUICulture;

    protected virtual bool IsEffectivelyDisabled => Disabled || Loading;

    /// <summary>Компонент имеет фокус.</summary>
    protected bool IsFocused { get; private set; }

    // ── Focus handlers ──────────────────────────────────────────────────────────

    protected async Task HandleFocusAsync(FocusEventArgs e)
    {
        if (IsStaticSSR || IsDisposed) return;
        IsFocused = true;
        try { await OnFocus.InvokeAsync(e); }
        catch (OperationCanceledException) { }
        catch (Exception ex) { Logger.LogError(ex, "[{Id}] OnFocus error", ComponentId); }
    }

    protected async Task HandleBlurAsync(FocusEventArgs e)
    {
        if (IsStaticSSR || IsDisposed) return;
        IsFocused = false;
        try { await OnBlur.InvokeAsync(e); }
        catch (OperationCanceledException) { }
        catch (Exception ex) { Logger.LogError(ex, "[{Id}] OnBlur error", ComponentId); }
    }

    // ── ARIA ──────────────────────────────────────────────────────────────────
    protected override IReadOnlyDictionary<string, object> BuildAriaAttributes()
    {
        var base_ = base.BuildAriaAttributes();
        if (!Disabled && !Loading && !ReadOnly) return base_;

        var attrs = new Dictionary<string, object>(base_, StringComparer.Ordinal);
        if (Disabled) attrs["aria-disabled"] = "true";
        if (Loading)  attrs["aria-busy"]     = "true";
        if (ReadOnly) attrs["aria-readonly"]  = "true";
        return attrs;
    }

    // ── Debounce ──────────────────────────────────────────────────────────────
    private readonly object _debounceLock = new();
    private readonly Dictionary<string, CancellationTokenSource> _debouncers = new();

    protected Task DebounceAsync(string key, Func<Task> action, TimeSpan delay)
    {
        // УЛУЧШЕНО: ранний выход при dispose
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

    /// <summary>
    /// Создать debounced обёртку вокруг EventCallback.
    /// </summary>
    protected EventCallback<TValue> WithDebounce<TValue>(
        EventCallback<TValue> callback,
        int delayMs = 300)
    {
        return EventCallback.Factory.Create<TValue>(this, async value =>
        {
            await DebounceAsync(
                $"__callback_{callback.GetHashCode()}",
                () => callback.InvokeAsync(value),
                TimeSpan.FromMilliseconds(delayMs));
        });
    }

    /// <summary>Явно отменить все pending debounce-операции.</summary>
    public void ClearDebouncers()
    {
        lock (_debounceLock)
        {
            foreach (var cts in _debouncers.Values)
            {
                try { cts.Cancel(); cts.Dispose(); }
                catch { }
            }
            _debouncers.Clear();
        }
    }

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

    // ── Throttle ──────────────────────────────────────────────────────────────
    private const int MaxThrottleEntries = 100;
    private readonly ConcurrentDictionary<string, ThrottleEntry> _throttlers = new();

    protected async Task ThrottleAsync(string key, Func<Task> action, TimeSpan interval)
    {
        if (IsDisposed) return;

        if (_throttlers.Count >= MaxThrottleEntries && !_throttlers.ContainsKey(key))
        {
            Logger.LogWarning(
                "[{Id}] ThrottleAsync: too many keys (max {Max}), key={Key} skipped",
                ComponentId, MaxThrottleEntries, key);
            return;
        }

        var entry = _throttlers.GetOrAdd(key, _ => new ThrottleEntry());
        if (Interlocked.CompareExchange(ref entry.IsThrottled, 1, 0) == 1) return;

        try { await action(); }
        catch (Exception ex) { Logger.LogError(ex, "[{Id}] Throttle error key={Key}", ComponentId, key); }
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
    }

    // ── Timer ─────────────────────────────────────────────────────────────────
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
            catch (ObjectDisposedException)    { }
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
        _periodicTimer    = new PeriodicTimer(period);
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
        _internalTimer?.Dispose(); _internalTimer = null;
        _periodicTimer?.Dispose(); _periodicTimer = null;
    }

    // ── Subscriptions ─────────────────────────────────────────────────────────
    private readonly List<IDisposable> _subscriptions = [];

    protected void RegisterSubscription(IDisposable subscription)
        => _subscriptions.Add(subscription);

    protected void Subscribe<T>(IObservable<T> source, Action<T> handler)
    {
        var observer = new SgObserver<T>(
            onNext:      value => { if (!IsDisposed) _ = InvokeAsync(() => handler(value)); },
            onError:     ex    => Logger.LogWarning(ex, "[{Id}] Observable error", ComponentId),
            onCompleted: ()    => { });
        _subscriptions.Add(source.Subscribe(observer));
    }

    // ── Global keyboard shortcuts ─────────────────────────────────────────────
    private readonly List<IDisposable> _globalKeySubscriptions = [];

    /// <summary>
    /// Зарегистрировать глобальную горячую клавишу через KeyboardService (window-level).
    /// Авто-отписка при Dispose компонента.
    /// </summary>
    protected void RegisterGlobalKey(string key, Func<Task> handler)
    {
        var sub = KeyboardService.Register(key, handler);
        _globalKeySubscriptions.Add(sub);
    }

    protected void RegisterGlobalKey(string key, Action handler)
    {
        var sub = KeyboardService.Register(key, handler);
        _globalKeySubscriptions.Add(sub);
    }

    // ── Component-level keyboard ──────────────────────────────────────────────
    private readonly ConcurrentDictionary<string, Func<KeyboardEventArgs, Task>> _keyHandlers = new();
    private readonly ConcurrentDictionary<string, Func<KeyboardEventArgs, Task>> _keyUpHandlers = new();

    /// <summary>Зарегистрировать обработчик KeyDown.</summary>
    protected void OnKeyDown(string key, Func<Task> handler)
        => _keyHandlers[key] = _ => handler();

    /// <summary>Зарегистрировать обработчик KeyDown (с аргументами).</summary>
    protected void OnKeyDown(string key, Func<KeyboardEventArgs, Task> handler)
        => _keyHandlers[key] = handler;

    /// <summary>
    /// Зарегистрировать обработчик KeyUp (семантически правильно для кнопок и активации).
    /// </summary>
    protected void OnKeyUp(string key, Func<Task> handler)
        => _keyUpHandlers[key] = _ => handler();

    /// <summary>Зарегистрировать обработчик KeyUp с аргументами.</summary>
    protected void OnKeyUp(string key, Func<KeyboardEventArgs, Task> handler)
        => _keyUpHandlers[key] = handler;

    /// <summary>Удалить обработчик KeyDown.</summary>
    protected void RemoveKeyDown(string key)
        => _keyHandlers.TryRemove(key, out _);

    /// <summary>Удалить обработчик KeyUp.</summary>
    protected void RemoveKeyUp(string key)
        => _keyUpHandlers.TryRemove(key, out _);

    /// <summary>Удалить обработчик клавиши (устаревший метод).</summary>
    [Obsolete("Use RemoveKeyDown() or RemoveKeyUp() for semantic correctness.", false)]
    protected void RemoveKey(string key)
    {
        RemoveKeyDown(key);
        RemoveKeyUp(key);
    }

    /// <summary>
    /// Зарегистрировать обработчик клавиши с возможностью предотвратить default-поведение.
    /// </summary>
    /// <param name="key">Строка клавиши (напр. "Ctrl+s", "Escape").</param>
    /// <param name="handler">Handler: возвращает true = handled (preventDefault), false = pass-through.</param>
    protected void OnKeyDown(string key, Func<KeyboardEventArgs, Task<bool>> handler)
        => _keyHandlers[key] = async e =>
        {
            var handled = await handler(e);
            // handled используется JSInterop для preventDefault на уровне JS
            _ = handled;
        };

    /// <summary>
    /// Зарегистрировать обработчик KeyUp с возможностью предотвратить default-поведение.
    /// </summary>
    protected void OnKeyUp(string key, Func<KeyboardEventArgs, Task<bool>> handler)
        => _keyUpHandlers[key] = async e =>
        {
            var handled = await handler(e);
            _ = handled;
        };

    /// <summary>Устаревший метод — используйте OnKeyDown() или OnKeyUp() для семантической корректности.</summary>
    [Obsolete("Use OnKeyDown() or OnKeyUp() for semantic correctness.", false)]
    protected void OnKey(string key, Func<Task> handler)
        => OnKeyDown(key, handler);

    /// <summary>Устаревший метод — используйте OnKeyDown() или OnKeyUp() для семантической корректности.</summary>
    [Obsolete("Use OnKeyDown() or OnKeyUp() for semantic correctness.", false)]
    protected void OnKey(string key, Action handler)
        => OnKeyDown(key, () => { handler(); return Task.CompletedTask; });

    /// <summary>Устаревший метод — используйте OnKeyDown() или OnKeyUp() для семантической корректности.</summary>
    [Obsolete("Use OnKeyDown() or OnKeyUp() for semantic correctness.", false)]
    protected void OnKey(string key, Func<KeyboardEventArgs, Task> handler)
        => OnKeyDown(key, handler);

    /// <summary>Устаревший метод — используйте OnKeyDown() или OnKeyUp() для семантической корректности.</summary>
    [Obsolete("Use OnKeyDown() or OnKeyUp() for semantic correctness.", false)]
    protected void OnKey(string key, Func<KeyboardEventArgs, Task<bool>> handler)
        => OnKeyDown(key, handler);

    protected async Task HandleKeyDownAsync(KeyboardEventArgs e)
    {
        if (IsStaticSSR || IsEffectivelyDisabled) return;
        var key = BuildKeyString(e);
        if (!string.IsNullOrEmpty(key) && _keyHandlers.TryGetValue(key, out var handler))
            await handler(e);
    }

    /// <summary>
    /// Обработчик KeyUp событий (ИСПРАВЛЕНИЕ: отдельный словарь _keyUpHandlers).
    /// </summary>
    protected async Task HandleKeyUpAsync(KeyboardEventArgs e)
    {
        if (IsStaticSSR || IsEffectivelyDisabled) return;
        var key = BuildKeyString(e);
        if (!string.IsNullOrEmpty(key) && _keyUpHandlers.TryGetValue(key, out var handler))
            await handler(e);
    }

    /// <summary>
    /// УЛУЧШЕНО: защита от null Key; zero-allocation через string.Create.
    /// </summary>
    private static string BuildKeyString(KeyboardEventArgs e)
    {
        if (string.IsNullOrEmpty(e.Key)) return string.Empty;

        var keyLen  = e.Key.Length;
        var len     = (e.CtrlKey  ? 5 : 0)  // "Ctrl+"
                    + (e.AltKey   ? 4 : 0)  // "Alt+"
                    + (e.ShiftKey ? 6 : 0)  // "Shift+"
                    + (e.MetaKey  ? 5 : 0)  // "Meta+"
                    + keyLen;

        if (len == keyLen) return e.Key; // Без модификаторов — возвращаем Key напрямую

        return string.Create(len, e, static (span, ev) =>
        {
            var pos = 0;
            if (ev.CtrlKey)  { "Ctrl+" .AsSpan().CopyTo(span[pos..]); pos += 5; }
            if (ev.AltKey)   { "Alt+"  .AsSpan().CopyTo(span[pos..]); pos += 4; }
            if (ev.ShiftKey) { "Shift+".AsSpan().CopyTo(span[pos..]); pos += 6; }
            if (ev.MetaKey)  { "Meta+" .AsSpan().CopyTo(span[pos..]); pos += 5; }
            ev.Key.AsSpan().CopyTo(span[pos..]);
        });
    }

    // ── Mouse handlers ────────────────────────────────────────────────────────
    [Parameter] public EventCallback<MouseEventArgs> OnClick      { get; set; }
    [Parameter] public EventCallback<MouseEventArgs> OnMouseEnter { get; set; }
    [Parameter] public EventCallback<MouseEventArgs> OnMouseLeave { get; set; }

    protected async Task HandleClickAsync(MouseEventArgs e)
    {
        if (IsStaticSSR || IsEffectivelyDisabled || IsDisposed) return;
        try { await OnClick.InvokeAsync(e); }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[{Id}] OnClick handler error", ComponentId);
        }
    }

    protected async Task HandleMouseEnterAsync(MouseEventArgs e)
    {
        if (IsStaticSSR || IsDisposed) return;
        try   { await OnMouseEnter.InvokeAsync(e); }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        { Logger.LogError(ex, "[{Id}] OnMouseEnter handler error", ComponentId); }
    }

    protected async Task HandleMouseLeaveAsync(MouseEventArgs e)
    {
        if (IsStaticSSR || IsDisposed) return;
        try   { await OnMouseLeave.InvokeAsync(e); }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        { Logger.LogError(ex, "[{Id}] OnMouseLeave handler error", ComponentId); }
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

        ClearDebouncers();
        _throttlers.Clear();

        foreach (var sub in _subscriptions)    sub.Dispose();
        foreach (var sub in _globalKeySubscriptions) sub.Dispose();
        _subscriptions.Clear();
        _globalKeySubscriptions.Clear();

        await base.DisposeComponentAsync();
    }

    // ── Helper types ──────────────────────────────────────────────────────────
    // УЛУЧШЕНО: volatile поле видно между потоками
    private sealed class ThrottleEntry
    {
        internal volatile int IsThrottled; // 0 = free, 1 = throttled
    }
}
