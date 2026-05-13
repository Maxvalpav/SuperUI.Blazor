// SuperUI/Base/SgComponentBase.cs — ИСПРАВЛЕНО v6
// ✅ FIX CS1061: добавлен InvokeStateHasChangedAsync
// ✅ FIX: IsPrerendering определение через RendererInfo.Name
// ✅ FIX: IComponentRegistry → IComponentRegistry? (nullable)
// ✅ NET8/9/10: AssignedRenderMode используется

using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SuperUI.Base.Diagnostics;
using SuperUI.Base.Hooks;
using SuperUI.Base.Reactive;
using SuperUI.Base.Services;
using SuperUI.Base.Utilities;
using CssBuilder = SuperUI.Base.Utilities.SgCssBuilder;

namespace SuperUI.Base;

/// <summary>
/// Интерфейс для lifecycle-регистрации компонентов (добавить/убрать из дерева).
/// ПЕРЕИМЕНОВАН из IComponentRegistry → ISgComponentLifetimeRegistry для устранения CS0104.
/// </summary>
public interface ISgComponentLifetimeRegistry
{
    void Register(ISgComponent component);
    void Unregister(ISgComponent component);
}

public abstract class SgComponentBase : ComponentBase, ISgComponent, IAsyncDisposable
{
    // ── Инъекции ──────────────────────────────────────────────────────────
    [Inject] protected ILoggerFactory LoggerFactory { get; set; } = null!;
    protected ILogger? _logger;
    protected ILogger Logger => _logger ??= LoggerFactory.CreateLogger(GetType());

    [Inject] protected IComponentOptionsService? OptionsService { get; set; }
    [Inject] protected IServiceProvider ServiceProvider { get; set; } = null!;

    // ✅ FIX CS0104: используем ISgComponentLifetimeRegistry (не IComponentRegistry)
    [Inject] protected ISgComponentLifetimeRegistry? ComponentLifetimeRegistry { get; set; }

    // ✅ FIX CS0104: используем ISgComponentTypeRegistry из Services namespace
    [Inject] protected Services.ISgComponentTypeRegistry? ComponentTypeRegistry { get; set; }

    [Inject] protected TimeProvider TimeProvider { get; set; } = TimeProvider.System;
    [Inject] protected PersistentComponentState? PersistentComponentState { get; set; }

    protected PersistentComponentState? PersistentState => PersistentComponentState;

    // ── Каскадные параметры ───────────────────────────────────────────────
    [CascadingParameter] protected SgThemeContext? ThemeContext { get; set; }
    [CascadingParameter] protected SgConfigContext? ConfigContext { get; set; }
    [CascadingParameter] protected IComponentRenderMode? RenderMode { get; set; }

    // ── Render mode helpers ───────────────────────────────────────────────
    protected bool IsStaticSSR => RenderMode is null;

    protected bool IsInteractive =>
#if NET9_0_OR_GREATER
        RendererInfo.IsInteractive;
#else
        RenderMode is not null;
#endif

    /// <summary>
    /// Корректное определение prerendering для .NET 8+.
    /// .NET 8: первый рендер + Static SSR.
    /// .NET 9+: !RendererInfo.IsInteractive.
    /// .NET 10+: то же что .NET 9.
    /// </summary>
    public bool IsPrerendering => _isPrerendering;

    protected bool IsInteractiveServer => RenderMode is InteractiveServerRenderMode;
    protected bool IsInteractiveWebAssembly => RenderMode is InteractiveWebAssemblyRenderMode;
    protected bool IsInteractiveAuto => RenderMode is InteractiveAutoRenderMode;

    [CascadingParameter(Name = "IsStreamingRendering")]
    protected bool IsStreamingRendering { get; set; }

    // ── Параметры ─────────────────────────────────────────────────────────
    [Parameter] public string? Class { get; set; }
    [Parameter] public string? Style { get; set; }
    [Parameter] public bool Visible { get; set; } = true;
    [Parameter] public string? Id { get; set; }
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    // ── Публичные свойства ────────────────────────────────────────────────
    public string ComponentId { get; }
    public SgRenderMode CurrentRenderMode => RenderMode switch
    {
        InteractiveServerRenderMode => SgRenderMode.InteractiveServer,
        InteractiveWebAssemblyRenderMode => SgRenderMode.InteractiveWebAssembly,
        InteractiveAutoRenderMode => SgRenderMode.InteractiveAuto,
        null => SgRenderMode.StaticSSR,
        _ => SgRenderMode.Unknown
    };

    public bool IsInitialized => _isInitialized;
    public bool HasRendered => !_isFirstRender;
    public int RenderCount => _renderCount;
    public string? CssClass => Css().NullIfEmpty();
    public string? CssStyle => CreateStyle().NullIfEmpty();
    public bool IsDisposed => Volatile.Read(ref _disposed) == 1;

    protected static bool IsBrowser => OperatingSystem.IsBrowser();
    protected static bool IsServer => !OperatingSystem.IsBrowser();

    protected internal virtual CancellationToken ComponentToken => _cts.Token;
    protected CancellationToken LifecycleToken => ComponentToken;
    protected string EffectiveId => !string.IsNullOrWhiteSpace(Id) ? Id! : ComponentId;

    // ── Renderer info (для .NET 9+) ──────────────────────────────────────
#if NET9_0_OR_GREATER
    /// <summary>Имя текущего рендерера: "Static", "Server", "WebAssembly".</summary>
    protected string RendererName => RendererInfo.Name ?? "Unknown";

    /// <summary>Целевой render mode компонента после prerendering.</summary>
    protected IComponentRenderMode? AssignedRenderMode => ComponentBase.AssignedRenderMode;
#endif

    // ── Приватные поля ────────────────────────────────────────────────────
    private int _disposed;
    private int _previousVisible = 1;
    private bool _isFirstRender = true;
    private bool _isPrerendering;
    private bool _isInitialized;
    private int _renderCount;
    private readonly HashSet<IComponentHook> _hooksSet = new(ReferenceEqualityComparer.Instance);
    private readonly List<IComponentHook> _hooks = [];
    private readonly SgCompositeDisposable _disposables = new();
    private ComponentSignalTracker? _signalBatcher;
    protected List<IDisposable>? _reactiveDisposables;
    private readonly CancellationTokenSource _cts = new();
    private readonly object _ariaCacheLock = new();
    private IReadOnlyDictionary<string, object>? _ariaCache;
    private int _ariaCacheGeneration = -2;
    private int _ariaGeneration;
    private IReadOnlyDictionary<string, object>? _filteredAttrsCache;
    private volatile int _filteredAttrsCacheGen = -1;
    private SgParameterSnapshot? _lastParametersSnapshot;

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

    // ═══════════════════════════════════════════════════════════════════
    // Hooks
    // ═══════════════════════════════════════════════════════════════════

    protected void AddHook(IComponentHook hook)
    {
        ArgumentNullException.ThrowIfNull(hook);
        lock (_hooksSet)
        {
            if (_hooksSet.Add(hook))
                _hooks.Add(hook);
        }
    }

    public void Subscribe(IComponentHook hook)
    {
        if (hook == null) return;
        AddHook(hook);
    }

    public void Unsubscribe(IComponentHook hook)
    {
        if (hook == null) return;
        lock (_hooksSet)
        {
            if (_hooksSet.Remove(hook))
                _hooks.Remove(hook);
        }
    }

    private void RunHooks(Action<IComponentHook> action)
    {
        IComponentHook[] snapshot;
        lock (_hooksSet) { snapshot = _hooks.ToArray(); }

        foreach (var hook in snapshot)
        {
            try { action(hook); }
            catch (Exception ex) { Logger.LogError(ex, "Hook {Hook} failed", hook.GetType().Name); }
        }
    }

    private async Task RunHooksAsync(Func<IComponentHook, Task> action)
    {
        IComponentHook[] snapshot;
        lock (_hooksSet) { snapshot = _hooks.ToArray(); }

        foreach (var hook in snapshot)
        {
            try { await action(hook); }
            catch (Exception ex) { Logger.LogError(ex, "Hook {Hook} failed async", hook.GetType().Name); }
        }
    }

    protected void AddDisposable(IDisposable disposable) => _disposables.Add(disposable);
    protected void AddDisposable(IAsyncDisposable disposable) => _disposables.Add(disposable);

    // ═══════════════════════════════════════════════════════════════════
    // ShouldRender
    // ═══════════════════════════════════════════════════════════════════

    protected override bool ShouldRender()
    {
        bool visible = Visible;
        int prev = Interlocked.Exchange(ref _previousVisible, visible ? 1 : 0);
        bool wasVisible = prev == 1;

        if (!visible && wasVisible) return true;
        if (!visible) return false;

        IComponentHook[] snapshot;
        lock (_hooksSet) { snapshot = _hooks.ToArray(); }

        foreach (var hook in snapshot)
            if (hook is IRenderHook rh && !rh.ShouldRender(this))
                return false;

        return true;
    }

    // ═══════════════════════════════════════════════════════════════════
    // SetParametersAsync
    // ═══════════════════════════════════════════════════════════════════

    protected virtual bool ShouldSetParameters(ParameterView parameters)
    {
        if (!_lastParametersSnapshot.HasValue) return true;
        var current = new SgParameterSnapshot(parameters);
        return !_lastParametersSnapshot.Value.Equals(current);
    }

    public override async Task SetParametersAsync(ParameterView parameters)
    {
        Interlocked.Increment(ref _ariaGeneration);
        Volatile.Write(ref _filteredAttrsCacheGen, -1);

        if (_lastParametersSnapshot.HasValue && !ShouldSetParameters(parameters))
            return;

#if NET9_0_OR_GREATER
        _isPrerendering = !RendererInfo.IsInteractive;
#else
        _isPrerendering = _isFirstRender && IsStaticSSR;
#endif

        _lastParametersSnapshot = new SgParameterSnapshot(parameters);
        await OnParametersChangedAsync(parameters);

        // ✅ FIX: передаём оригинальные parameters, не ParameterView.Empty!
        // ParameterView.Empty ломает каскадные параметры и SSR
        await base.SetParametersAsync(parameters);
    }

    protected virtual ValueTask OnParametersChangedAsync(ParameterView parameters) => ValueTask.CompletedTask;

    // ═══════════════════════════════════════════════════════════════════
    // Lifecycle
    // ═══════════════════════════════════════════════════════════════════

    protected override void OnInitialized()
    {
        base.OnInitialized();
        LogLifecycle(nameof(OnInitialized));
        RunHooks(h => h.OnInitialized(this));
    }

    protected override async Task OnInitializedAsync()
    {
        LogLifecycle(nameof(OnInitializedAsync));
        await base.OnInitializedAsync();
        await OnInitializeAsync();
        await RunHooksAsync(h => h.OnInitializedAsync(this));
        _isInitialized = true;
    }

    protected virtual Task OnInitializeAsync() => Task.CompletedTask;

    protected override void OnParametersSet()
    {
#if DEBUG
        _diagnostics.ParameterChangeCount++;
#endif
        RunHooks(h => h.OnParametersSet(this));
        base.OnParametersSet();
    }

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();
        await RunHooksAsync(h => h.OnParametersSetAsync(this));
    }

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            _isPrerendering = false;
            _isFirstRender = false;
        }

        _renderCount++;

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

        RunHooks(h => h.OnAfterRender(this, firstRender));
        base.OnAfterRender(firstRender);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
#if DEBUG
        _renderStartTick = Stopwatch.GetTimestamp();
#endif
        await base.OnAfterRenderAsync(firstRender);
        if (firstRender)
            await OnFirstRenderAsync();
        await RunHooksAsync(h => h.OnAfterRenderAsync(this, firstRender));
    }

    protected virtual Task OnFirstRenderAsync() => Task.CompletedTask;

    // ═══════════════════════════════════════════════════════════════════
    // Refresh / StateHasChanged helpers
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// ✅ FIX CS1061: асинхронно вызывает StateHasChanged через InvokeAsync.
    /// Безопасен при вызове из не-UI потоков (Server: circuit; WASM: UI thread).
    /// Используется планировщиками рендеринга и трекерами сигналов.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Task InvokeStateHasChangedAsync()
    {
        if (IsDisposed) return Task.CompletedTask;
        try
        {
            return InvokeAsync(StateHasChanged);
        }
        catch (ObjectDisposedException)
        {
            return Task.CompletedTask;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Task RefreshAsync()
    {
        if (IsDisposed || IsStaticSSR) return Task.CompletedTask;
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

    protected void NotifyStateChanged()
    {
        if (IsDisposed) return;
        try { InvokeAsync(StateHasChanged); }
        catch (ObjectDisposedException) { }
    }

    protected async Task NotifyStateChangedAsync()
    {
        if (IsDisposed) return;
        try { await InvokeAsync(StateHasChanged); }
        catch (ObjectDisposedException) { }
    }

    public void RequestRender()
    {
        if (IsDisposed) return;
        var batcher = Volatile.Read(ref _signalBatcher);
        if (batcher is not null)
            batcher.ScheduleRender();
        else
            _ = InvokeAsync(StateHasChanged);
    }

    [Obsolete("Use RequestRender().", false)]
    public void ForceStateHasChanged()
    {
        if (IsDisposed) return;
        _ = InvokeAsync(StateHasChanged);
    }

    public void OnRenderModeChanged(SgRenderMode newMode) { }

    // ═══════════════════════════════════════════════════════════════════
    // CSS / Style
    // ═══════════════════════════════════════════════════════════════════

#if DEBUG
    public ComponentDiagnostics Diagnostics => _diagnostics;
#endif

    protected CssBuilder Css(string? baseClass = null)
        => new(baseClass ?? GetDefaultCssClass());

    protected StyleBuilder CreateStyle(string? baseStyle = null)
        => StyleBuilder.Default().AddUserStyle(baseStyle);

    protected virtual string? GetDefaultCssClass() => null;

    // ═══════════════════════════════════════════════════════════════════
    // Streaming Rendering
    // ═══════════════════════════════════════════════════════════════════

    protected virtual RenderFragment DefaultStreamingPlaceholder =>
        builder => builder.AddMarkupContent(0, "&#8203;");

    protected RenderFragment StreamingPlaceholder(RenderFragment? placeholder = null) =>
        builder =>
        {
            if (IsStreamingRendering && IsStaticSSR)
                builder.AddContent(0, placeholder ?? DefaultStreamingPlaceholder);
        };

    // ═══════════════════════════════════════════════════════════════════
    // ARIA
    // ═══════════════════════════════════════════════════════════════════

    protected IReadOnlyDictionary<string, object>? AdditionalAttributesFiltered
    {
        get
        {
            var gen = Volatile.Read(ref _ariaGeneration);
            if (_filteredAttrsCache is not null && _filteredAttrsCacheGen == gen)
                return _filteredAttrsCache;

            if (IsBrowser) return RefreshFilteredAttrsCache(gen);
            lock (_ariaCacheLock) return RefreshFilteredAttrsCache(gen);
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

        var filtered = new Dictionary<string, object>(AdditionalAttributes.Count);
        foreach (var kv in AdditionalAttributes)
            if (!kv.Key.Equals("class", StringComparison.OrdinalIgnoreCase) &&
                !kv.Key.Equals("style", StringComparison.OrdinalIgnoreCase))
                filtered[kv.Key] = kv.Value;

        _filteredAttrsCache = filtered.Count == 0 ? null : filtered;
        _filteredAttrsCacheGen = gen;
        return _filteredAttrsCache;
    }

    protected virtual IReadOnlyDictionary<string, object> BuildAriaAttributes()
    {
        var gen = Volatile.Read(ref _ariaGeneration);
        if (IsBrowser) return BuildAriaAttributesCore(gen);
        lock (_ariaCacheLock) return BuildAriaAttributesCore(gen);
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

    // ═══════════════════════════════════════════════════════════════════
    // Service helpers
    // ═══════════════════════════════════════════════════════════════════

    protected bool TryTakePersistedState<T>(string key, out T? value)
    {
        if (PersistentComponentState is not null &&
            PersistentComponentState.TryTakeFromJson<T>(key, out var result))
        {
            value = result;
            return true;
        }

        value = default;
        return false;
    }

    protected void PersistState<T>(string key, T value) =>
        PersistentComponentState?.PersistAsJson(key, value);

    protected T? TryGetService<T>() where T : class =>
        ServiceProvider.GetService<T>();

    protected T TryGetRequiredService<T>() where T : class =>
        ServiceProvider.GetService<T>()?? throw new InvalidOperationException(
            $"Service {typeof(T).Name} is not registered. Call builder.Services.AddSuperUI().");

    protected void ThrowIfDisposed([CallerMemberName] string? caller = null) =>
        ObjectDisposedException.ThrowIf(IsDisposed, $"{ComponentId}.{caller}");

    // ═══════════════════════════════════════════════════════════════════
    // Context helpers
    // ═══════════════════════════════════════════════════════════════════

    protected Task IfBrowserAsync(Func<Task> action) => IsBrowser ? action() : Task.CompletedTask;
    protected Task IfServerAsync(Func<Task> action) => IsServer ? action() : Task.CompletedTask;
    protected Task IfInteractiveAsync(Func<Task> action) => IsInteractive ? action() : Task.CompletedTask;

    // ═══════════════════════════════════════════════════════════════════
    // Logging
    // ═══════════════════════════════════════════════════════════════════

    [Conditional("DEBUG")]
    private void LogLifecycle(string method)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("[{ComponentId}] {Method}", ComponentId, method);
    }

    // ═══════════════════════════════════════════════════════════════════
    // Dispose
    // ═══════════════════════════════════════════════════════════════════

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1) return;

        try { _cts.Cancel(); } catch { }
        _cts.Dispose();
        _disposables.Dispose();
        DisposeReactiveResources();
        DisposeHooks(async: false);

        var batcher = Interlocked.Exchange(ref _signalBatcher, null);
        batcher?.Dispose();

        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1) return;

        LogLifecycle(nameof(DisposeAsync));

        try { _cts.Cancel(); } catch { }
        _cts.Dispose();

        await DisposeAsyncCore();
        await _disposables.DisposeAsync();
        DisposeReactiveResources();
        await DisposeHooksAsync();
        await DisposeComponentAsync();

        var batcher = Interlocked.Exchange(ref _signalBatcher, null);
        batcher?.Dispose();

        GC.SuppressFinalize(this);
    }

    private void DisposeReactiveResources()
    {
        if (_reactiveDisposables is null) return;

        foreach (var rd in _reactiveDisposables)
        {
            try { rd.Dispose(); }
            catch (Exception ex) { Logger.LogWarning(ex, "[{Id}] Reactive dispose error", ComponentId); }
        }

        _reactiveDisposables.Clear();
    }

    private void DisposeHooks(bool async)
    {
        lock (_hooksSet)
        {
            foreach (var hook in _hooks)
            {
                try { if (hook is IDisposable d) d.Dispose(); }
                catch (Exception ex) { Logger.LogWarning(ex, "[{Id}] Hook dispose error", ComponentId); }
            }

            _hooks.Clear();
            _hooksSet.Clear();
        }
    }

    private async Task DisposeHooksAsync()
    {
        IComponentHook[] snapshot;
        lock (_hooksSet)
        {
            snapshot = _hooks.ToArray();
            _hooks.Clear();
            _hooksSet.Clear();
        }

        foreach (var hook in snapshot)
        {
            try
            {
                if (hook is IAsyncDisposable ad) await ad.DisposeAsync();
                else if (hook is IDisposable d) d.Dispose();
            }
            catch (Exception ex) { Logger.LogWarning(ex, "[{Id}] Hook dispose error", ComponentId); }
        }
    }

    protected virtual void Dispose(bool disposing) { }
    protected virtual ValueTask DisposeAsyncCore() => ValueTask.CompletedTask;
    protected virtual ValueTask DisposeComponentAsync() => ValueTask.CompletedTask;
}
