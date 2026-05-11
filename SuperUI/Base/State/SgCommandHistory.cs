// SuperUI/Base/State/SgCommandHistory.cs

using System.Collections;

namespace SuperUI.Base.State;

/// <summary>
/// Command pattern с Undo/Redo поддержкой.
/// Для DataGrid inline edit, форм, диаграм.
/// </summary>
public sealed class SgCommandHistory
{
    private readonly Stack<ISgCommand> _undoStack = new();
    private readonly Stack<ISgCommand> _redoStack = new();
    private readonly int _maxHistory;

    public SgCommandHistory(int maxHistory = 100) => _maxHistory = maxHistory;

    public bool CanUndo => _undoStack.Count > 0;
    public bool CanRedo => _redoStack.Count > 0;
    public int UndoCount => _undoStack.Count;

    public async Task ExecuteAsync(ISgCommand command)
    {
        await command.ExecuteAsync();
        _undoStack.Push(command);
        _redoStack.Clear();

        // Trim history
        while (_undoStack.Count > _maxHistory)
        {
            var arr = _undoStack.ToArray();
            _undoStack.Clear();
            foreach (var c in arr.Take(_maxHistory).Reverse()) _undoStack.Push(c);
        }
    }

    public async Task UndoAsync()
    {
        if (!CanUndo) return;
        var command = _undoStack.Pop();
        await command.UndoAsync();
        _redoStack.Push(command);
    }

    public async Task RedoAsync()
    {
        if (!CanRedo) return;
        var command = _redoStack.Pop();
        await command.ExecuteAsync();
        _undoStack.Push(command);
    }
}

public interface ISgCommand
{
    string Description { get; }
    Task ExecuteAsync();
    Task UndoAsync();
}

/// <summary>
/// Базовый класс для команд изменения значения.
/// </summary>
public sealed class SgSetValueCommand<T> : ISgCommand
{
    private readonly Action<T?> _setter;
    private readonly T? _oldValue;
    private readonly T? _newValue;

    public string Description { get; }

    public SgSetValueCommand(string description, Action<T?> setter, T? oldValue, T? newValue)
    {
        Description = description;
        _setter = setter;
        _oldValue = oldValue;
        _newValue = newValue;
    }

    public Task ExecuteAsync() { _setter(_newValue); return Task.CompletedTask; }
    public Task UndoAsync()    { _setter(_oldValue); return Task.CompletedTask; }
}
