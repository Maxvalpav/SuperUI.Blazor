// SuperUI/Base/Services/SgToastService.cs
// ИСПРАВЛЕНИЯ:
// ✅ NET8 COMPAT: Lock → object _lock (System.Threading.Lock только .NET 9+)
// ✅ NET8/9/10: #if NET9_0_OR_GREATER для Lock
// ✅ ASYNC DISMISS: правильная отмена через LinkedTokenSource
// ✅ IDEMPOTENT DISPOSE: Interlocked

using System.Collections.Generic;
using System.Threading;

namespace SuperUI.Base.Services;

/// <summary>
/// Сервис управления toast-уведомлениями.
/// Scoped: per-circuit (Server), per-instance (WASM).
///
/// ИСПРАВЛЕНИЕ:
/// System.Threading.Lock добавлен в .NET 9. Для совместимости с .NET 8
/// используем object + lock() statement через #if.
/// </summary>
public sealed class SgToastService : ISgToastService, IAsyncDisposable, IDisposable
{
    private readonly List<SgToastMessage> _toasts = [];

#if NET9_0_OR_GREATER
    private readonly System.Threading.Lock _lock = new();
#else
    private readonly object _lock = new();
#endif

    private int _nextId;
    private int _disposed;
    private readonly CancellationTokenSource _cts = new();

    // Словарь токенов для отмены auto-dismiss конкретного toast
    private readonly Dictionary<int, CancellationTokenSource> _dismissTokens = [];

    /// <summary>Максимальное количество одновременных toast.</summary>
    public int MaxToasts { get; set; } = 10;

    /// <summary>Длительность по умолчанию (мс).</summary>
    public int DefaultDurationMs { get; set; } = 4000;

    // ── ISgToastService ──────────────────────────────────────────────────────

    public IReadOnlyList<SgToastMessage> Toasts
    {
        get { lock (_lock) return [.._toasts]; }
    }

    public event Action<SgToastMessage>? Added;
    public event Action<SgToastMessage>? Removed;
    public event Action? OnChange;

    // ── Показ ────────────────────────────────────────────────────────────────

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

        lock (_lock)
        {
            _toasts.Add(toast);

            // Вытесняем старые если превышен лимит
            while (_toasts.Count > MaxToasts)
            {
                var oldest = _toasts[0];
                _toasts.RemoveAt(0);

                // Отменяем auto-dismiss для вытесненного toast
                CancelDismissToken(oldest.Id);
                Removed?.Invoke(oldest);
            }
        }

        Added?.Invoke(toast);
        OnChange?.Invoke();

        if (durationMs.HasValue && durationMs.Value > 0)
            _ = AutoDismissAsync(toast.Id, durationMs.Value);

        return toast;
    }

    // ── Управление ──────────────────────────────────────────────────────────

    public void Dismiss(int id)
    {
        CancelDismissToken(id);

        SgToastMessage? removed = null;
        lock (_lock)
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
        List<SgToastMessage> snapshot;
        lock (_lock)
        {
            snapshot = [.._toasts];
            _toasts.Clear();

            // Отменяем все pending auto-dismiss
            foreach (var cts in _dismissTokens.Values)
            {
                try { cts.Cancel(); cts.Dispose(); } catch { }
            }
            _dismissTokens.Clear();
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
        lock (_lock)
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

            // Сбрасываем старый таймер и запускаем новый
            CancelDismissToken(id);
            if (durationMs.HasValue && durationMs.Value > 0)
                _ = AutoDismissAsync(id, durationMs.Value);
        }
    }

    private void CancelDismissToken(int id)
    {
        CancellationTokenSource? existingCts;
        lock (_lock)
        {
            if (_dismissTokens.TryGetValue(id, out existingCts))
                _dismissTokens.Remove(id);
        }

        if (existingCts is not null)
        {
            try { existingCts.Cancel(); existingCts.Dispose(); } catch { }
        }
    }

    private async Task AutoDismissAsync(int id, int delayMs)
    {
        // Создаём linked token: сервис _cts + индивидуальный dismiss token
        var dismissCts = new CancellationTokenSource();
        lock (_lock)
            _dismissTokens[id] = dismissCts;

        using var linked = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, dismissCts.Token);

        try
        {
            await Task.Delay(delayMs, linked.Token);
            if (Volatile.Read(ref _disposed) == 0)
                Dismiss(id);
        }
        catch (OperationCanceledException)
        {
            // Toast уже dismissed или сервис disposed — нормально
        }
    }

    // ── IDisposable / IAsyncDisposable ───────────────────────────────────────

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1) return;

        _cts.Cancel();
        _cts.Dispose();

        lock (_lock)
        {
            foreach (var cts in _dismissTokens.Values)
            {
                try { cts.Cancel(); cts.Dispose(); } catch { }
            }
            _dismissTokens.Clear();
            _toasts.Clear();
        }
    }

    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }
}
