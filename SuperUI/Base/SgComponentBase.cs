// SuperUI/Base/SgComponentBase.cs
// ИСПРАВЛЕНО:
// ✅ StateHasChanged: new → отдельный RequestRender() public метод (не shadow)
// ✅ AdditionalAttributesFiltered: thread-safe кэш с lock
// ✅ OnParametersChangedAsync: вызывается даже при исключении хука (try/finally)
// ✅ IfBrowserAsync: убран [SupportedOSPlatform] — он ломает AOT предупреждениями
// УЛУЧШЕНО:
// ✅ RequestRender() — публичный метод для планирования рендера
// ✅ ComponentTokenSource — CancellationTokenSource для жизни компонента
// ✅ ThrowIfDisposed с CallerMemberName для debug-диагностики

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
/// Thread safety:
///   WASM: однопоточный. Interlocked для ARM-корректности.
///   Server: per-circuit. _disposed требует Interlocked/Volatile.
/// </summary>
public abstract class SgComponentBase : ComponentBase, IAsyncDisposable
{
    // ── Инъекции ──────────────────────────────────────────────────────────────
    [Inject] protected ILogger<SgComponentBase> Logger { get; set; } = null!;
    [Inject] protected IComponentOptionsService OptionsService { get; set; } = null!;
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

    /// <summary>Дополнительные HTML-атрибуты. class/style фильтруются.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    // ── Публичные свойства ─────────────────────────────────────────────────────

    /// <summary>Уникальный ID компонента.</summary>
    public string ComponentId { get; }

    /// <summary>Эффективный ID: Id если задан, иначе ComponentId.</summary>
    protected string EffectiveId => !string.IsNullOrWhiteSpace(Id) ? Id! : ComponentId;

    /// <summary>Компонент был утилизирован.</summary>
    public bool IsDisposed => Volatile.Read(ref _disposed) == 1;

    /// <summary>true на WASM (браузерный хост).</summary>
    protected static bool IsBrowser => OperatingSystem.IsBrowser();

    /// <summary>true на Server (Blazor Server / Web App Server).</summary>
    protected static bool IsServer => !OperatingSystem.IsBrowser();

    /// <summary>
    /// CancellationToken, отменяемый при DisposeAsync компонента.
    /// Используйте в async-операциях для автоматической отмены.
    /// </summary>
    protected CancellationToken ComponentToken => _cts.Token;

    /// <summary>
    /// Дополнительные атрибуты без class и style.
    /// Thread-safe кэш. ИСПРАВЛЕНО: lock для Server-side.
    /// </summary>
    protected IReadOnlyDictionary<string, object>? AdditionalAttributesFiltered
    {
        get
        {
            var gen = Volatile.Read(ref _ariaGeneration);

            // Double-check без lock для fast-path
            if (_filteredAttrsCache is not null && _filteredAttrsCacheGen == gen)
                return _filteredAttrsCache;

            lock (_ariaCacheLock)  // ИСПРАВЛЕНИЕ: lock для Server thread-safety
            {
                // Re-check внутри lock
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

    // ARIA cache (shared lock для и AriaAttributes, и FilteredAttrs)
    private readonly object _ariaCacheLock = new();
    private IReadOnlyDictionary<string, object>? _ariaCache;
    private int _ariaCacheGeneration = -2;
    private int _ariaGeneration;

    // AdditionalAttributesFiltered cache
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
    /// Переопределите: <c>protected override string ComponentPrefix =&gt; "btn";</c>
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
        if (!visible) return false;                // остаётся невидимым → пропуск

        foreach (var hook in _hooks)
            if (hook is IRenderHook rh && !rh.ShouldRender(this)) return false;

        return true;
    }

    // ── RequestRender / StateHasChanged ────────────────────────────────────────
    // ИСПРАВЛЕНИЕ: вместо `public new StateHasChanged()` (shadow — нарушает LSP)
    // используем отдельный публичный RequestRender() + оставляем base.StateHasChanged protected.

    /// <summary>
    /// Запланировать перерисовку с batch-рендерингом.
    /// Используйте вместо StateHasChanged() для вызова из сигналов/сервисов.
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

    /// <summary>
    /// Для обратной совместимости с кодом который вызывает StateHasChanged() напрямую.
    /// DEPRECATED: используйте RequestRender().
    /// </summary>
    [Obsolete("Use RequestRender() for external callers. StateHasChanged() is protected in ComponentBase.", false)]
    public void ForceStateHasChanged()
    {
        if (IsDisposed) return;
        _ = InvokeAsync(StateHasChanged);
    }

    /// <summary>Вызвать StateHasChanged в потоке компонента.</summary>
    internal Task InvokeStateHasChangedAsync()
    {
        if (IsDisposed) return Task.CompletedTask;
        return InvokeAsync(StateHasChanged);
    }

    // ── SetParametersAsync ─────────────────────────────────────────────────────

    public override Task SetParametersAsync(ParameterView parameters)
    {
        Interlocked.Increment(ref _ariaGeneration);
        // Инвалидируем оба кэша при изменении параметров
        Volatile.Write(ref _filteredAttrsCacheGen, -1);
        return base.SetParametersAsync(parameters);
    }

    // ── Lifecycle ──────────────────────────────────────────────────────────────

    protected override void OnInitialized()
    {
        base.OnInitialized();
        LogLifecycle(nameof(OnInitialized));
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

        // ИСПРАВЛЕНИЕ: try/finally — OnParametersChangedAsync вызывается даже при исключении хука
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
    /// Вызывается при первом рендере (componentDidMount в React).
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

    /// <summary>Создать StyleBuilder.</summary>
    protected StyleBuilder CreateStyle(string? baseStyle = null)
        => new(baseStyle);

    /// <summary>Создать StyleBuilder.</summary>
    protected StyleBuilder CreateStyle(string? baseStyle = null)
        => new(baseStyle);

    /// <summary>CSS-класс по умолчанию. Переопределите: <c>protected override string? GetDefaultCssClass() =&gt; "sg-button";</c></summary>
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

    /// <summary>Запланировать перерисовку.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Task RefreshAsync()
    {
        if (IsDisposed) return Task.CompletedTask;
        return InvokeAsync(StateHasChanged);
    }

    /// <summary>Выполнить действие и запланировать перерисовку.</summary>
    public Task RefreshAsync(Action action)
    {
        if (IsDisposed) return Task.CompletedTask;
        return InvokeAsync(() => { action(); StateHasChanged(); });
    }

    /// <summary>Выполнить async действие и запланировать перерисовку.</summary>
    public Task RefreshAsync(Func<Task> action)
    {
        if (IsDisposed) return Task.CompletedTask;
        return InvokeAsync(async () => { await action(); StateHasChanged(); });
    }

    /// <summary>Безопасный StateHasChanged для фоновых потоков.</summary>
    protected Task RefreshFromBackgroundAsync()
        => IsDisposed ? Task.CompletedTask : InvokeAsync(StateHasChanged);

    /// <summary>InvokeAsync с проверкой IsDisposed.</summary>
    protected Task SafeInvokeAsync(Func<Task> action)
    {
        if (IsDisposed) return Task.CompletedTask;
        return InvokeAsync(action);
    }

    /// <summary>InvokeAsync (sync) с проверкой IsDisposed.</summary>
    protected Task SafeInvokeAsync(Action action)
    {
        if (IsDisposed) return Task.CompletedTask;
        return InvokeAsync(action);
    }

    /// <summary>InvokeAsync с возвратом результата.</summary>
    protected Task<TResult> SafeInvokeAsync<TResult>(Func<TResult> func)
    {
        if (IsDisposed) return Task.FromResult(default(TResult)!);
        return InvokeAsync(func);
    }

    // ── Service helpers ────────────────────────────────────────────────────────

    /// <summary>Получить сервис из DI или null.</summary>
    protected T? TryGetService<T>() where T : class
        => ServiceProvider.GetService<T>();

    /// <summary>Получить сервис или бросить понятное исключение.</summary>
    protected T TryGetRequiredService<T>() where T : class
        => ServiceProvider.GetService<T>()
           ?? throw new InvalidOperationException(
               $"Service {typeof(T).Name} is not registered. " +
               $"Call builder.Services.AddSuperUI() in Program.cs.");

    /// <summary>Бросить ObjectDisposedException если компонент утилизирован.</summary>
    protected void ThrowIfDisposed([CallerMemberName] string? caller = null)
        => ObjectDisposedException.ThrowIf(IsDisposed, $"{ComponentId}.{caller}");

    // ── Context helpers ────────────────────────────────────────────────────────

    /// <summary>
    /// Выполнить действие только в Blazor WebAssembly.
    /// ИСПРАВЛЕНО: убран [SupportedOSPlatform("browser")] — ломал AOT предупреждениями.
    /// </summary>
    protected Task IfBrowserAsync(Func<Task> action)
        => IsBrowser ? action() : Task.CompletedTask;

    /// <summary>Выполнить действие только в Blazor Server.</summary>
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

        // 1. Отменяем ComponentToken
        try { await _cts.CancelAsync(); } catch { /* ignored */ }
        _cts.Dispose();

        // 2. Реактивные ресурсы
        if (_reactiveDisposables is not null)
        {
            foreach (var rd in _reactiveDisposables)
            {
                try { rd.Dispose(); }
                catch (Exception ex) { Logger.LogWarning(ex, "[{Id}] Reactive dispose error", ComponentId); }
            }
            _reactiveDisposables.Clear();
        }

        // 3. Хуки
        foreach (var hook in _hooks)
        {
            try
            {
                if (hook is IAsyncDisposable ad) await ad.DisposeAsync();
                else if (hook is IDisposable d) d.Dispose();
            }
            catch (Exception ex) { Logger.LogWarning(ex, "[{Id}] Hook dispose error", ComponentId); }
        }
        _hooks.Clear();

        // 4. Дочерние ресурсы
        await DisposeComponentAsync();

        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Точка расширения для освобождения ресурсов дочерних классов.
    /// Вызывается в конце DisposeAsync.
    /// </summary>
    protected virtual ValueTask DisposeComponentAsync()
    {
        var batcher = Interlocked.Exchange(ref _signalBatcher, null);
        batcher?.Dispose();
        return ValueTask.CompletedTask;
    }
}
