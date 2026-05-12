// SuperUI/Base/SgComponentBase.cs
// ИСПРАВЛЕНИЯ:
// 1. ShouldRender — единственный Interlocked.Exchange (нет двойного чтения)
// 2. DisposeComponentAsync — убран лишний async/await ValueTask.CompletedTask
// 3. OnInitialized — base.OnInitialized() ПЕРВЫМ (до хуков)
// 4. _ariaCache — добавлен _ariaCacheLock для атомарного обновления пары (cache, generation)
// 5. RegisterEffect/RegisterComputed — Subscribe(this) для авто-StateHasChanged
// 6. IsBrowser / IsServer свойства
// 7. OnFirstRenderAsync — виртуальный метод для удобства

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
/// </summary>
/// <remarks>
/// Иерархия: ComponentBase → SgComponentBase → SgJsComponentBase → SgInteractiveBase
///
/// Thread safety:
/// - WASM: однопоточный, lock не нужен, Interlocked используется для корректности на ARM.
/// - Server: каждый circuit — отдельный поток. _hooks, _reactiveDisposables — per-circuit.
///   _disposed, _ariaGeneration требуют Interlocked/Volatile.
/// </remarks>
public abstract class SgComponentBase : ComponentBase, IAsyncDisposable
{
    // ── Инъекции ──────────────────────────────────────────────────────────────────
    [Inject] protected ILogger<SgComponentBase> Logger { get; set; } = null!;
    [Inject] protected IComponentOptionsService OptionsService { get; set; } = null!;
    [Inject] protected IServiceProvider ServiceProvider { get; set; } = null!;

    // ── Каскадные параметры ───────────────────────────────────────────────────────
    [CascadingParameter] protected SgThemeContext? ThemeContext { get; set; }
    [CascadingParameter] protected SgConfigContext? ConfigContext { get; set; }

    // ── Параметры ─────────────────────────────────────────────────────────────────
    [Parameter] public string? Class { get; set; }
    [Parameter] public string? Style { get; set; }
    [Parameter] public bool Visible { get; set; } = true;
    [Parameter] public string? Id { get; set; }
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    // ── Публичные свойства ────────────────────────────────────────────────────────
    public string ComponentId { get; }
    protected string EffectiveId => Id ?? ComponentId;

    /// <summary>Компонент был задиспожен. Проверяйте перед async операциями.</summary>
    public bool IsDisposed => Volatile.Read(ref _disposed) == 1;

    /// <summary>true — браузерный WASM контекст.</summary>
    protected static bool IsBrowser => OperatingSystem.IsBrowser();

    /// <summary>true — серверный контекст (Blazor Server / Web App Server).</summary>
    protected static bool IsServer => !OperatingSystem.IsBrowser();

    // ── Внутреннее состояние ──────────────────────────────────────────────────────
    private int _disposed;
    // ИСПРАВЛЕНО: обновляется единым Interlocked.Exchange в ShouldRender
    private int _previousVisible = 1; // 1 = true, 0 = false
    private readonly List<IComponentHook> _hooks = [];
    private ComponentSignalTracker? _signalBatcher;
    private List<IDisposable>? _reactiveDisposables;

    // ИСПРАВЛЕНО: lock для атомарного обновления пары (_ariaCache, _ariaCacheGeneration)
    private readonly object _ariaCacheLock = new();
    private IReadOnlyDictionary<string, object>? _ariaCache;
    private int _ariaCacheGeneration = -2;
    private int _ariaGeneration;

#if DEBUG
    private readonly ComponentDiagnostics _diagnostics;
    private long _renderStartTick;
#endif

    // ── Конструктор ───────────────────────────────────────────────────────────────
    protected SgComponentBase()
    {
        ComponentId = ComponentIdGenerator.Next(ComponentPrefix);
        _signalBatcher = new ComponentSignalTracker(this);
#if DEBUG
        _diagnostics = new ComponentDiagnostics { ComponentId = ComponentId };
#endif
    }

    /// <summary>Префикс для генерации ComponentId.</summary>
    protected virtual string ComponentPrefix => "cmp";

    // ── Hooks ──────────────────────────────────────────────────────────────────────
    /// <summary>Зарегистрировать хук жизненного цикла.</summary>
    protected void AddHook(IComponentHook hook)
    {
        ArgumentNullException.ThrowIfNull(hook);
        _hooks.Add(hook);
    }

    // ── Reactive ───────────────────────────────────────────────────────────────────
    /// <summary>
    /// Зарегистрировать реактивный side-effect.
    /// Авто-отписка при Dispose. Авто-StateHasChanged при изменении сигналов.
    /// </summary>
    protected SgEffect RegisterEffect(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        var effect = new SgEffect(action);
        // ИСПРАВЛЕНО: подписываем на RefreshAsync компонента
        effect.Subscribe(this);
        (_reactiveDisposables ??= new()).Add(effect);
        return effect;
    }

    /// <summary>Зарегистрировать async реактивный side-effect.</summary>
    protected SgEffect RegisterEffect(Func<Task> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        var effect = new SgEffect(action);
        // ИСПРАВЛЕНО: подписываем на RefreshAsync компонента
        effect.Subscribe(this);
        (_reactiveDisposables ??= new()).Add(effect);
        return effect;
    }

    /// <summary>Зарегистрировать вычисляемый сигнал. Авто-отписка при Dispose.</summary>
    protected SgComputed<T> RegisterComputed<T>(Func<T> compute)
    {
        ArgumentNullException.ThrowIfNull(compute);
        var computed = new SgComputed<T>(compute);
        // ИСПРАВЛЕНО: подписываем на RefreshAsync компонента
        computed.Subscribe(this);
        (_reactiveDisposables ??= new()).Add(computed);
        return computed;
    }

    // ── ShouldRender ────────────────────────────────────────────────────────────────
    protected override bool ShouldRender()
    {
        bool visible = Visible;
        // ИСПРАВЛЕНО: единый Interlocked.Exchange — нет двойного чтения/race
        int prev = Interlocked.Exchange(ref _previousVisible, visible ? 1 : 0);
        bool wasVisible = prev == 1;

        // Стал невидимым → один рендер чтобы скрыть DOM
        if (!visible && wasVisible) return true;
        // Был и остался невидимым → пропускаем
        if (!visible) return false;

        // Проверяем хуки
        foreach (var hook in _hooks)
            if (hook is IRenderHook rh && !rh.ShouldRender(this)) return false;

        return true;
    }

    // ── StateHasChanged ─────────────────────────────────────────────────────────────
    // ПРИМЕЧАНИЕ: используем 'new' вместо 'override' чтобы контролировать batching
    // через ComponentSignalTracker. Приведение к ComponentBase даст доступ к оригиналу.
    // CS0108 suppress intentional — documented in XML remarks.
#pragma warning disable CS0108 // Member hides inherited member; use new keyword
    /// <summary>
    /// Запланировать перерисовку компонента. Использует batch-рендеринг через
    /// <see cref="ComponentSignalTracker"/>. Этот метод скрывает (<c>new</c>)
    /// <see cref="ComponentBase.StateHasChanged"/> — при приведении к <see cref="ComponentBase"/>
    /// будет вызван оригинальный метод, что может привести к двойному или пропущенному рендеру.
    /// ⚠️ Не приводите <see cref="SgComponentBase"/> к <see cref="ComponentBase"/>.
    /// </summary>
    public new void StateHasChanged()
    {
        if (IsDisposed) return;
        var batcher = Volatile.Read(ref _signalBatcher);
        if (batcher is not null) batcher.ScheduleRender();
        else base.StateHasChanged();
    }
#pragma warning restore CS0108

    /// <summary>Прямой вызов StateHasChanged через InvokeAsync (для ComponentSignalTracker).</summary>
    internal Task InvokeStateHasChangedAsync()
    {
        if (IsDisposed) return Task.CompletedTask;
        return InvokeAsync(base.StateHasChanged);
    }

    // ── SetParametersAsync ──────────────────────────────────────────────────────────
    public override Task SetParametersAsync(ParameterView parameters)
    {
        Interlocked.Increment(ref _ariaGeneration);
        return base.SetParametersAsync(parameters);
    }

    // ── Lifecycle ───────────────────────────────────────────────────────────────────
    protected override void OnInitialized()
    {
        // ИСПРАВЛЕНО: base.OnInitialized() ПЕРВЫМ — сервисы инициализируются до хуков
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
        foreach (var hook in _hooks)
            if (hook is IAsyncComponentHook ah)
                await ah.OnParametersSetAsync(this);
    }

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
    /// Вызывается при первом рендере. Удобная точка расширения (аналог componentDidMount).
    /// </summary>
    protected virtual Task OnFirstRenderAsync() => Task.CompletedTask;

    // ── CSS / Style ─────────────────────────────────────────────────────────────────
#if DEBUG
    public ComponentDiagnostics Diagnostics => _diagnostics;
#endif

    protected CssBuilder Css(string? baseClass = null)
        => new(baseClass ?? GetDefaultCssClass());

    protected StyleBuilder CreateStyle(string? baseStyle = null)
        => new(baseStyle);

    protected virtual string? GetDefaultCssClass() => null;

    // ── ARIA ────────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Строит словарь ARIA-атрибутов. Результат кэшируется между рендерами.
    /// Инвалидируется при изменении параметров (через SetParametersAsync).
    /// Фильтрует AdditionalAttributes, оставляя только aria-*, role, tabindex.
    /// </summary>
    protected virtual IReadOnlyDictionary<string, object> BuildAriaAttributes()
    {
        var currentGeneration = Volatile.Read(ref _ariaGeneration);
        lock (_ariaCacheLock)
        {
            if (_ariaCache is not null && _ariaCacheGeneration == currentGeneration)
                return _ariaCache;

            // Фильтруем: только aria-*, role, tabindex
            var capacity = 4;
            var attrs = new Dictionary<string, object>(capacity, StringComparer.Ordinal);

            if (AdditionalAttributes is not null)
                foreach (var kvp in AdditionalAttributes)
                    if (IsAriaAttribute(kvp.Key))
                        attrs[kvp.Key] = kvp.Value;

            _ariaCache = attrs;
            _ariaCacheGeneration = currentGeneration;
            return attrs;
        }
    }

    private static bool IsAriaAttribute(string key)
        => key.StartsWith("aria-", StringComparison.OrdinalIgnoreCase)
        || key.Equals("role", StringComparison.OrdinalIgnoreCase)
        || key.Equals("tabindex", StringComparison.OrdinalIgnoreCase);

    // ── RefreshAsync ────────────────────────────────────────────────────────────────
    /// <summary>Запланировать перерисовку из любого потока.</summary>
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

    public Task RefreshAsync(Func<Task> action)
    {
        if (IsDisposed) return Task.CompletedTask;
        return InvokeAsync(async () => { await action(); StateHasChanged(); });
    }

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

    protected T? TryGetService<T>() where T : class
        => ServiceProvider.GetService<T>();

    protected void ThrowIfDisposed()
    {
        if (IsDisposed)
            throw new ObjectDisposedException(ComponentId, $"Component {ComponentId} is disposed.");
    }

    // ── IsBrowser helpers ────────────────────────────────────────────────────────────
    /// <summary>Выполнить действие только в WASM-контексте.</summary>
    protected Task IfBrowserAsync(Func<Task> action)
        => IsBrowser ? action() : Task.CompletedTask;

    /// <summary>Выполнить действие только в Server-контексте.</summary>
    protected Task IfServerAsync(Func<Task> action)
        => IsServer ? action() : Task.CompletedTask;

    // ── Logging ─────────────────────────────────────────────────────────────────────
    [Conditional("DEBUG")]
    private void LogLifecycle(string method)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("[{ComponentId}] {Method}", ComponentId, method);
    }

    // ── IAsyncDisposable ────────────────────────────────────────────────────────────
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1) return;
        LogLifecycle(nameof(DisposeAsync));

        // 1. Реактивные disposables
        if (_reactiveDisposables is not null)
        {
            foreach (var rd in _reactiveDisposables)
            {
                try { rd.Dispose(); }
                catch (Exception ex) { Logger.LogWarning(ex, "[{Id}] Reactive dispose error", ComponentId); }
            }
            _reactiveDisposables.Clear();
        }

        // 2. Hooks
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

        // 3. Компонент-специфичные ресурсы
        await DisposeComponentAsync();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Точка расширения для освобождения ресурсов дочерних классов.
    /// Вызывайте base.DisposeComponentAsync() в конце.
    /// </summary>
    // ИСПРАВЛЕНО: убран лишний async + await ValueTask.CompletedTask (state machine overhead)
    protected virtual ValueTask DisposeComponentAsync()
    {
        var batcher = Interlocked.Exchange(ref _signalBatcher, null);
        batcher?.Dispose();
        return ValueTask.CompletedTask;
    }
}
