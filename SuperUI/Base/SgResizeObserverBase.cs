// SuperUI/Base/SgResizeObserverBase.cs — НОВЫЙ (MISSING-4)
//
// НОВОЕ:
// ✅ Отслеживание изменения размеров элемента через ResizeObserver API
// ✅ Callback при изменении размеров
// ✅ Удобные свойства: ElementWidth, ElementHeight, AspectRatio
// ✅ Поддержка кастомных элементов для наблюдения
// ✅ Правильная очистка ресурсов

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace SuperUI.Base;

/// <summary>
/// Базовый класс для компонентов, реагирующих на изменение своих размеров.
/// Использует нативный ResizeObserver API для отслеживания размеров.
/// </summary>
/// <remarks>
/// ResizeObserver поддерживается всеми современными браузерами.
/// Позволяет компонентам адаптироваться к доступному пространству.
/// </remarks>
public abstract class SgResizeObserverBase : SgJsComponentBase
{
    private DotNetObjectReference<SgResizeObserverBase>? _selfRef;
    private bool _observing;

    // ── Параметры ───────────────────────────────────────────────────────────

    /// <summary>Элемент для наблюдения. По умолчанию — корневой элемент компонента.</summary>
    protected ElementReference? ObservedElement { get; set; }

    // ── Состояние ───────────────────────────────────────────────────────────

    /// <summary>Последний известный размер элемента.</summary>
    protected SgElementSize? ElementSize { get; private set; }

    /// <summary>Ширина элемента в пикселях (0 если нет данных).</summary>
    protected double ElementWidth => ElementSize?.Width ?? 0;

    /// <summary>Высота элемента в пикселях (0 если нет данных).</summary>
    protected double ElementHeight => ElementSize?.Height ?? 0;

    /// <summary>Соотношение сторон элемента (Width / Height).</summary>
    protected double AspectRatio => ElementSize?.AspectRatio ?? double.NaN;

    /// <summary>true если элемент в ландшафтной ориентации (Width > Height).</summary>
    protected bool IsLandscape => ElementSize?.IsLandscape ?? false;

    // ── Жизненный цикл ──────────────────────────────────────────────────────

    protected override async Task OnFirstRenderAsync()
    {
        await base.OnFirstRenderAsync();
        await StartObservingAsync();
    }

    // ── Публичные методы ────────────────────────────────────────────────────

    /// <summary>Начать наблюдение за изменением размеров элемента.</summary>
    protected async Task StartObservingAsync()
    {
        if (_observing || ObservedElement is null || IsPrerendering) return;

        _selfRef ??= DotNetObjectReference.Create(this);

        await SafeInvokeVoidAsync(
            "superui.observeResize",
            ObservedElement.Value,
            _selfRef,
            nameof(OnResizeCallback));

        _observing = true;
    }

    /// <summary>Остановить наблюдение за размерами элемента.</summary>
    protected async Task StopObservingAsync()
    {
        if (!_observing || ObservedElement is null) return;

        await SafeInvokeVoidAsync("superui.unobserveResize", ObservedElement.Value);
        _observing = false;
    }

    // ── JS Callback ─────────────────────────────────────────────────────────

    /// <summary>
    /// Вызывается из JS при изменении размеров наблюдаемого элемента.
    /// </summary>
    [JSInvokable]
    public async Task OnResizeCallback(double width, double height)
    {
        if (IsDisposed) return;

        var newSize = new SgElementSize(width, height);
        var changed = ElementSize != newSize;

        ElementSize = newSize;

        if (changed)
        {
            await OnElementResizedAsync(newSize);
            await InvokeAsync(StateHasChanged);
        }
    }

    // ── Виртуальные методы ───────────────────────────────────────────────────

    /// <summary>
    /// Вызывается при изменении размеров элемента.
    /// Override для реакции на изменение размеров.
    /// </summary>
    /// <param name="newSize">Новые размеры элемента.</param>
    protected virtual Task OnElementResizedAsync(SgElementSize newSize) => Task.CompletedTask;

    // ── Dispose ─────────────────────────────────────────────────────────────

    protected override async ValueTask DisposeComponentAsync()
    {
        await StopObservingAsync();
        _selfRef?.Dispose();
        _selfRef = null;
        await base.DisposeComponentAsync();
    }
}

/// <summary>
/// Размеры HTML-элемента.
/// </summary>
/// <remarks>
/// Содержит информацию о размерах элемента, полученную от ResizeObserver.
/// Включает удобные вычисляемые свойства для адаптивного дизайна.
/// </remarks>
public readonly record struct SgElementSize(double Width, double Height)
{
    /// <summary>Соотношение сторон (Width / Height). NaN если Height == 0.</summary>
    public double AspectRatio => Height == 0 ? double.NaN : Width / Height;

    /// <summary>true если элемент в ландшафтной ориентации (Width > Height).</summary>
    public bool IsLandscape => Width > Height;

    /// <summary>true если элемент в портретной ориентации (Width ≤ Height).</summary>
    public bool IsPortrait => Width <= Height;

    /// <summary>true если элемент квадратный (Width ≈ Height).</summary>
    public bool IsSquare => Math.Abs(Width - Height) < 1;

    /// <summary>Площадь элемента в квадратных пикселях.</summary>
    public double Area => Width * Height;

    /// <summary>Периметр элемента в пикселях.</summary>
    public double Perimeter => 2 * (Width + Height);

    /// <summary>Диагональ элемента в пикселях.</summary>
    public double Diagonal => Math.Sqrt(Width * Width + Height * Height);

    /// <summary>Информативное представление размеров.</summary>
    public override string ToString() => $"{Width:F1}×{Height:F1}px";
}
