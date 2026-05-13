// SuperUI/Base/Reactive/SgSignalHistory.cs
// НОВЫЙ КЛАСС
// Аналог: Immer (React), MobX state tree patches
// Поддержка: .NET 8/9/10

namespace SuperUI.Base.Reactive;

/// <summary>
/// Обёртка над SgSignal с поддержкой истории изменений.
/// Позволяет делать Undo/Redo операции.
///
/// Использование:
/// <code>
/// var textHistory = new SgSignalHistory&lt;string&gt;("", maxHistory: 50);
/// textHistory.Signal.Set("Hello");
/// textHistory.Signal.Set("Hello World");
/// textHistory.Undo();  // Signal.Value = "Hello"
/// textHistory.Redo();  // Signal.Value = "Hello World"
/// bool canUndo = textHistory.CanUndo;  // true/false
/// </code>
/// </summary>
public sealed class SgSignalHistory<T> : IDisposable
{
    private readonly SgSignal<T> _signal;
    private readonly int _maxHistory;
    private readonly LinkedList<T> _undoStack = new();
    private readonly LinkedList<T> _redoStack = new();
    private readonly object _lock = new();
    private bool _isUndoRedoing;
    private int _disposed;

    /// <summary>Сигнал, историю которого отслеживаем.</summary>
    public SgSignal<T> Signal => _signal;

    /// <summary>Можно ли отменить последнее действие.</summary>
    public bool CanUndo { get { lock (_lock) return _undoStack.Count > 0; } }

    /// <summary>Можно ли повторить отменённое действие.</summary>
    public bool CanRedo { get { lock (_lock) return _redoStack.Count > 0; } }

    /// <summary>Количество шагов в истории undo.</summary>
    public int UndoCount { get { lock (_lock) return _undoStack.Count; } }

    /// <summary>Количество шагов в истории redo.</summary>
    public int RedoCount { get { lock (_lock) return _redoStack.Count; } }

    /// <summary>Событие: история изменилась (для обновления UI).</summary>
    public event Action? HistoryChanged;

    public SgSignalHistory(
        T initialValue,
        int maxHistory = 100,
        string? debugName = null,
        IEqualityComparer<T>? comparer = null)
    {
        _maxHistory = maxHistory;
        _signal = new SgSignal<T>(initialValue, comparer, debugName ?? $"History<{typeof(T).Name}>");

        // Подписываемся на изменения сигнала для записи в историю
        _signal.Subscribe(new SignalHistoryObserver(this));
    }

    /// <summary>Записать текущее значение в стек (вызывается автоматически при Set).</summary>
    internal void RecordChange(T previousValue)
    {
        if (_isUndoRedoing) return;

        lock (_lock)
        {
            _undoStack.AddLast(previousValue);

            // Ограничиваем размер истории
            while (_undoStack.Count > _maxHistory)
                _undoStack.RemoveFirst();

            // При новом изменении — очищаем redo stack
            _redoStack.Clear();
        }

        HistoryChanged?.Invoke();
    }

    /// <summary>Отменить последнее изменение.</summary>
    public bool Undo()
    {
        T? previousValue;
        T currentValue;

        lock (_lock)
        {
            if (_undoStack.Count == 0) return false;

            currentValue = _signal.Value;
            previousValue = _undoStack.Last!.Value;
            _undoStack.RemoveLast();
            _redoStack.AddFirst(currentValue);
        }

        _isUndoRedoing = true;
        try
        {
            _signal.Set(previousValue!);
        }
        finally
        {
            _isUndoRedoing = false;
        }

        HistoryChanged?.Invoke();
        return true;
    }

    /// <summary>Повторить отменённое изменение.</summary>
    public bool Redo()
    {
        T? nextValue;
        T currentValue;

        lock (_lock)
        {
            if (_redoStack.Count == 0) return false;

            currentValue = _signal.Value;
            nextValue = _redoStack.First!.Value;
            _redoStack.RemoveFirst();
            _undoStack.AddLast(currentValue);
        }

        _isUndoRedoing = true;
        try
        {
            _signal.Set(nextValue!);
        }
        finally
        {
            _isUndoRedoing = false;
        }

        HistoryChanged?.Invoke();
        return true;
    }

    /// <summary>Очистить всю историю.</summary>
    public void ClearHistory()
    {
        lock (_lock)
        {
            _undoStack.Clear();
            _redoStack.Clear();
        }

        HistoryChanged?.Invoke();
    }

    /// <summary>Получить историю как IReadOnlyList (от старых к новым).</summary>
    public IReadOnlyList<T> GetUndoHistory()
    {
        lock (_lock) return [.._undoStack];
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1) return;
        _signal.Dispose();
    }

    private sealed class SignalHistoryObserver : ISignalObserver
    {
        private readonly SgSignalHistory<T> _history;
        private T _previousValue;

        public SignalHistoryObserver(SgSignalHistory<T> history)
        {
            _history = history;
            _previousValue = history._signal.Value;
        }

        public void OnSignalChanged(ISgSignal signal)
        {
            var prev = _previousValue;
            if (signal is IReadOnlySignal<T> typed)
                _previousValue = typed.Value;

            _history.RecordChange(prev);
        }
    }
}
