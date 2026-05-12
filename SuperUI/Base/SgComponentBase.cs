// SuperUI/Base/SgComponentBase.cs
//
// ИСПРАВЛЕНИЯ:
// ✅ CS0111: удалён дублирующий CreateStyle() — был объявлен дважды
// ✅ IComponentOptionsService — необязательная инъекция (может не быть в тестах)
// УЛУЧШЕНИЯ:
// ✅ RequestRender() — публичный метод вместо shadow StateHasChanged
// ✅ ComponentToken — автоотмена при DisposeAsync
// ✅ ThrowIfDisposed с CallerMemberName
// ✅ CreateStyle() и Css() — единственные объявления

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

/// <summary>
/// Базовый класс уровня 1 для всех компонентов SuperUI.
/// Иерархия: ComponentBase → SgComponentBase → SgJsComponentBase → SgInteractiveBase
/// </summary>
public abstract class SgComponentBase : ComponentBase, IAsyncDisposable
{
    // ── Инъекции ──────────────────────────────────────────────────────────────

    [Inject] protected ILogger<SgComponentBase> Logger { get; set; } = null!;

    /// <summary>Необязательная инъекция — может быть null в тестах.</summary>
    [Inject(Key = null)] protected IComponentOptionsService? OptionsService { get; set; }

    [Inject] protected IServiceProvider ServiceProvider { get; set; } = null!;

    // ── Каскадные параметры ────────────────────────────────────────────────────

    [CascadingParameter] protected SgThemeContext? ThemeContext { get; set; }
    [CascadingParameter] protected SgConfigContext? ConfigContext { get; set; }

    // ── Параметры ──────────────────────────────────────────────────────────────

    /// <summary>Дополнительные CSS-классы.</summary>
    [Parameter] public string? Class { get; set; }

    /// <summary>Инлайн стили.</summary>
    [Parameter] public string? Style { get; set; }

    /// <summary>Видимость компонента.</summary>
    [Parameter] public bool Visible { get; set; } = true;

    /// <summary>HTML id атрибут.</summary>
    [Parameter] public string? Id { get; set; }

    /// <summary>Дополнительные HTML-атрибуты (class/style фильтруются).</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    // ── Публичные свойства ─────────────────────────────────────────────────────

    /// <summary>Уникальный ID компонента.</summary>
    public string ComponentId { get; }

    /// <summary>Эффективный ID: Id если задан, иначе ComponentId.</summary>
    protected string EffectiveId =>
        !string.IsNullOrWhiteSpace(Id) ? Id! : ComponentId;

    /// <summary>Компонент был утилизирован.</summary>
    public bool IsDisposed => Volatile.Read(ref _disposed) == 1;

    /// <summary>true на WASM (браузерный хост).</summary>
    protected static bool IsBrowser => OperatingSystem.IsBrowser();

    /// <summary>true на Server (Blazor Server / Web App Server).</summary>
    protected static bool IsServer => !OperatingSystem.IsBrowser();

    /// <summary>CancellationToken, отменяемый при DisposeAsync компонента.</summary>
    protected virtual CancellationToken ComponentToken => _cts.Token;

    /// <summary>
    /// Дополнительные атрибуты без class и style.
    /// Thread-safe кэш.
    /// </summary>
    protected IReadOnlyDictionary<string, object>? AdditionalAttributesFiltered
    {
        get
        {
            var gen = Volatile.Read(ref _ariaGeneration);
            if (_filteredAttrsCache is not null && _filteredAttrsCacheGen == gen)
                return _filteredAttrsCache;

            lock (_ariaCacheLock)
            {
                if (_filteredAttrsCache is not null && _filteredAttrsCacheGen == gen)
                    return _filteredAttrsCache;

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
        }
    }

    // ── Внутреннее состояние ───────────────────────────────────────────────────

    private int _disposed;
    private int _previousVisible = 1;
    private readonly List<IComponentHook> _hooks = [];
    private ComponentSignalTracker? _signalBatcher;
    private List<IDisposable>? _reactiveDisposables;
    private readonly CancellationTokenSource _cts = new();

    // ARIA / Filter cache
    private readonly object _ariaCacheLock = new();
    private IReadOnlyDictionary<string, object>? _ariaCache;
    private int _ariaCacheGeneration = -2;
    private int _ariaGeneration;
    private volatile IReadOnlyDictionary<string, object>? _filteredAttrsCache;
    private volatile int _filteredAttrsCacheGen = -1;

#if DEBUG
    private readonly ComponentDiagnostics _diagnostics;
    private long _renderStartTick;
#endif

    // ── Конструктор ────────────────────────────────────────────────────────────

    protected SgComponentBase()
    {
        ComponentId = ComponentIdGenerator.Next(ComponentPrefix);
        _signalBatcher = new ComponentSignalTracker(this);

#if DEBUG
        _diagnostics = new ComponentDiagnostics { ComponentId = ComponentId };
#endif
    }

    /// <summary>
    /// Префикс для генерации ComponentId.
    /// Переопределите: <c>protected override string ComponentPrefix => "btn";</c>
    /// </summary>
    protected virtual string ComponentPrefix => "cmp";

    // ── Hooks ──────────────────────────────────────────────────────────────────

    /// <summary>Зарегистрировать хук жизненного цикла.</summary>
    protected void AddHook(IComponentHook hook)
    {
        ArgumentNullException.ThrowIfNull(hook);
        _hooks.Add(hook);
    }

    // ── Reactive ───────────────────────────────────────────────────────────────

    /// <summary>Создать реактивный сигнал с авто-StateHasChanged.</summary>
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

    /// <summary>Зарегистрировать реактивный side-effect.</summary>
    protected SgEffect RegisterEffect(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        var effect = new SgEffect(action);
        effect.Subscribe(this);
        (_reactiveDisposables ??= []).Add(effect);
        return effect;
    }

    /// <summary>Зарегистрировать async реактивный side-effect.</summary>
    protected SgEffect RegisterEffect(Func<Task> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        var effect = new SgEffect(action);
        effect.Subscribe(this);
        (_reactiveDisposables ??= []).Add(effect);
        return effect;
    }

    /// <summary>Зарегистрировать вычисляемый сигнал.</summary>
    protected SgComputed<TValue> RegisterComputed<TValue>(Func<TValue> compute)
    {
        ArgumentNullException.ThrowIfNull(compute);
        var computed = new SgComputed<TValue>(compute);
        computed.Subscribe(this);
        (_reactiveDisposables ??= []).Add(computed);
        return computed;
    }

    /// <summary>Регистрация для внутреннего использования фабриками.</summary>
    protected void RegisterEffectInternal(IDisposable disposable)
        => (_reactiveDisposables ??= []).Add(disposable);

    // ── ShouldRender ───────────────────────────────────────────────────────────

    protected override bool ShouldRender()
    {
        bool visible = Visible;
        int prev = Interlocked.Exchange(ref _previousVisible, visible ? 1 : 0);
        bool wasVisible = prev == 1;

        if (!visible && wasVisible) return true;  // стал невидимым → один рендер
        if (!visible) return false;               // остаётся невидимым → пропуск

        foreach (var hook in _hooks)
            if (hook is IRenderHook rh && !rh.ShouldRender(this))
                return false;

        return true;
    }

    // ── RequestRender ──────────────────────────────────────────────────────────

    /// <summary>
    /// Запланировать перерисовку с batch-рендерингом.
    /// Безопасен для вызова из любого потока.
    /// </summary>
    public void RequestRender()
    {
        if (IsDisposed) return;

        var batcher = Volatile.Read(ref _signalBatcher);
        if (batcher is not null)
            batcher.ScheduleRender();
        else
            _ = InvokeAsync(StateHasChanged);
    }

    /// <summary>Для совместимости с кодом, вызывающим StateHasChanged напрямую.</summary>
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

    public override Task SetParametersAsync(ParameterView parameters)
    {
        Interlocked.Increment(ref _ariaGeneration);
        Volatile.Write(ref _filteredAttrsCacheGen, -1);
        return base.SetParametersAsync(parameters);
    }

    // ── Lifecycle ──────────────────────────────────────────────────────────────

    protected override void OnInitialized()
    {
        base.OnInitialized();
        LogLifecycle(nameof(OnInitialized));
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

    /// <summary>
    /// Вызывается при каждом изменении параметров ПОСЛЕ базового OnParametersSetAsync.
    /// Переопределяйте вместо OnParametersSetAsync.
    /// </summary>
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

    /// <summary>
    /// Вызывается при первом рендере (componentDidMount).
    /// JS Interop доступен здесь.
    /// </summary>
    protected virtual Task OnFirstRenderAsync() => Task.CompletedTask;

    // ── CSS / Style ────────────────────────────────────────────────────────────

#if DEBUG
    /// <summary>Диагностические данные (только DEBUG).</summary>
    public ComponentDiagnostics Diagnostics => _diagnostics;
#endif

    /// <summary>Создать SgCssBuilder с базовым CSS-классом.</summary>
    protected CssBuilder Css(string? baseClass = null)
        => new(baseClass ?? GetDefaultCssClass());

    /// <summary>
    /// Создать StyleBuilder.
    /// ✅ FIX CS0111: единственное объявление (было дублирование).
    /// </summary>
    protected StyleBuilder CreateStyle(string? baseStyle = null)
        => new(baseStyle);

    /// <summary>
    /// CSS-класс по умолчанию.
    /// Переопределите: <c>protected override string? GetDefaultCssClass() => "sg-button";</c>
    /// </summary>
    protected virtual string? GetDefaultCssClass() => null;

    // ── ARIA ───────────────────────────────────────────────────────────────────

    protected virtual IReadOnlyDictionary<string, object> BuildAriaAttributes()
    {
        var currentGeneration = Volatile.Read(ref _ariaGeneration);
        lock (_ariaCacheLock)
        {
            if (_ariaCache is not null && _ariaCacheGeneration == currentGeneration)
                return _ariaCache;

            var attrs = new Dictionary<string, object>(4, StringComparer.Ordinal);
            if (AdditionalAttributes is not null)
                foreach (var kvp in AdditionalAttributes)
                    if (IsAriaAttribute(kvp.Key))
                        attrs[kvp.Key] = kvp.Value;

            _ariaCache = attrs;
            _ariaCacheGeneration = currentGeneration;
            return attrs;
        }
    }

    private static bool IsAriaAttribute(string key) =>
        key.StartsWith("aria-", StringComparison.OrdinalIgnoreCase) ||
        key.Equals("role", StringComparison.OrdinalIgnoreCase) ||
        key.Equals("tabindex", StringComparison.OrdinalIgnoreCase);

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
            try
            {
                var result = func();
                tcs.SetResult(result);
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });
        return tcs.Task;
    }

    // ── Service helpers ────────────────────────────────────────────────────────

    protected T? TryGetService<T>() where T : class
        => ServiceProvider.GetService<T>();

    protected T TryGetRequiredService<T>() where T : class
        => ServiceProvider.GetService<T>()
           ?? throw new InvalidOperationException(
               $"Service {typeof(T).Name} is not registered. " +
               $"Call builder.Services.AddSuperUI() in Program.cs.");

    protected void ThrowIfDisposed([CallerMemberName] string? caller = null)
        => ObjectDisposedException.ThrowIf(IsDisposed, $"{ComponentId}.{caller}");

    // ── Context helpers ────────────────────────────────────────────────────────

    protected Task IfBrowserAsync(Func<Task> action)
        => IsBrowser ? action() : Task.CompletedTask;

    protected Task IfServerAsync(Func<Task> action)
        => IsServer ? action() : Task.CompletedTask;

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

        try { await _cts.CancelAsync(); } catch { /* ignored */ }
        _cts.Dispose();

        if (_reactiveDisposables is not null)
        {
            foreach (var rd in _reactiveDisposables)
            {
                try { rd.Dispose(); }
                catch (Exception ex)
                {
                    Logger.LogWarning(ex, "[{Id}] Reactive dispose error", ComponentId);
                }
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
            {
                Logger.LogWarning(ex, "[{Id}] Hook dispose error", ComponentId);
            }
        }
        _hooks.Clear();

        await DisposeComponentAsync();
        GC.SuppressFinalize(this);
    }

    /// <summary>Точка расширения для освобождения ресурсов дочерних классов.</summary>
    protected virtual ValueTask DisposeComponentAsync()
    {
        var batcher = Interlocked.Exchange(ref _signalBatcher, null);
        batcher?.Dispose();
        return ValueTask.CompletedTask;
    }
}
