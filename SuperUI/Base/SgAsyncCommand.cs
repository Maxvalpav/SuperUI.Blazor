// SuperUI/Base/SgAsyncCommand.cs
// ИСПРАВЛЕНИЯ v2:
// ✅ C3: _cts защищён через Interlocked.Exchange — нет утечки Handle
// ✅ CanExecute + ExecuteAsync атомарность через CAS на _isExecuting
// ✅ _isDisposed через Interlocked

using System;
using System.Threading;
using System.Threading.Tasks;

namespace SuperUI.Base;

/// <summary>
/// Async команда для Blazor компонентов.
/// Поддерживает: async execution, CancellationToken, CanExecute, LastError.
/// Thread-safe.
/// </summary>
public sealed class SgAsyncCommand : IDisposable
{
    private readonly Func<CancellationToken, Task> _execute;
    private readonly Func<bool>? _canExecute;

    // ✅ FIX C3: _cts через Interlocked — нет утечки
    private CancellationTokenSource? _cts;

    // ✅ FIX: используем int для Interlocked CAS
    private int _isExecuting; // 0 = false, 1 = true
    private int _isDisposed;  // 0 = false, 1 = true

    /// <summary>Вызывается при изменении состояния (IsExecuting, LastError).</summary>
    public Func<Task>? OnStateChanged { get; set; }

    /// <summary>Выполняется ли команда в данный момент.</summary>
    public bool IsExecuting => Volatile.Read(ref _isExecuting) == 1;

    /// <summary>Последняя ошибка выполнения.</summary>
    public Exception? LastError { get; private set; }

    /// <summary>Успешно ли выполнилась последняя команда.</summary>
    public bool LastSucceeded { get; private set; }

    public SgAsyncCommand(Func<CancellationToken, Task> execute, Func<bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    /// <summary>Короткий конструктор для команды без отмены.</summary>
    public SgAsyncCommand(Func<Task> execute, Func<bool>? canExecute = null)
        : this(_ => execute(), canExecute) { }

    /// <summary>Может ли команда быть выполнена.</summary>
    public bool CanExecute()
        => Volatile.Read(ref _isExecuting) == 0
        && Volatile.Read(ref _isDisposed) == 0
        && (_canExecute?.Invoke() ?? true);

    /// <summary>Выполняет команду. Повторный вызов во время выполнения — игнорируется.</summary>
    public async Task ExecuteAsync()
    {
        // ✅ FIX: атомарный CAS — только один поток входит
        if (Interlocked.CompareExchange(ref _isExecuting, 1, 0) == 1) return;
        if (Volatile.Read(ref _isDisposed) == 1)
        {
            Volatile.Write(ref _isExecuting, 0);
            return;
        }

        // ✅ FIX: создаём новый CTS и сохраняем, старый (если есть) — dispose
        var newCts = new CancellationTokenSource();
        var oldCts = Interlocked.Exchange(ref _cts, newCts);
        oldCts?.Cancel();
        oldCts?.Dispose();

        LastError = null;
        LastSucceeded = false;

        if (OnStateChanged is not null)
            await OnStateChanged();

        try
        {
            await _execute(newCts.Token);
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
            Volatile.Write(ref _isExecuting, 0);
            // Dispose CTS если это ещё наш
            var currentCts = Interlocked.CompareExchange(ref _cts, null, newCts);
            if (ReferenceEquals(currentCts, newCts))
                newCts.Dispose();

            if (OnStateChanged is not null && Volatile.Read(ref _isDisposed) == 0)
                await OnStateChanged();
        }
    }

    /// <summary>Отменяет текущее выполнение.</summary>
    public void Cancel()
    {
        var cts = Volatile.Read(ref _cts);
        try { cts?.Cancel(); }
        catch (ObjectDisposedException) { }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _isDisposed, 1) == 1) return;
        var cts = Interlocked.Exchange(ref _cts, null);
        try { cts?.Cancel(); } catch { }
        cts?.Dispose();
    }
}