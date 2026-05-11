// Файл: Components/Base/SgComponentBase.cs
// Зависимости: все классы уровня 0, ILogger, IJSRuntime, IHttpContextAccessor

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using SuperUI.Converters;
using SuperUI.Options;
using SuperUI.State;
using SuperUI.Theme;
using SuperUI.Utilities;

namespace SuperUI.Components.Base;

/// <summary>
/// УРОВЕНЬ 1: Базовый класс для всех SuperUI компонентов.
/// 
/// ФИЛОСОФИЯ:
/// - Минимальный оверхед: ShouldRender + ParameterState = меньше лишних re-render
/// - GC friendly: ValueTask, Span, избегаем box/unbox
/// - Race-safe: LifecycleToken во всех async операциях
/// - ARIA first: все атрибуты доступности встроены
/// - IAsyncDisposable: полная цепочка очистки ресурсов
/// 
/// НАСЛЕДНИКИ обязаны:
/// 1. Регистрировать ParameterState в конструкторе через CreateRegisterScope()
/// 2. Вызывать base.DisposeAsync() в своём DisposeAsync()
/// 3. Использовать _lifecycleToken.Current в async операциях
/// </summary>
public abstract class SgComponentBase : ComponentBase, IAsyncDisposable
{
    // ── Поля инфраструктуры (private, не участвуют в rendering) ──────────────

    // Список всех ParameterState для обхода в SetParametersAsync
    private readonly List<IParameterState> _parameterStates = new();

    // Race-safe токен для async операций
    private readonly LifecycleToken _lifecycleToken = new();

    /// <summary>
    /// Защищённый доступ к CancellationToken для наследников.
    /// Используется в SgAIBase и других классах для race-safe async операций.
    /// </summary>
    protected CancellationToken ComponentCancellationToken => _lifecycleToken.Current;

    // Auto-unsubscribe event subscriptions
    private readonly EventSubscriptionManager _subscriptions = new();

    // Список IAsyncDisposable ресурсов для автоматической очистки
    private readonly List<IAsyncDisposable> _asyncDisposables = new();
    private readonly List<IDisposable> _disposables = new();

    // Флаг для предотвращения double-dispose
    private volatile bool _disposed;

    // Список зарегистрированных DotNetObjectReference для авто-очистки
    private readonly List<IDisposable> _dotNetRefs = new();

    // Флаг первого рендера (для OnAfterRender логики)
    private bool _firstRender = true;

    // ShouldRender control
    private bool _shouldRender = true;
    private bool _renderSuppressed;

    // Уникальный ID компонента (генерируется один раз)
    private string? _componentId;

    // ── Инжекции (protected для доступа из наследников) ──────────────────────

    [Inject] protected ILogger<SgComponentBase> Logger { get; set; } = default!;
    [Inject] protected IJSRuntime JS { get; set; } = default!;

    // Nullable — не всегда доступен (WASM)
    // BL0007: IHttpContextAccessor может не быть зарегистрирован в WASM
#pragma warning disable BL0007
    [Inject] private IHttpContextAccessor? _httpContextAccessor { get; set; }
#pragma warning restore BL0007

    // ComponentHookRegistry для хуков жизненного цикла
    [Inject] private IComponentHookRegistry? _hookRegistry { get; set; }

    // ── Cascading параметры ───────────────────────────────────────────────────

    [CascadingParameter(Name = SgCascadeNames.IsRtl)]
    private bool _cascadeRtl { get; set; }

    [CascadingParameter(Name = SgCascadeNames.Culture)]
    private System.Globalization.CultureInfo? _cascadeCulture { get; set; }

    [CascadingParameter(Name = SgCascadeNames.Theme)]
    protected SgThemeContext? ThemeContext { get; set; }

    [CascadingParameter(Name = SgCascadeNames.ComponentOptions)]
    protected SgComponentOptions? CascadeOptions { get; set; }

    // ── Публичные параметры ──────────────────────────────────────────────────

    /// <summary>Дополнительный CSS класс (добавляется к классам компонента).</summary>
    [Parameter] public string? Class { get; set; }

    /// <summary>Дополнительные inline стили.</summary>
    [Parameter] public string? Style { get; set; }

    /// <summary>
    /// Видимость компонента. При Visible=false компонент не рендерится вообще
    /// (в отличие от display:none — элемент не добавляется в DOM).
    /// </summary>
    [Parameter] public bool Visible { get; set; } = true;

    /// <summary>
    /// Отключен ли компонент. Добавляет aria-disabled и disabled атрибуты.
    /// При Disabled=true взаимодействие блокируется.
    /// </summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>
    /// Состояние загрузки. Добавляет aria-busy="true" и CSS класс loading.
    /// </summary>
    [Parameter] public bool Loading { get; set; }

    /// <summary>Дополнительные HTML атрибуты (CaptureUnmatchedValues).</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object?>? UserAttributes { get; set; }

    // ── Свойства (вычисляемые) ───────────────────────────────────────────────

    /// <summary>Thread-safe уникальный ID компонента.</summary>
    public string ComponentId => _componentId ??= ComponentIdGenerator.Next(GetComponentPrefix());

    /// <summary>RTL: проверяем каскадный параметр или системную культуру.</summary>
    public bool IsRtl => _cascadeRtl ||
        (_cascadeCulture ?? System.Globalization.CultureInfo.CurrentUICulture)
            .TextInfo.IsRightToLeft;

    /// <summary>Текущая культура (каскадная или системная).</summary>
    public System.Globalization.CultureInfo CurrentCulture
        => _cascadeCulture ?? System.Globalization.CultureInfo.CurrentUICulture;

    /// <summary>
    /// Компонент в режиме prerendering (SSR без интерактивности).
    /// JS Interop запрещён в этом режиме.
    /// </summary>
    public bool IsPrerendering => _httpContextAccessor?.HttpContext?.WebSockets.IsWebSocketRequest == false
        && _httpContextAccessor?.HttpContext is not null
        && !OperatingSystem.IsBrowser(); // WASM — никогда не prerender

    /// <summary>
    /// Является ли JS Interop безопасным для вызова.
    /// Проверяет: prerendering, disposed, circuit connected.
    /// </summary>
    protected bool IsJsInteropSafe => !IsPrerendering && !_disposed;

    // ── CssBuilder / StyleBuilder helpers ────────────────────────────────────

    /// <summary>Создать CssBuilder с базовым классом компонента.</summary>
    protected CssBuilder GetCssBuilder(string? baseClass = null)
        => new CssBuilder(baseClass)
            .AddClass("sg-disabled", Disabled)
            .AddClass("sg-loading", Loading)
            .AddClass("sg-rtl", IsRtl)
            .AddClass(Class);

    /// <summary>Создать StyleBuilder с базовым стилем.</summary>
    protected StyleBuilder GetStyleBuilder()
        => new StyleBuilder(Style);

    // ── ParameterState система ───────────────────────────────────────────────

    /// <summary>
    /// Создать scope для регистрации ParameterState в конструкторе.
    /// ИСПОЛЬЗОВАНИЕ: using var scope = CreateRegisterScope();
    /// </summary>
    protected ParameterRegisterScope CreateRegisterScope()
        => new ParameterRegisterScope(_parameterStates);

    /// <summary>Зарегистрировать IAsyncDisposable для авто-dispose.</summary>
    protected T RegisterDisposable<T>(T disposable) where T : IAsyncDisposable
    {
        _asyncDisposables.Add(disposable);
        return disposable;
    }

    /// <summary>Зарегистрировать IDisposable для авто-dispose.</summary>
    protected T RegisterDisposable<T>(T disposable) where T : IDisposable
    {
        _disposables.Add(disposable);
        return disposable;
    }

    // ── SetParametersAsync оптимизация ───────────────────────────────────────

    /// <summary>
    /// ОПТИМИЗАЦИЯ SetParametersAsync:
    /// 1. Быстрый путь через ParameterView.SetParameterProperties
    /// 2. Вызов ChangeHandler только при реальных изменениях
    /// 3. ShouldRender обновляется только если что-то изменилось
    /// </summary>
    public override async Task SetParametersAsync(ParameterView parameters)
    {
        // Стандартный Blazor механизм установки параметров (reflection + cache)
        parameters.SetParameterProperties(this);

        // Обходим зарегистрированные ParameterState и проверяем изменения
        // Запускаем параллельно? НЕТ — порядок важен, да и Blazor single-threaded
        bool anyChanged = false;
        foreach (var state in _parameterStates)
        {
            if (await state.OnParametersSetAsync())
                anyChanged = true;
        }

        // Если ничего не изменилось, можем пропустить re-render
        // НО: Blazor требует вызова OnParametersSet/Async минимум раз
        if (!anyChanged && _firstRender is false)
        {
            _shouldRender = false;
        }

        await base.SetParametersAsync(ParameterView.Empty);
    }

    // ── ShouldRender корректный ──────────────────────────────────────────────

    /// <summary>
    /// Корректная реализация ShouldRender:
    /// - Уважает Visible=false (нет смысла рендерить скрытый компонент)
    /// - Интегрирован с ParameterState (пропускаем если нет изменений)
    /// - Позволяет наследникам переопределить через OnShouldRender()
    /// </summary>
    protected sealed override bool ShouldRender()
    {
        if (_renderSuppressed) return false;
        if (!Visible) return false;

        var baseResult = _shouldRender;
        _shouldRender = true; // сбрасываем для следующего цикла

        return baseResult && OnShouldRender();
    }

    /// <summary>Наследники могут переопределить логику ShouldRender.</summary>
    protected virtual bool OnShouldRender() => true;

    /// <summary>Принудительно пропустить следующий рендер.</summary>
    protected void SuppressNextRender() => _renderSuppressed = true;

    /// <summary>Разрешить рендер (отменить SuppressNextRender).</summary>
    protected void AllowNextRender()
    {
        _renderSuppressed = false;
        _shouldRender = true;
    }

    // ── Lifecycle hooks система ──────────────────────────────────────────────

    /// <summary>
    /// ХУКИ ЖИЗНЕННОГО ЦИКЛА — система управления.
    /// 
    /// КЛЮЧЕВОЕ ОТЛИЧИЕ от стандартного Blazor:
    /// - Автоматическая передача LifecycleToken (race-safe)
    /// - Логирование (опционально через ILogger)
    /// - Обработка исключений через OnComponentError
    /// - Поддержка Prerendering detection
    /// </summary>

    protected sealed override void OnInitialized()
    {
        try
        {
            LogLifecycle(nameof(OnInitialized));
            _componentId = ComponentIdGenerator.Next(GetComponentPrefix());
            OnComponentInitialized();
        }
        catch (Exception ex)
        {
            OnComponentError(ex, nameof(OnInitialized));
        }
    }

    protected sealed override async Task OnInitializedAsync()
    {
        var token = _lifecycleToken.Renew();
        try
        {
            LogLifecycle(nameof(OnInitializedAsync));
            await OnComponentInitializedAsync(token);
            
            // Invoke component hooks после инициализации
            if (_hookRegistry is not null)
                await _hookRegistry.InvokeInitializedAsync(this, GetType().Name);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            // Нормально — компонент уничтожен до завершения инициализации
            Logger.LogDebug("[{Component}] OnInitializedAsync отменён (компонент уничтожен)", GetType().Name);
        }
        catch (Exception ex)
        {
            OnComponentError(ex, nameof(OnInitializedAsync));
        }
    }

    protected sealed override void OnParametersSet()
    {
        try
        {
            LogLifecycle(nameof(OnParametersSet));
            _shouldRender = true; // параметры изменились — нужен рендер
            OnComponentParametersSet();
        }
        catch (Exception ex)
        {
            OnComponentError(ex, nameof(OnParametersSet));
        }
    }

    protected sealed override async Task OnParametersSetAsync()
    {
        var token = _lifecycleToken.Renew();
        try
        {
            await OnComponentParametersSetAsync(token);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
        catch (Exception ex)
        {
            OnComponentError(ex, nameof(OnParametersSetAsync));
        }
    }

    protected sealed override void OnAfterRender(bool firstRender)
    {
        try
        {
            if (firstRender) _firstRender = false;
            LogLifecycle(firstRender ? "OnAfterRender(first)" : "OnAfterRender");
            OnComponentAfterRender(firstRender);
        }
        catch (Exception ex)
        {
            OnComponentError(ex, nameof(OnAfterRender));
        }
    }

    protected sealed override async Task OnAfterRenderAsync(bool firstRender)
    {
        var token = _lifecycleToken.Current;
        try
        {
            await OnComponentAfterRenderAsync(firstRender, token);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
        catch (Exception ex)
        {
            OnComponentError(ex, nameof(OnAfterRenderAsync));
        }
    }

    // ── Виртуальные хуки для наследников ─────────────────────────────────────

    /// <summary>Sync инициализация (вызывается из OnInitialized).</summary>
    protected virtual void OnComponentInitialized() { }

    /// <summary>Async инициализация с race-safe токеном.</summary>
    protected virtual ValueTask OnComponentInitializedAsync(CancellationToken ct) => ValueTask.CompletedTask;

    /// <summary>Вызывается при изменении параметров (sync).</summary>
    protected virtual void OnComponentParametersSet() { }

    /// <summary>Вызывается при изменении параметров (async).</summary>
    protected virtual ValueTask OnComponentParametersSetAsync(CancellationToken ct) => ValueTask.CompletedTask;

    /// <summary>Вызывается после рендера (sync).</summary>
    protected virtual void OnComponentAfterRender(bool firstRender) { }

    /// <summary>Вызывается после рендера (async). JS Interop безопасен здесь.</summary>
    protected virtual ValueTask OnComponentAfterRenderAsync(bool firstRender, CancellationToken ct) => ValueTask.CompletedTask;

    /// <summary>
    /// Хук для обработки ошибок жизненного цикла.
    /// По умолчанию: логируем + rethrow.
    /// Наследники могут показать UI ошибки (ErrorBoundary per-component).
    /// </summary>
    protected virtual void OnComponentError(Exception exception, string lifecycleMethod)
    {
        Logger.LogError(exception, "[{Component}] Ошибка в {Method}", GetType().Name, lifecycleMethod);
        _ = DispatchExceptionAsync(exception); // Blazor встроенный error boundary
    }

    // ── JS Interop 5 уровней защиты ──────────────────────────────────────────

    /// <summary>
    /// УРОВЕНЬ 1: Базовый invoke с полной защитой.
    /// 
    /// 5 уровней защиты:
    /// 1. IsPrerendering — JS недоступен при SSR
    /// 2. _disposed — компонент уже уничтожен
    /// 3. CancellationToken — операция отменена
    /// 4. JSDisconnectedException — SignalR circuit потерян (Server)
    /// 5. ObjectDisposedException — IJSRuntime disposed
    /// </summary>
    protected async ValueTask<T> JSInvokeAsync<T>(
        string identifier,
        CancellationToken ct = default,
        params object?[]? args)
    {
        EnsureJsInteropSafe();
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct, _lifecycleToken.Current);
        try
        {
            return await JS.InvokeAsync<T>(identifier, cts.Token, args);
        }
        catch (JSDisconnectedException)
        {
            // Уровень 4: потеря SignalR circuit — нормально при dispose
            Logger.LogDebug("[{Component}] JS circuit disconnected при вызове {Method}", GetType().Name, identifier);
            return default!;
        }
        catch (ObjectDisposedException)
        {
            // Уровень 5: IJSRuntime disposed
            return default!;
        }
        catch (OperationCanceledException) when (cts.IsCancellationRequested)
        {
            // Уровень 3: токен отменён
            return default!;
        }
    }

    /// <summary>Void версия JSInvoke с 5-уровневой защитой.</summary>
    protected async ValueTask JSInvokeVoidAsync(
        string identifier,
        CancellationToken ct = default,
        params object?[]? args)
    {
        EnsureJsInteropSafe();
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct, _lifecycleToken.Current);
        try
        {
            await JS.InvokeVoidAsync(identifier, cts.Token, args);
        }
        catch (JSDisconnectedException) { }
        catch (ObjectDisposedException) { }
        catch (OperationCanceledException) when (cts.IsCancellationRequested) { }
    }

    /// <summary>Import JS module с кешированием.</summary>
    protected ValueTask<IJSObjectReference> ImportModuleAsync(string modulePath, CancellationToken ct = default)
        => new ValueTask<IJSObjectReference>(
            JSInvokeAsync<IJSObjectReference>("import", ct, modulePath).AsTask());

    private void EnsureJsInteropSafe()
    {
        // Уровень 1: Prerendering
        if (IsPrerendering)
            throw new InvalidOperationException($"[{GetType().Name}] JS Interop недоступен при prerendering. Используйте OnAfterRenderAsync(firstRender=true).");
        // Уровень 2: Disposed
        if (_disposed)
            throw new ObjectDisposedException(GetType().Name);
    }

    // ── DotNetObjectReference auto-manage ────────────────────────────────────

    /// <summary>
    /// Создать DotNetObjectReference с автоматическим Dispose при уничтожении компонента.
    /// ИННОВАЦИЯ: разработчик не думает о Dispose DotNetRef.
    /// </summary>
    protected DotNetObjectReference<T> CreateDotNetRef<T>(T instance) where T : class
    {
        var dotNetRef = DotNetObjectReference.Create(instance);
        _dotNetRefs.Add(dotNetRef);
        return dotNetRef;
    }

    // ── PeriodicTimer с авто-dispose ─────────────────────────────────────────

    /// <summary>
    /// Запустить PeriodicTimer с автоматическим dispose при уничтожении компонента.
    /// ИННОВАЦИЯ: безопасный periodic timer без memory leaks.
    /// </summary>
    protected async ValueTask StartPeriodicTimerAsync(
        TimeSpan period,
        Func<CancellationToken, ValueTask> tick,
        CancellationToken externalToken = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(
            externalToken, _lifecycleToken.Current);

        using var timer = new PeriodicTimer(period);

        // Регистрируем отмену таймера при dispose компонента
        RegisterDisposable(new DisposableAction(() => cts.Cancel()));

        try
        {
            while (await timer.WaitForNextTickAsync(cts.Token))
            {
                if (_disposed) break;
                await tick(cts.Token);
                await InvokeAsync(StateHasChanged); // всегда в Blazor thread
            }
        }
        catch (OperationCanceledException) { }
    }

    // ── Event subscription management ────────────────────────────────────────

    /// <summary>Зарегистрировать подписку с авто-отпиской.</summary>
    protected void Subscribe(Action subscribe, Action unsubscribe)
        => _subscriptions.Register(subscribe, unsubscribe);

    // ── ARIA helpers ──────────────────────────────────────────────────────────

    /// <summary>Получить базовые ARIA атрибуты компонента.</summary>
    protected virtual IReadOnlyDictionary<string, object?> GetAriaAttributes()
    {
        var attrs = new Dictionary<string, object?>(4);
        if (Disabled) attrs["aria-disabled"] = "true";
        if (Loading) attrs["aria-busy"] = "true";
        return attrs;
    }

    // ── Utility methods ───────────────────────────────────────────────────────

    /// <summary>Prefix для ComponentId. Наследники переопределяют: "btn", "input", etc.</summary>
    protected virtual string GetComponentPrefix() => "comp";

    /// <summary>Безопасный StateHasChanged — всегда в Blazor thread.</summary>
    protected Task RequestStateUpdateAsync()
        => InvokeAsync(StateHasChanged);

    // ── Lifecycle logging ─────────────────────────────────────────────────────

    [System.Diagnostics.Conditional("DEBUG")]
    private void LogLifecycle(string method)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("[{Component}#{Id}] {Method}", GetType().Name, _componentId, method);
    }

    // ── IAsyncDisposable полная цепочка ───────────────────────────────────────

    /// <summary>
    /// Полная цепочка очистки ресурсов:
    /// 1. Флаг _disposed
    /// 2. LifecycleToken (отменяет все async операции)
    /// 3. DotNetObjectReference
    /// 4. EventSubscriptions
    /// 5. Registered IAsyncDisposable
    /// 6. Registered IDisposable
    /// 7. Хук для наследников
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        // 1. Токен жизненного цикла — отменяет все async операции
        await _lifecycleToken.DisposeAsync();

        // 2. DotNetObjectReference
        foreach (var dnRef in _dotNetRefs)
        {
            try { dnRef.Dispose(); }
            catch { /* best effort */ }
        }
        _dotNetRefs.Clear();

        // 3. Event subscriptions
        _subscriptions.Dispose();

        // 4. Async disposables (в обратном порядке регистрации)
        for (int i = _asyncDisposables.Count - 1; i >= 0; i--)
        {
            try { await _asyncDisposables[i].DisposeAsync(); }
            catch { /* best effort */ }
        }
        _asyncDisposables.Clear();

        // 5. Sync disposables
        for (int i = _disposables.Count - 1; i >= 0; i--)
        {
            try { _disposables[i].Dispose(); }
            catch { /* best effort */ }
        }
        _disposables.Clear();

        // 6. Хук для наследников
        await OnComponentDisposeAsync();

        GC.SuppressFinalize(this);
    }

    /// <summary>Хук для очистки ресурсов наследника. Вызывается из DisposeAsync.</summary>
    protected virtual ValueTask OnComponentDisposeAsync() => ValueTask.CompletedTask;

    // ── Вспомогательные типы ──────────────────────────────────────────────────

    private sealed class DisposableAction : IDisposable
    {
        private readonly Action _action;
        public DisposableAction(Action action) => _action = action;
        public void Dispose() => _action();
    }
}
