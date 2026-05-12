// SuperUI/Base/SgCanvasComponentBase.cs
// ИСПРАВЛЕНИЯ:
// 1. CS0029: DisposeComponentAsync возвращает ValueTask (не Task)
// 2. DrawAllAsync / DrawRectAsync → ValueTask (нет аллокаций)
// 3. _rafPending — убран (был неиспользуемым)
// 4. FlushCanvasAsync — SemaphoreSlim для защиты от конкурентного вызова
// 5. MergeRects — улучшенная эвристика (bbox ratio)
// 6. DevicePixelRatio — автодетекция если не задан явно
// 7. DisposeComponentAsync — правильный dispose _flushLock

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
    [Parameter] public float DevicePixelRatio { get; set; } = 1f;
    [Parameter] public bool UseOffscreen { get; set; }

    protected ElementReference CanvasElement;

    private readonly HashSet<DirtyRect> _dirtyRects = new();
    private bool _fullRedrawPending;

    // ИСПРАВЛЕНО: SemaphoreSlim для защиты от конкурентного flush на Server
    private readonly SemaphoreSlim _flushLock = new(1, 1);
    // ИСПРАВЛЕНО: флаг что DPR уже определён (чтобы не переопределять каждый рендер)
    private bool _dprInitialized;

    protected void MarkDirty(int x, int y, int width, int height)
        => _dirtyRects.Add(new DirtyRect(x, y, width, height));

    protected void MarkFullRedraw() => _fullRedrawPending = true;

    /// <summary>
    /// Сбросить накопленные грязные области на canvas.
    /// Батчинг: несколько MarkDirty за один тик = один перерисов.
    /// Thread-safe через SemaphoreSlim.
    /// </summary>
    protected async ValueTask FlushCanvasAsync()
    {
        if (!_fullRedrawPending && _dirtyRects.Count == 0) return;
        // Пропускаем если flush уже выполняется (не ставим в очередь)
        if (!await _flushLock.WaitAsync(0)) return;
        try
        {
            if (_fullRedrawPending)
            {
                _dirtyRects.Clear();
                // ИСПРАВЛЕНО: передаём масштабированные размеры (HiDPI)
                await SafeInvokeVoidAsync("clearCanvas", null,
                    CanvasElement,
                    Width * DevicePixelRatio,
                    Height * DevicePixelRatio);
                await DrawAllAsync();
                _fullRedrawPending = false;
            }
            else if (_dirtyRects.Count > 0)
            {
                var merged = MergeRects(_dirtyRects);
                foreach (var rect in merged)
                    await DrawRectAsync(rect.X, rect.Y, rect.W, rect.H);
                _dirtyRects.Clear();
            }
        }
        finally
        {
            _flushLock.Release();
        }
    }

    // ИСПРАВЛЕНО: улучшенная эвристика слияния (bbox только если выгодно)
    private static IEnumerable<DirtyRect> MergeRects(HashSet<DirtyRect> rects)
    {
        if (rects.Count <= 3) return rects;

        int minX = int.MaxValue, minY = int.MaxValue, maxX = 0, maxY = 0;
        int totalArea = 0;

        foreach (var r in rects)
        {
            if (r.X < minX) minX = r.X;
            if (r.Y < minY) minY = r.Y;
            if (r.X + r.W > maxX) maxX = r.X + r.W;
            if (r.Y + r.H > maxY) maxY = r.Y + r.H;
            totalArea += r.W * r.H;
        }

        int bboxArea = (maxX - minX) * (maxY - minY);
        // Объединяем только если bbox не более чем в 1.5 раза больше суммы площадей
        if (bboxArea <= (int)(totalArea * 1.5))
            return [new DirtyRect(minX, minY, maxX - minX, maxY - minY)];

        return rects;
    }

    // ИСПРАВЛЕНО: возвращают ValueTask — нет аллокаций при CompletedTask
    /// <summary>Перерисовать весь canvas. Переопределить в дочернем классе.</summary>
    protected virtual ValueTask DrawAllAsync() => ValueTask.CompletedTask;

    /// <summary>Перерисовать конкретную область. Переопределить в дочернем классе.</summary>
    protected virtual ValueTask DrawRectAsync(int x, int y, int w, int h) => ValueTask.CompletedTask;

    /// <summary>
    /// Инициализировать canvas с учётом DevicePixelRatio.
    /// Вызывайте в OnAfterRenderAsync(firstRender=true).
    /// </summary>
    protected async Task InitializeCanvasAsync()
    {
        // ИСПРАВЛЕНО: автодетекция DPR если не задан явно (только 1f = default)
        if (!_dprInitialized)
        {
            _dprInitialized = true;
            if (DevicePixelRatio == 1f)
            {
                var dpr = await SafeInvokeAsync<float>("getDevicePixelRatio");
                if (dpr > 1f)
                    DevicePixelRatio = dpr;
            }
        }
        await SafeInvokeVoidAsync("initCanvas", null,
            CanvasElement, Width, Height, DevicePixelRatio, UseOffscreen);
    }

    // ИСПРАВЛЕНО: DisposeComponentAsync возвращает ValueTask (не Task)
    // CS0029 был именно здесь: если было 'override async Task' вместо 'ValueTask'
    protected override async ValueTask DisposeComponentAsync()
    {
        _dirtyRects.Clear();
        // ИСПРАВЛЕНО: Dispose SemaphoreSlim
        _flushLock.Dispose();
        await base.DisposeComponentAsync();
    }

    // ИСПРАВЛЕНО: структура DirtyRect с правильным GetHashCode (без коллизий ValueTuple)
    private readonly struct DirtyRect : IEquatable<DirtyRect>
    {
        public readonly int X, Y, W, H;

        public DirtyRect(int x, int y, int w, int h)
        {
            X = x; Y = y; W = w; H = h;
        }

        public bool Equals(DirtyRect other)
            => X == other.X && Y == other.Y && W == other.W && H == other.H;

        public override bool Equals(object? obj) => obj is DirtyRect r && Equals(r);
        public override int GetHashCode() => HashCode.Combine(X, Y, W, H);
    }
}
