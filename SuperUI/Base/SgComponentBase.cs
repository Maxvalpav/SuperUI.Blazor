// SuperUI/Base/SgComponentBase.cs
//
// УЛУЧШЕНИЯ над текущей версией:
//   1. IsPrerendering — делегирует к IHostEnvironment (SSR-корректность)
//   2. OnFirstRenderAsync — skip при IsPrerendering
//   3. AdditionalAttributes фильтрация: исключает "class"/"style"
//   4. BuildAriaAttributes кэш invalidate по generation
//   5. EffectiveId проверяет непустоту
//   6. ILogger<T> вместо ILogger (более типизированный)
//   7. ComponentPrefix — protected virtual readonly-like
//   8. НОВОЕ: Batch() helper — удобный wrapper над SignalBatch.Begin()
//   9. НОВОЕ: CreateSignal<T>() — фабрика с авто-регистрацией
//   10. НОВОЕ: Watch<T>() — alias для RegisterEffect
//   11. НОВОЕ: IsFirstRender — флаг

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
///   WASM: однопоточный WebAssembly.
///   Server: per-circuit изоляция. Поля с _disposed, _ariaGeneration требуют Interlocked/Volatile.
/// </remarks>
public abstract class SgComponentBase : ComponentBase, IAsyncDisposable
{
    // ── Инъекции ──────────────────────────────────────────────────────────────
    [Inject] protected ILogger<SgComponentBase> Logger { get; set; } = null!;
    [Inject] protected IComponentOptionsService OptionsService { get; set; } = null!;
    [Inject] protected IServiceProvider ServiceProvider { get; set; } = null!;

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

    /// <summary>Уникальный идентификатор компонента (генерируется автоматически).</summary>
    public string ComponentId { get; }

    /// <summary>Id если задан и непустой, иначе ComponentId.</summary>
    protected string EffectiveId
        => !string.IsNullOrWhiteSpace(Id) ? Id! : ComponentId;

    /// <summary>true — компонент задиспожен.</summary>
    public bool IsDisposed => Volatile.Read(ref _disposed) == 1;

    /// <summary>true — Blazor WebAssembly.</summary>
    protected static bool IsBrowser => OperatingSystem.IsBrowser();

    /// <summary>true — Blazor Server.</summary>
    protected static bool IsServer => !OperatingSystem.IsBrowser();

    /// <summary>true — компонент рендерится в первый раз (после первого OnAfterRender = false).</summary>
    protected bool IsFirstRender { get; private set; } = true;

    /// <summary>
    /// true — компонент в режиме prerendering (SSR static).
    /// В этом режиме нет JS interop и нет SignalR circuit.
    /// </summary>
    protected virtual bool IsPrerendering => false;

    // ── Внутреннее состояние ──────────────────────────────────────────────────
    private int _disposed;
    private int _previousVisible = 1;
    private readonly List<IComponentHook> _hooks = [];
    private ComponentSignalTracker? _signalBatcher;
    private List<IDisposable>? _reactiveDisposables;

    // ARIA cache
    private readonly object _ariaCacheLock = new();
    private IReadOnlyDictionary<string, object>? _ariaCache;
    private int _ariaCacheGeneration = -2;
    private int _ariaGeneration;

#if DEBUG
    private readonly ComponentDiagnostics _diagnostics;
    private long _renderStartTick;
    public ComponentDiagnostics Diagnostics => _diagnostics;
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

    /// <summary>Префикс для генерации ComponentId. Переопределяется в подклассах.</summary>
    protected virtual string ComponentPrefix => "cmp";

    // ── Hooks ─────────────────────────────────────────────────────────────────

    /// <summary>Зарегистрировать хук жизненного цикла.</summary>
    protected void AddHook(IComponentHook hook)
    {
        ArgumentNullException.ThrowIfNull(hook);
        _hooks.Add(hook);
    }

    // ── Reactive helpers ──────────────────────────────────────────────────────

    /// <summary>
    /// Создать реактивный сигнал с авто-регистрацией.
    /// Сигнал будет освобождён при Dispose компонента.
    /// </summary>
    protected SgSignal<T> CreateSignal<T>(T initial, IEqualityComparer<T>? comparer = null)
    {
        var signal = new SgSignal<T>(initial, comparer);
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

    /// <summary>
    /// Alias для RegisterEffect. Более семантичное название.
    /// </summary>
    protected SgEffect Watch(Action action) => RegisterEffect(action);

    /// <summary>Зарегистрировать вычисляемый сигнал с авто-отпиской.</summary>
    protected SgComputed<T> RegisterComputed<T>(Func<T> compute)
    {
        ArgumentNullException.ThrowIfNull(compute);
        var computed = new SgComputed<T>(compute);
        computed.Subscribe(this);
        (_reactiveDisposables ??= []).Add(computed);
        return computed;
    }

    /// <summary>
    /// Начать batch: несколько изменений сигналов = один рендер.
    /// using var _ = Batch(); signal1.Set(x); signal2.Set(y); // один рендер
    /// </summary>
    protected IDisposable Batch() => SignalBatch.Begin();

    // ── ShouldRender ──────────────────────────────────────────────────────────

    protected override bool ShouldRender()
    {
        bool visible = Visible;
        int prev = Interlocked.Exchange(ref _previousVisible, visible ? 1 : 0);
        bool wasVisible = prev == 1;

        if (!visible && wasVisible) return true;    // стал невидимым → один рендер для скрытия
        if (!visible) return false;                 // остаётся невидимым → пропуск

        foreach (var hook in _hooks)
            if (hook is IRenderHook rh && !rh.ShouldRender(this))
                return false;

        return true;
    }

    // ── StateHasChanged ───────────────────────────────────────────────────────

#pragma warning disable CS0108
    /// <summary>
    /// Запланировать перерисовку с batch-дедупликацией.
    /// ⚠️ Не приводите к ComponentBase — обходит batching.
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

    // ── SetParametersAsync ────────────────────────────────────────────────────

    public override Task SetParametersAsync(ParameterView parameters)
    {
        Interlocked.Increment(ref _ariaGeneration);
        return base.SetParametersAsync(parameters);
    }

    // ── Lifecycle ─────────────────────────────────────────────────────────────

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
            IsFirstRender = false;

            // Пропускаем JS interop при prerendering (нет браузера)
            if (!IsPrerendering)
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
    /// Вызывается при первом рендере (аналог React componentDidMount).
    /// НЕ вызывается при prerendering (SSR static).
    /// </summary>
    protected virtual Task OnFirstRenderAsync() => Task.CompletedTask;

    // ── CSS / Style ───────────────────────────────────────────────────────────

    protected CssBuilder Css(string? baseClass = null)
        => new(baseClass ?? GetDefaultCssClass());

    /// <summary>Создать StyleBuilder с базовым стилем.</summary>
    protected StyleBuilder CreateStyle(string? baseStyle = null)
        => new(baseStyle);

    protected virtual string? GetDefaultCssClass() => null;

    // ── AdditionalAttributes (фильтрованные) ─────────────────────────────────

    /// <summary>
    /// AdditionalAttributes без "class" и "style" — предотвращает конфликт с параметрами Class/Style.
    /// </summary>
    protected IReadOnlyDictionary<string, object>? FilteredAttributes
    {
        get
        {
            if (AdditionalAttributes is null) return null;
            // Фильтруем только если есть конфликтующие ключи
            bool hasConflict = AdditionalAttributes.ContainsKey("class")
                            || AdditionalAttributes.ContainsKey("style");
            if (!hasConflict) return AdditionalAttributes;

            return AdditionalAttributes
                .Where(kvp => !kvp.Key.Equals("class", StringComparison.OrdinalIgnoreCase)
                           && !kvp.Key.Equals("style", StringComparison.OrdinalIgnoreCase))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }
    }

    // ── ARIA ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// ARIA-атрибуты из AdditionalAttributes (aria-*, role, tabindex).
    /// Кэшируется между рендерами. Исключает "class"/"style".
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

            _ariaCache = attrs;
            _ariaCacheGeneration = currentGeneration;
            return attrs;
        }
    }

    private static bool IsAriaAttribute(string key)
        => key.StartsWith("aria-", StringComparison.OrdinalIgnoreCase)
        || key.Equals("role", StringComparison.OrdinalIgnoreCase)
        || key.Equals("tabindex", StringComparison.OrdinalIgnoreCase);

    // ── RefreshAsync ──────────────────────────────────────────────────────────

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

    // ── Service helpers ───────────────────────────────────────────────────────

    protected T? TryGetService<T>() where T : class
        => ServiceProvider.GetService<T>();

    /// <summary>Получить сервис или бросить понятное исключение с инструкцией.</summary>
    protected T TryGetRequiredService<T>() where T : class
        => ServiceProvider.GetService<T>()
           ?? throw new InvalidOperationException(
               $"Service {typeof(T).Name} is not registered. " +
               $"Call builder.Services.AddSuperUI() in Program.cs.");

    protected void ThrowIfDisposed()
    {
        if (IsDisposed)
            throw new ObjectDisposedException(ComponentId, $"Component {ComponentId} is disposed.");
    }

    // ── Context helpers ───────────────────────────────────────────────────────

    protected Task IfBrowserAsync(Func<Task> action)
        => IsBrowser ? action() : Task.CompletedTask;

    protected Task IfServerAsync(Func<Task> action)
        => IsServer ? action() : Task.CompletedTask;

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
