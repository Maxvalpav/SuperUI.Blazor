// SuperUI/Base/Services/SgToastService.cs
// ИСПРАВЛЕНИЯ:
// ✅ CS0535: реализует ISgToastService — все члены совпадают
// ✅ MaxToasts — ограничение количества
// УЛУЧШЕНИЯ:
// ✅ AutoDismiss через Task.Delay с CancellationToken
// ✅ Lock<T> (System.Threading.Lock) — .NET 9

using System.Collections.Concurrent;

namespace SuperUI.Base.Services;

/// <summary>
/// Сервис управления toast-уведомлениями.
/// Scoped: per-circuit (Server), per-app (WASM).
/// </summary>
public sealed class SgToastService : ISgToastService, IDisposable
{
    private readonly List<SgToastMessage> _toasts = [];
    private readonly Lock _lock = new();
    private int _nextId;
    private volatile bool _disposed;
    private readonly CancellationTokenSource _cts = new();

    /// <summary>Максимальное количество одновременных toast.</summary>
    public int MaxToasts { get; set; } = 10;

    /// <summary>Длительность по умолчанию (мс).</summary>
    public int DefaultDurationMs { get; set; } = 4000;

    // ── ISgToastService ──────────────────────────────────────────────────────
    /// <inheritdoc/>
    public IReadOnlyList<SgToastMessage> Toasts
    {
        get { lock (_lock) return [.. _toasts]; }
    }

    /// <inheritdoc/>
    public event Action<SgToastMessage>? Added;

    /// <inheritdoc/>
    public event Action<SgToastMessage>? Removed;

    /// <inheritdoc/>
    public event Action? OnChange;

    // ── Показ ────────────────────────────────────────────────────────────────
    /// <inheritdoc/>
    public SgToastMessage Success(string message, int? durationMs = null)
        => Show(message, SgToastType.Success, durationMs ?? DefaultDurationMs);

    /// <inheritdoc/>
    public SgToastMessage Info(string message, int? durationMs = null)
        => Show(message, SgToastType.Info, durationMs ?? DefaultDurationMs);

    /// <inheritdoc/>
    public SgToastMessage Warning(string message, int? durationMs = null)
        => Show(message, SgToastType.Warning, durationMs ?? DefaultDurationMs);

    /// <inheritdoc/>
    public SgToastMessage Error(string message, int? durationMs = null)
        => Show(message, SgToastType.Error, durationMs ?? DefaultDurationMs);

    /// <inheritdoc/>
    public SgToastMessage Loading(string message)
        => Show(message, SgToastType.Loading, durationMs: null);

    /// <inheritdoc/>
    public SgToastMessage Show(string message,
        SgToastType type = SgToastType.Default,
        int? durationMs = 4000)
    {
        if (_disposed)
            return new SgToastMessage(-1, message, type);

        var toast = new SgToastMessage(
            Id:         Interlocked.Increment(ref _nextId),
            Message:    message,
            Type:       type,
            DurationMs: durationMs,
            CreatedAt:  DateTimeOffset.UtcNow);

        lock (_lock)
        {
            _toasts.Add(toast);
            while (_toasts.Count > MaxToasts)
                _toasts.RemoveAt(0);
        }

        Added?.Invoke(toast);
        OnChange?.Invoke();

        if (durationMs.HasValue && durationMs.Value > 0)
            _ = AutoDismissAsync(toast.Id, durationMs.Value);

        return toast;
    }

    // ── Управление ───────────────────────────────────────────────────────────
    /// <inheritdoc/>
    public void Dismiss(int id)
    {
        SgToastMessage? removed = null;
        lock (_lock)
        {
            var idx = _toasts.FindIndex(t => t.Id == id);
            if (idx >= 0) { removed = _toasts[idx]; _toasts.RemoveAt(idx); }
        }
        if (removed is not null) { Removed?.Invoke(removed); OnChange?.Invoke(); }
    }

    /// <inheritdoc/>
    public void DismissAll()
    {
        List<SgToastMessage> snapshot;
        lock (_lock) { snapshot = [.. _toasts]; _toasts.Clear(); }
        foreach (var t in snapshot) Removed?.Invoke(t);
        OnChange?.Invoke();
    }

    /// <inheritdoc/>
    public void Update(int id, string message,
        SgToastType type = SgToastType.Success, int? durationMs = 3000)
    {
        bool updated = false;
        lock (_lock)
        {
            var idx = _toasts.FindIndex(t => t.Id == id);
            if (idx >= 0)
            {
                _toasts[idx] = _toasts[idx] with
                { Message = message, Type = type, DurationMs = durationMs };
                updated = true;
            }
        }
        if (updated)
        {
            OnChange?.Invoke();
            if (durationMs.HasValue && durationMs.Value > 0)
                _ = AutoDismissAsync(id, durationMs.Value);
        }
    }

    private async Task AutoDismissAsync(int id, int delayMs)
    {
        try
        {
            await Task.Delay(delayMs, _cts.Token);
            if (!_disposed) Dismiss(id);
        }
        catch (OperationCanceledException) { }
    }

    // ── IDisposable / IAsyncDisposable ───────────────────────────────────────
    public void Dispose()
    {
        _disposed = true;
        _cts.Cancel();
        _cts.Dispose();
        lock (_lock) _toasts.Clear();
    }

    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }
}

