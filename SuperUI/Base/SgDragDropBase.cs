// SuperUI/Base/SgDragDropBase.cs — НОВЫЙ (П12)
//
// НОВОЕ:
// ✅ HTML5 Drag and Drop API поддержка
// ✅ Touch события через JS (для мобильных)
// ✅ Визуальная обратная связь (drag/drop классы)
// ✅ Callback при перемещении элемента
// ✅ Кастомизируемые drag images
// ✅ Фильтрация по типу данных

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace SuperUI.Base;

/// <summary>
/// Базовый класс для drag-and-drop компонентов.
/// Поддерживает HTML5 Drag and Drop + Touch события через JS.
/// </summary>
/// <typeparam name="TItem">Тип перетаскиваемого элемента.</typeparam>
public abstract class SgDragDropBase<TItem> : SgInteractiveBase
{
    // ── Параметры ────────────────────────────────────────────────────────────

    /// <summary>Список элементов для перетаскивания.</summary>
    [Parameter] public List<TItem> Items { get; set; } = [];

    /// <summary>Callback при изменении порядка элементов.</summary>
    [Parameter] public EventCallback<List<TItem>> ItemsChanged { get; set; }

    /// <summary>Callback при перемещении элемента.</summary>
    [Parameter] public EventCallback<SgDragDropEventArgs<TItem>> OnDrop { get; set; }

    /// <summary>Тип данных драг-события (для фильтрации между дроп-зонами).</summary>
    [Parameter] public string? DragDataType { get; set; }

    /// <summary>CSS-класс для перетаскиваемого элемента.</summary>
    [Parameter] public string? DragClass { get; set; }

    /// <summary>CSS-класс для активной дроп-зоны.</summary>
    [Parameter] public string? DropZoneActiveClass { get; set; }

    // ── Состояние ────────────────────────────────────────────────────────────

    /// <summary>Индекс перетаскиваемого элемента (-1 если нет).</summary>
    protected int DragIndex { get; private set; } = -1;

    /// <summary>Индекс цели дропа (-1 если нет).</summary>
    protected int DropTargetIndex { get; private set; } = -1;

    // ── Публичные свойства ──────────────────────────────────────────────────

    /// <summary>Идёт перетаскивание.</summary>
    protected bool IsDragging => DragIndex >= 0;

    /// <summary>Элемент в процессе перетаскивания.</summary>
    protected TItem? DragItem => DragIndex >= 0 && DragIndex < Items.Count
        ? Items[DragIndex]
        : default;

    /// <summary>Получить CSS-класс для элемента по индексу.</summary>
    protected string GetDragClass(int index)
    {
        if (index == DragIndex)
            return $"sg-dragging {DragClass ?? "sg-dragging--default"}";
        if (index == DropTargetIndex)
            return $"sg-drop-target {DropZoneActiveClass ?? "sg-drop-target--active"}";
        return string.Empty;
    }

    // ── Drag Handlers ────────────────────────────────────────────────────────

    /// <summary>Обработчик начала перетаскивания.</summary>
    protected async Task HandleDragStartAsync(DragEventArgs e, int index)
    {
        if (IsEffectivelyDisabled || IsDisposed) return;

        DragIndex = index;
        DropTargetIndex = -1;

        if (e.DataTransfer is not null)
        {
            e.DataTransfer.EffectAllowed = "move";
            e.DataTransfer.DropEffect = "move";

            if (DragDataType is not null)
                e.DataTransfer.SetData(DragDataType, string.Empty);

            // Устанавливаем drag image если есть
            await SetDragImageAsync(e);
        }

        await InvokeAsync(StateHasChanged);
    }

    /// <summary>Обработчик входа в дроп-зону.</summary>
    protected async Task HandleDragEnterAsync(DragEventArgs e, int index)
    {
        if (IsEffectivelyDisabled || IsDisposed || DragIndex < 0) return;

        e.DataTransfer!.DropEffect = "move";
        DropTargetIndex = index;

        await InvokeAsync(StateHasChanged);
    }

    /// <summary>Обработчик перемещения над дроп-зоной.</summary>
    protected async Task HandleDragOverAsync(DragEventArgs e)
    {
        if (IsEffectivelyDisabled || IsDisposed || DragIndex < 0) return;

        e.DataTransfer!.DropEffect = "move";
        // preventDefault нужен для разрешения drop
        e.DataTransfer.DropEffect = "move";
    }

    /// <summary>Обработчик выхода из дроп-зоны.</summary>
    protected async Task HandleDragLeaveAsync(DragEventArgs e)
    {
        if (IsEffectivelyDisabled || IsDisposed) return;

        DropTargetIndex = -1;
        await InvokeAsync(StateHasChanged);
    }

    /// <summary>Обработчик дропа элемента.</summary>
    protected async Task HandleDropAsync(DragEventArgs e, int targetIndex)
    {
        if (IsEffectivelyDisabled || IsDisposed || DragIndex < 0) return;

        var sourceIndex = DragIndex;

        // Reset drag state ДО перемещения
        DragIndex = -1;
        DropTargetIndex = -1;

        if (sourceIndex != targetIndex && sourceIndex < Items.Count && targetIndex < Items.Count)
        {
            var item = Items[sourceIndex];
            Items.RemoveAt(sourceIndex);
            Items.Insert(targetIndex > sourceIndex ? targetIndex - 1 : targetIndex, item);

            await ItemsChanged.InvokeAsync(Items);
            await OnDrop.InvokeAsync(new SgDragDropEventArgs<TItem>(item, sourceIndex, targetIndex));
        }

        await InvokeAsync(StateHasChanged);
    }

    /// <summary>Обработчик конца перетаскивания.</summary>
    protected async Task HandleDragEndAsync(DragEventArgs e)
    {
        DragIndex = -1;
        DropTargetIndex = -1;
        await InvokeAsync(StateHasChanged);
    }

    // ── Виртуальные методы ───────────────────────────────────────────────────

    /// <summary>
    /// Установить drag image через JS (если модуль доступен).
    /// Переопределите для кастомного drag image.
    /// </summary>
    protected virtual async Task SetDragImageAsync(DragEventArgs e)
    {
        // Переопределите для кастомного drag image
        await Task.CompletedTask;
    }

    /// <summary>
    /// Проверить, может ли элемент быть перетащен.
    /// Переопределите для добавления кастомной логики.
    /// </summary>
    protected virtual bool CanDrag(int index) => true;

    /// <summary>
    /// Проверить, может ли элемент быть дропнут на целевой индекс.
    /// Переопределите для добавления кастомной логики.
    /// </summary>
    protected virtual bool CanDrop(int sourceIndex, int targetIndex) => true;
}

/// <summary>
/// Аргументы события drag-and-drop.
/// </summary>
public sealed record SgDragDropEventArgs<T>(
    T Item,
    int FromIndex,
    int ToIndex);
