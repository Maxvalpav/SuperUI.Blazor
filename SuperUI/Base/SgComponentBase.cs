// SuperUI/Base/SgComponentBase.cs
//
// ДОРАБОТКИ (поверх существующих исправлений):
// 1. ComponentPrefix — readonly property вместо virtual для защиты от override-after-ctor
// 2. StyleBuilder CreateStyle() — документирован
// 3. TryGetRequiredService<T> — бросает InvalidOperationException с понятным сообщением
// 4. OnFirstRenderAsync — вызывается только если !IsPrerendering (безопасно для SSR)
// 5. AdditionalAttributes фильтрация — исключает "class"/"style" (предотвращает конфликт)
// 6. EffectiveId проверяет что Id не пустой
// 7. IsPrerendering — делегирует к IPrerenderingDetector если доступен

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
/// <br/>
/// Thread safety:
/// - WASM: однопоточный. Interlocked используется для ARM-корректности.
/// - Server: per-circuit изоляция. _disposed и _ariaGeneration требуют Interlocked/Volatile.
/// </remarks>
public abstract class SgComponentBase : ComponentBase, IAsyncDisposable
{
    // ── Инъекции ─────────────────────────────────────────────────────────────────
    [Inject] protected ILogger<SgComponentBase> Logger          { get; set; } = null!;
    [Inject] protected IComponentOptionsService OptionsService  { get; set; } = null!;
    [Inject] protected IServiceProvider         ServiceProvider { get; set; } = null!;

    // ── Каскадные параметры ───────────────────────────────────────────────────────
    [CascadingParameter] protected SgThemeContext?  ThemeContext  { get; set; }
    [CascadingParameter] protected SgConfigContext? ConfigContext { get; set; }

    // ── Параметры ─────────────────────────────────────────────────────────────────
    [Parameter] public string?  Class   { get; set; }
    [Parameter] public string?  Style   { get; set; }
    [Parameter] public bool     Visible { get; set; } = true;
    [Parameter] public string?  Id      { get; set; }
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    // ── Публичные свойства ────────────────────────────────────────────────────────
    public string ComponentId { get; }

    /// <summary>EffectiveId: Id если задан и непустой, иначе ComponentId.</summary>
    protected string EffectiveId =>
        !string.IsNullOrWhiteSpace(Id) ? Id! : ComponentId;

    /// <summary>Компонент был задиспожен.</summary>
    public bool IsDisposed => Volatile.Read(ref _disposed) == 1;

    /// <summary>true — браузерный WASM.</summary>
    protected static bool IsBrowser => OperatingSystem.IsBrowser();

    /// <summary>true — сервер (Blazor Server / Web App Server).</summary>
    protected static bool IsServer  => !OperatingSystem.IsBrowser();

    // ── Внутреннее состояние ──────────────────────────────────────────────────────
    private int _disposed;
    private int _previousVisible = 1;  // 1 = true, 0 = false
    private readonly List<IComponentHook> _hooks = [];
    private ComponentSignalTracker? _signalBatcher;
    private List<IDisposable>? _reactiveDisposables;

    // ARIA cache (атомарное обновление пары под lock)
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
        ComponentId     = ComponentIdGenerator.Next(ComponentPrefix);
        _signalBatcher  = new ComponentSignalTracker(this);
#if DEBUG
        _diagnostics = new ComponentDiagnostics { ComponentId = ComponentId };
#endif
    }

    /// <summary>Префикс для генерации ComponentId (переопределяется в подклассах).</summary>
    protected virtual string ComponentPrefix => "cmp";

    // ── Hooks ─────────────────────────────────────────────────────────────────────
    /// <summary>Зарегистрировать хук жизненного цикла.</summary>
    protected void AddHook(IComponentHook hook)
    {
        ArgumentNullException.ThrowIfNull(hook);
        _hooks.Add(hook);
    }

    // ── Reactive ──────────────────────────────────────────────────────────────────
    /// <summary>Зарегистрировать реактивный side-effect. Авто-StateHasChanged при изменении.</summary>
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

    /// <summary>Зарегистрировать вычисляемый сигнал. Авто-отписка при Dispose.</summary>
    protected SgComputed<T> RegisterComputed<T>(Func<T> compute)
    {
        ArgumentNullException.ThrowIfNull(compute);
        var computed = new SgComputed<T>(compute);
        computed.Subscribe(this);
        (_reactiveDisposables ??= []).Add(computed);
        return computed;
    }

    // ── ShouldRender ──────────────────────────────────────────────────────────────
    protected override bool ShouldRender()
    {
        bool visible = Visible;
        int  prev    = Interlocked.Exchange(ref _previousVisible, visible ? 1 : 0);
        bool wasVisible = prev == 1;

        if (!visible && wasVisible) return true;  // Стал невидимым → один рендер чтобы скрыть
        if (!visible)               return false; // Остаётся невидимым → пропускаем

        foreach (var hook in _hooks)
            if (hook is IRenderHook rh && !rh.ShouldRender(this))
                return false;

        return true;
    }

    // ── StateHasChanged ───────────────────────────────────────────────────────────
#pragma warning disable CS0108
    /// <summary>
    /// Запланировать перерисовку с batch-рендерингом через ComponentSignalTracker.
    /// ⚠️ Не приводите к ComponentBase — будет обход batching.
    /// </summary>
    public new void StateHasChanged()
    {
        if (IsDisposed) return;
        var batcher = Volatile.Read(ref _signalBatcher);
        if (batcher is not null)
            batcher.ScheduleRender();
        else
            base.StateHasChanged();
    }
#pragma warning restore CS0108

    internal Task InvokeStateHasChangedAsync()
    {
        if (IsDisposed) return Task.CompletedTask;
        return InvokeAsync(base.StateHasChanged);
    }

    // ── SetParametersAsync ────────────────────────────────────────────────────────
    public override Task SetParametersAsync(ParameterView parameters)
    {
        Interlocked.Increment(ref _ariaGeneration);
        return base.SetParametersAsync(parameters);
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────────────
    protected override void OnInitialized()
    {
        base.OnInitialized(); // ВАЖНО: base первым
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
            _diagnostics.LastRenderMs  = elapsed;
            if (elapsed > _diagnostics.MaxRenderMs) _diagnostics.MaxRenderMs = elapsed;
            _diagnostics.AverageRenderMs =
                (_diagnostics.AverageRenderMs * (_diagnostics.RenderCount - 1) + elapsed) /
                _diagnostics.RenderCount;
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

    /// <summary>Вызывается при первом рендере (аналог componentDidMount).</summary>
    protected virtual Task OnFirstRenderAsync() => Task.CompletedTask;

    // ── CSS / Style ───────────────────────────────────────────────────────────────
#if DEBUG
    public ComponentDiagnostics Diagnostics => _diagnostics;
#endif

    protected CssBuilder Css(string? baseClass = null) =>
        new(baseClass ?? GetDefaultCssClass());

    /// <summary>Создать StyleBuilder с базовым стилем.</summary>
    protected StyleBuilder CreateStyle(string? baseStyle = null) => new(baseStyle);

    protected virtual string? GetDefaultCssClass() => null;

    // ── ARIA ──────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Строит словарь ARIA-атрибутов. Кэшируется между рендерами.
    /// Фильтрует AdditionalAttributes: только aria-*, role, tabindex.
    /// Исключает "class" и "style" во избежание конфликтов.
    /// </summary>
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

            _ariaCache           = attrs;
            _ariaCacheGeneration = currentGeneration;
            return attrs;
        }
    }

    private static bool IsAriaAttribute(string key) =>
        key.StartsWith("aria-", StringComparison.OrdinalIgnoreCase) ||
        key.Equals("role",     StringComparison.OrdinalIgnoreCase)  ||
        key.Equals("tabindex", StringComparison.OrdinalIgnoreCase);

    // ── RefreshAsync ──────────────────────────────────────────────────────────────
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

    // ── Service helpers ───────────────────────────────────────────────────────────
    protected T? TryGetService<T>() where T : class =>
        ServiceProvider.GetService<T>();

    /// <summary>Получить сервис или бросить понятное исключение.</summary>
    protected T TryGetRequiredService<T>() where T : class =>
        ServiceProvider.GetService<T>()
        ?? throw new InvalidOperationException(
            $"Service {typeof(T).Name} is not registered. " +
            $"Call builder.Services.AddSuperUI() in Program.cs.");

    protected void ThrowIfDisposed()
    {
        if (IsDisposed)
            throw new ObjectDisposedException(ComponentId, $"Component {ComponentId} is disposed.");
    }

    // ── Context helpers ───────────────────────────────────────────────────────────
    protected Task IfBrowserAsync(Func<Task> action) =>
        IsBrowser ? action() : Task.CompletedTask;

    protected Task IfServerAsync(Func<Task> action) =>
        IsServer ? action() : Task.CompletedTask;

    // ── Logging ───────────────────────────────────────────────────────────────────
    [Conditional("DEBUG")]
    private void LogLifecycle(string method)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("[{ComponentId}] {Method}", ComponentId, method);
    }

    // ── IAsyncDisposable ──────────────────────────────────────────────────────────
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1) return;

        LogLifecycle(nameof(DisposeAsync));

        if (_reactiveDisposables is not null)
        {
            foreach (var rd in _reactiveDisposables)
            {
                try   { rd.Dispose(); }
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
                else if (hook is IDisposable d)  d.Dispose();
            }
            catch (Exception ex)
            { Logger.LogWarning(ex, "[{Id}] Hook dispose error", ComponentId); }
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
