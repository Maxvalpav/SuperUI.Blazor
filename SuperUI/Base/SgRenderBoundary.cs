// SuperUI/Base/SgRenderBoundary.cs
//
// Защита от рендер-рекурсии и избыточных рендеров.
// Используется для компонентов с дорогим рендером (DataGrid, Canvas, Chart).
//
// Принцип: если рендер уже идёт — следующий StateHasChanged откладывается.

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using System.Threading;

namespace SuperUI.Base;

/// <summary>
/// Компонент-обёртка, предотвращающая рендер-рекурсию.
/// Дочерние компоненты рендерятся только после полного завершения текущего рендера.
/// </summary>
public sealed class SgRenderBoundary : ComponentBase, IDisposable
{
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>Максимальное количество отложенных рендеров.</summary>
    [Parameter] public int MaxPendingRenders { get; set; } = 3;

    private int _renderDepth;
    private int _pendingRenders;
    private bool _disposed;

    protected override bool ShouldRender()
    {
        // Если уже рендеримся — откладываем
        if (_renderDepth > 0)
        {
            Interlocked.Increment(ref _pendingRenders);
            return false;
        }
        return true;
    }

    protected override void OnAfterRender(bool firstRender)
    {
        _renderDepth--;
        var pending = Interlocked.Exchange(ref _pendingRenders, 0);
        if (pending > 0 && !_disposed)
            _ = InvokeAsync(StateHasChanged);
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        _renderDepth++;
        ChildContent?.Invoke(builder);
    }

    public void Dispose() => _disposed = true;
}
