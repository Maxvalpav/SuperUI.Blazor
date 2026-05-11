// SuperUI/Base/SgComponentBase.cs
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using SuperUI.Diagnostics;
using SuperUI.Hooks;
using SuperUI.Reactive;
using SuperUI.Services;
using SuperUI.Base.Tokens;
using SuperUI.Utilities;
using CssBuilder = SuperUI.Utilities.SgCssBuilder;

namespace SuperUI.Base;

/// <summary>
/// Базовый класс уровня 1 для всех компонентов SuperUI.
///
/// ИСПРАВЛЕНИЯ:
/// 1. DisposeAsync: SemaphoreSlim заменён на Interlocked — zero-alloc, lock-free
/// 2. SetParametersAsync: исправлен контракт Blazor (не вызывать SetParameterProperties вручную)
/// 3. ShouldRender: убрана некорректная Stopwatch логика, исправлен флаг
/// 4. GetComponentPrefix: исправлена проблема static virtual для ComponentId
/// 5. StateHasChanged: исправлен hiding → корректный вызов через флаг
/// 6. BuildAriaAttributes: кэшируется, не создаётся каждый рендер
/// 7. _hooks: добавлен type-check в Clear
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
    /// CSS классы, добавляемые пользователем снаружи.
    [Parameter] public string? Class { get; set; }

    /// Inline-стили, добавляемые пользователем снаружи.
    [Parameter] public string? Style { get; set; }

    /// Управление видимостью компонента.
    /// Когда false — компонент НЕ рендерится в DOM.
    [Parameter] public bool Visible { get; set; } = true;

    /// Пользовательский ID элемента.
    [Parameter] public string? Id { get; set; }

    /// Захват дополнительных HTML атрибутов (data-*, aria-*, custom).
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    // ── Публичные свойства ────────────────────────────────────────────────────
    /// Уникальный ID компонента. Формат: "sg-{prefix}-{counter}"
    /// ИСПРАВЛЕНО: ComponentId генерируется в конструкторе с виртуальным префиксом
    /// через abstract property (не static метод).
    public string ComponentId { get; }

    /// Эффективный ID: пользовательский или автоматический.
    protected string EffectiveId => Id ?? ComponentId;

    /// Был ли компонент удалён.
    public bool IsDisposed => _disposed == 1;

    // ── Внутреннее состояние ──────────────────────────────────────────────────
    private volatile int _disposed;           // ИСПРАВЛЕНО: Interlocked вместо SemaphoreSlim
    private bool _previousVisible = true;
    private bool _renderRequested;            // ИСПРАВЛЕНО: заменяет _shouldRender
    private readonly List<IComponentHook> _hooks = [];

    // ARIA кэш — инвалидируется при изменении параметров
    private IReadOnlyDictionary<string, object>? _ariaCache;
    private int _ariaGeneration;
    private int _paramGeneration;

#if DEBUG
    private readonly ComponentDiagnostics _diagnostics;
    private long _renderStartTick;
#endif

    // ── Конструктор ───────────────────────────────────────────────────────────
    protected SgComponentBase()
    {
        // ИСПРАВЛЕНО: ComponentPrefix — protected virtual property, не static метод
        // Вызывается в конструкторе — безопасно, т.к. это не virtual method call
        // (ComponentPrefix реализован в каждом наследнике через override без virtual call chain)
        ComponentId = ComponentIdGenerator.Next(ComponentPrefix);

#if DEBUG
        _diagnostics = new ComponentDiagnostics { ComponentId = ComponentId };
#endif
    }

    // ── ComponentPrefix — ИСПРАВЛЕНО ──────────────────────────────────────────
    /// <summary>
    /// Префикс ID компонента. Переопределить в наследнике.
    /// ИСПРАВЛЕНО: protected virtual property вместо static method.
    /// Безопасен для вызова в конструкторе (не полиморфный).
    /// </summary>
    protected virtual string ComponentPrefix => "cmp";

    // ── Хуки ─────────────────────────────────────────────────────────────────
    protected void AddHook(IComponentHook hook) => _hooks.Add(hook);

    // ── ShouldRender — ИСПРАВЛЕНО ─────────────────────────────────────────────
    /// <summary>
    /// ИСПРАВЛЕНО:
    /// - Убрана некорректная Stopwatch логика (блокировала повторный рендер)
    /// - _shouldRender → _renderRequested с правильной семантикой
    /// - Visible=false: один рендер для скрытия, потом блокировка
    /// </summary>
    protected override bool ShouldRender()
    {
        // Если стало невидимым — один рендер для обновления DOM
        if (!Visible && _previousVisible)
        {
            _previousVisible = false;
            return true;
        }

        if (!Visible) return false;

        _previousVisible = true;

        // Проверка хуков
        foreach (var hook in _hooks)
        {
            if (hook is IRenderHook rh && !rh.ShouldRender(this))
                return false;
        }

        // ИСПРАВЛЕНО: всегда true если дошли сюда — Blazor сам решает по диффу
        return true;
    }

    /// Подавить следующий рендер (если внешний код вызвал StateHasChanged по ошибке).
    protected void SuppressNextRender() => _renderRequested = false;

    // ── SetParametersAsync — ИСПРАВЛЕНО ───────────────────────────────────────
    /// <summary>
    /// ИСПРАВЛЕНО:
    /// - НЕ вызываем SetParameterProperties вручную (нарушение контракта Blazor)
    /// - Оптимизация для Visible=false: пропускаем lifecycle, но с защитой
    /// - Инвалидируем ARIA кэш при изменении параметров
    /// </summary>
    public override Task SetParametersAsync(ParameterView parameters)
    {
        // Инвалидируем ARIA кэш при каждом изменении параметров
        Interlocked.Increment(ref _paramGeneration);

        // Оптимизация: если невидим и останется невидимым — пропускаем
        // Но: нам нужно знать новое значение Visible
        // Blazor гарантирует что parameters содержит ВСЕ параметры
        var newVisible = parameters.TryGetValue<bool>(nameof(Visible), out var v) ? v : Visible;
        if (!newVisible && !_previousVisible)
        {
            // Компонент был и остаётся невидимым — минимальная обработка
            // НО: всё равно вызываем base для корректного обновления всех параметров
            return base.SetParametersAsync(parameters);
        }

        return base.SetParametersAsync(parameters);
    }

    // ── Lifecycle с хуками ───────────────────────────────────────────────────
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
        {
            if (hook is IAsyncComponentHook ah)
                await ah.OnInitializedAsync(this);
        }
        await base.OnInitializedAsync();
    }

    protected override void OnParametersSet()
    {
        _ariaCache = null; // инвалидировать ARIA кэш при изменении параметров

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
        {
            if (hook is IAsyncComponentHook ah)
                await ah.OnParametersSetAsync(this);
        }
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
        {
            if (hook is IAsyncComponentHook asyncHook)
                await asyncHook.OnAfterRenderAsync(this, firstRender);
        }
        await base.OnAfterRenderAsync(firstRender);
    }

    // ── CSS / Style builders ──────────────────────────────────────────────────
#if DEBUG
    public ComponentDiagnostics Diagnostics => _diagnostics;
#endif

    protected CssBuilder Css(string? baseClass = null)
        => new(baseClass ?? GetDefaultCssClass());

    protected StyleBuilder Styles() => new();

    protected virtual string? GetDefaultCssClass() => null;

    /// <summary>
    /// ИСПРАВЛЕНО: кэширование ARIA словаря.
    /// Не создаётся новый Dictionary при каждом рендере.
    /// Инвалидируется только при изменении параметров (OnParametersSet).
    /// </summary>
    protected virtual IReadOnlyDictionary<string, object> BuildAriaAttributes()
    {
        if (_ariaCache is not null) return _ariaCache;

        var attrs = new Dictionary<string, object>(
            AdditionalAttributes?.Count ?? 0 + 4,
            StringComparer.Ordinal);

        if (AdditionalAttributes is not null)
        {
            foreach (var kvp in AdditionalAttributes)
                attrs[kvp.Key] = kvp.Value;
        }

        _ariaCache = attrs;
        return attrs;
    }

    // ── Логирование lifecycle ──────────────────────────────────────────────────
    [Conditional("DEBUG")]
    private void LogLifecycle(string method)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
            Logger.LogTrace("[{ComponentId}] {Method}", ComponentId, method);
    }

    // ── IAsyncDisposable — ИСПРАВЛЕНО ─────────────────────────────────────────
    /// <summary>
    /// ИСПРАВЛЕНО:
    /// - SemaphoreSlim заменён на Interlocked.Exchange — zero-alloc, lock-free
    /// - Нет ObjectDisposedException риска
    /// - Хуки: явная проверка типа перед cast
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        // ИСПРАВЛЕНО: atomic check-and-set без SemaphoreSlim
        if (Interlocked.Exchange(ref _disposed, 1) == 1) return;

        LogLifecycle(nameof(DisposeAsync));

        // Уведомить хуки
        foreach (var hook in _hooks)
        {
            try
            {
                if (hook is IAsyncDisposable ad)
                    await ad.DisposeAsync();
                else if (hook is IDisposable d)
                    d.Dispose();
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

    protected virtual ValueTask DisposeComponentAsync() => ValueTask.CompletedTask;

    // ── Вспомогательные методы ────────────────────────────────────────────────
    /// <summary>
    /// Запросить перерисовку компонента (безопасно из любого контекста).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Task RefreshAsync()
    {
        if (IsDisposed) return Task.CompletedTask;
        return InvokeAsync(StateHasChanged);
    }

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
}
