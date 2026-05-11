using System.Diagnostics;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using SuperUI.Diagnostics;
using SuperUI.Hooks;
using SuperUI.Reactive;
using SuperUI.Services;
using SuperUI.Utilities;
using CssBuilder = SuperUI.Utilities.SgCssBuilder;

namespace SuperUI.Base;

/// <summary>
/// Базовый класс уровня 1 для всех компонентов SuperUI.
/// 
/// Обеспечивает:
/// - Уникальный ComponentId (thread-safe)
/// - Управление Visible + ShouldRender корректный
/// - CssBuilder / StyleBuilder
/// - Захват AdditionalAttributes
/// - ARIA базовые атрибуты
/// - IAsyncDisposable полная цепочка
/// - Система хуков lifecycle
/// - Логирование lifecycle
/// - ConfigProvider / ComponentOptions
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

    /// <summary>CSS классы, добавляемые пользователем снаружи.</summary>
    [Parameter] public string? Class { get; set; }

    /// <summary>Inline-стили, добавляемые пользователем снаружи.</summary>
    [Parameter] public string? Style { get; set; }

    /// <summary>
    /// Управление видимостью компонента.
    /// Когда false — компонент НЕ рендерится в DOM (полное удаление).
    /// Используйте Display:none через CSS если нужно сохранить DOM.
    /// </summary>
    [Parameter] public bool Visible { get; set; } = true;

    /// <summary>Пользовательский ID элемента. Если не задан — генерируется автоматически.</summary>
    [Parameter] public string? Id { get; set; }

    /// <summary>Захват дополнительных HTML атрибутов (data-*, aria-*, custom).</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    // ── Публичные свойства ────────────────────────────────────────────────────

    /// <summary>
    /// Уникальный ID компонента. Thread-safe, короткий, читаемый.
    /// Формат: "sg-cmp-42"
    /// </summary>
    public string ComponentId { get; } = ComponentIdGenerator.Next(GetComponentPrefix());

    /// <summary>Эффективный ID: пользовательский или автоматический.</summary>
    protected string EffectiveId => Id ?? ComponentId;

    /// <summary>Был ли компонент удалён.</summary>
    internal bool IsDisposed { get; private set; }

    // ── Внутреннее состояние ──────────────────────────────────────────────────

    private bool _previousVisible = true;
    private readonly List<IComponentHook> _hooks = [];
    private readonly SemaphoreSlim _disposeLock = new(1, 1);

#if DEBUG
    private readonly ComponentDiagnostics _diagnostics = new();
    private Stopwatch _renderTimer = new();
#endif

    // ── Хуки ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Регистрация хука жизненного цикла.
    /// Вызывать в конструкторе или OnInitialized.
    /// </summary>
    protected void AddHook(IComponentHook hook)
    {
        _hooks.Add(hook);
    }

    // ── ShouldRender — корректное управление ─────────────────────────────────

    private bool _shouldRender = true;

    /// <summary>
    /// Корректное управление рендерингом.
    /// Учитывает Visible + внутренний флаг + хуки.
    /// </summary>
    protected override bool ShouldRender()
    {
#if DEBUG
        if (_renderTimer.IsRunning) return false;
        _renderTimer.Restart();
#endif
        if (!Visible && _previousVisible)
        {
            _previousVisible = false;
            return true;
        }

        if (!Visible) return false;
        _previousVisible = true;

        foreach (var hook in _hooks)
        {
            if (hook is IRenderHook rh && !rh.ShouldRender(this))
                return false;
        }

        return _shouldRender;
    }

    /// <summary>Принудительно подавить следующий рендер (оптимизация).</summary>
    protected void SuppressNextRender() => _shouldRender = false;

    /// <summary>Запросить рендер.</summary>
    protected new void StateHasChanged()
    {
        _shouldRender = true;
        base.StateHasChanged();
    }

    // ── SetParametersAsync — оптимизация ─────────────────────────────────────

    public override Task SetParametersAsync(ParameterView parameters)
    {
        // Быстрый путь: если компонент невидим и остаётся невидимым — пропускаем
        // НО: нам всё равно нужно обновить параметры чтобы отреагировать на Visible=true
        parameters.SetParameterProperties(this);

        // Если невидим — пропускаем lifecycle, но с одним рендером для скрытия
        if (!Visible && !_previousVisible)
            return Task.CompletedTask;

        return base.SetParametersAsync(ParameterView.Empty);
    }

    // ── Lifecycle с хуками ────────────────────────────────────────────────────

    protected override void OnInitialized()
    {
        LogLifecycle(nameof(OnInitialized));
        foreach (var hook in _hooks) hook.OnInitialized(this);
        base.OnInitialized();
    }

    protected override async Task OnInitializedAsync()
    {
        LogLifecycle(nameof(OnInitializedAsync));
#if DEBUG
        _diagnostics.ComponentId = ComponentId;
#endif
        foreach (var hook in _hooks)
        {
            if (hook is IAsyncComponentHook ah)
                await ah.OnInitializedAsync(this);
        }
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
        {
            if (hook is IAsyncComponentHook ah)
                await ah.OnParametersSetAsync(this);
        }
        await base.OnParametersSetAsync();
    }

    protected override void OnAfterRender(bool firstRender)
    {
        using var _ = SignalTracker.EnterScope(this);
#if DEBUG
        if (_renderTimer.IsRunning)
        {
            _renderTimer.Stop();
            var elapsed = _renderTimer.ElapsedMilliseconds;
            _diagnostics.RenderCount++;
            _diagnostics.LastRenderMs = elapsed;
            _diagnostics.AverageRenderMs = (_diagnostics.AverageRenderMs * (_diagnostics.RenderCount - 1) + elapsed) / _diagnostics.RenderCount;
        }
#endif
        foreach (var hook in _hooks) hook.OnAfterRender(this, firstRender);
        _shouldRender = true;
        base.OnAfterRender(firstRender);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        foreach (var hook in _hooks)
        {
            if (hook is IAsyncComponentHook asyncHook)
                await asyncHook.OnAfterRenderAsync(this, firstRender);
        }
        await base.OnAfterRenderAsync(firstRender);
    }

    // ── CSS / Style builders ──────────────────────────────────────────────────

#if DEBUG
    /// <summary>Диагностика компонента (только в DEBUG режиме).</summary>
    public ComponentDiagnostics Diagnostics => _diagnostics;
#endif

    /// <summary>Создать CssBuilder с базовым классом компонента.</summary>
    protected CssBuilder Css(string? baseClass = null)
        => new CssBuilder(baseClass ?? GetDefaultCssClass());

    /// <summary>Создать StyleBuilder.</summary>
    protected StyleBuilder Styles() => new StyleBuilder();

    /// <summary>Базовый CSS класс компонента. Переопределить в наследнике.</summary>
    protected virtual string? GetDefaultCssClass() => null;

    // ── ARIA атрибуты ─────────────────────────────────────────────────────────

    /// <summary>
    /// Строит словарь ARIA атрибутов для компонента.
    /// Объединяет базовые ARIA + AdditionalAttributes пользователя.
    /// </summary>
    protected virtual IReadOnlyDictionary<string, object?> BuildAriaAttributes()
    {
        var attrs = new Dictionary<string, object?>();

        // Пользовательские атрибуты имеют приоритет
        if (AdditionalAttributes != null)
        {
            foreach (var kvp in AdditionalAttributes)
                attrs[kvp.Key] = kvp.Value;
        }

        return attrs;
    }

    // ── Логирование lifecycle ────────────────────────────────────────────────

    [Conditional("DEBUG")]
    private void LogLifecycle(string method)
    {
        if (Logger.IsEnabled(LogLevel.Trace))
        {
            Logger.LogTrace("[{ComponentId}] {Method}", ComponentId, method);
        }
    }

    // ── IAsyncDisposable ──────────────────────────────────────────────────────

    public async ValueTask DisposeAsync()
    {
        await _disposeLock.WaitAsync();
        try
        {
            if (IsDisposed) return;
            IsDisposed = true;

            LogLifecycle(nameof(DisposeAsync));

            // Уведомить хуки
            foreach (var hook in _hooks)
            {
                if (hook is IAsyncDisposable ad)
                    await ad.DisposeAsync();
                else if (hook is IDisposable d)
                    d.Dispose();
            }
            _hooks.Clear();

            await DisposeComponentAsync();
        }
        finally
        {
            _disposeLock.Release();
            _disposeLock.Dispose();
        }
    }

    /// <summary>Переопределить для освобождения ресурсов компонента.</summary>
    protected virtual ValueTask DisposeComponentAsync() => ValueTask.CompletedTask;

    // ── Вспомогательные методы ────────────────────────────────────────────────

    /// <summary>Префикс ID по имени типа. Можно переопределить.</summary>
    protected static string GetComponentPrefix()
        => "cmp"; // Наследники переопределяют: "btn", "inp", "dlg"

    /// <summary>Запросить перерисовку компонента (безопасно из любого контекста).</summary>
    public Task RefreshAsync()
    {
        if (IsDisposed) return Task.CompletedTask;
        return InvokeAsync(StateHasChanged);
    }

    /// <summary>Запросить перерисовку компонента с выполнением действия (безопасно из любого контекста).</summary>
    public Task RefreshAsync(Action action)
    {
        if (IsDisposed) return Task.CompletedTask;
        return InvokeAsync(() => { action(); StateHasChanged(); });
    }

    /// <summary>Безопасный InvokeAsync для событий — не бросает если компонент удалён.</summary>
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
