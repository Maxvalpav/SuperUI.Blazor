// SuperUI/Base/State/UndoRedoStack.cs
// УНИКАЛЬНАЯ ФИЧА: Undo/Redo стек как переиспользуемый компонент
// Ни одна Blazor библиотека не предоставляет такого

namespace SuperUI.Base.State;

/// <summary>
/// Команда для Undo/Redo стека.
/// </summary>
public interface IUndoableCommand
{
    void Execute();
    void Undo();
    string Description { get; }
}

/// <summary>
/// Универсальный стек Undo/Redo с ограничением глубины.
/// Поддерживает:
/// - Undo / Redo
/// - Группировку команд (Transaction)
/// - Auto-merge последовательных одинаковых команд
/// - События изменения состояния
/// </summary>
public class UndoRedoStack<T> where T : IUndoableCommand
{
    private readonly int _maxDepth;
    private readonly LinkedList<T> _undoStack = new();
    private readonly Stack<T> _redoStack = new();
    private Transaction? _currentTransaction;

    public event Action? StateChanged;

    public UndoRedoStack(int maxDepth = 100)
    {
        _maxDepth = maxDepth;
    }

    public bool CanUndo => _undoStack.Count > 0;
    public bool CanRedo => _redoStack.Count > 0;

    /// <summary>
    /// Выполнить и записать команду в стек Undo.
    /// </summary>
    public void Execute(T command)
    {
        // Если транзакция активна — добавляем в неё
        if (_currentTransaction is not null)
        {
            _currentTransaction.Commands.Add(command);
            command.Execute();
            return;
        }

        command.Execute();

        // Auto-merge: если последняя команда того же типа — заменяем
        if (_undoStack.Count > 0)
        {
            var last = _undoStack.Last!.Value;
            if (last.GetType() == command.GetType() && CanMerge(last, command))
            {
                _undoStack.Last!.Value = Merge(last, command);
                _redoStack.Clear();
                StateChanged?.Invoke();
                return;
            }
        }

        _undoStack.AddLast(command);

        // Ограничение глубины
        while (_undoStack.Count > _maxDepth)
            _undoStack.RemoveFirst();

        _redoStack.Clear();
        StateChanged?.Invoke();
    }

    /// <summary>
    /// Начать транзакцию — группа команд будет отменена/повторена как одна.
    /// </summary>
    public IDisposable BeginTransaction(string description = "Transaction")
    {
        _currentTransaction = new Transaction(description);
        return new TransactionScope(this);
    }

    public void Undo()
    {
        if (!CanUndo) return;

        var command = _undoStack.Last!.Value;
        _undoStack.RemoveLast();

        command.Undo();
        _redoStack.Push(command);

        StateChanged?.Invoke();
    }

    public void Redo()
    {
        if (!CanRedo) return;

        var command = _redoStack.Pop();
        command.Execute();
        _undoStack.AddLast(command);

        StateChanged?.Invoke();
    }

    public void Clear()
    {
        _undoStack.Clear();
        _redoStack.Clear();
        _currentTransaction = null;
        StateChanged?.Invoke();
    }

    public string? GetUndoDescription() =>
        _undoStack.Last?.Value.Description;

    public string? GetRedoDescription() =>
        _redoStack.Count > 0 ? _redoStack.Peek().Description : null;

    protected virtual bool CanMerge(T older, T newer) => false;
    protected virtual T Merge(T older, T newer) => newer;

    private void CommitTransaction()
    {
        if (_currentTransaction is not { Commands.Count: > 0 }) return;

        var transactionCommand = new TransactionCommand(_currentTransaction);
        _undoStack.AddLast((T)(object)transactionCommand);
        _redoStack.Clear();
        _currentTransaction = null;
        StateChanged?.Invoke();
    }

    private void RollbackTransaction()
    {
        if (_currentTransaction is not { Commands.Count: > 0 })
        {
            _currentTransaction = null;
            return;
        }

        // Отменяем команды в обратном порядке
        for (int i = _currentTransaction.Commands.Count - 1; i >= 0; i--)
        {
            try { _currentTransaction.Commands[i].Undo(); }
            catch { /* best effort */ }
        }

        _currentTransaction = null;
    }

    private class TransactionScope : IDisposable
    {
        private readonly UndoRedoStack<T> _stack;

        public TransactionScope(UndoRedoStack<T> stack) => _stack = stack;

        public void Dispose()
        {
            try { _stack.CommitTransaction(); }
            catch { _stack.RollbackTransaction(); }
        }
    }

    private sealed class Transaction
    {
        public string Description { get; }
        public List<T> Commands { get; } = [];

        public Transaction(string description) => Description = description;
    }

    private sealed class TransactionCommand : IUndoableCommand
    {
        private readonly Transaction _transaction;

        public TransactionCommand(Transaction transaction) =>
            _transaction = transaction;

        public string Description => _transaction.Description;

        public void Execute()
        {
            foreach (var cmd in _transaction.Commands)
                cmd.Execute();
        }

        public void Undo()
        {
            for (int i = _transaction.Commands.Count - 1; i >= 0; i--)
                _transaction.Commands[i].Undo();
        }
    }
}