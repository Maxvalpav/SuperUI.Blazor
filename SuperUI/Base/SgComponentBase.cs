// SuperUI/Base/SgComponentBase.cs
// ИСПРАВЛЕНО:
// 1. CreateStyle(baseStyle) — передаём baseStyle в StyleBuilder
// 2. RefreshAsync: SignalTracker.EnterScope — убран (не нужен, StateHasChanged синхронный)
// 3. _previousVisible через int + Interlocked (thread-safe на Server)
// 4. StateHasChanged — null-check с Volatile.Read(_signalBatcher)
// 5. Документация lifecycle methods

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
///   Только _disposed и _ariaGeneration требуют Interlocked.
/// </remarks>
public abstract class SgComponentBase : ComponentBase, IAsyncDisposable
{
    // ── Инъекции ────────────────────────────────────────────────────────────────
    [Inject] protected ILogger<SgComponentBase> Logger { get; set; } = null!;
    [Inject] protected IComponentOptionsService OptionsService { get; set; } = null!;

    // ── Каскадные параметры ─────────────────────────────────────────────────────
    [CascadingParameter] protected SgThemeContext? ThemeContext { get; set; }
    [CascadingParameter] protected SgConfigContext? ConfigContext { get; set; }

    // ── Параметры ───────────────────────────────────────────────────────────────
    [Parameter] public string? Class { get; set; }
    [Parameter] public string? Style { get; set; }
    [Parameter] public bool Visible { get; set; } = true;
    [Parameter] public string? Id { get; set; }
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    // ── Публичные свойства ──────────────────────────────────────────────────────
    public string ComponentId { get; }
    protected string EffectiveId => Id ?? ComponentId;

    /// <summary>Компонент был задиспожен. Проверяйте перед async операциями.</summary>
    public bool IsDisposed => Volatile.Read(ref _disposed) == 1;

    // ── Внутреннее состояние ────────────────────────────────────────────────────
    // ИСПРАВЛЕНО: _disposed через Volatile.Read в IsDisposed (не свойство с Interlocked)
    private int _disposed;
    // ИСПРАВЛЕНО: _previousVisible через int + Interlocked (thread-safe на Server)
    private int _previousVisible = 1; // 1 = true, 0 = false
    private readonly List<IComponentHook> _hooks = [];
    private ComponentSignalTracker? _signalBatcher;
    private List<IDisposable>? _reactiveDisposables;

    // ИСПРАВЛЕНО: generation-based ARIA кэш (Interlocked для ARM)
    private IReadOnlyDictionary<string, object>? _ariaCache;
    private int _ariaCacheGeneration = -1;
    private int _ariaGeneration;

#if DEBUG
    private readonly ComponentDiagnostics _diagnostics;
    private long _renderStartTick;
#endif

    // ── Конструктор ─────────────────────────────────────────────────────────────
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

    // ── Hooks ────────────────────────────────────────────────────────────────────
    /// <summary>Зарегистрировать хук жизненного цикла.</summary>
    protected void AddHook(IComponentHook hook)
    {
        ArgumentNullException.ThrowIfNull(hook);
        _hooks.Add(hook);
    }

    // ── Reactive ─────────────────────────────────────────────────────────────────
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

    // ── ShouldRender ──────────────────────────────────────────────────────────────
    protected override bool ShouldRender()
    {
        var visible = Visible;
        var prevVisible = Interlocked.CompareExchange(ref _previousVisible, 0, 0) == 1;

        if (!visible && prevVisible)
        {
            // Только что стал невидимым — рендерим один раз (чтобы скрыть DOM)
            Interlocked.Exchange(ref _previousVisible, 0);
            return true;
        }

        if (!visible) return false;

        Interlocked.Exchange(ref _previousVisible, 1);

        // Проверяем хуки (хук может запретить рендер)
        foreach (var hook in _hooks)
        {
            if (hook is IRenderHook rh && !rh.ShouldRender(this))
                return false;
        }

        return true;
    }

    // ── StateHasChanged ───────────────────────────────────────────────────────────
    public new void StateHasChanged()
    {
        if (IsDisposed) return;

        // ИСПРАВЛЕНО: Volatile.Read для видимости между потоками на Server
        var batcher = Volatile.Read(ref _signalBatcher);
        if (batcher is not null)
            batcher.ScheduleRender();
        else
            base.StateHasChanged();
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

    // ── SetParametersAsync ────────────────────────────────────────────────────────
    public override Task SetParametersAsync(ParameterView parameters)
    {
        // Interlocked.Increment: полный memory barrier — видимость на Blazor Server (ARM)
        Interlocked.Increment(ref _ariaGeneration);
        return base.SetParametersAsync(parameters);
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────────────
    protected override void OnInitialized()
    {
        LogLifecycle(nameof(OnInitialized));
        foreach (var hook in _hooks)
            hook.OnInitialized(this);
        base.OnInitialized();
    }

    protected override async Task OnInitializedAsync()
    {
        LogLifecycle(nameof(OnInitializedAsync));
        foreach (var hook in _hooks)
            if (hook is IAsyncComponentHook ah)
                await ah.OnInitializedAsync(this);
        await base.OnInitializedAsync();
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
        foreach (var hook in _hooks)
            if (hook is IAsyncComponentHook ah)
                await ah.OnParametersSetAsync(this);
        await base.OnParametersSetAsync();
    }

    protected override void OnAfterRender(bool firstRender)
    {
#if DEBUG
        if (_renderStartTick > 0)
        {
            var elapsed = Stopwatch.GetElapsedTime(_renderStartTick).TotalMilliseconds;
            _diagnostics.RenderCount++;
            _diagnostics.LastRenderMs = elapsed;
            if (elapsed > _diagnostics.MaxRenderMs)
                _diagnostics.MaxRenderMs = elapsed;
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
        foreach (var hook in _hooks)
            if (hook is IAsyncComponentHook ah)
                await ah.OnAfterRenderAsync(this, firstRender);
        await base.OnAfterRenderAsync(firstRender);
    }

    // ── CSS / Style ───────────────────────────────────────────────────────────────
#if DEBUG
    public ComponentDiagnostics Diagnostics => _diagnostics;
#endif

    protected CssBuilder Css(string? baseClass = null)
        => new(baseClass ?? GetDefaultCssClass());

    // ИСПРАВЛЕНО: baseStyle передаётся в StyleBuilder (был проигнорирован)
    protected StyleBuilder CreateStyle(string? baseStyle = null)
        => new(baseStyle);

    protected virtual string? GetDefaultCssClass() => null;

    // ── ARIA ──────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Строит словарь ARIA-атрибутов. Результат кэшируется между рендерами
    /// и инвалидируется при изменении параметров (через SetParametersAsync).
    /// </summary>
    protected virtual IReadOnlyDictionary<string, object> BuildAriaAttributes()
    {
        // Volatile.Read достаточен — Interlocked.Increment в SetParametersAsync уже даёт release-barrier.
        var currentGeneration = Volatile.Read(ref _ariaGeneration);
        var cache = Volatile.Read(ref _ariaCache);

        if (cache is not null && _ariaCacheGeneration == currentGeneration)
            return cache;

        var capacity = (AdditionalAttributes?.Count ?? 0) + 4;
        var attrs = new Dictionary<string, object>(capacity, StringComparer.Ordinal);

        if (AdditionalAttributes is not null)
            foreach (var kvp in AdditionalAttributes)
                attrs[kvp.Key] = kvp.Value;

        // Publish: сначала ПОЛНОСТЬЮ записываем данные, затем generation —
        // читатель, увидевший новый generation, гарантированно видит новый словарь.
        Volatile.Write(ref _ariaCache, attrs);
        Volatile.Write(ref _ariaCacheGeneration, currentGeneration);
        return attrs;
    }

    // ── RefreshAsync ──────────────────────────────────────────────────────────────
    /// <summary>Запланировать перерисовку компонента из любого потока.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Task RefreshAsync()
    {
        if (IsDisposed) return Task.CompletedTask;
        return InvokeAsync(StateHasChanged);
    }

    /// <summary>Выполнить <paramref name="action"/> и запланировать перерисовку.</summary>
    public Task RefreshAsync(Action action)
    {
        if (IsDisposed) return Task.CompletedTask;
        return InvokeAsync(() =>
        {
            action();
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

        // 1. Реактивные disposables (SgEffect, SgComputed)
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

        // 3. Компонент-специфичные ресурсы (переопределяется в дочерних классах)
        await DisposeComponentAsync();

        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Точка расширения для освобождения ресурсов дочерних классов.
    /// Вызывайте base.DisposeComponentAsync() в конце.
    /// </summary>
    protected virtual async ValueTask DisposeComponentAsync()
    {
        // Останавливаем batching — новых рендеров больше не будет
        var batcher = Interlocked.Exchange(ref _signalBatcher, null);
        batcher?.Dispose();
        await ValueTask.CompletedTask;
    }
}