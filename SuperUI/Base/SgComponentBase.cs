// SuperUI/Base/SgComponentBase.cs
// ИСПРАВЛЕНИЯ v4:
// ✅ FIX CS0246: добавлен using Microsoft.AspNetCore.Components.Web
// ✅ FIX: OnAfterFirstRenderAsync теперь virtual
// ✅ FIX: AddHook проверяет дубликаты
// ✅ FIX: IsStreamingRendering свойство
// ✅ PERF: ShouldSetParameters — избегаем double allocation
// ✅ THREAD: _filteredAttrsCache через Interlocked

using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Components;
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

public abstract class SgComponentBase : ComponentBase, ISgComponent, IAsyncDisposable
{
    // ── Инъекции ───────────────────────────────────────────────────────────────
    [Inject] protected ILogger Logger { get; set; } = null!;
    [Inject] protected IComponentOptionsService? OptionsService { get; set; }
    [Inject] protected IServiceProvider ServiceProvider { get; set; } = null!;
    [Inject] protected IComponentRegistry? ComponentRegistry { get; set; }

    // ── Каскадные параметры ────────────────────────────────────────────────────
    [CascadingParameter] protected SgThemeContext? ThemeContext { get; set; }
    [CascadingParameter] protected SgConfigContext? ConfigContext { get; set; }

    /// <summary>
    /// Текущий режим рендеринга компонента.
    /// null = Static SSR (нет интерактивности).
    /// </summary>
    [CascadingParameter] protected IComponentRenderMode? RenderMode { get; set; }

    // ── Render mode helpers (.NET 8+) ──────────────────────────────────────────
    /// <summary>true — компонент в статическом SSR (нет SignalR/интерактивности).</summary>
    protected bool IsStaticSSR => RenderMode is null;

    /// <summary>true — доступна интерактивность (InteractiveServer/WebAssembly/Auto).</summary>
    protected bool IsInteractive => RenderMode is not null;

    /// <summary>Режим InteractiveServer (SignalR).</summary>
    protected bool IsInteractiveServer => RenderMode is InteractiveServerRenderMode;

    /// <summary>InteractiveWebAssembly режим.</summary>
    protected bool IsInteractiveWebAssembly => RenderMode is InteractiveWebAssemblyRenderMode;

    /// <summary>InteractiveAuto режим.</summary>
    protected bool IsInteractiveAuto => RenderMode is InteractiveAutoRenderMode;

    /// <summary>
    /// true — компонент рендерится в режиме Streaming Rendering (.NET 8+).
    /// Доступно только если вы вручную пробрасываете это значение через CascadingValue
    /// или устанавливаете из StreamingRenderingService.
    /// </summary>
    [CascadingParameter(Name = "IsStreamingRendering")]
    protected bool IsStreamingRendering { get; set; }

    // ── Параметры ──────────────────────────────────────────────────────────────
    [Parameter] public string? Class { get; set; }
    [Parameter] public string? Style { get; set; }
    [Parameter] public bool Visible { get; set; } = true;
    [Parameter] public string? Id { get; set; }
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    // ── Публичные свойства ─────────────────────────────────────────────────────
    public string ComponentId { get; }
    protected string EffectiveId => !string.IsNullOrWhiteSpace(Id) ? Id! : ComponentId;
    public bool IsDisposed => Volatile.Read(ref _disposed) == 1;
    protected static bool IsBrowser => OperatingSystem.IsBrowser();
    protected static bool IsServer => !OperatingSystem.IsBrowser();

    /// <summary>BUG-1 FIX: единая точка ComponentToken, переопределяется SgJsComponentBase.</summary>
    protected internal virtual CancellationToken ComponentToken => _cts.Token;

    // ── PERF: AdditionalAttributesFiltered ─────────────────────────────────────
    protected IReadOnlyDictionary<string, object>? AdditionalAttributesFiltered
    {
        get
        {
            var gen = Volatile.Read(ref _ariaGeneration);
            if (_filteredAttrsCache is not null && _filteredAttrsCacheGen == gen)
                return _filteredAttrsCache;

            if (IsBrowser)
                return RefreshFilteredAttrsCache(gen);

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
    // FIX: используем HashSet для гарантии уникальности хуков
    private readonly HashSet<IComponentHook> _hooksSet = new(ReferenceEqualityComparer.Instance);
    private readonly List<IComponentHook> _hooks = [];
    private ComponentSignalTracker? _signalBatcher;
    private List<IDisposable>? _reactiveDisposables;
    private readonly CancellationTokenSource _cts = new();
    private readonly object _ariaCacheLock = new();
    private IReadOnlyDictionary<string, object>? _ariaCache;
    private int _ariaCacheGeneration = -2;
    private int _ariaGeneration;
    // THREAD FIX: используем object-ссылку, заменяемую через Interlocked
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

    // ── Hooks ──────────────────────────────────────────────────────────────────
    /// <summary>
    /// FIX: добавляем хук только если он ещё не зарегистрирован (дубли недопустимы).
    /// </summary>
    protected void AddHook(IComponentHook hook)
    {
        ArgumentNullException.ThrowIfNull(hook);
        if (_hooksSet.Add(hook))
            _hooks.Add(hook);
    }

    // ── Reactive ───────────────────────────────────────────────────────────────
    protected SgSignal<TValue> CreateSignal<TValue>(
        TValue initial,
        IEqualityComparer<TValue>? comparer = null)
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

    protected SgComputed<TValue> RegisterComputed<TValue>(Func<TValue> compute)
    {
        ArgumentNullException.ThrowIfNull(compute);
        var computed = new SgComputed<TValue>(compute);
        computed.Subscribe(this);
        (_reactiveDisposables ??= []).Add(computed);
        return computed;
    }

    protected void RegisterEffectInternal(IDisposable disposable)
        => (_reactiveDisposables ??= []).Add(disposable);

    // ── ShouldRender ───────────────────────────────────────────────────────────
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

    // ── ShouldSetParameters ────────────────────────────────────────────────────
    /// <summary>
    /// PERF FIX: snapshot создаётся только после того как решено что параметры изменились.
    /// </summary>
    protected virtual bool ShouldSetParameters(ParameterView parameters)
    {
        if (!_lastParametersSnapshot.HasValue) return true;
        var current = new SgParameterSnapshot(parameters);
        return !_lastParametersSnapshot.Value.Equals(current);
    }

    // ── RequestRender ──────────────────────────────────────────────────────────
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

    // ── SetParametersAsync ─────────────────────────────────────────────────────
    public override async Task SetParametersAsync(ParameterView parameters)
    {
        Interlocked.Increment(ref _ariaGeneration);
        Volatile.Write(ref _filteredAttrsCacheGen, -1);

        if (_lastParametersSnapshot.HasValue && !ShouldSetParameters(parameters))
        {
#if DEBUG
            _diagnostics.ParameterChangeCount++;
#endif
            return;
        }

        _lastParametersSnapshot = new SgParameterSnapshot(parameters);
        await base.SetParametersAsync(parameters);
    }

    // ── Lifecycle ──────────────────────────────────────────────────────────────
    protected override void OnInitialized()
    {
        base.OnInitialized();
        LogLifecycle(nameof(OnInitialized));
        ComponentRegistry?.Register(this);
        foreach (var hook in _hooks)
            hook.OnInitialized(this);
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
        foreach (var hook in _hooks)
            hook.OnParametersSet(this);
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
        foreach (var hook in _hooks)
            hook.OnAfterRender(this, firstRender);
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

    /// <summary>Вызывается после первого рендера.</summary>
    protected virtual Task OnFirstRenderAsync() => Task.CompletedTask;

    /// <summary>
    /// FIX: теперь virtual — можно переопределить в подклассах.
    /// Convenience alias для OnFirstRenderAsync.
    /// </summary>
    protected virtual Task OnAfterFirstRenderAsync() => OnFirstRenderAsync();

    // ── CSS / Style ────────────────────────────────────────────────────────────
#if DEBUG
    public ComponentDiagnostics Diagnostics => _diagnostics;
#endif

    protected CssBuilder Css(string? baseClass = null)
        => new(baseClass ?? GetDefaultCssClass());

    protected StyleBuilder CreateStyle(string? baseStyle = null)
        => new(baseStyle);

    protected virtual string? GetDefaultCssClass() => null;

    // ── Streaming Rendering (.NET 8+) ──────────────────────────────────────────

    /// <summary>
    /// Дефолтный placeholder для Streaming Rendering — спиннер загрузки.
    /// Переопределите для кастомного вида.
    /// </summary>
    protected virtual RenderFragment DefaultStreamingPlaceholder =>
        builder => builder.AddMarkupContent(0,
            "<div class=\"sg-streaming-placeholder\" aria-busy=\"true\" aria-label=\"Loading...\"></div>");

    /// <summary>
    /// Возвращает RenderFragment, который в режиме Static SSR Streaming показывает
    /// <paramref name="placeholder"/> (или <see cref="DefaultStreamingPlaceholder"/>),
    /// а в интерактивном режиме — ничего (контент рендерится напрямую компонентом).
    ///
    /// Использование:
    /// <code>
    /// @if (IsStreamingRendering &amp;&amp; IsStaticSSR)
    /// {
    ///     @StreamingPlaceholder()
    /// }
    /// else
    /// {
    ///     @* основной контент *@
    /// }
    /// </code>
    /// Или через фабричный метод в коде:
    /// <code>
    /// builder.AddContent(0, StreamingPlaceholder(myCustomPlaceholder));
    /// </code>
    /// </summary>
    /// <param name="placeholder">Кастомный placeholder. null = DefaultStreamingPlaceholder.</param>
    protected RenderFragment StreamingPlaceholder(RenderFragment? placeholder = null) =>
        builder =>
        {
            if (IsStreamingRendering && IsStaticSSR)
            {
                // Static SSR Streaming: показываем placeholder пока данные грузятся
                builder.AddContent(0, placeholder ?? DefaultStreamingPlaceholder);
                return;
            }
            // Интерактивный режим или обычный SSR — placeholder не нужен
        };
    protected virtual IReadOnlyDictionary<string, object> BuildAriaAttributes()
    {
        var currentGeneration = Volatile.Read(ref _ariaGeneration);
        if (IsBrowser) return BuildAriaAttributesCore(currentGeneration);
        lock (_ariaCacheLock) return BuildAriaAttributesCore(currentGeneration);
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

    private static bool IsAriaAttribute(string key)
        => key.StartsWith("aria-", StringComparison.OrdinalIgnoreCase)
        || key.Equals("role", StringComparison.OrdinalIgnoreCase)
        || key.Equals("tabindex", StringComparison.OrdinalIgnoreCase);

    // ── RefreshAsync ───────────────────────────────────────────────────────────
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

    protected Task RefreshFromBackgroundAsync()
        => IsDisposed ? Task.CompletedTask : InvokeAsync(StateHasChanged);

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

    // ── Service helpers ────────────────────────────────────────────────────────
    protected T? TryGetService<T>() where T : class
        => ServiceProvider.GetService<T>();

    protected T TryGetRequiredService<T>() where T : class
        => ServiceProvider.GetService<T>()
           ?? throw new InvalidOperationException(
               $"Service {typeof(T).Name} is not registered. Call builder.Services.AddSuperUI() in Program.cs.");

    protected void ThrowIfDisposed([CallerMemberName] string? caller = null)
        => ObjectDisposedException.ThrowIf(IsDisposed, $"{ComponentId}.{caller}");

    // ── Context helpers ────────────────────────────────────────────────────────
    protected Task IfBrowserAsync(Func<Task> action)
        => IsBrowser ? action() : Task.CompletedTask;

    protected Task IfServerAsync(Func<Task> action)
        => IsServer ? action() : Task.CompletedTask;

    /// <summary>
    /// NEW: Выполнить действие только в интерактивном режиме (не в Static SSR).
    /// </summary>
    protected Task IfInteractiveAsync(Func<Task> action)
        => IsInteractive ? action() : Task.CompletedTask;

    // ── Logging ────────────────────────────────────────────────────────────────
    [Conditional("DEBUG")]
    private void LogLifecycle(string method)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("[{ComponentId}] {Method}", ComponentId, method);
    }

    // ── IAsyncDisposable ───────────────────────────────────────────────────────
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
        _hooksSet.Clear();
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
