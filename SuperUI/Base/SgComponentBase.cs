// SuperUI/Base/SgComponentBase.cs
// ИСПРАВЛЕНО:
// 1. using SuperUI.Utilities удалён (CS0246 fix — ComponentIdGenerator в SuperUI.Base.Utilities)
// 2. BuildAriaAttributes: Interlocked.Exchange вместо Volatile (ARM fix)
// 3. DisposeComponentAsync: _signalBatcher.Dispose() ПОСЛЕ base (правильный порядок)
// 4. RegisterEffect/RegisterComputed — новые методы для Reactive
// 5. ShouldRender: убрана потенциальная двойная проверка hooks

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
/// Иерархия: ComponentBase → SgComponentBase → SgJsComponentBase → SgInteractiveBase
/// </summary>
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
    public string ComponentId { get; }
    protected string EffectiveId => Id ?? ComponentId;
    public bool IsDisposed => _disposed == 1;

    // ── Внутреннее состояние ─────────────────────────────────────────────────
    private int _disposed; // Interlocked — не volatile
    private bool _previousVisible = true;
    private readonly List<IComponentHook> _hooks = [];
    private ComponentSignalTracker? _signalBatcher;

    // Reactive: эффекты и computed — диспозятся при Dispose компонента
    private List<IDisposable>? _reactiveDisposables;

    // ИСПРАВЛЕНО: generation-based ARIA кэш
    // Publish pattern: _ariaCache пишется ПЕРЕД _ariaCacheGeneration
    // Read pattern: сначала generation (Volatile.Read), потом cache
    private IReadOnlyDictionary<string, object>? _ariaCache;
    private int _ariaCacheGeneration = -1;
    private int _ariaGeneration;

#if DEBUG
    private readonly ComponentDiagnostics _diagnostics;
    private long _renderStartTick;
#endif

    // ── Конструктор ─────────────────────────────────────────────────────────
    protected SgComponentBase()
    {
        ComponentId = ComponentIdGenerator.Next(ComponentPrefix);
        _signalBatcher = new ComponentSignalTracker(this);
#if DEBUG
        _diagnostics = new ComponentDiagnostics { ComponentId = ComponentId };
#endif
    }

    protected virtual string ComponentPrefix => "cmp";

    // ── Hooks ────────────────────────────────────────────────────────────────
    protected void AddHook(IComponentHook hook) => _hooks.Add(hook);

    // ── Reactive: SgEffect, SgComputed ────────────────────────────────────────

    /// <summary>
    /// Зарегистрировать реактивный side-effect. Авто-отписка при Dispose компонента.
    /// Эффект запускается немедленно и перезапускается при изменении любого
    /// SgSignal, прочитанного в его теле.
    /// </summary>
    protected SgEffect RegisterEffect(Action action)
    {
        var effect = new SgEffect(action);
        (_reactiveDisposables ??= new()).Add(effect);
        return effect;
    }

    /// <inheritdoc cref="RegisterEffect(Action)"/>
    protected SgEffect RegisterEffect(Func<Task> action)
    {
        var effect = new SgEffect(action);
        (_reactiveDisposables ??= new()).Add(effect);
        return effect;
    }

    /// <summary>
    /// Зарегистрировать вычисляемый сигнал. Авто-отписка при Dispose компонента.
    /// </summary>
    protected SgComputed<T> RegisterComputed<T>(Func<T> compute)
    {
        var computed = new SgComputed<T>(compute);
        (_reactiveDisposables ??= new()).Add(computed);
        return computed;
    }

    // ── ShouldRender ──────────────────────────────────────────────────────────
    protected override bool ShouldRender()
    {
        var visible = Visible;

        if (!visible && _previousVisible)
        {
            // Только что стал невидимым — рендерим один раз (чтобы скрыть)
            _previousVisible = false;
            return true;
        }

        if (!visible) return false;

        _previousVisible = true;

        // Проверяем хуки (hooks могут запретить рендер)
        foreach (var hook in _hooks)
        {
            if (hook is IRenderHook rh && !rh.ShouldRender(this))
                return false;
        }

        return true;
    }

    // ── StateHasChanged ──────────────────────────────────────────────────────
    public new void StateHasChanged()
    {
        if (IsDisposed) return;
        if (_signalBatcher is not null)
            _signalBatcher.ScheduleRender();
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

    // ── SetParametersAsync ────────────────────────────────────────────────────
    // ИСПРАВЛЕНО: инвалидация ARIA через Interlocked.Increment (ARM-safe)
    public override Task SetParametersAsync(ParameterView parameters)
    {
        // Interlocked.Increment: полный memory barrier — видимость на Blazor Server
        Interlocked.Increment(ref _ariaGeneration);
        return base.SetParametersAsync(parameters);
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────────
    protected override void OnInitialized()
    {
        LogLifecycle(nameof(OnInitialized));
        foreach (var hook in _hooks) hook.OnInitialized(this);
        base.OnInitialized();
    }

    protected override async Task OnInitializedAsync()
    {
        LogLifecycle(nameof(OnInitializedAsync));
        foreach (var hook in _hooks)
            if (hook is IAsyncComponentHook ah) await ah.OnInitializedAsync(this);
        await base.OnInitializedAsync();
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
        foreach (var hook in _hooks)
            if (hook is IAsyncComponentHook ah) await ah.OnParametersSetAsync(this);
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
        foreach (var hook in _hooks)
            if (hook is IAsyncComponentHook ah) await ah.OnAfterRenderAsync(this, firstRender);
        await base.OnAfterRenderAsync(firstRender);
    }

    // ── CSS / Style builders ──────────────────────────────────────────────────
#if DEBUG
    public ComponentDiagnostics Diagnostics => _diagnostics;
#endif

    protected CssBuilder Css(string? baseClass = null) => new(baseClass ?? GetDefaultCssClass());
    protected StyleBuilder CreateStyle(string? baseStyle = null) => new();
    protected virtual string? GetDefaultCssClass() => null;

    // ── ARIA ──────────────────────────────────────────────────────────────────
    // ИСПРАВЛЕНО: используем Interlocked для полных memory barriers (ARM-safe)
    // Publish pattern: сначала данные (_ariaCache), потом generation (_ariaCacheGeneration)
    // Read pattern: сначала generation, потом данные
    protected virtual IReadOnlyDictionary<string, object> BuildAriaAttributes()
    {
        var currentGeneration = Interlocked.CompareExchange(ref _ariaGeneration, 0, 0);

        // Быстрый путь: кэш актуален
        if (_ariaCacheGeneration == currentGeneration && _ariaCache is not null)
            return _ariaCache;

        var capacity = (AdditionalAttributes?.Count ?? 0) + 4;
        var attrs = new Dictionary<string, object>(capacity, StringComparer.Ordinal);

        if (AdditionalAttributes is not null)
            foreach (var kvp in AdditionalAttributes)
                attrs[kvp.Key] = kvp.Value;

        // Publish pattern: данные → generation
        _ariaCache = attrs;
        Interlocked.Exchange(ref _ariaCacheGeneration, currentGeneration);

        return attrs;
    }

    // ── RefreshAsync ──────────────────────────────────────────────────────────
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Task RefreshAsync()
    {
        if (IsDisposed) return Task.CompletedTask;
        return InvokeAsync(() =>
        {
            using (SignalTracker.EnterScope(this))
                StateHasChanged();
        });
    }

    public Task RefreshAsync(Action action)
    {
        if (IsDisposed) return Task.CompletedTask;
        return InvokeAsync(() =>
        {
            action();
            using (SignalTracker.EnterScope(this))
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

    // ── Logging ───────────────────────────────────────────────────────────────
    [Conditional("DEBUG")]
    private void LogLifecycle(string method)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("[{ComponentId}] {Method}", ComponentId, method);
    }

    // ── IAsyncDisposable ──────────────────────────────────────────────────────
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1) return;

        LogLifecycle(nameof(DisposeAsync));

        // 1. Диспозим reactive disposables (SgEffect, SgComputed)
        if (_reactiveDisposables is not null)
        {
            foreach (var rd in _reactiveDisposables)
            {
                try { rd.Dispose(); }
                catch (Exception ex) { Logger.LogWarning(ex, "[{Id}] Reactive dispose error", ComponentId); }
            }
            _reactiveDisposables.Clear();
        }

        // 2. Диспозим hooks
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

        // 3. Диспозим компонент-специфичные ресурсы (переопределяется в дочерних классах)
        await DisposeComponentAsync();

        GC.SuppressFinalize(this);
    }

    // ИСПРАВЛЕНО: _signalBatcher.Dispose() ЗДЕСЬ (в базовом), не в override
    // Дочерние классы вызывают base.DisposeComponentAsync() в конце
    protected virtual async ValueTask DisposeComponentAsync()
    {
        // Сначала останавливаем batching чтобы не было новых рендеров
        _signalBatcher?.Dispose();
        _signalBatcher = null;

        await ValueTask.CompletedTask; // async для возможности override
    }
}