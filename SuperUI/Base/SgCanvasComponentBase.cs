// SuperUI/Base/SgCanvasComponentBase.cs

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace SuperUI.Base;

/// <summary>
/// Base для Canvas-рендеринга. Используется в SgCanvasGrid.
/// Оптимизации:
/// - Dirty rectangles — перерисовываем только изменившиеся области
/// - requestAnimationFrame батчинг
/// - OffscreenCanvas для Web Workers (WASM)
/// </summary>
public abstract class SgCanvasComponentBase : SgJsComponentBase
{
    [Parameter] public int Width { get; set; } = 800;
    [Parameter] public int Height { get; set; } = 600;
    [Parameter] public float DevicePixelRatio { get; set; } = 1;

    protected ElementReference CanvasElement;
    private readonly HashSet<(int x, int y, int w, int h)> _dirtyRects = new();
    private bool _fullRedrawPending;

    protected void MarkDirty(int x, int y, int width, int height)
        => _dirtyRects.Add((x, y, width, height));

    protected void MarkFullRedraw()
        => _fullRedrawPending = true;

    protected async Task FlushCanvasAsync()
    {
        if (_fullRedrawPending)
        {
            _dirtyRects.Clear();
            await SafeInvokeVoidAsync("clearCanvas", CanvasElement);
            await DrawAllAsync();
            _fullRedrawPending = false;
        }
        else if (_dirtyRects.Count > 0)
        {
            foreach (var rect in _dirtyRects)
                await DrawRectAsync(rect.x, rect.y, rect.w, rect.h);
            _dirtyRects.Clear();
        }
    }

    protected virtual Task DrawAllAsync() => Task.CompletedTask;
    protected virtual Task DrawRectAsync(int x, int y, int w, int h) => Task.CompletedTask;
}
