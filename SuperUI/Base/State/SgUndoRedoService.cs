// SuperUI/Base/State/SgUndoRedoService.cs
// Встроенный Undo/Redo для любого компонента
// Работает на WASM и Server (per-circuit для Server)
namespace SuperUI.Base.State;

public interface ISgUndoRedoService
{
    bool CanUndo { get; }
    bool CanRedo { get; }
    int UndoCount { get; }
    int RedoCount { get; }

    void Execute(ISgCommand command);
    Task ExecuteAsync(ISgAsyncCommand command);
    void Undo();
    void Redo();
    void Clear();

    event Action? StateChanged;
}

public interface ISgCommand
{
    string Description { get; }
    void Execute();
    void Undo();
}

public interface ISgAsyncCommand
{
    string Description { get; }
    Task ExecuteAsync();
    Task UndoAsync();
}

/// <summary>
/// Простая синхронная команда на основе делегатов.
/// </summary>
public sealed class SgDelegateCommand : ISgCommand
{
    private readonly Action _execute;
    private readonly Action _undo;

    public string Description { get; }

    public SgDelegateCommand(string description, Action execute, Action undo)
    {
        Description = description;
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _undo = undo ?? throw new ArgumentNullException(nameof(undo));
    }

    public void Execute() => _execute();
    public void Undo() => _undo();
}

/// <summary>
/// Команда с захватом состояния ДО и ПОСЛЕ.
/// Удобна для form fields где нужно хранить old/new value.
/// </summary>
public sealed class SgValueChangeCommand<T> : ISgCommand
{
    private readonly T _oldValue;
    private readonly T _newValue;
    private readonly Action<T> _setter;

    public string Description { get; }

    public SgValueChangeCommand(string description, T oldValue, T newValue, Action<T> setter)
    {
        Description = description;
        _oldValue = oldValue;
        _newValue = newValue;
        _setter = setter ?? throw new ArgumentNullException(nameof(setter));
    }

    public void Execute() => _setter(_newValue);
    public void Undo() => _setter(_oldValue);
}

/// <summary>
/// Thread-safe Undo/Redo стек.
/// Scoped сервис — один экземпляр на Blazor circuit (Server) или страницу (WASM).
/// </summary>
public sealed class SgUndoRedoService : ISgUndoRedoService
{
    private readonly LinkedList<ISgCommand> _undoStack = new();
    private readonly LinkedList<ISgCommand> _redoStack = new();
    private readonly Lock _lock = new();
    private readonly int _maxHistory;

    public SgUndoRedoService(int maxHistory = 100)
    {
        _maxHistory = maxHistory;
    }

    public bool CanUndo { get { lock (_lock) return _undoStack.Count > 0; } }
    public bool CanRedo { get { lock (_lock) return _redoStack.Count > 0; } }
    public int UndoCount { get { lock (_lock) return _undoStack.Count; } }
    public int RedoCount { get { lock (_lock) return _redoStack.Count; } }

    public event Action? StateChanged;

    public void Execute(ISgCommand command)
    {
        command.Execute();
        lock (_lock)
        {
            _undoStack.AddLast(command);
            _redoStack.Clear(); // новое действие инвалидирует redo stack

            // Ограничиваем размер истории
            while (_undoStack.Count > _maxHistory)
                _undoStack.RemoveFirst();
        }
        StateChanged?.Invoke();
    }

    public async Task ExecuteAsync(ISgAsyncCommand command)
    {
        await command.ExecuteAsync();
        // Оборачиваем в ISgCommand для синхронного стека
        var wrapper = new AsyncCommandWrapper(command);
        lock (_lock)
        {
            _undoStack.AddLast(wrapper);
            _redoStack.Clear();
            while (_undoStack.Count > _maxHistory)
                _undoStack.RemoveFirst();
        }
        StateChanged?.Invoke();
    }

    public void Undo()
    {
        ISgCommand? cmd;
        lock (_lock)
        {
            if (_undoStack.Count == 0) return;
            cmd = _undoStack.Last!.Value;
            _undoStack.RemoveLast();
            _redoStack.AddLast(cmd);
        }
        cmd.Undo();
        StateChanged?.Invoke();
    }

    public void Redo()
    {
        ISgCommand? cmd;
        lock (_lock)
        {
            if (_redoStack.Count == 0) return;
            cmd = _redoStack.Last!.Value;
            _redoStack.RemoveLast();
            _undoStack.AddLast(cmd);
        }
        cmd.Execute();
        StateChanged?.Invoke();
    }

    public void Clear()
    {
        lock (_lock) { _undoStack.Clear(); _redoStack.Clear(); }
        StateChanged?.Invoke();
    }

    private sealed class AsyncCommandWrapper : ISgCommand
    {
        private readonly ISgAsyncCommand _inner;
        public string Description => _inner.Description;
        public AsyncCommandWrapper(ISgAsyncCommand inner) { _inner = inner; }
        // Синхронный вызов — Undo/Redo UI всегда через InvokeAsync
        public void Execute() => _inner.ExecuteAsync().GetAwaiter().GetResult();
        public void Undo() => _inner.UndoAsync().GetAwaiter().GetResult();
    }
}