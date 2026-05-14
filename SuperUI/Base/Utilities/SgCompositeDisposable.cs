// SuperUI/Base/Utilities/SgCompositeDisposable.cs
// ИСПРАВЛЕНИЯ v2:
// ✅ Dispose НЕ бросает AggregateException (нарушение IDisposable контракта)
// ✅ Ошибки логируются через Debug.WriteLine
// ✅ DisposeAsync: ошибки агрегируются и бросаются (допустимо в async context)

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SuperUI.Base.Utilities;

public sealed class SgCompositeDisposable : IAsyncDisposable, IDisposable
{
    private readonly List<object> _disposables = new();
    private readonly object _lock = new();
    private bool _disposed;

    public void Add(IDisposable disposable)
    {
        lock (_lock)
        {
            if (_disposed) { disposable.Dispose(); return; }
            _disposables.Add(disposable);
        }
    }

    public void Add(IAsyncDisposable disposable)
    {
        lock (_lock)
        {
            if (_disposed)
            {
                disposable.DisposeAsync().AsTask().GetAwaiter().GetResult();
                return;
            }
            _disposables.Add(disposable);
        }
    }

    public void Add(Action disposeAction)
        => Add(new ActionDisposable(disposeAction));

    public bool Remove(IDisposable disposable)
    {
        lock (_lock) return _disposables.Remove(disposable);
    }

    /// <summary>
    /// ✅ FIX: Dispose НЕ бросает исключения (IDisposable контракт).
    /// Ошибки логируются.
    /// </summary>
    public void Dispose()
    {
        List<object> toDispose;
        lock (_lock)
        {
            if (_disposed) return;
            _disposed = true;
            toDispose = new List<object>(_disposables);
            _disposables.Clear();
        }

        foreach (var d in toDispose)
        {
            try
            {
                if (d is IDisposable sync) sync.Dispose();
                else if (d is IAsyncDisposable async)
                    async.DisposeAsync().AsTask().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                // ✅ FIX: НЕ бросаем из Dispose — логируем
                System.Diagnostics.Debug.WriteLine(
                    $"[SgCompositeDisposable] Dispose error: {ex}");
            }
        }
    }

    /// <summary>
    /// ✅ DisposeAsync может бросать AggregateException (допустимо в async context).
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        List<object> toDispose;
        lock (_lock)
        {
            if (_disposed) return;
            _disposed = true;
            toDispose = new List<object>(_disposables);
            _disposables.Clear();
        }

        List<Exception>? errors = null;
        foreach (var d in toDispose)
        {
            try
            {
                if (d is IAsyncDisposable async) await async.DisposeAsync();
                else if (d is IDisposable sync) sync.Dispose();
            }
            catch (Exception ex)
            {
                errors ??= new List<Exception>();
                errors.Add(ex);
            }
        }

        if (errors is not null)
            throw new AggregateException(errors);
    }

    private sealed class ActionDisposable : IDisposable
    {
        private readonly Action _action;
        private int _disposed;
        public ActionDisposable(Action action) => _action = action;
        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
                _action();
        }
    }
}