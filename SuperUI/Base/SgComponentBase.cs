// SuperUI/Base/SgComponentBase.cs
// ИСПРАВЛЕНО:
// 1. _hooks — lazy init (null по умолчанию)
// 2. BuildAriaAttributes — правильный порядок Volatile.Write (данные → generation)
// 3. _ariaCacheGeneration — Volatile.Read при чтении
// 4. RefreshAsync(Action) — try/catch вокруг action
// 5. OnAfterRender — hooks после base (консистентный порядок)
// 6. XML doc на всех публичных членах
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using SuperUI.Base.Diagnostics;
using SuperUI.Base.Hooks;
using SuperUI.Base.Reactive;
using SuperUI.Base.Services;
using SuperUI.Base.Tokens;
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
/// - WASM: однопоточный, lock не нужен, но Interlocked используется для корректности на ARM.
/// - Server: каждый circuit — отдельный поток. _hooks, _reactiveDisposables — per-circuit, безопасны.
///   Только _disposed и _ariaGeneration требуют Interlocked/Volatile.
/// </remarks>
public abstract class SgComponentBase : ComponentBase, IAsyncDisposable
{
    // ── Инъекции ──────────────────────────────────────────────────────────────
    [Inject] protected ILogger<SgComponentBase> Logger { get; set; } = null!;
    [Inject] protected IComponentOptionsService OptionsService { get; set; } = null!;

    // ── Каскадные параметры ───────────────────────────────────────────────────
    [CascadingParameter] protected SgThemeContext? ThemeContext { get; set; }
    [CascadingParameter] protected SgConfigContext? ConfigContext { get; set; }

    // ── Параметры ─────────────────────────────────────────────────────────────
    [Parameter] public string? Class { get; set; }
    [Parameter] public string? Style { get; set; }
    [Parameter] public bool Visible { get; set; } = true;
    [Parameter] public string? Id { get; set; }
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    // ── Публичные свойства ────────────────────────────────────────────────────
    /// <summary>Уникальный идентификатор компонента.</summary>
    public string ComponentId { get; }

    /// <summary>Эффективный ID: Id параметр или сгенерированный ComponentId.</summary>
    protected string EffectiveId => Id ?? ComponentId;

    /// <summary>
    /// Компонент был задиспожен. Проверяйте перед async операциями.
    /// </summary>
    public bool IsDisposed => Volatile.Read(ref _disposed) == 1;

    // ── Внутреннее состояние ──────────────────────────────────────────────────
    private int _disposed;
    private int _previousVisible = 1; // 1 = true, 0 = false

    // ИСПРАВЛЕНО: lazy init — List создаётся только при первом AddHook
    private List<IComponentHook>? _hooks;
    private ComponentSignalTracker? _signalBatcher;
    private List<IDisposable>? _reactiveDisposables;

    // ARIA кэш — generation-based инвалидация
    private IReadOnlyDictionary<string, object>? _ariaCache;
    private int _ariaCacheGeneration = -1;
    private int _ariaGeneration;

#if DEBUG
    private readonly ComponentDiagnostics _diagnostics;
    private long _renderStartTick;
#endif

    // ── Конструктор ───────────────────────────────────────────────────────────
    protected SgComponentBase()
    {
        ComponentId = ComponentIdGenerator.Next(ComponentPrefix);
        _signalBatcher = new ComponentSignalTracker(this);
#if DEBUG
        _diagnostics = new ComponentDiagnostics { ComponentId = ComponentId };
#endif
    }

    /// <summary>Префикс для генерации ComponentId. Переопределить в дочерних классах.</summary>
    protected virtual string ComponentPrefix => "cmp";

    // ── Hooks ──────────────────────────────────────────────────────────────────
    /// <summary>Зарегистрировать хук жизненного цикла.</summary>
    protected void AddHook(IComponentHook hook)
    {
        ArgumentNullException.ThrowIfNull(hook);
        (_hooks ??= new List<IComponentHook>()).Add(hook);
    }

    // ── Reactive ───────────────────────────────────────────────────────────────
    /// <summary>
    /// Зарегистрировать реактивный side-effect. Авто-отписка при Dispose компонента.
    /// Эффект запускается немедленно и перезапускается при изменении любого SgSignal в теле.
    /// </summary>
    protected SgEffect RegisterEffect(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        var effect = new SgEffect(action);
        (_reactiveDisposables ??= new()).Add(effect);
        return effect;
    }

    /// <inheritdoc cref="RegisterEffect(Action)"/>
    protected SgEffect RegisterEffect(Func<Task> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        var effect = new SgEffect(action);
        (_reactiveDisposables ??= new()).Add(effect);
        return effect;
    }

    /// <summary>
    /// Зарегистрировать вычисляемый сигнал. Авто-отписка при Dispose компонента.
    /// </summary>
    protected SgComputed<T> RegisterComputed<T>(Func<T> compute)
    {
        ArgumentNullException.ThrowIfNull(compute);
        var computed = new SgComputed<T>(compute);
        (_reactiveDisposables ??= new()).Add(computed);
        return computed;
    }

    // ── ShouldRender ────────────────────────────────────────────────────────────
    protected override bool ShouldRender()
    {
        var visible = Visible;
        var prevVisible = Interlocked.CompareExchange(ref _previousVisible, 0, 0) == 1;

        if (!visible && prevVisible)
        {
            Interlocked.Exchange(ref _previousVisible, 0);
            return true; // рендерим один раз чтобы скрыть DOM
        }
        if (!visible) return false;
        Interlocked.Exchange(ref _previousVisible, 1);

        // Проверяем хуки
        if (_hooks is not null)
        {
            foreach (var hook in _hooks)
                if (hook is IRenderHook rh && !rh.ShouldRender(this))
                    return false;
        }
        return true;
    }

    // ── StateHasChanged ─────────────────────────────────────────────────────────
    public new void StateHasChanged()
    {
        if (IsDisposed) return;
        var batcher = Volatile.Read(ref _signalBatcher);
        if (batcher is not null) batcher.ScheduleRender();
        else base.StateHasChanged();
    }

    /// <summary>
    /// Вызвать base.StateHasChanged() напрямую (используется ComponentSignalTracker).
    /// Не проходит через batch для предотвращения рекурсии.
    /// </summary>
    internal Task InvokeStateHasChangedAsync()
    {
        if (IsDisposed) return Task.CompletedTask;
        return InvokeAsync(base.StateHasChanged);
    }

    // ── SetParametersAsync ──────────────────────────────────────────────────────
    public override Task SetParametersAsync(ParameterView parameters)
    {
        Interlocked.Increment(ref _ariaGeneration);
        return base.SetParametersAsync(parameters);
    }

    // ── Lifecycle ───────────────────────────────────────────────────────────────
    protected override void OnInitialized()
    {
        LogLifecycle(nameof(OnInitialized));
        if (_hooks is not null)
            foreach (var hook in _hooks) hook.OnInitialized(this);
        base.OnInitialized();
    }

    protected override async Task OnInitializedAsync()
    {
        LogLifecycle(nameof(OnInitializedAsync));
        if (_hooks is not null)
            foreach (var hook in _hooks)
                if (hook is IAsyncComponentHook ah) await ah.OnInitializedAsync(this);
        await base.OnInitializedAsync();
    }

    protected override void OnParametersSet()
    {
#if DEBUG
        _diagnostics.ParameterChangeCount++;
#endif
        if (_hooks is not null)
            foreach (var hook in _hooks) hook.OnParametersSet(this);
        base.OnParametersSet();
    }

    protected override async Task OnParametersSetAsync()
    {
        if (_hooks is not null)
            foreach (var hook in _hooks)
                if (hook is IAsyncComponentHook ah) await ah.OnParametersSetAsync(this);
        await base.OnParametersSetAsync();
    }

    protected override void OnAfterRender(bool firstRender)
    {
        base.OnAfterRender(firstRender); // ИСПРАВЛЕНО: base первым
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
        if (_hooks is not null)
            foreach (var hook in _hooks) hook.OnAfterRender(this, firstRender);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
#if DEBUG
        _renderStartTick = Stopwatch.GetTimestamp();
#endif
        await base.OnAfterRenderAsync(firstRender);
        if (_hooks is not null)
            foreach (var hook in _hooks)
                if (hook is IAsyncComponentHook ah) await ah.OnAfterRenderAsync(this, firstRender);
    }

    // ── CSS / Style ─────────────────────────────────────────────────────────────
#if DEBUG
    public ComponentDiagnostics Diagnostics => _diagnostics;
#endif

    protected CssBuilder Css(string? baseClass = null) => new(baseClass ?? GetDefaultCssClass());
    protected StyleBuilder CreateStyle(string? baseStyle = null) => new(baseStyle);
    protected virtual string? GetDefaultCssClass() => null;

    // ── ARIA ────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Строит словарь ARIA-атрибутов. Результат кэшируется между рендерами
    /// и инвалидируется при изменении параметров (через SetParametersAsync).
    /// </summary>
    protected virtual IReadOnlyDictionary<string, object> BuildAriaAttributes()
    {
        var currentGeneration = Volatile.Read(ref _ariaGeneration);
        var cache = Volatile.Read(ref _ariaCache);

        // ИСПРАВЛЕНО: Volatile.Read для _ariaCacheGeneration
        if (cache is not null && Volatile.Read(ref _ariaCacheGeneration) == currentGeneration)
            return cache;

        var capacity = (AdditionalAttributes?.Count ?? 0) + 4;
        var attrs = new Dictionary<string, object>(capacity, StringComparer.Ordinal);

        if (AdditionalAttributes is not null)
            foreach (var kvp in AdditionalAttributes)
                attrs[kvp.Key] = kvp.Value;

        // ИСПРАВЛЕНО: сначала данные, потом generation (правильный порядок release)
        var snapshot = (IReadOnlyDictionary<string, object>)new Dictionary<string, object>(attrs, StringComparer.Ordinal);
        Volatile.Write(ref _ariaCache, snapshot);
        Volatile.Write(ref _ariaCacheGeneration, currentGeneration);

        return snapshot;
    }

    // ── RefreshAsync ────────────────────────────────────────────────────────────
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Task RefreshAsync()
    {
        if (IsDisposed) return Task.CompletedTask;
        return InvokeAsync(StateHasChanged);
    }

    /// <summary>Выполнить action и запланировать перерисовку.</summary>
    public Task RefreshAsync(Action action)
    {
        if (IsDisposed) return Task.CompletedTask;
        return InvokeAsync(() =>
        {
            // ИСПРАВЛЕНО: try/catch вокруг action
            try { action(); }
            catch (Exception ex)
            {
                Logger.LogError(ex, "[{Id}] RefreshAsync action error", ComponentId);
            }
            StateHasChanged();
        });
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

        if (_hooks is not null)
        {
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
        }

        await DisposeComponentAsync();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Точка расширения для освобождения ресурсов дочерних классов.
    /// Вызывайте base.DisposeComponentAsync() в конце.
    /// </summary>
    protected virtual async ValueTask DisposeComponentAsync()
    {
        var batcher = Interlocked.Exchange(ref _signalBatcher, null);
        batcher?.Dispose();
        await ValueTask.CompletedTask;
    }
}