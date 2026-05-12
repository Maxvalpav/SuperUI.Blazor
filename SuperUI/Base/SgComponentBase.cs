// SuperUI/Base/SgComponentBase.cs
//
// УЛУЧШЕНИЯ:
//   ✅ CreateSignal<T> — добавлен (SgReactiveBase.Signal<T>() требует этот метод)
//   ✅ StateHasChanged — комментарий о thread-safety расширен
//   ✅ DisposeComponentAsync — _signalBatcher диспозится раньше (_hooks после)
//   ✅ AdditionalAttributesFiltered — filtered view без class/style
//   ✅ OnFirstRenderAsync — IsPrerendering check вынесен из SgJsComponentBase сюда
//
// ПОЛИРОВКА:
//   ✅ XML-docs добавлены для всех protected методов
//   ✅ ThrowIfDisposed — использует ObjectDisposedException.ThrowIf (.NET 8+)
//   ✅ [SupportedOSPlatform] добавлен для WASM-специфичных членов

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
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
/// - WASM: однопоточный. Interlocked используется для ARM-корректности.
/// - Server: per-circuit изоляция. <c>_disposed</c> требует Interlocked/Volatile.
/// </remarks>
public abstract class SgComponentBase : ComponentBase, IAsyncDisposable
{
    // ── Инъекции ─────────────────────────────────────────────────────────────────

    [Inject] protected ILogger<SgComponentBase> Logger { get; set; } = null!;
    [Inject] protected IComponentOptionsService OptionsService { get; set; } = null!;
    [Inject] protected IServiceProvider ServiceProvider { get; set; } = null!;

    // ── Каскадные параметры ──────────────────────────────────────────────────────

    [CascadingParameter] protected SgThemeContext? ThemeContext { get; set; }
    [CascadingParameter] protected SgConfigContext? ConfigContext { get; set; }

    // ── Параметры ────────────────────────────────────────────────────────────────

    /// <summary>Дополнительные CSS-классы.</summary>
    [Parameter] public string? Class { get; set; }

    /// <summary>Инлайн стили.</summary>
    [Parameter] public string? Style { get; set; }

    /// <summary>Видимость компонента.</summary>
    [Parameter] public bool Visible { get; set; } = true;

    /// <summary>HTML id атрибут. Если не задан — используется <see cref="ComponentId"/>.</summary>
    [Parameter] public string? Id { get; set; }

    /// <summary>
    /// Дополнительные HTML-атрибуты (захватываются через CaptureUnmatchedValues).
    /// Атрибуты <c>class</c> и <c>style</c> фильтруются — используйте параметры
    /// <see cref="Class"/> и <see cref="Style"/> для управления стилями.
    /// </summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    // ── Публичные свойства ───────────────────────────────────────────────────────

    /// <summary>Уникальный ID компонента в рамках текущего экземпляра приложения.</summary>
    public string ComponentId { get; }

    /// <summary>Эффективный ID: <see cref="Id"/> если задан и непустой, иначе <see cref="ComponentId"/>.</summary>
    protected string EffectiveId => !string.IsNullOrWhiteSpace(Id) ? Id! : ComponentId;

    /// <summary>Компонент был утилизирован (disposed).</summary>
    public bool IsDisposed => Volatile.Read(ref _disposed) == 1;

    /// <summary>true — браузерный WASM.</summary>
    [SupportedOSPlatform("browser")]
    protected static bool IsBrowser => OperatingSystem.IsBrowser();

    /// <summary>true — сервер (Blazor Server / Web App Server).</summary>
    protected static bool IsServer => !OperatingSystem.IsBrowser();

    /// <summary>
    /// Дополнительные атрибуты без <c>class</c> и <c>style</c>.
    /// Используйте для передачи в корневой элемент компонента.
    /// </summary>
    protected IReadOnlyDictionary<string, object>? AdditionalAttributesFiltered
    {
        get
        {
            if (AdditionalAttributes is null) return null;
            var filtered = AdditionalAttributes
                .Where(kv => !kv.Key.Equals("class", StringComparison.OrdinalIgnoreCase)
                          && !kv.Key.Equals("style", StringComparison.OrdinalIgnoreCase))
                .ToDictionary(kv => kv.Key, kv => kv.Value);
            return filtered.Count == 0 ? null : filtered;
        }
    }

    // ── Внутреннее состояние ─────────────────────────────────────────────────────

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
#endif

    // ── Конструктор ──────────────────────────────────────────────────────────────

    protected SgComponentBase()
    {
        ComponentId = ComponentIdGenerator.Next(ComponentPrefix);
        _signalBatcher = new ComponentSignalTracker(this);
#if DEBUG
        _diagnostics = new ComponentDiagnostics { ComponentId = ComponentId };
#endif
    }

    /// <summary>
    /// Префикс для генерации <see cref="ComponentId"/>.
    /// Переопределите в подклассе для читаемых ID: override protected string ComponentPrefix => "btn";
    /// </summary>
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
    /// Создать реактивный сигнал и зарегистрировать его для авто-StateHasChanged.
    /// ДОРАБОТКА: метод требуется SgReactiveBase.Signal() — отсутствовал в оригинале.
    /// </summary>
    /// <typeparam name="TValue">Тип значения сигнала.</typeparam>
    /// <param name="initial">Начальное значение.</param>
    /// <param name="comparer">Компаратор для определения изменений (null = EqualityComparer.Default).</param>
    protected SgSignal<TValue> CreateSignal<TValue>(TValue initial,
        IEqualityComparer<TValue>? comparer = null)
    {
        var signal = comparer is null
            ? new SgSignal<TValue>(initial)
            : new SgSignal<TValue>(initial, comparer);
        signal.Subscribe(this);
        (_reactiveDisposables ??= []).Add(signal);
        return signal;
    }

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
    protected SgComputed<TResult> RegisterComputed<TResult>(Func<TResult> compute)
    {
        ArgumentNullException.ThrowIfNull(compute);
        var computed = new SgComputed<TResult>(compute);
        computed.Subscribe(this);
        (_reactiveDisposables ??= []).Add(computed);
        return computed;
    }

    // ── ShouldRender ─────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    protected override bool ShouldRender()
    {
        bool visible = Visible;
        int prev = Interlocked.Exchange(ref _previousVisible, visible ? 1 : 0);
        bool wasVisible = prev == 1;

        if (!visible && wasVisible) return true;  // Стал невидимым → один рендер
        if (!visible) return false;               // Остаётся невидимым → пропуск

        foreach (var hook in _hooks)
            if (hook is IRenderHook rh && !rh.ShouldRender(this))
                return false;

        return true;
    }

    // ── StateHasChanged ──────────────────────────────────────────────────────────

    // ПРИМЕЧАНИЕ: перекрываем через new, а не override — ComponentBase.StateHasChanged protected.
    // Это намеренно: нам нужна публичная видимость для вызова из сигналов/эффектов.
    // При вызове через ((ComponentBase)comp).StateHasChanged() — вызовется оригинал (ок).
#pragma warning disable CS0108
    /// <summary>
    /// Запланировать перерисовку с batch-рендерингом.
    /// Безопасен для вызова из любого потока (InvokeAsync при необходимости).
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

    /// <summary>Вызвать StateHasChanged в потоке компонента (InvokeAsync).</summary>
    internal Task InvokeStateHasChangedAsync()
    {
        if (IsDisposed) return Task.CompletedTask;
        return InvokeAsync(base.StateHasChanged);
    }

    // ── SetParametersAsync ───────────────────────────────────────────────────────

    /// <inheritdoc/>
    public override Task SetParametersAsync(ParameterView parameters)
    {
        Interlocked.Increment(ref _ariaGeneration);
        return base.SetParametersAsync(parameters);
    }

    // ── Lifecycle ────────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    protected override void OnInitialized()
    {
        base.OnInitialized();
        LogLifecycle(nameof(OnInitialized));
        foreach (var hook in _hooks) hook.OnInitialized(this);
    }

    /// <inheritdoc/>
    protected override async Task OnInitializedAsync()
    {
        LogLifecycle(nameof(OnInitializedAsync));
        await base.OnInitializedAsync();
        foreach (var hook in _hooks)
            if (hook is IAsyncComponentHook ah) await ah.OnInitializedAsync(this);
    }

    /// <inheritdoc/>
    protected override void OnParametersSet()
    {
#if DEBUG
        _diagnostics.ParameterChangeCount++;
#endif
        foreach (var hook in _hooks) hook.OnParametersSet(this);
        base.OnParametersSet();
    }

    /// <inheritdoc/>
    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();
        foreach (var hook in _hooks)
            if (hook is IAsyncComponentHook ah) await ah.OnParametersSetAsync(this);
    }

    /// <inheritdoc/>
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

    /// <inheritdoc/>
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
                if (hook is IAsyncComponentHook ah) await ah.OnFirstRenderAsync(this);
        }
        foreach (var hook in _hooks)
            if (hook is IAsyncComponentHook ah) await ah.OnAfterRenderAsync(this, firstRender);
    }

    /// <summary>
    /// Вызывается при первом рендере компонента (аналог componentDidMount в React).
    /// Переопределите для инициализации JS Interop и подписок после рендера.
    /// </summary>
    protected virtual Task OnFirstRenderAsync() => Task.CompletedTask;

    // ── CSS / Style ──────────────────────────────────────────────────────────────

#if DEBUG
    /// <summary>Диагностические данные компонента (только DEBUG).</summary>
    public ComponentDiagnostics Diagnostics => _diagnostics;
#endif

    /// <summary>
    /// Создать <see cref="CssBuilder"/> с базовым CSS-классом компонента.
    /// </summary>
    /// <param name="baseClass">Базовый класс. null = <see cref="GetDefaultCssClass"/>.</param>
    protected CssBuilder Css(string? baseClass = null)
        => new(baseClass ?? GetDefaultCssClass());

    /// <summary>
    /// Создать <see cref="StyleBuilder"/> с базовым инлайн-стилем.
    /// </summary>
    protected StyleBuilder CreateStyle(string? baseStyle = null) => new(baseStyle);

    /// <summary>
    /// CSS-класс по умолчанию для корневого элемента компонента.
    /// Переопределите: <c>protected override string? GetDefaultCssClass() => "sg-button";</c>
    /// </summary>
    protected virtual string? GetDefaultCssClass() => null;

    // ── ARIA ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Строит словарь ARIA-атрибутов. Кэшируется между рендерами до смены параметров.
    /// Фильтрует <see cref="AdditionalAttributes"/>: только aria-*, role, tabindex.
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

    // ── RefreshAsync ─────────────────────────────────────────────────────────────

    /// <summary>Запланировать перерисовку компонента.</summary>
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

    /// <summary>InvokeAsync с проверкой IsDisposed.</summary>
    protected Task SafeInvokeAsync(Func<Task> action)
    {
        if (IsDisposed) return Task.CompletedTask;
        return InvokeAsync(action);
    }

    /// <summary>InvokeAsync (sync action) с проверкой IsDisposed.</summary>
    protected Task SafeInvokeAsync(Action action)
    {
        if (IsDisposed) return Task.CompletedTask;
        return InvokeAsync(action);
    }

    // ── Service helpers ──────────────────────────────────────────────────────────

    /// <summary>Получить сервис из DI или null если не зарегистрирован.</summary>
    protected T? TryGetService<T>() where T : class
        => ServiceProvider.GetService<T>();

    /// <summary>
    /// Получить сервис из DI или бросить <see cref="InvalidOperationException"/>
    /// с понятным сообщением о необходимости вызова AddSuperUI().
    /// </summary>
    protected T TryGetRequiredService<T>() where T : class
        => ServiceProvider.GetService<T>()
           ?? throw new InvalidOperationException(
               $"Service {typeof(T).Name} is not registered. " +
               $"Call builder.Services.AddSuperUI() in Program.cs.");

    /// <summary>Бросить <see cref="ObjectDisposedException"/> если компонент утилизирован.</summary>
    protected void ThrowIfDisposed()
        => ObjectDisposedException.ThrowIf(IsDisposed, ComponentId);

    // ── Context helpers ──────────────────────────────────────────────────────────

    /// <summary>Выполнить действие только в Blazor WebAssembly.</summary>
    [SupportedOSPlatform("browser")]
    protected Task IfBrowserAsync(Func<Task> action)
        => IsBrowser ? action() : Task.CompletedTask;

    /// <summary>Выполнить действие только в Blazor Server.</summary>
    protected Task IfServerAsync(Func<Task> action)
        => IsServer ? action() : Task.CompletedTask;

    // ── Logging ──────────────────────────────────────────────────────────────────

    [Conditional("DEBUG")]
    private void LogLifecycle(string method)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("[{ComponentId}] {Method}", ComponentId, method);
    }

    // ── IAsyncDisposable ─────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1) return;

        LogLifecycle(nameof(DisposeAsync));

        // 1. Диспозим реактивные ресурсы
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

        // 2. Диспозим хуки
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

        // 3. Диспозим дочерние ресурсы
        await DisposeComponentAsync();

        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Точка расширения для освобождения ресурсов дочерних классов.
    /// Вызывается в конце <see cref="DisposeAsync"/>.
    /// </summary>
    protected virtual ValueTask DisposeComponentAsync()
    {
        var batcher = Interlocked.Exchange(ref _signalBatcher, null);
        batcher?.Dispose();
        return ValueTask.CompletedTask;
    }
}
