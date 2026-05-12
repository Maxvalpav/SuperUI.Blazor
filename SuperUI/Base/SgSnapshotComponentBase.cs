using Microsoft.AspNetCore.Components;

namespace SuperUI.Base;

/// <summary>
/// Базовый класс для компонентов с поддержкой снимков состояния (undo/redo).
/// Используется в редакторах (SgDiagramEditor, SgRichTextEditor и т.д.).
/// </summary>
/// <typeparam name="TState">Тип снимка состояния.</typeparam>
public abstract class SgSnapshotComponentBase<TState> : SgInteractiveBase
    where TState : class
{
    // FIX: LinkedList вместо Stack — эффективное удаление с хвоста O(1) без ToArray
    private readonly LinkedList<TState> _undoStack = new();
    private readonly Stack<TState> _redoStack = new();
    private int _maxSnapshots = 50;

    /// <summary>Максимальное количество снимков в истории.</summary>
    [Parameter]  // ← FIX CS0246: using Microsoft.AspNetCore.Components добавлен
    public int MaxSnapshots
    {
        get => _maxSnapshots;
        set => _maxSnapshots = Math.Max(1, value);
    }

    /// <summary>Можно выполнить Undo.</summary>
    public bool CanUndo => _undoStack.Count > 0;

    /// <summary>Можно выполнить Redo.</summary>
    public bool CanRedo => _redoStack.Count > 0;

    /// <summary>Количество снимков в undo-стеке.</summary>
    public int SnapshotCount => _undoStack.Count;

    /// <summary>
    /// Сохранить текущее состояние для undo.
    /// Вызывайте ПЕРЕД мутирующей операцией.
    /// </summary>
    /// <remarks>
    /// УЛУЧШЕНИЕ: LinkedList → удаление первого (самого старого) элемента O(1).
    /// Оригинал: Stack.ToArray() + rebuild → O(n).
    /// </remarks>
    protected void SaveSnapshot(TState snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        _redoStack.Clear(); // новое действие сбрасывает redo

        // FIX: LinkedList.RemoveFirst() — O(1) вместо O(n) через ToArray
        while (_undoStack.Count >= _maxSnapshots)
            _undoStack.RemoveFirst();

        _undoStack.AddLast(snapshot);
    }

    /// <summary>Отменить последнее действие.</summary>
    public async Task UndoAsync()
    {
        if (!CanUndo || IsDisposed) return;

        var current = GetCurrentState();
        if (current is not null)
            _redoStack.Push(current);

        // FIX: LinkedList → берём с конца (последний = самый новый)
        var snapshot = _undoStack.Last!.Value;
        _undoStack.RemoveLast();

        await ApplySnapshotAsync(snapshot);
        StateHasChanged();
    }

    /// <summary>Повторить отменённое действие.</summary>
    public async Task RedoAsync()
    {
        if (!CanRedo || IsDisposed) return;

        var current = GetCurrentState();
        if (current is not null)
            _undoStack.AddLast(current);

        var snapshot = _redoStack.Pop();
        await ApplySnapshotAsync(snapshot);
        StateHasChanged();
    }

    /// <summary>Очистить историю снимков.</summary>
    public void ClearHistory()
    {
        _undoStack.Clear();
        _redoStack.Clear();
    }

    /// <summary>Количество шагов redo в стеке.</summary>
    public int RedoCount => _redoStack.Count;

    /// <summary>Получить текущее состояние компонента (для сохранения перед undo/redo).</summary>
    protected abstract TState? GetCurrentState();

    /// <summary>Применить снимок состояния.</summary>
    protected abstract Task ApplySnapshotAsync(TState snapshot);
}
