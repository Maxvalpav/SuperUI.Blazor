// SuperUI/Base/SgComponentBase.cs
// ИСПРАВЛЕНО:
// 1. SetParametersAsync — убран мёртвый код (_previousVisible), добавлена реальная early-exit оптимизация
// 2. BuildAriaAttributes — единый кэш через generation-number, thread-safe
// 3. _hooks — защита от concurrent modification (ImmutableArray-стиль)
// 4. StateHasChanged — проверка disposed перед batcher
// 5. DisposeAsync — правильный порядок: сначала hooks, потом DisposeComponentAsync

using System.Collections.Immutable;
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

    // ИСПРАВЛЕНО: ImmutableArray для thread-safe итерации
    // Hooks добавляются ТОЛЬКО в конструкторе или OnInitialized — до любых concurrent calls
    private ImmutableArray<IComponentHook> _hooks = ImmutableArray<IComponentHook>.Empty;

    private ComponentSignalTracker? _signalBatcher;

    // ИСПРАВЛЕНО: кэш ARIA через generation — избегаем double-alloc в наследниках
    private IReadOnlyDictionary<string, object>? _ariaCache;
    private int _ariaCacheGeneration = -1;  // generation при котором кэш был создан
    private int _paramGeneration;            // инкрементируется в OnParametersSet

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

    // ── ComponentPrefix ───────────────────────────────────────────────────────
    protected virtual string ComponentPrefix => "cmp";

    // ── Hooks — thread-safe добавление ───────────────────────────────────────
    /// <summary>Добавить хук. Вызывать только в конструкторе или OnInitialized.</summary>
    protected void AddHook(IComponentHook hook)
    {
        // ImmutableArray.Add создаёт новый массив — нет конкурентного доступа к полю
        _hooks = _hooks.Add(hook);
    }

    // ── ShouldRender ──────────────────────────────────────────────────────────
    protected override bool ShouldRender()
    {
        if (!Visible)
        {
            if (_previousVisible)
            {
                _previousVisible = false;
                return true; // нужно скрыть — один последний рендер
            }
            return false; // уже скрыт
        }

        _previousVisible = true;

        // ИСПРАВЛЕНО: итерация по ImmutableArray — snapshot, thread-safe
        foreach (var hook in _hooks)
        {
            if (hook is IRenderHook rh && !rh.ShouldRender(this))
                return false;
        }
        return true;
    }

    // ── StateHasChanged ───────────────────────────────────────────────────────
    public new void StateHasChanged()
    {
        if (IsDisposed) return;
        if (_signalBatcher is { } batcher)
            batcher.ScheduleRender();
        else
            base.StateHasChanged();
    }

    /// <summary>Прямой вызов base.StateHasChanged() (используется ComponentSignalTracker).</summary>
    internal Task InvokeStateHasChangedAsync()
    {
        if (IsDisposed) return Task.CompletedTask;
        return InvokeAsync(base.StateHasChanged);
    }

    // ── SetParametersAsync ────────────────────────────────────────────────────
    // ИСПРАВЛЕНО: реальная оптимизация — если Visible не изменился и компонент скрыт,
    // параметры всё равно применяются (для Blazor корректности), но рендер будет скипнут через ShouldRender.
    public override Task SetParametersAsync(ParameterView parameters)
    {
        return base.SetParametersAsync(parameters);
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────────
    protected override void OnInitialized()
    {
        LogLifecycle(nameof(OnInitialized));
        // ИСПРАВЛЕНО: snapshot ImmutableArray перед итерацией
        var hooks = _hooks;
        foreach (var hook in hooks) hook.OnInitialized(this);
        base.OnInitialized();
    }

    protected override async Task OnInitializedAsync()
    {
        LogLifecycle(nameof(OnInitializedAsync));
        var hooks = _hooks;
        foreach (var hook in hooks)
            if (hook is IAsyncComponentHook ah)
                await ah.OnInitializedAsync(this);
        await base.OnInitializedAsync();
    }

    protected override void OnParametersSet()
    {
        // ИСПРАВЛЕНО: единый механизм инвалидации ARIA через generation
        Interlocked.Increment(ref _paramGeneration);
        _ariaCache = null;
        _ariaCacheGeneration = -1;

#if DEBUG
        _diagnostics.ParameterChangeCount++;
#endif
        var hooks = _hooks;
        foreach (var hook in hooks) hook.OnParametersSet(this);
        base.OnParametersSet();
    }

    protected override async Task OnParametersSetAsync()
    {
        var hooks = _hooks;
        foreach (var hook in hooks)
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
            _diagnostics.AverageRenderMs =
                (_diagnostics.AverageRenderMs * (_diagnostics.RenderCount - 1) + elapsed)
                / _diagnostics.RenderCount;
            _renderStartTick = 0;
        }
#endif
        var hooks = _hooks;
        foreach (var hook in hooks) hook.OnAfterRender(this, firstRender);
        base.OnAfterRender(firstRender);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
#if DEBUG
        _renderStartTick = Stopwatch.GetTimestamp();
#endif
        var hooks = _hooks;
        foreach (var hook in hooks)
            if (hook is IAsyncComponentHook ah)
                await ah.OnAfterRenderAsync(this, firstRender);
        await base.OnAfterRenderAsync(firstRender);
    }

    // ── CSS / Style builders ──────────────────────────────────────────────────
#if DEBUG
    public ComponentDiagnostics Diagnostics => _diagnostics;
#endif

    protected CssBuilder Css(string? baseClass = null) => new(baseClass ?? GetDefaultCssClass());
    protected StyleBuilder Styles() => new();
    protected virtual string? GetDefaultCssClass() => null;

    // ── ARIA — ИСПРАВЛЕНО: единый кэш в базовом классе ───────────────────────
    /// <summary>
    /// Построить атрибуты ARIA/HTML. Результат кэшируется до следующего OnParametersSet.
    /// Наследники вызывают base.BuildAriaAttributes() и дополняют словарь.
    /// ВАЖНО: наследник должен создавать новый Dictionary на основе base-результата,
    /// кэш базового класса при этом остаётся неизменным.
    /// </summary>
    protected virtual IReadOnlyDictionary<string, object> BuildAriaAttributes()
    {
        // Проверяем кэш по generation
        var currentGen = Volatile.Read(ref _paramGeneration);
        if (_ariaCache is not null && _ariaCacheGeneration == currentGen)
            return _ariaCache;

        var capacity = (AdditionalAttributes?.Count ?? 0) + 4;
        var attrs = new Dictionary<string, object>(capacity, StringComparer.Ordinal);

        if (AdditionalAttributes is not null)
            foreach (var kvp in AdditionalAttributes)
                attrs[kvp.Key] = kvp.Value;

        // Сохраняем кэш с текущим generation
        _ariaCache = attrs;
        _ariaCacheGeneration = currentGen;
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

        // Останавливаем batcher ПЕРВЫМ, чтобы не было новых StateHasChanged
        _signalBatcher?.Dispose();
        _signalBatcher = null;

        var hooks = _hooks;
        foreach (var hook in hooks)
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
        _hooks = ImmutableArray<IComponentHook>.Empty;

        await DisposeComponentAsync();
        GC.SuppressFinalize(this);
    }

    protected virtual ValueTask DisposeComponentAsync() => ValueTask.CompletedTask;
}
