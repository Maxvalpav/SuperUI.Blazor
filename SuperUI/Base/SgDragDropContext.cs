// SuperUI/Base/SgDragDropContext.cs
// НОВЫЙ КЛАСС: cascade-контекст для компонентов drag & drop
// Дополняет существующий SgDragDropBase

using System;

namespace SuperUI.Base;

/// <summary>
/// Cascade-контекст для drag &amp; drop сценариев.
/// Передаётся через &lt;CascadingValue&gt; от SgDropZone к дочерним SgDraggable.
/// </summary>
public sealed class SgDragDropContext<TItem>
{
    /// <summary>Элемент, который перетаскивается в данный момент.</summary>
    public TItem? DraggedItem { get; internal set; }

    /// <summary>Перетаскивание активно.</summary>
    public bool IsDragging { get; internal set; }

    /// <summary>Индекс источника (для упорядочивания).</summary>
    public int SourceIndex { get; internal set; } = -1;

    /// <summary>Индекс цели (под курсором).</summary>
    public int TargetIndex { get; internal set; } = -1;

    /// <summary>
    /// Callback при начале перетаскивания.
    /// </summary>
    public Action<TItem, int>? OnDragStart { get; init; }

    /// <summary>
    /// Callback при окончании перетаскивания (drop).
    /// </summary>
    public Action<TItem, int, int>? OnDrop { get; init; }

    /// <summary>
    /// Callback при отмене перетаскивания.
    /// </summary>
    public Action? OnDragCancel { get; init; }

    /// <summary>CSS класс для drag-over состояния drop-зоны.</summary>
    public string DragOverClass { get; init; } = "sg-drag-over";

    /// <summary>CSS класс для перетаскиваемого элемента.</summary>
    public string DraggingClass { get; init; } = "sg-dragging";

    internal void BeginDrag(TItem item, int sourceIndex)
    {
        DraggedItem = item;
        SourceIndex = sourceIndex;
        IsDragging = true;
        OnDragStart?.Invoke(item, sourceIndex);
    }

    internal void EndDrag(int targetIndex)
    {
        if (DraggedItem is not null)
            OnDrop?.Invoke(DraggedItem, SourceIndex, targetIndex);

        Reset();
    }

    internal void CancelDrag()
    {
        OnDragCancel?.Invoke();
        Reset();
    }

    private void Reset()
    {
        DraggedItem = default;
        SourceIndex = -1;
        TargetIndex = -1;
        IsDragging = false;
    }
}
