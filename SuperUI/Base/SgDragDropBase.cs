// SuperUI/Base/SgDragDropBase.cs
// ИСПРАВЛЕНИЯ v2:
// ✅ BUG-6: SemaphoreSlim для HandleDropAsync — Server thread-safety
// ✅ UX-9: Keyboard-accessible drag (Space/Enter/ArrowUp/ArrowDown) — WCAG 2.1
// ✅ НОВОЕ: DragHandle — только определённые элементы можно тащить
// ✅ НОВОЕ: OnDragStart / OnDragEnd callbacks

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace SuperUI.Base;

public abstract class SgDragDropBase<TItem> : SgInteractiveBase
{
    [Parameter] public List<TItem> Items { get; set; } = [];
    [Parameter] public EventCallback<List<TItem>> ItemsChanged { get; set; }
    [Parameter] public EventCallback<SgDragDropEventArgs<TItem>> OnDrop { get; set; }
    [Parameter] public EventCallback<SgDragDropEventArgs<TItem>> OnDragStart { get; set; }
    [Parameter] public EventCallback<SgDragDropEventArgs<TItem>> OnDragEnd { get; set; }
    [Parameter] public string? DragDataType { get; set; }
    [Parameter] public string? DragClass { get; set; }
    [Parameter] public string? DropZoneActiveClass { get; set; }
    [Parameter] public RenderFragment<TItem>? DragHandle { get; set; }

    protected int DragIndex { get; private set; } = -1;
    protected int DropTargetIndex { get; private set; } = -1;
    protected bool IsDragging => DragIndex >= 0;
    protected TItem? DragItem => DragIndex >= 0 && DragIndex < Items.Count ? Items[DragIndex] : default;

    // BUG-6: Server thread-safety
    private readonly SemaphoreSlim _dropLock = new(1, 1);

    // UX-9: keyboard drag state
    private int _keyboardDragIndex = -1;
    private bool _isKeyboardDragging;

    protected string GetDragClass(int index)
    {
        if (index == DragIndex || index == _keyboardDragIndex)
            return $"sg-dragging {DragClass ?? "sg-dragging--default"}";
        if (index == DropTargetIndex)
            return $"sg-drop-target {DropZoneActiveClass ?? "sg-drop-target--active"}";
        return string.Empty;
    }

    // ── Mouse/Touch handlers ────────────────────────────────────────────────────
    protected async Task HandleDragStartAsync(DragEventArgs e, int index)
    {
        if (IsEffectivelyDisabled || IsDisposed || !CanDrag(index)) return;
        DragIndex = index;
        DropTargetIndex = -1;
        if (e.DataTransfer is not null)
        {
            e.DataTransfer.EffectAllowed = "move";
            e.DataTransfer.DropEffect = "move";
            // Note: Blazor DataTransfer doesn't expose SetData; payload must be set via JS interop.
            // Use JS function `superui.setDragData(event, type, value)` if needed.
            _ = DragDataType;
            await SetDragImageAsync(e);
        }
        await OnDragStart.InvokeAsync(new SgDragDropEventArgs<TItem>(DragItem!, index, index));
        await InvokeAsync(StateHasChanged);
    }

    protected async Task HandleDragEnterAsync(DragEventArgs e, int index)
    {
        if (IsEffectivelyDisabled || IsDisposed || DragIndex < 0) return;
        if (!CanDrop(DragIndex, index)) return;
        DropTargetIndex = index;
        await InvokeAsync(StateHasChanged);
    }

    protected async Task HandleDragOverAsync(DragEventArgs e)
    {
        if (IsEffectivelyDisabled || IsDisposed || DragIndex < 0) return;
        await Task.CompletedTask;
    }

    protected async Task HandleDragLeaveAsync(DragEventArgs e)
    {
        if (IsEffectivelyDisabled || IsDisposed) return;
        DropTargetIndex = -1;
        await InvokeAsync(StateHasChanged);
    }

    // BUG-6 FIX: SemaphoreSlim
    protected async Task HandleDropAsync(DragEventArgs e, int targetIndex)
    {
        if (IsEffectivelyDisabled || IsDisposed || DragIndex < 0 || DragIndex == targetIndex)
        {
            DragIndex = -1; DropTargetIndex = -1;
            await InvokeAsync(StateHasChanged);
            return;
        }

        if (!await _dropLock.WaitAsync(TimeSpan.FromSeconds(1))) return;
        try
        {
            if (!CanDrop(DragIndex, targetIndex)) return;
            var sourceIndex = DragIndex;
            var item = Items[sourceIndex];
            Items.RemoveAt(sourceIndex);
            Items.Insert(sourceIndex > targetIndex ? targetIndex : targetIndex - 1, item);
            DragIndex = -1;
            DropTargetIndex = -1;
            await ItemsChanged.InvokeAsync(Items);
            await OnDrop.InvokeAsync(new SgDragDropEventArgs<TItem>(item, sourceIndex, targetIndex));
        }
        finally { _dropLock.Release(); }

        await InvokeAsync(StateHasChanged);
    }

    protected async Task HandleDragEndAsync(DragEventArgs e)
    {
        var item = DragItem;
        var fromIndex = DragIndex;
        DragIndex = -1;
        DropTargetIndex = -1;
        if (item is not null)
            await OnDragEnd.InvokeAsync(new SgDragDropEventArgs<TItem>(item, fromIndex, fromIndex));
        await InvokeAsync(StateHasChanged);
    }

    // ── UX-9: Keyboard drag ──────────────────────────────────────────────────────
    /// <summary>
    /// Обработать нажатие клавиши на элементе списка для keyboard-accessible drag.
    /// Подключите: @onkeydown="e => HandleItemKeyDownAsync(e, index)"
    /// </summary>
    protected async Task HandleItemKeyDownAsync(KeyboardEventArgs e, int index)
    {
        if (IsEffectivelyDisabled || IsDisposed) return;

        switch (e.Key)
        {
            case " ": // Space — начать/завершить drag
            case "Enter":
                if (!_isKeyboardDragging)
                {
                    if (!CanDrag(index)) return;
                    _isKeyboardDragging = true;
                    _keyboardDragIndex = index;
                }
                else
                {
                    // Завершить drop на текущем DropTargetIndex
                    await PerformKeyboardDropAsync(_keyboardDragIndex, DropTargetIndex >= 0 ? DropTargetIndex : _keyboardDragIndex);
                    _isKeyboardDragging = false;
                    _keyboardDragIndex = -1;
                    DropTargetIndex = -1;
                }
                break;

            case "Escape":
                _isKeyboardDragging = false;
                _keyboardDragIndex = -1;
                DropTargetIndex = -1;
                break;

            case "ArrowUp":
                if (_isKeyboardDragging)
                {
                    var newTarget = Math.Max(0, (DropTargetIndex >= 0 ? DropTargetIndex : _keyboardDragIndex) - 1);
                    DropTargetIndex = newTarget;
                }
                break;

            case "ArrowDown":
                if (_isKeyboardDragging)
                {
                    var newTarget = Math.Min(Items.Count - 1,
                        (DropTargetIndex >= 0 ? DropTargetIndex : _keyboardDragIndex) + 1);
                    DropTargetIndex = newTarget;
                }
                break;
        }

        if (!IsDisposed) await InvokeAsync(StateHasChanged);
    }

    private async Task PerformKeyboardDropAsync(int fromIndex, int toIndex)
    {
        if (fromIndex == toIndex) return;
        if (!await _dropLock.WaitAsync(TimeSpan.FromSeconds(1))) return;
        try
        {
            var item = Items[fromIndex];
            Items.RemoveAt(fromIndex);
            Items.Insert(fromIndex > toIndex ? toIndex : toIndex - 1, item);
            await ItemsChanged.InvokeAsync(Items);
            await OnDrop.InvokeAsync(new SgDragDropEventArgs<TItem>(item, fromIndex, toIndex));
        }
        finally { _dropLock.Release(); }
    }

    protected virtual Task SetDragImageAsync(DragEventArgs e) => Task.CompletedTask;
    protected virtual bool CanDrag(int index) => true;
    protected virtual bool CanDrop(int sourceIndex, int targetIndex) => true;

    protected override async ValueTask DisposeComponentAsync()
    {
        _dropLock.Dispose();
        await base.DisposeComponentAsync();
    }
}

public sealed record SgDragDropEventArgs<T>(T Item, int FromIndex, int ToIndex);
