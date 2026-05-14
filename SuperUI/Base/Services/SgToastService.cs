// SuperUI/Base/Services/SgToastService.cs
// ИСПРАВЛЕНИЯ v2:
// ✅ W5: DismissAll — нет deadlock: CancelDismissToken вызывается ВНЕ _lock
// ✅ Show — вытеснение старых toast происходит вне внутреннего lock на _dismissTokens
// ✅ .NET 8/9/10: #if NET9_0_OR_GREATER для System.Threading.Lock
// ✅ Dispose идемпотентен через Interlocked

using System.Collections.Generic;
using System.Threading;

namespace SuperUI.Base.Services;

public sealed class SgToastService : ISgToastService, IAsyncDisposable, IDisposable
{
    private readonly List<SgToastMessage> _toasts = [];

#if NET9_0_OR_GREATER
    private readonly System.Threading.Lock _toastLock = new();
    private readonly System.Threading.Lock _dismissLock = new();
#else
    private readonly object _toastLock = new();
    private readonly object _dismissLock = new();
#endif

    private int _nextId;
    private int _disposed;
    private readonly CancellationTokenSource _cts = new();
    private readonly Dictionary<int, CancellationTokenSource> _dismissTokens = [];

    public int MaxToasts { get; set; } = 10;
    public int DefaultDurationMs { get; set; } = 4000;

    public IReadOnlyList<SgToastMessage> Toasts
    {
        get { lock (_toastLock) return [.._toasts]; }
    }

    public event Action<SgToastMessage>? Added;
    public event Action<SgToastMessage>? Removed;
    public event Action? OnChange;

    public SgToastMessage Success(string message, int? durationMs = null)
        => Show(message, SgToastType.Success, durationMs ?? DefaultDurationMs);
    public SgToastMessage Info(string message, int? durationMs = null)
        => Show(message, SgToastType.Info, durationMs ?? DefaultDurationMs);
    public SgToastMessage Warning(string message, int? durationMs = null)
        => Show(message, SgToastType.Warning, durationMs ?? DefaultDurationMs);
    public SgToastMessage Error(string message, int? durationMs = null)
        => Show(message, SgToastType.Error, durationMs ?? DefaultDurationMs);
    public SgToastMessage Loading(string message)
        => Show(message, SgToastType.Loading, durationMs: null);

    public SgToastMessage Show(
        string message,
        SgToastType type = SgToastType.Default,
        int? durationMs = 4000)
    {
        if (Volatile.Read(ref _disposed) == 1)
            return new SgToastMessage(-1, message, type);

        var toast = new SgToastMessage(
            Id: Interlocked.Increment(ref _nextId),
            Message: message,
            Type: type,
            DurationMs: durationMs,
            CreatedAt: DateTimeOffset.UtcNow);

        // ✅ FIX W5: вытесненные toast-ы собираем внутри lock,
        // отменяем их dismiss ПОСЛЕ снятия lock
        List<SgToastMessage>? evicted = null;

        lock (_toastLock)
        {
            _toasts.Add(toast);
            while (_toasts.Count > MaxToasts)
            {
                var oldest = _toasts[0];
                _toasts.RemoveAt(0);
                (evicted ??= []).Add(oldest);
            }
        }

        // ✅ FIX: отмена и события — ВНЕ _toastLock
        if (evicted is not null)
        {
            foreach (var evictedToast in evicted)
            {
                CancelAndRemoveDismissToken(evictedToast.Id);
                Removed?.Invoke(evictedToast);
            }
        }

        Added?.Invoke(toast);
        OnChange?.Invoke();

        if (durationMs.HasValue && durationMs.Value > 0)
            _ = AutoDismissAsync(toast.Id, durationMs.Value);

        return toast;
    }

    public void Dismiss(int id)
    {
        CancelAndRemoveDismissToken(id);

        SgToastMessage? removed = null;
        lock (_toastLock)
        {
            var idx = _toasts.FindIndex(t => t.Id == id);
            if (idx >= 0)
            {
                removed = _toasts[idx];
                _toasts.RemoveAt(idx);
            }
        }

        if (removed is not null)
        {
            Removed?.Invoke(removed);
            OnChange?.Invoke();
        }
    }

    public void DismissAll()
    {
        // ✅ FIX W5: собираем все dismiss tokens внутри _dismissLock,
        // список toast — внутри _toastLock,
        // cancel/dispose — ВНЕ обоих lock-ов
        CancellationTokenSource[] tokensToCancel;
        lock (_dismissLock)
        {
            tokensToCancel = _dismissTokens.Values.ToArray();
            _dismissTokens.Clear();
        }

        List<SgToastMessage> snapshot;
        lock (_toastLock)
        {
            snapshot = [.._toasts];
            _toasts.Clear();
        }

        // Отменяем токены ВНЕ lock
        foreach (var cts in tokensToCancel)
        {
            try { cts.Cancel(); cts.Dispose(); } catch { }
        }

        foreach (var t in snapshot)
            Removed?.Invoke(t);

        OnChange?.Invoke();
    }

    public void Update(
        int id,
        string message,
        SgToastType type = SgToastType.Success,
        int? durationMs = 3000)
    {
        bool updated = false;
        lock (_toastLock)
        {
            var idx = _toasts.FindIndex(t => t.Id == id);
            if (idx >= 0)
            {
                _toasts[idx] = _toasts[idx] with { Message = message, Type = type, DurationMs = durationMs };
                updated = true;
            }
        }

        if (updated)
        {
            OnChange?.Invoke();
            CancelAndRemoveDismissToken(id);
            if (durationMs.HasValue && durationMs.Value > 0)
                _ = AutoDismissAsync(id, durationMs.Value);
        }
    }

    // ✅ FIX: разделены _toastLock и _dismissLock — нет вложенных lock
    private void CancelAndRemoveDismissToken(int id)
    {
        CancellationTokenSource? existing;
        lock (_dismissLock)
        {
            _dismissTokens.TryGetValue(id, out existing);
            if (existing is not null)
                _dismissTokens.Remove(id);
        }
        if (existing is not null)
        {
            try { existing.Cancel(); existing.Dispose(); } catch { }
        }
    }

    private async Task AutoDismissAsync(int id, int delayMs)
    {
        var dismissCts = new CancellationTokenSource();
        lock (_dismissLock) _dismissTokens[id] = dismissCts;

        using var linked = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, dismissCts.Token);
        try
        {
            await Task.Delay(delayMs, linked.Token);
            if (Volatile.Read(ref _disposed) == 0)
                Dismiss(id);
        }
        catch (OperationCanceledException)
        {
            // Нормально: dismissed или disposed
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1) return;
        _cts.Cancel();
        _cts.Dispose();

        CancellationTokenSource[] tokens;
        lock (_dismissLock)
        {
            tokens = _dismissTokens.Values.ToArray();
            _dismissTokens.Clear();
        }
        foreach (var t in tokens)
        {
            try { t.Cancel(); t.Dispose(); } catch { }
        }

        lock (_toastLock) _toasts.Clear();
    }

    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }
}