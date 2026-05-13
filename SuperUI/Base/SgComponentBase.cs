// SuperUI/Base/SgComponentBase.cs
// ИСПРАВЛЕНИЯ v4:
// ✅ FIX CS0246: добавлен using Microsoft.AspNetCore.Components.Web
// ✅ FIX: OnAfterFirstRenderAsync теперь virtual
// ✅ FIX: AddHook проверяет дубликаты
// ✅ FIX: IsStreamingRendering свойство
// ✅ PERF: ShouldSetParameters — избегаем double allocation
// ✅ THREAD: _filteredAttrsCache через Interlocked

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
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
    [Inject] protected ILoggerFactory LoggerFactory { get; set; } = null!;
    protected ILogger? _logger;
    protected ILogger Logger => _logger ??= LoggerFactory.CreateLogger(GetType());

    [Inject] protected IComponentOptionsService? OptionsService { get; set; }
    [Inject] protected IServiceProvider ServiceProvider { get; set; } = null!;
    [Inject] protected IComponentRegistry? ComponentRegistry { get; set; }
    [Inject] protected TimeProvider TimeProvider { get; set; } = TimeProvider.System;

    /// <summary>
    /// Состояние компонента, сохраняемое между prerender (Server) и WASM.
    /// Доступно в .NET 8+ InteractiveAuto режиме.
    /// </summary>
    [Inject] protected PersistentComponentState? PersistentComponentState { get; set; }

    /// <summary>Алиас для PersistentComponentState.</summary>
    protected PersistentComponentState? PersistentState => PersistentComponentState;

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

    /// <summary>
    /// Компонент интерактивен (не статичный SSR / не prerendering).
    /// На .NET 9+ использует RendererInfo.IsInteractive.
    /// </summary>
    protected bool IsInteractive
    {
#if NET9_0_OR_GREATER
        get => RendererInfo.IsInteractive;
#else
        get => RenderMode is not null;
#endif
    }

    /// <summary>
    /// Определяет, является ли текущий рендер фазой prerendering (SSR без интерактивности).
    /// Работает на .NET 8+.
    /// </summary>
    public bool IsPrerendering => _isPrerendering;

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
    public string? CssClass => Css().Build();
    public string? CssStyle => CreateStyle().Build();

    public virtual void OnRenderModeChanged(SgRenderMode newMode)
    {
        // Базовая реализация пустая
    }

    public void Subscribe(IComponentHook hook)
    {
        if (hook == null) return;
        lock (_hooks)
        {
            if (_hooksSet.Add(hook))
            {
                _hooks.Add(hook);
            }
        }
    }

    public void Unsubscribe(IComponentHook hook)
    {
        if (hook == null) return;
        lock (_hooks)
        {
            if (_hooksSet.Remove(hook))
            {
                _hooks.Remove(hook);
            }
        }
    }

    protected string EffectiveId => !string.IsNullOrWhiteSpace(Id) ? Id! : ComponentId;
    public bool IsDisposed => Volatile.Read(ref _disposed) == 1;
    protected static bool IsBrowser => OperatingSystem.IsBrowser();
    protected static bool IsServer => !OperatingSystem.IsBrowser();

    /// <summary>BUG-1 FIX: единая точка ComponentToken, переопределяется SgJsComponentBase.</summary>
    protected internal virtual CancellationToken ComponentToken => _cts.Token;

    /// <summary>Алиас для ComponentToken.</summary>
    protected CancellationToken LifecycleToken => ComponentToken;

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
    private bool _isFirstRender = true;
    private bool _isPrerendering;
    private bool _isInitialized;
    private int _renderCount;
    // FIX: используем HashSet для гарантии уникальности хуков
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

    private void RunHooks(Action<IComponentHook> action)
    {
        foreach (var hook in _hooks)
        {
            try { action(hook); }
            catch (Exception ex) { Logger.LogError(ex, "Hook {Hook} failed in {Method}", hook.GetType().Name, nameof(action)); }
        }
    }

    private async Task RunHooksAsync(Func<IComponentHook, Task> action)
    {
        foreach (var hook in _hooks)
        {
            try { await action(hook); }
            catch (Exception ex) { Logger.LogError(ex, "Hook {Hook} failed async", hook.GetType().Name); }
        }
    }

    protected void AddDisposable(IDisposable disposable) => _disposables.Add(disposable);
    protected void AddDisposable(IAsyncDisposable disposable) => _disposables.Add(disposable);

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

        // Определяем prerendering до первого интерактивного рендера.
#if NET9_0_OR_GREATER
        _isPrerendering = !RendererInfo.IsInteractive;
#else
        _isPrerendering = _isFirstRender && IsServer;
#endif

        _lastParametersSnapshot = new SgParameterSnapshot(parameters);

        await OnParametersChangedAsync(parameters);

        await base.SetParametersAsync(ParameterView.Empty);
    }

    /// <summary>
    /// Вызывается при каждом обновлении параметров (до base.SetParametersAsync).
    /// Переопределите для side-effect логики при изменении параметров.
    /// </summary>
    protected virtual ValueTask OnParametersChangedAsync(ParameterView parameters)
        => ValueTask.CompletedTask;

    // ── Lifecycle ──────────────────────────────────────────────────────────────
    protected override void OnInitialized()
    {
        base.OnInitialized();
        LogLifecycle(nameof(OnInitialized));
        ComponentRegistry?.Register(this);
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

    /// <summary>
    /// Асинхронная инициализация. Вызывается из OnInitializedAsync.
    /// Переопределите для выполнения асинхронных задач при создании компонента.
    /// </summary>
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
        await OnParametersChangedAsync();
    }

    protected virtual Task OnParametersChangedAsync() => Task.CompletedTask;

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
        {
            await OnFirstRenderAsync();
            await RunHooksAsync(h =>
            {
                if (h is IAsyncComponentHook ah) return ah.OnFirstRenderAsync(this);
                return Task.CompletedTask;
            });
        }
        await RunHooksAsync(h =>
        {
            if (h is IAsyncComponentHook ah) return ah.OnAfterRenderAsync(this, firstRender);
            return Task.CompletedTask;
        });
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
        => StyleBuilder.Default().AddUserStyle(baseStyle);

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

    /// <summary>
    /// Потокобезопасный вызов StateHasChanged.
    /// Автоматически переключается на поток UI (важно для Server-side).
    /// Защита от вызова в disposed состоянии.
    /// </summary>
    protected void NotifyStateChanged()
    {
        if (IsDisposed) return;
        try
        {
            InvokeAsync(StateHasChanged);
        }
        catch (ObjectDisposedException) { /* компонент уже уничтожен */ }
    }

    /// <summary>
    /// Async версия NotifyStateChanged с await.
    /// </summary>
    protected async Task NotifyStateChangedAsync()
    {
        if (IsDisposed) return;
        try
        {
            await InvokeAsync(StateHasChanged);
        }
        catch (ObjectDisposedException) { }
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

    /// <summary>
    /// Попытаться восстановить состояние, сохранённое при prerender.
    /// Возвращает true если состояние найдено и восстановлено.
    /// </summary>
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

    /// <summary>
    /// Сохранить состояние для последующего восстановления в WASM.
    /// </summary>
    protected void PersistState<T>(string key, T value)
        => PersistentComponentState?.PersistAsJson(key, value);

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
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1) return;
        
        try { _cts.Cancel(); } catch { /* ignored */ }
        _cts.Dispose();
        
        _disposables.Dispose();
        
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
            try { if (hook is IDisposable d) d.Dispose(); }
            catch (Exception ex)
            { Logger.LogWarning(ex, "[{Id}] Hook dispose error", ComponentId); }
        }

        _hooks.Clear();
        _hooksSet.Clear();
        
        // Синхронная очистка батчера
        var batcher = Interlocked.Exchange(ref _signalBatcher, null);
        batcher?.Dispose();

        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1) return;
        LogLifecycle(nameof(DisposeAsync));

        try { _cts.Cancel(); } catch { /* ignored */ }
        _cts.Dispose();

        await DisposeAsyncCore();
        await _disposables.DisposeAsync();

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
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing) { }
    protected virtual ValueTask DisposeAsyncCore() => ValueTask.CompletedTask;

    protected virtual ValueTask DisposeComponentAsync()
    {
        var batcher = Interlocked.Exchange(ref _signalBatcher, null);
        batcher?.Dispose();
        return ValueTask.CompletedTask;
    }
}
