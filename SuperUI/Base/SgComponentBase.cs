// SuperUI/Base/SgComponentBase.cs
// ИСПРАВЛЕНИЯ v3:
// ✅ CS0019 FIX: RenderPriority используется из SuperUI.Base.Reactive
// ✅ BUG-1: ComponentToken — единая точка, SgJsComponentBase использует override
// ✅ BUG: ISgComponent явно реализован
// ✅ PERF-1: AdditionalAttributesFiltered — lock только на Server
// ✅ UX: ThrowIfDisposed работает и без CallerMemberName
// ✅ НОВОЕ: SetParametersAsync с ShouldSetParameters virtual hook
// ✅ НОВОЕ: OnAfterFirstRenderAsync (convenience alias)
// ✅ НОВОЕ: Blazor United — RenderMode cascading parameter, IsStaticSSR, IsInteractive
// ✅ НОВОЕ: ComponentRegistry auto-registration

using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Components;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SuperUI.Base.Diagnostics;
using SuperUI.Base.Hooks;
using SuperUI.Base.Reactive;
using SuperUI.Base.Services;
using SuperUI.Base.Utilities;
using CssBuilder = SuperUI.Base.Utilities.SgCssBuilder;

namespace SuperUI.Base;

public abstract class SgComponentBase : ComponentBase, ISgComponent, IAsyncDisposable
{
    // ── Инъекции ────────────────────────────────────────────────────────────────
    [Inject] protected ILogger<SgComponentBase> Logger { get; set; } = null!;
    [Inject] protected IComponentOptionsService? OptionsService { get; set; }
    [Inject] protected IServiceProvider ServiceProvider { get; set; } = null!;
    [Inject] protected IComponentRegistry? ComponentRegistry { get; set; }

    // ── Каскадные параметры ─────────────────────────────────────────────────────
    [CascadingParameter] protected SgThemeContext? ThemeContext { get; set; }
    [CascadingParameter] protected SgConfigContext? ConfigContext { get; set; }

    /// <summary>
    /// Текущий режим рендеринга компонента.
    /// null = Static SSR (нет интерактивности).
    /// </summary>
    [CascadingParameter] protected IComponentRenderMode? RenderMode { get; set; }

    /// <summary>
    /// true — компонент в статическом SSR (нет SignalR/интерактивности).
    /// В этом режиме события (onclick, onchange) не работают.
    /// </summary>
    protected bool IsStaticSSR => RenderMode is null;

    /// <summary>
    /// true — доступна интерактивность (InteractiveServer/WebAssembly/Auto).
    /// </summary>
    protected bool IsInteractive => RenderMode is not null;

    /// <summary>
    /// Режим InteractiveServer.
    /// </summary>
    protected bool IsInteractiveServer => RenderMode is InteractiveServerRenderMode;

    /// <summary>InteractiveWebAssembly режим.</summary>
    protected bool IsInteractiveWebAssembly => RenderMode is InteractiveWebAssemblyRenderMode;

    /// <summary>InteractiveAuto режим.</summary>
    protected bool IsInteractiveAuto => RenderMode is InteractiveAutoRenderMode;

    // ── Параметры ───────────────────────────────────────────────────────────────
    [Parameter] public string? Class { get; set; }
    [Parameter] public string? Style { get; set; }
    [Parameter] public bool Visible { get; set; } = true;
    [Parameter] public string? Id { get; set; }
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    // ── Публичные свойства ──────────────────────────────────────────────────────
    public string ComponentId { get; }
    protected string EffectiveId => !string.IsNullOrWhiteSpace(Id) ? Id! : ComponentId;
    public bool IsDisposed => Volatile.Read(ref _disposed) == 1;
    protected static bool IsBrowser => OperatingSystem.IsBrowser();
    protected static bool IsServer => !OperatingSystem.IsBrowser();

    /// BUG-1 FIX: единая точка ComponentToken, переопределяется SgJsComponentBase
    protected internal virtual CancellationToken ComponentToken => _cts.Token;

    // ── PERF-1: AdditionalAttributesFiltered без lock на WASM ──────────────────
    protected IReadOnlyDictionary<string, object>? AdditionalAttributesFiltered
    {
        get
        {
            var gen = Volatile.Read(ref _ariaGeneration);
            if (_filteredAttrsCache is not null && _filteredAttrsCacheGen == gen)
                return _filteredAttrsCache;

            // На WASM однопоточный runtime — lock не нужен
            if (IsBrowser)
                return RefreshFilteredAttrsCache(gen);

            // На Server — полная синхронизация
            lock (_ariaCacheLock)
            {
                if (_filteredAttrsCache is not null && _filteredAttrsCacheGen == gen)
                    return _filteredAttrsCache;
                return RefreshFilteredAttrsCache(gen);
            }
        }
    }

    private IReadOnlyDictionary<string, object>? RefreshFilteredAttrsCache(int gen)
    {
        if (AdditionalAttributes is null)
        {
            _filteredAttrsCache = null;
            _filteredAttrsCacheGen = gen;
            return null;
        }
        var filtered = AdditionalAttributes
            .Where(kv =>
                !kv.Key.Equals("class", StringComparison.OrdinalIgnoreCase) &&
                !kv.Key.Equals("style", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(kv => kv.Key, kv => kv.Value);
        _filteredAttrsCache = filtered.Count == 0 ? null : filtered;
        _filteredAttrsCacheGen = gen;
        return _filteredAttrsCache;
    }

    private int _disposed;
    private int _previousVisible = 1;
    private readonly List<IComponentHook> _hooks = [];
    private ComponentSignalTracker? _signalBatcher;
    private List<IDisposable>? _reactiveDisposables;
    private readonly CancellationTokenSource _cts = new();

    private readonly object _ariaCacheLock = new();
    private IReadOnlyDictionary<string, object>? _ariaCache;
    private int _ariaCacheGeneration = -2;
    private int _ariaGeneration;
    private volatile IReadOnlyDictionary<string, object>? _filteredAttrsCache;
    private volatile int _filteredAttrsCacheGen = -1;

    // ── ShouldSetParameters: кэш снимка параметров ───────────────────────────────
    private SgParameterSnapshot<SgComponentBase>? _lastParametersSnapshot;

#if DEBUG
    private readonly ComponentDiagnostics _diagnostics;
    private long _renderStartTick;
#endif

    protected SgComponentBase()
    {
        ComponentId = ComponentIdGenerator.Next(ComponentPrefix);
        _signalBatcher = new ComponentSignalTracker(this);
#if DEBUG
        _diagnostics = new ComponentDiagnostics { ComponentId = ComponentId };
#endif
    }

    protected virtual string ComponentPrefix => "cmp";

    // ── Hooks ───────────────────────────────────────────────────────────────────
    protected void AddHook(IComponentHook hook)
    {
        ArgumentNullException.ThrowIfNull(hook);
        _hooks.Add(hook);
    }

    // ── Reactive ────────────────────────────────────────────────────────────────
    protected SgSignal<TValue> CreateSignal<TValue>(
        TValue initial, IEqualityComparer<TValue>? comparer = null)
    {
        var signal = comparer is null
            ? new SgSignal<TValue>(initial)
            : new SgSignal<TValue>(initial, comparer);
        signal.Subscribe(this);
        (_reactiveDisposables ??= []).Add(signal);
        return signal;
    }

    protected SgEffect RegisterEffect(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        var effect = new SgEffect(action);
        effect.Subscribe(this);
        (_reactiveDisposables ??= []).Add(effect);
        return effect;
    }

    protected SgEffect RegisterEffect(Func<Task> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        var effect = new SgEffect(action);
        effect.Subscribe(this);
        (_reactiveDisposables ??= []).Add(effect);
        return effect;
    }

    protected SgComputed<T> RegisterComputed<T>(Func<T> compute)
    {
        ArgumentNullException.ThrowIfNull(compute);
        var computed = new SgComputed<T>(compute);
        computed.Subscribe(this);
        (_reactiveDisposables ??= []).Add(computed);
        return computed;
    }

    protected void RegisterEffectInternal(IDisposable disposable)
        => (_reactiveDisposables ??= []).Add(disposable);

    // ── ShouldRender ────────────────────────────────────────────────────────────
    protected override bool ShouldRender()
    {
        bool visible = Visible;
        int prev = Interlocked.Exchange(ref _previousVisible, visible ? 1 : 0);
        bool wasVisible = prev == 1;
        if (!visible && wasVisible) return true;
        if (!visible) return false;
        foreach (var hook in _hooks)
            if (hook is IRenderHook rh && !rh.ShouldRender(this))
                return false;
        return true;
    }

    // ── ShouldSetParameters — пропуск обработки если параметры не изменились ──
    /// <summary>
    /// Возвращает false если параметры структурно не изменились.
    /// Базовая реализация использует SgParameterSnapshot для сравнения.
    /// Переопределите для кастомной логики.
    /// Примечание: snapshot обновляется в SetParametersAsync, а не здесь.
    /// </summary>
    protected virtual bool ShouldSetParameters(ParameterView parameters)
    {
        if (!_lastParametersSnapshot.HasValue) return true;
        var current = new SgParameterSnapshot<SgComponentBase>(parameters);
        return !_lastParametersSnapshot.Value.Equals(current);
    }

    // ── RequestRender ───────────────────────────────────────────────────────────
    public void RequestRender()
    {
        if (IsDisposed) return;
        var batcher = Volatile.Read(ref _signalBatcher);
        if (batcher is not null)
            batcher.ScheduleRender();
        else
            _ = InvokeAsync(StateHasChanged);
    }

    [Obsolete("Use RequestRender() for external callers.", false)]
    public void ForceStateHasChanged()
    {
        if (IsDisposed) return;
        _ = InvokeAsync(StateHasChanged);
    }

    internal Task InvokeStateHasChangedAsync()
    {
        if (IsDisposed) return Task.CompletedTask;
        return InvokeAsync(StateHasChanged);
    }

    // ── SetParametersAsync ──────────────────────────────────────────────────────
    public override async Task SetParametersAsync(ParameterView parameters)
    {
        Interlocked.Increment(ref _ariaGeneration);
        Volatile.Write(ref _filteredAttrsCacheGen, -1);

        // BUG-FIX: первый вызов (snapshot == null) — всегда обрабатываем,
        // иначе компонент никогда не получит свои первые параметры.
        if (_lastParametersSnapshot.HasValue && !ShouldSetParameters(parameters))
        {
#if DEBUG
            _diagnostics.ParameterChangeCount++;
#endif
            return;
        }

        // Обновляем snapshot после проверки
        _lastParametersSnapshot = new SgParameterSnapshot<SgComponentBase>(parameters);

        await base.SetParametersAsync(parameters);
    }

    // ── Lifecycle ───────────────────────────────────────────────────────────────
    protected override void OnInitialized()
    {
        base.OnInitialized();
        LogLifecycle(nameof(OnInitialized));
        ComponentRegistry?.Register(this);
        foreach (var hook in _hooks) hook.OnInitialized(this);
    }

    protected override async Task OnInitializedAsync()
    {
        LogLifecycle(nameof(OnInitializedAsync));
        await base.OnInitializedAsync();
        foreach (var hook in _hooks)
            if (hook is IAsyncComponentHook ah)
                await ah.OnInitializedAsync(this);
    }

    protected override void OnParametersSet()
    {
#if DEBUG
        _diagnostics.ParameterChangeCount++;
#endif
        foreach (var hook in _hooks) hook.OnParametersSet(this);
        base.OnParametersSet();
    }

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();
        Exception? hookException = null;
        foreach (var hook in _hooks)
        {
            if (hook is IAsyncComponentHook ah)
            {
                try { await ah.OnParametersSetAsync(this); }
                catch (Exception ex) { hookException = ex; }
            }
        }
        await OnParametersChangedAsync();
        if (hookException is not null)
            Logger.LogError(hookException, "[{Id}] Hook.OnParametersSetAsync threw", ComponentId);
    }

    protected virtual Task OnParametersChangedAsync() => Task.CompletedTask;

    protected override void OnAfterRender(bool firstRender)
    {
#if DEBUG
        if (_renderStartTick > 0)
        {
            var elapsed = Stopwatch.GetElapsedTime(_renderStartTick).TotalMilliseconds;
            _diagnostics.RenderCount++;
            _diagnostics.LastRenderMs = elapsed;
            if (elapsed > _diagnostics.MaxRenderMs) _diagnostics.MaxRenderMs = elapsed;
            _diagnostics.AverageRenderMs =
                (_diagnostics.AverageRenderMs * (_diagnostics.RenderCount - 1) + elapsed)
                / _diagnostics.RenderCount;
            _renderStartTick = 0;
        }
#endif
        foreach (var hook in _hooks) hook.OnAfterRender(this, firstRender);
        base.OnAfterRender(firstRender);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
#if DEBUG
        _renderStartTick = Stopwatch.GetTimestamp();
#endif
        await base.OnAfterRenderAsync(firstRender);
        if (firstRender)
        {
            await OnFirstRenderAsync();
            foreach (var hook in _hooks)
                if (hook is IAsyncComponentHook ah)
                    await ah.OnFirstRenderAsync(this);
        }
        foreach (var hook in _hooks)
            if (hook is IAsyncComponentHook ah)
                await ah.OnAfterRenderAsync(this, firstRender);
    }

    /// <summary>
    /// Вызывается после первого рендера. Переопределите для startup-логики.
    /// </summary>
    protected virtual Task OnFirstRenderAsync() => Task.CompletedTask;

    /// <summary>
    /// Convenience alias для OnFirstRenderAsync (более явное имя).
    /// </summary>
    protected Task OnAfterFirstRenderAsync() => OnFirstRenderAsync();

    // ── CSS / Style ─────────────────────────────────────────────────────────────
#if DEBUG
    public ComponentDiagnostics Diagnostics => _diagnostics;
#endif

    protected CssBuilder Css(string? baseClass = null) =>
        new(baseClass ?? GetDefaultCssClass());

    protected StyleBuilder CreateStyle(string? baseStyle = null) =>
        new(baseStyle);

    protected virtual string? GetDefaultCssClass() => null;

    // ── ARIA ────────────────────────────────────────────────────────────────────
    protected virtual IReadOnlyDictionary<string, object> BuildAriaAttributes()
    {
        var currentGeneration = Volatile.Read(ref _ariaGeneration);
        // PERF: lock только на Server
        if (IsBrowser)
            return BuildAriaAttributesCore(currentGeneration);
        lock (_ariaCacheLock)
            return BuildAriaAttributesCore(currentGeneration);
    }

    private IReadOnlyDictionary<string, object> BuildAriaAttributesCore(int gen)
    {
        if (_ariaCache is not null && _ariaCacheGeneration == gen)
            return _ariaCache;
        var attrs = new Dictionary<string, object>(4, StringComparer.Ordinal);
        if (AdditionalAttributes is not null)
            foreach (var kvp in AdditionalAttributes)
                if (IsAriaAttribute(kvp.Key))
                    attrs[kvp.Key] = kvp.Value;
        _ariaCache = attrs;
        _ariaCacheGeneration = gen;
        return attrs;
    }

    private static bool IsAriaAttribute(string key) =>
        key.StartsWith("aria-", StringComparison.OrdinalIgnoreCase) ||
        key.Equals("role", StringComparison.OrdinalIgnoreCase) ||
        key.Equals("tabindex", StringComparison.OrdinalIgnoreCase);

    // ── RefreshAsync ────────────────────────────────────────────────────────────
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Task RefreshAsync()
    {
        if (IsDisposed) return Task.CompletedTask;
        return InvokeAsync(StateHasChanged);
    }

    public Task RefreshAsync(Action action)
    {
        if (IsDisposed) return Task.CompletedTask;
        return InvokeAsync(() => { action(); StateHasChanged(); });
    }

    public Task RefreshAsync(Func<Task> action)
    {
        if (IsDisposed) return Task.CompletedTask;
        return InvokeAsync(async () => { await action(); StateHasChanged(); });
    }

    protected Task RefreshFromBackgroundAsync() =>
        IsDisposed ? Task.CompletedTask : InvokeAsync(StateHasChanged);

    protected Task SafeInvokeAsync(Func<Task> action)
    {
        if (IsDisposed) return Task.CompletedTask;
        return InvokeAsync(action);
    }

    protected Task SafeInvokeAsync(Action action)
    {
        if (IsDisposed) return Task.CompletedTask;
        return InvokeAsync(action);
    }

    protected Task<TResult> SafeInvokeAsync<TResult>(Func<TResult> func)
    {
        if (IsDisposed) return Task.FromResult(default(TResult)!);
        var tcs = new TaskCompletionSource<TResult>();
        InvokeAsync(async () =>
        {
            try { var result = func(); tcs.SetResult(result); }
            catch (Exception ex) { tcs.SetException(ex); }
        });
        return tcs.Task;
    }

    // ── Service helpers ─────────────────────────────────────────────────────────
    protected T? TryGetService<T>() where T : class => ServiceProvider.GetService<T>();
    protected T TryGetRequiredService<T>() where T : class =>
        ServiceProvider.GetService<T>() ?? throw new InvalidOperationException(
            $"Service {typeof(T).Name} is not registered. Call builder.Services.AddSuperUI() in Program.cs.");

    protected void ThrowIfDisposed([CallerMemberName] string? caller = null) =>
        ObjectDisposedException.ThrowIf(IsDisposed, $"{ComponentId}.{caller}");

    // ── Context helpers ─────────────────────────────────────────────────────────
    protected Task IfBrowserAsync(Func<Task> action) =>
        IsBrowser ? action() : Task.CompletedTask;
    protected Task IfServerAsync(Func<Task> action) =>
        IsServer ? action() : Task.CompletedTask;

    // ── Logging ─────────────────────────────────────────────────────────────────
    [Conditional("DEBUG")]
    private void LogLifecycle(string method)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("[{ComponentId}] {Method}", ComponentId, method);
    }

    // ── IAsyncDisposable ────────────────────────────────────────────────────────
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1) return;
        LogLifecycle(nameof(DisposeAsync));
        try { _cts.Cancel(); } catch { /* ignored */ }
        _cts.Dispose();
        if (_reactiveDisposables is not null)
        {
            foreach (var rd in _reactiveDisposables)
            {
                try { rd.Dispose(); }
                catch (Exception ex)
                { Logger.LogWarning(ex, "[{Id}] Reactive dispose error", ComponentId); }
            }
            _reactiveDisposables.Clear();
        }
        foreach (var hook in _hooks)
        {
            try
            {
                if (hook is IAsyncDisposable ad) await ad.DisposeAsync();
                else if (hook is IDisposable d) d.Dispose();
            }
            catch (Exception ex)
            { Logger.LogWarning(ex, "[{Id}] Hook dispose error", ComponentId); }
        }
        _hooks.Clear();
        await DisposeComponentAsync();
        GC.SuppressFinalize(this);
    }

    protected virtual ValueTask DisposeComponentAsync()
    {
        var batcher = Interlocked.Exchange(ref _signalBatcher, null);
        batcher?.Dispose();
        return ValueTask.CompletedTask;
    }
}
