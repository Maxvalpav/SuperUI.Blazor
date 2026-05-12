// SuperUI/Base/Reactive/SignalBatch.cs
using SuperUI.Base;

namespace SuperUI.Base.Reactive;

/// <summary>
/// Batches component refresh notifications within a scope to prevent
/// multiple sequential renders. When multiple signals update during a
/// batch, only a single RefreshAsync is invoked per component at the end.
/// </summary>
public static class SignalBatch
{
    [ThreadStatic]
    private static int _depth;

    [ThreadStatic]
    private static HashSet<SgComponentBase>? _pending;

    /// <summary>
    /// Starts a batching scope. All signal notifications within the returned
    /// IDisposable's lifetime will be collected and flushed upon disposal.
    /// </summary>
    public static IDisposable Begin()
    {
        _depth++;
        return new BatchScope();
    }

    /// <summary>
    /// Notifies a component of a change. If inside a batch scope, the
    /// component is added to the pending set; otherwise, RefreshAsync is called immediately.
    /// </summary>
    internal static void NotifyComponent(SgComponentBase component)
    {
        if (_depth > 0)
        {
            (_pending ??= new()).Add(component);
            return;
        }
        _ = component.RefreshAsync();
    }

    private sealed class BatchScope : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            if (--_depth > 0) return; // вложенный scope — не флашим

            if (_pending is not { Count: > 0 }) return;

            var snapshot = new List<SgComponentBase>(_pending);
            _pending.Clear();

            // ИСПРАВЛЕНО: исключение в одном компоненте не блокирует остальных
            foreach (var c in snapshot)
            {
                if (c.IsDisposed) continue;
                try { _ = c.RefreshAsync(); }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[SignalBatch] RefreshAsync error: {ex}");
                }
            }
        }
    }
}
