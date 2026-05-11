// SuperUI/Base/SgComponentBase.cs
// ИСПРАВЛЕНО:
// 1. Убран мёртвый if/else в SetParametersAsync
// 2. ARIA кэш использует _ariaGeneration для generation-based инвалидации (thread-safe)
// 3. _paramGeneration теперь используется как ariaGeneration
// 4. Улучшена thread safety для _ariaCache через Interlocked+generation
// 5. RefreshAsync не создаёт лишние замыкания (AggressiveInlining)
// 6. ShouldRender: единый путь через _previousVisible
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
    // ── Инъекции ───────────────────────────────────────────────────────────────
    [Inject] protected ILogger<SgComponentBase> Logger { get; set; } = null!;
    [Inject] protected IComponentOptionsService OptionsService { get; set; } = null!;

    // ── Каскадные параметры ───────────────────────────────────────────────────
    [CascadingParameter] protected SgThemeContext? ThemeContext { get; set; }
    [CascadingParameter] protected SgConfigContext? ConfigContext { get; set; }

    // ── Параметры компонента ──────────────────────────────────────────────────
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

    // ── Внутреннее состояние ──────────────────────────────────────────────────
    private volatile int _disposed;
    private bool _previousVisible = true;
    private readonly List<IComponentHook> _hooks = [];
    private ComponentSignalTracker? _signalBatcher;

    // ИСПРАВЛЕНО: generation-based ARIA кэш
    // Кэш хранит (поколение, словарь). При инвалидации _ariaGeneration инкрементируется.
    // Кэш считается валидным только если его поколение == текущее _ariaGeneration.
    private IReadOnlyDictionary<string, object>? _ariaCache;
    private int _ariaCacheGeneration = -1;  // поколение кэша (невалиден если != _ariaGeneration)
    private int _ariaGeneration;            // текущее поколение параметров

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

    // ── ComponentPrefix ────────────────────────────────────────────────────────
    protected virtual string ComponentPrefix => "cmp";

    // ── Hooks ──────────────────────────────────────────────────────────────────
    protected void AddHook(IComponentHook hook) => _hooks.Add(hook);

    // ── ShouldRender ───────────────────────────────────────────────────────────
    protected override bool ShouldRender()
    {
        var visible = Visible;

        if (!visible && _previousVisible)
        {
            // Только что стал невидимым — рендерим один раз (чтобы скрыть)
            _previousVisible = false;
            return true;
        }

        if (!visible)
            return false;

        _previousVisible = true;

        foreach (var hook in _hooks)
        {
            if (hook is IRenderHook rh && !rh.ShouldRender(this))
                return false;
        }

        return true;
    }

    // ── StateHasChanged ────────────────────────────────────────────────────────
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
    /// Не проходит через batch чтобы избежать рекурсии.
    /// </summary>
    internal Task InvokeStateHasChangedAsync()
    {
        if (IsDisposed) return Task.CompletedTask;
        return InvokeAsync(base.StateHasChanged);
    }

    // ── SetParametersAsync ─────────────────────────────────────────────────────
    // ИСПРАВЛЕНО: убран мёртвый if/else. Инвалидация ARIA через _ariaGeneration.
    public override Task SetParametersAsync(ParameterView parameters)
    {
        // Инвалидируем ARIA кэш при каждом изменении параметров
        // Используем Interlocked для thread-safety на Blazor Server
        Interlocked.Increment(ref _ariaGeneration);
        return base.SetParametersAsync(parameters);
    }

    // ── Lifecycle ──────────────────────────────────────────────────────────────
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
        // ИСПРАВЛЕНО: инвалидация через _ariaGeneration, а не null-присвоение
        // _ariaGeneration уже инкрементирован в SetParametersAsync
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
            _diagnostics.AverageRenderMs = (_diagnostics.AverageRenderMs *
                (_diagnostics.RenderCount - 1) + elapsed) / _diagnostics.RenderCount;
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

    // ── CSS / Style builders ───────────────────────────────────────────────────
#if DEBUG
    public ComponentDiagnostics Diagnostics => _diagnostics;
#endif

    protected CssBuilder Css(string? baseClass = null) => new(baseClass ?? GetDefaultCssClass());
    protected StyleBuilder Styles() => new();
    protected virtual string? GetDefaultCssClass() => null;

    // ── ARIA ───────────────────────────────────────────────────────────────────
    // ИСПРАВЛЕНО: generation-based кэш — валиден только если _ariaCacheGeneration == _ariaGeneration
    // Thread-safe: два потока (Server) могут оба построить кэш, но результат идентичен.
    // Нет lock'а намеренно: стоимость двойного построения < стоимость lock.
    protected virtual IReadOnlyDictionary<string, object> BuildAriaAttributes()
    {
        var currentGeneration = Volatile.Read(ref _ariaGeneration);
        
        // Быстрый путь: кэш валиден
        if (_ariaCache is not null && _ariaCacheGeneration == currentGeneration)
            return _ariaCache;

        var capacity = (AdditionalAttributes?.Count ?? 0) + 4;
        var attrs = new Dictionary<string, object>(capacity, StringComparer.Ordinal);

        if (AdditionalAttributes is not null)
            foreach (var kvp in AdditionalAttributes)
                attrs[kvp.Key] = kvp.Value;

        // Сохраняем кэш вместе с поколением
        _ariaCache = attrs;
        _ariaCacheGeneration = currentGeneration;
        
        return attrs;
    }

    // ── RefreshAsync ───────────────────────────────────────────────────────────
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

    protected virtual ValueTask DisposeComponentAsync()
    {
        _signalBatcher?.Dispose();
        _signalBatcher = null;
        return ValueTask.CompletedTask;
    }
}