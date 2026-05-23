using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using SuperUI;
using SuperUI.Enums;

namespace SuperUI.Components;

/// <summary>
/// Один тост.
/// </summary>
public sealed class SgToast
{
    /// <summary>Уникальный идентификатор тоста.</summary>
    public string Id { get; init; } = Guid.NewGuid().ToString("N");

    /// <summary>Заголовок.</summary>
    public string? Title { get; set; }

    /// <summary>Текст.</summary>
    public string? Message { get; set; }

    /// <summary>Вариант оформления.</summary>
    public SgToastVariant Variant { get; set; } = SgToastVariant.Default;

    /// <summary>Длительность авто-закрытия (мс). 0 или меньше — тост остаётся до ручного закрытия.</summary>
    public int DurationMs { get; set; } = 4000;
}

/// <summary>
/// Сервис показа тостов. Singleton.
/// Хост (<c>SgToastHost</c>) подписывается на <see cref="Added"/>/<see cref="Removed"/>/<see cref="Updated"/>.
/// </summary>
/// <remarks>
/// <para>Если хост ещё не подписан (например при первом prerender), тосты складываются в pending-очередь
/// и доставляются при первом подписавшемся хосте — без потерь.</para>
/// <para>Thread-safe: события вызываются с snapshot подписчиков, ошибки одного не срывают остальных.</para>
/// </remarks>
public sealed class SgToastService : IAsyncDisposable
{
    private readonly int _defaultDurationMs;
    private readonly ConcurrentDictionary<string, SgToast> _activeToasts = new();
    private readonly Queue<SgToast> _pending = new();
    private readonly object _pendingGate = new();
    private int _disposed; // 0 / 1

    private Action<SgToast>? _added;
    private Action<string>? _removed;
    private Action<SgToast>? _updated;

    /// <summary>Initializes a new instance.</summary>
    public SgToastService() : this(null) { }

    /// <summary>Initializes a new instance with options.</summary>
    public SgToastService(IOptions<SuperUiOptions>? options)
    {
        _defaultDurationMs = options?.Value.DefaultToastDurationMs ?? 4000;
        if (_defaultDurationMs < 0) _defaultDurationMs = 4000;
    }

    /// <summary>Возникает при добавлении нового тоста.</summary>
    public event Action<SgToast>? Added
    {
        add    { _added += value; FlushPending(); }
        remove { _added -= value; }
    }

    /// <summary>Возникает при удалении тоста.</summary>
    public event Action<string>? Removed
    {
        add    { _removed += value; }
        remove { _removed -= value; }
    }

    /// <summary>Возникает при обновлении полей существующего тоста (<see cref="Update"/>).</summary>
    public event Action<SgToast>? Updated
    {
        add    { _updated += value; }
        remove { _updated -= value; }
    }

    /// <summary>Текущее количество активных тостов (без учёта pending до подписки хоста).</summary>
    public int ActiveCount => _activeToasts.Count;

    /// <summary>Показывает тост.</summary>
    public string Show(
        string message,
        string? title = null,
        SgToastVariant variant = SgToastVariant.Default,
        int? durationMs = null)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) == 1, this);

        var toast = new SgToast
        {
            Message = message,
            Title = title,
            Variant = variant,
            DurationMs = durationMs ?? _defaultDurationMs
        };
        return ShowCore(toast);
    }

    /// <summary>Builder-овариант показа: <c>Show(t =&gt; { t.Title = "..."; t.Message = "..."; })</c>.</summary>
    public string Show(Action<SgToast> configure)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) == 1, this);
        ArgumentNullException.ThrowIfNull(configure);

        var toast = new SgToast { DurationMs = _defaultDurationMs };
        configure(toast);
        return ShowCore(toast);
    }

    private string ShowCore(SgToast toast)
    {
        _activeToasts[toast.Id] = toast;

        var handler = _added;
        if (handler is null)
        {
            lock (_pendingGate) _pending.Enqueue(toast);
        }
        else
        {
            RaiseAdded(toast, handler);
        }
        return toast.Id;
    }

    /// <summary>Обновляет существующий тост (полезно для «прогресс-тостов»).</summary>
    public bool Update(string id, Action<SgToast> mutate)
    {
        ArgumentNullException.ThrowIfNull(mutate);
        if (string.IsNullOrEmpty(id)) return false;
        if (!_activeToasts.TryGetValue(id, out var toast)) return false;

        mutate(toast);
        var handler = _updated;
        if (handler is not null)
        {
            foreach (var d in handler.GetInvocationList())
            {
                try { ((Action<SgToast>)d).Invoke(toast); } catch { }
            }
        }
        return true;
    }

    /// <summary>Success-тост.</summary>
    public string Success(string message, string? title = null) => Show(message, title, SgToastVariant.Success);

    /// <summary>Error-тост (вариант Danger).</summary>
    public string Error(string message, string? title = null) => Show(message, title, SgToastVariant.Danger);

    /// <summary>Warning-тост.</summary>
    public string Warn(string message, string? title = null) => Show(message, title, SgToastVariant.Warn);

    /// <summary>Info-тост (вариант Default).</summary>
    public string Info(string message, string? title = null) => Show(message, title, SgToastVariant.Default);

    /// <summary>Закрывает тост по id. Если такого тоста нет — событие не вызывается.</summary>
    public void Dismiss(string id)
    {
        if (string.IsNullOrEmpty(id)) return;
        if (Volatile.Read(ref _disposed) == 1) return;

        if (_activeToasts.TryRemove(id, out _))
        {
            var handler = _removed;
            if (handler is null) return;
            foreach (var d in handler.GetInvocationList())
            {
                try { ((Action<string>)d).Invoke(id); } catch { }
            }
        }
    }

    /// <summary>Закрывает все активные тосты.</summary>
    public void DismissAll()
    {
        if (Volatile.Read(ref _disposed) == 1) return;
        foreach (var id in _activeToasts.Keys)
            Dismiss(id);
    }

    private void FlushPending()
    {
        var handler = _added;
        if (handler is null) return;

        SgToast[]? snapshot = null;
        lock (_pendingGate)
        {
            if (_pending.Count == 0) return;
            snapshot = _pending.ToArray();
            _pending.Clear();
        }
        foreach (var t in snapshot) RaiseAdded(t, handler);
    }

    private static void RaiseAdded(SgToast toast, Action<SgToast> handler)
    {
        foreach (var d in handler.GetInvocationList())
        {
            try { ((Action<SgToast>)d).Invoke(toast); } catch { }
        }
    }

    /// <summary>Освобождает сервис.</summary>
    /// <remarks>
    /// Это singleton DI-сервис — компоненты-хосты НЕ должны вызывать DisposeAsync сами,
    /// диспозит контейнер при shutdown.
    /// </remarks>
    public ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1) return ValueTask.CompletedTask;

        _activeToasts.Clear();
        lock (_pendingGate) _pending.Clear();
        _added = null;
        _removed = null;
        _updated = null;
        return ValueTask.CompletedTask;
    }
}
