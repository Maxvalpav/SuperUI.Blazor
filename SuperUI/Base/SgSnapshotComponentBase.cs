namespace SuperUI.Base;

/// <summary>
/// Базовый класс для компонентов с поддержкой снимков состояния (undo/redo).
/// Используется в редакторах (SgDiagramEditor, SgRichTextEditor и т.д.).
/// </summary>
/// <typeparam name="TState">Тип снимка состояния.</typeparam>
public abstract class SgSnapshotComponentBase<TState> : SgInteractiveBase
    where TState : class
{
    private readonly Stack<TState> _undoStack = new();
    private readonly Stack<TState> _redoStack = new();
    private int _maxSnapshots = 50;

    /// <summary>Максимальное количество снимков в истории.</summary>
    [Parameter]
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
    protected void SaveSnapshot(TState snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        _redoStack.Clear(); // новое действие сбрасывает redo

        // Ограничиваем размер стека
        while (_undoStack.Count >= _maxSnapshots)
        {
            // Stack не поддерживает RemoveLast → конвертируем временно
            var temp = _undoStack.ToArray();
            _undoStack.Clear();
            for (int i = temp.Length - 2; i >= 0; i--) // пропускаем последний (самый старый)
                _undoStack.Push(temp[i]);
        }

        _undoStack.Push(snapshot);
    }

    /// <summary>Отменить последнее действие.</summary>
    public async Task UndoAsync()
    {
        if (!CanUndo || IsDisposed) return;
        var current = GetCurrentState();
        if (current is not null) _redoStack.Push(current);
        var snapshot = _undoStack.Pop();
        await ApplySnapshotAsync(snapshot);
        StateHasChanged();
    }

    /// <summary>Повторить отменённое действие.</summary>
    public async Task RedoAsync()
    {
        if (!CanRedo || IsDisposed) return;
        var current = GetCurrentState();
        if (current is not null) _undoStack.Push(current);
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

    /// <summary>Получить текущее состояние компонента (для сохранения перед undo/redo).</summary>
    protected abstract TState? GetCurrentState();

    /// <summary>Применить снимок состояния.</summary>
    protected abstract Task ApplySnapshotAsync(TState snapshot);
}
