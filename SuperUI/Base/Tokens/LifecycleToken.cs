namespace SuperUI.Base.Tokens;

/// <summary>
/// CancellationTokenSource с lifecycle семантикой для Blazor компонента.
/// Создаётся при OnInitialized, отменяется при Dispose.
/// </summary>
public sealed class LifecycleToken : IDisposable
{
    private readonly CancellationTokenSource _cts = new();

    public CancellationToken Token => _cts.Token;

    public void Cancel()
    {
        if (!_cts.IsCancellationRequested)
        {
            try { _cts.Cancel(); }
            catch (ObjectDisposedException) { /* already disposed */ }
        }
    }

    public void Dispose() => _cts.Dispose();
}
