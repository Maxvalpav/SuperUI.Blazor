// SuperUI/Base/SgAsyncCommand.cs
// НОВЫЙ КЛАСС:
// ✅ ICommand паттерн адаптированный для Blazor
// ✅ Async исполнение с CancellationToken
// ✅ IsExecuting, CanExecute, LastError
// ✅ Интеграция с SgAsyncButton
// ✅ Автоматический StateHasChanged через Action

using System;
using System.Threading;
using System.Threading.Tasks;

namespace SuperUI.Base;

/// <summary>
/// Async команда для Blazor компонентов.
/// Поддерживает: async execution, CancellationToken, CanExecute, LastError.
/// </summary>
/// <example>
/// <code>
/// private readonly SgAsyncCommand _saveCommand;
///
/// public MyComponent()
/// {
///     _saveCommand = new SgAsyncCommand(SaveAsync, () => !string.IsNullOrEmpty(_name))
///     {
///         OnStateChanged = () => InvokeAsync(StateHasChanged)
///     };
/// }
///
/// private async Task SaveAsync(CancellationToken ct)
/// {
///     await _service.SaveAsync(_name, ct);
/// }
/// </code>
/// </example>
public sealed class SgAsyncCommand : IDisposable
{
    private readonly Func<CancellationToken, Task> _execute;
    private readonly Func<bool>? _canExecute;
    private CancellationTokenSource? _cts;
    private volatile bool _isExecuting;
    private volatile bool _isDisposed;

    /// <summary>Вызывается при изменении состояния (IsExecuting, LastError).</summary>
    public Func<Task>? OnStateChanged { get; set; }

    /// <summary>Выполняется ли команда в данный момент.</summary>
    public bool IsExecuting => _isExecuting;

    /// <summary>Последняя ошибка выполнения.</summary>
    public Exception? LastError { get; private set; }

    /// <summary>Успешно ли выполнилась последняя команда.</summary>
    public bool LastSucceeded { get; private set; }

    public SgAsyncCommand(Func<CancellationToken, Task> execute,
        Func<bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    /// <summary>Короткий конструктор для команды без отмены.</summary>
    public SgAsyncCommand(Func<Task> execute, Func<bool>? canExecute = null)
        : this(_ => execute(), canExecute) { }

    /// <summary>Может ли команда быть выполнена.</summary>
    public bool CanExecute() => !_isExecuting && !_isDisposed && (_canExecute?.Invoke() ?? true);

    /// <summary>Выполняет команду. Повторный вызов во время выполнения — игнорируется.</summary>
    public async Task ExecuteAsync()
    {
        if (!CanExecute()) return;

        _cts = new CancellationTokenSource();
        _isExecuting = true;
        LastError = null;
        LastSucceeded = false;

        if (OnStateChanged is not null)
            await OnStateChanged();

        try
        {
            await _execute(_cts.Token);
            LastSucceeded = true;
        }
        catch (OperationCanceledException)
        {
            LastSucceeded = false;
        }
        catch (Exception ex)
        {
            LastError = ex;
            LastSucceeded = false;
        }
        finally
        {
            _isExecuting = false;
            _cts?.Dispose();
            _cts = null;

            if (OnStateChanged is not null)
                await OnStateChanged();
        }
    }

    /// <summary>Отменяет текущее выполнение.</summary>
    public void Cancel() => _cts?.Cancel();

    public void Dispose()
    {
        _isDisposed = true;
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }
}
