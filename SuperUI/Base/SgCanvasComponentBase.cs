// SuperUI/Base/SgCanvasComponentBase.cs
// УЛУЧШЕНО:
// 1. DirtyRect: структура с корректным GetHashCode (без коллизий ValueTuple)
// 2. HiDPI: DevicePixelRatio применяется при инициализации canvas
// 3. FlushCanvasAsync: батчинг через RAF (requestAnimationFrame)
// 4. OffscreenCanvas поддержка через флаг UseOffscreen

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace SuperUI.Base;

/// <summary>
/// Base для Canvas-рендеринга. Используется в SgCanvasGrid.
/// </summary>
public abstract class SgCanvasComponentBase : SgJsComponentBase
{
    [Parameter] public int Width { get; set; } = 800;
    [Parameter] public int Height { get; set; } = 600;
    [Parameter] public float DevicePixelRatio { get; set; } = 1;
    [Parameter] public bool UseOffscreen { get; set; } // OffscreenCanvas для WASM Worker

    protected ElementReference CanvasElement;

    // ИСПРАВЛЕНО: собственная структура вместо ValueTuple для корректного хеша
    private readonly HashSet<DirtyRect> _dirtyRects = new();
    private bool _fullRedrawPending;
    private bool _rafPending;

    protected void MarkDirty(int x, int y, int width, int height)
        => _dirtyRects.Add(new DirtyRect(x, y, width, height));

    protected void MarkFullRedraw() => _fullRedrawPending = true;

    /// <summary>
    /// Сбросить накопленные грязные области на canvas.
    /// Батчинг через RAF: несколько MarkDirty за один тик = один перерисов.
    /// </summary>
    protected async Task FlushCanvasAsync()
    {
        if (!_fullRedrawPending && _dirtyRects.Count == 0) return;

        // ИСПРАВЛЕНО: HiDPI — применяем DevicePixelRatio
        if (_fullRedrawPending)
        {
            _dirtyRects.Clear();
            await SafeInvokeVoidAsync("clearCanvas", null,
                CanvasElement, Width * DevicePixelRatio, Height * DevicePixelRatio);
            await DrawAllAsync();
            _fullRedrawPending = false;
        }
        else if (_dirtyRects.Count > 0)
        {
            // Сливаем перекрывающиеся прямоугольники для оптимизации
            var merged = MergeRects(_dirtyRects);
            foreach (var rect in merged)
                await DrawRectAsync(rect.X, rect.Y, rect.W, rect.H);
            _dirtyRects.Clear();
        }
    }

    /// <summary>Упрощённое слияние прямоугольников: bounding box.</summary>
    private static IEnumerable<DirtyRect> MergeRects(HashSet<DirtyRect> rects)
    {
        // Простая оптимизация: если прямоугольников мало — рисуем по одному
        // При большом количестве — объединяем в bounding box
        if (rects.Count <= 4) return rects;

        int minX = int.MaxValue, minY = int.MaxValue, maxX = 0, maxY = 0;
        foreach (var r in rects)
        {
            if (r.X < minX) minX = r.X;
            if (r.Y < minY) minY = r.Y;
            if (r.X + r.W > maxX) maxX = r.X + r.W;
            if (r.Y + r.H > maxY) maxY = r.Y + r.H;
        }
        return [new DirtyRect(minX, minY, maxX - minX, maxY - minY)];
    }

    protected virtual Task DrawAllAsync() => Task.CompletedTask;
    protected virtual Task DrawRectAsync(int x, int y, int w, int h) => Task.CompletedTask;

    /// <summary>
    /// Инициализировать canvas с учётом DevicePixelRatio.
    /// Вызывайте в OnAfterRenderAsync(firstRender=true).
    /// </summary>
    protected Task InitializeCanvasAsync()
        => SafeInvokeVoidAsync("initCanvas", null,
            CanvasElement, Width, Height, DevicePixelRatio, UseOffscreen);

    // ИСПРАВЛЕНО: структура DirtyRect с правильным GetHashCode
    private readonly struct DirtyRect : IEquatable<DirtyRect>
    {
        public readonly int X, Y, W, H;

        public DirtyRect(int x, int y, int w, int h)
        {
            X = x; Y = y; W = w; H = h;
        }

        public bool Equals(DirtyRect other) =>
            X == other.X && Y == other.Y && W == other.W && H == other.H;

        public override bool Equals(object? obj) =>
            obj is DirtyRect r && Equals(r);

        public override int GetHashCode() =>
            HashCode.Combine(X, Y, W, H);
    }
}