// SuperUI/Base/Services/SgMediaQueryService.cs — НОВЫЙ (MISSING-1)
//
// НОВОЕ:
// ✅ Отслеживание размера окна и брейкпоинтов
// ✅ Стандартные Bootstrap-совместимые брейкпоинты
// ✅ Событие при изменении брейкпоинта
// ✅ Удобные свойства: IsMobile, IsTablet, IsDesktop
// ✅ JS Interop для window resize events

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Microsoft.Extensions.Logging;

namespace SuperUI.Base.Services;

/// <summary>Стандартные брейкпоинты (Bootstrap-совместимые).</summary>
public enum SgBreakpoint
{
    /// <summary>Extra small: &lt; 576px (мобильные телефоны).</summary>
    Xs = 0,

    /// <summary>Small: ≥ 576px (горизонтальные телефоны).</summary>
    Sm = 576,

    /// <summary>Medium: ≥ 768px (планшеты).</summary>
    Md = 768,

    /// <summary>Large: ≥ 992px (маленькие десктопы).</summary>
    Lg = 992,

    /// <summary>Extra large: ≥ 1200px (десктопы).</summary>
    Xl = 1200,

    /// <summary>Extra extra large: ≥ 1400px (большие десктопы).</summary>
    Xxl = 1400
}

/// <summary>
/// Сервис для отслеживания размера окна и брейкпоинтов.
/// Позволяет компонентам реагировать на изменение размера экрана.
/// </summary>
public interface ISgMediaQueryService : IAsyncDisposable
{
    /// <summary>Текущий брейкпоинт.</summary>
    SgBreakpoint CurrentBreakpoint { get; }

    /// <summary>Ширина окна в пикселях.</summary>
    int WindowWidth { get; }

    /// <summary>Событие изменения брейкпоинта.</summary>
    event Action<SgBreakpoint>? OnBreakpointChanged;

    /// <summary>true если текущий брейкпоинт ≥ указанного.</summary>
    bool IsBreakpoint(SgBreakpoint breakpoint);

    /// <summary>true если мобильный (Xs, Sm).</summary>
    bool IsMobile => CurrentBreakpoint < SgBreakpoint.Md;

    /// <summary>true если планшет (Md..Lg).</summary>
    bool IsTablet => CurrentBreakpoint >= SgBreakpoint.Md && CurrentBreakpoint < SgBreakpoint.Lg;

    /// <summary>true если десктоп (Lg и выше).</summary>
    bool IsDesktop => CurrentBreakpoint >= SgBreakpoint.Lg;
}

/// <summary>
/// Реализация ISgMediaQueryService через JS Interop.
/// Отслеживает window resize события и уведомляет подписчиков.
/// </summary>
public sealed class SgMediaQueryService : ISgMediaQueryService
{
    private readonly IJSRuntime _js;
    private readonly ILogger<SgMediaQueryService> _logger;
    private DotNetObjectReference<SgMediaQueryService>? _dotNetRef;
    private int _windowWidth;
    private int _disposed;

    // Брейкпоинты в порядке убывания для быстрого поиска
    private static readonly (int Width, SgBreakpoint Bp)[] _breakpoints =
    [
        (1400, SgBreakpoint.Xxl),
        (1200, SgBreakpoint.Xl),
        (992,  SgBreakpoint.Lg),
        (768,  SgBreakpoint.Md),
        (576,  SgBreakpoint.Sm),
        (0,    SgBreakpoint.Xs),
    ];

    // ── Свойства ────────────────────────────────────────────────────────────

    /// <summary>Текущий брейкпоинт.</summary>
    public SgBreakpoint CurrentBreakpoint { get; private set; } = SgBreakpoint.Md;

    /// <summary>Ширина окна в пикселях.</summary>
    public int WindowWidth => _windowWidth;

    /// <summary>Событие при изменении брейкпоинта.</summary>
    public event Action<SgBreakpoint>? OnBreakpointChanged;

    // ── Конструктор ─────────────────────────────────────────────────────────

    public SgMediaQueryService(IJSRuntime js, ILogger<SgMediaQueryService> logger)
    {
        _js = js;
        _logger = logger;
    }

    // ── Инициализация ───────────────────────────────────────────────────────

    /// <summary>Инициализировать сервис и начать отслеживание resize событий.</summary>
    public async Task InitializeAsync()
    {
        try
        {
            _dotNetRef = DotNetObjectReference.Create(this);

            // Получить текущую ширину окна
            _windowWidth = await _js.InvokeAsync<int>("eval", "window.innerWidth");
            CurrentBreakpoint = GetBreakpoint(_windowWidth);

            // Зарегистрировать JS callback для resize событий
            await _js.InvokeVoidAsync(
                "superui.mediaQuery.observe",
                _dotNetRef,
                nameof(OnWindowResized));

            _logger.LogDebug("MediaQueryService initialized: {Width}px, {Breakpoint}",
                _windowWidth, CurrentBreakpoint);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "MediaQueryService init failed");
        }
    }

    // ── JS Callback ─────────────────────────────────────────────────────────

    /// <summary>
    /// Вызывается из JS при изменении размера окна.
    /// </summary>
    [JSInvokable]
    public void OnWindowResized(int newWidth)
    {
        if (Volatile.Read(ref _disposed) == 1) return;

        var prev = CurrentBreakpoint;
        _windowWidth = newWidth;
        CurrentBreakpoint = GetBreakpoint(newWidth);

        // Уведомляем только если брейкпоинт изменился
        if (CurrentBreakpoint != prev)
        {
            _logger.LogDebug("Breakpoint changed: {Previous} → {Current} ({Width}px)",
                prev, CurrentBreakpoint, newWidth);
            OnBreakpointChanged?.Invoke(CurrentBreakpoint);
        }
    }

    // ── Публичные методы ────────────────────────────────────────────────────

    /// <summary>Проверить, находится ли текущий брейкпоинт на уровне или выше указанного.</summary>
    public bool IsBreakpoint(SgBreakpoint breakpoint)
        => CurrentBreakpoint >= breakpoint;

    // ── Приватные методы ────────────────────────────────────────────────────

    /// <summary>Определить брейкпоинт по ширине окна.</summary>
    private static SgBreakpoint GetBreakpoint(int width)
    {
        foreach (var (w, bp) in _breakpoints)
        {
            if (width >= w) return bp;
        }
        return SgBreakpoint.Xs;
    }

    // ── Dispose ─────────────────────────────────────────────────────────────

    /// <summary>Очистить ресурсы и отписаться от resize событий.</summary>
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1) return;

        try
        {
            await _js.InvokeVoidAsync("superui.mediaQuery.dispose");
        }
        catch { }

        _dotNetRef?.Dispose();
        _dotNetRef = null;
    }
}
