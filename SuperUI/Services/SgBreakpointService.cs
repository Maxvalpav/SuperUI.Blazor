// SuperUI/Services/SgBreakpointService.cs
// Сервис брейкпоинтов на базе SgViewportService с явным набором порогов.

namespace SuperUI.Services;

/// <summary>
/// Именованные брейкпоинты по умолчанию (px).
/// </summary>
public enum SgBreakpoint
{
    /// <summary>0 – 575.99px (портретные телефоны).</summary>
    Xs,
    /// <summary>576 – 767.99px (ландшафтные телефоны).</summary>
    Sm,
    /// <summary>768 – 991.99px (планшеты).</summary>
    Md,
    /// <summary>992 – 1199.99px (ноутбуки).</summary>
    Lg,
    /// <summary>1200 – 1399.99px (десктопы).</summary>
    Xl,
    /// <summary>≥ 1400px (большие десктопы).</summary>
    Xxl,
}

/// <summary>
/// Сервис брейкпоинтов: текущий <see cref="Breakpoint"/>, уведомление о смене.
/// </summary>
/// <remarks>
/// <para>Оборачивает <see cref="SgViewportService"/>, добавляет пороги и уведомляет
/// подписчиков только при РЕАЛЬНОЙ смене (не каждое resize-событие).</para>
/// <para>Пороги можно переопределить в конструкторе. По умолчанию — Bootstrap 5.</para>
/// </remarks>
public sealed class SgBreakpointService : IAsyncDisposable
{
    private readonly SgViewportService _viewport;
    private readonly int _xs, _sm, _md, _lg, _xl;
    private int _disposed;

    public SgBreakpointService(SgViewportService viewport)
    {
        _viewport = viewport;
        // Bootstrap 5 defaults
        _xs = 0; _sm = 576; _md = 768; _lg = 992; _xl = 1200;
        _viewport.Changed += OnViewportChanged;
    }

    public SgBreakpointService(SgViewportService viewport, int xs, int sm, int md, int lg, int xl)
    {
        _viewport = viewport;
        _xs = xs; _sm = sm; _md = md; _lg = lg; _xl = xl;
        _viewport.Changed += OnViewportChanged;
    }

    /// <summary>Текущий брейкпоинт.</summary>
    public SgBreakpoint Breakpoint { get; private set; } = SgBreakpoint.Xs;

    /// <summary>True, если ширина ≥ указанного брейкпоинта.</summary>
    public bool IsUp(SgBreakpoint bp) => ResolveOrder(Breakpoint) >= ResolveOrder(bp);
    /// <summary>True, если ширина &lt; указанного брейкпоинта.</summary>
    public bool IsDown(SgBreakpoint bp) => ResolveOrder(Breakpoint) < ResolveOrder(bp);
    /// <summary>True, если ширина строго между <paramref name="min"/> и <paramref name="max"/>.</summary>
    public bool IsBetween(SgBreakpoint min, SgBreakpoint max)
    {
        var order = ResolveOrder(Breakpoint);
        return order >= ResolveOrder(min) && order < ResolveOrder(max);
    }

    /// <summary>Событие смены брейкпоинта.</summary>
    public event Action<SgBreakpoint>? Changed;

    /// <summary>Принудительно пересчитывает брейкпоинт (полезно после hot-reload).</summary>
    public void Recalculate() => Evaluate();

    private void OnViewportChanged() => Evaluate();

    private void Evaluate()
    {
        var w = _viewport.Width;
        SgBreakpoint next;
        if (w < _sm) next = SgBreakpoint.Xs;
        else if (w < _md) next = SgBreakpoint.Sm;
        else if (w < _lg) next = SgBreakpoint.Md;
        else if (w < _xl) next = SgBreakpoint.Lg;
        else next = SgBreakpoint.Xl;

        if (w >= 1400) next = SgBreakpoint.Xxl;

        if (next != Breakpoint)
        {
            Breakpoint = next;
            Changed?.Invoke(Breakpoint);
        }
    }

    private static int ResolveOrder(SgBreakpoint bp) => bp switch
    {
        SgBreakpoint.Xs => 0,
        SgBreakpoint.Sm => 1,
        SgBreakpoint.Md => 2,
        SgBreakpoint.Lg => 3,
        SgBreakpoint.Xl => 4,
        SgBreakpoint.Xxl => 5,
        _ => 0,
    };

    /// <inheritdoc/>
    public ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1) return ValueTask.CompletedTask;
        _viewport.Changed -= OnViewportChanged;
        return ValueTask.CompletedTask;
    }
}
