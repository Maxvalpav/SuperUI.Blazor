using System.Collections.Concurrent;
using Microsoft.AspNetCore.Components;
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

    /// <summary>Optional action buttons rendered inside the toast.</summary>
    public RenderFragment? ActionsContent { get; set; }

    /// <summary>Click callback invoked when the toast is clicked (receives toast ID).</summary>
    public Action<string>? OnClick { get; set; }

    /// <summary>Close callback invoked when the toast is dismissed (by timeout or manual close). Receives toast ID.</summary>
    public Action<string>? OnClose { get; set; }

    /// <summary>Toast size preset. Default Md.</summary>
    public SgSize Size { get; set; } = SgSize.Md;

    /// <summary>Whether to show a visual countdown progress bar. Default true when DurationMs &gt; 0.</summary>
    public bool ShowProgress { get; set; } = true;

    /// <summary>Per-toast position override. When set, the toast renders in the specified position container instead of the host's default.</summary>
    public SgToastPlacement? Position { get; set; }

    /// <summary>Simple action button label shown in the toast.</summary>
    public string? ActionText { get; set; }

    /// <summary>Callback when the action button is clicked (receives toast ID).</summary>
    public Action<string>? OnAction { get; set; }

    /// <summary>Group tag for batch dismiss (see <see cref="SgToastService.DismissByGroup"/>).</summary>
    public string? Group { get; set; }

    /// <summary>Whether the toast starts in expanded mode.</summary>
    public bool Expanded { get; set; }

    /// <summary>Content revealed when the toast is expanded.</summary>
    public RenderFragment? ExpandedContent { get; set; }

    /// <summary>Приоритет тоста. Используется для упорядочивания в стеке и фильтрации при переполнении.</summary>
    public SgToastPriority Priority { get; set; } = SgToastPriority.Normal;

    /// <summary>Категория для фильтрации и дедупликации (например, "network", "auth").</summary>
    public string? Category { get; set; }
}

/// <summary>
/// Приоритет тоста. Высокий приоритет означает, что тост показывается первым
/// и остаётся в очереди, если новых тостов больше <c>MaxVisibleToasts</c>.
/// </summary>
public enum SgToastPriority
{
    /// <summary>Обычный приоритет. Может быть вытеснен низкоприоритетными тостами.</summary>
    Low = 0,
    /// <summary>Стандартный приоритет (default).</summary>
    Normal = 1,
    /// <summary>Высокий приоритет. Показывается первым в очереди.</summary>
    High = 2,
    /// <summary>Критический — показывается в обход MaxVisibleToasts, требует ручного закрытия.</summary>
    Critical = 3,
}

/// <summary>
/// Сервис показа тостов. Регистрируется как Scoped (per-circuit / per-session).
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

    /// <summary>Закрывает все тосты с указанным тегом группы.</summary>
    public void DismissByGroup(string group)
    {
        if (string.IsNullOrEmpty(group)) return;
        if (Volatile.Read(ref _disposed) == 1) return;

        foreach (var kvp in _activeToasts)
        {
            if (string.Equals(kvp.Value.Group, group, StringComparison.Ordinal))
                Dismiss(kvp.Key);
        }
    }

    /// <summary>Закрывает все тосты с указанной категорией.</summary>
    public int DismissByCategory(string category)
    {
        if (string.IsNullOrEmpty(category)) return 0;
        if (Volatile.Read(ref _disposed) == 1) return 0;
        var dismissed = 0;
        // Snapshot keys to avoid concurrent modification during iteration.
        var toDismiss = new List<string>();
        foreach (var kvp in _activeToasts)
            if (string.Equals(kvp.Value.Category, category, StringComparison.Ordinal))
                toDismiss.Add(kvp.Key);
        foreach (var id in toDismiss) { Dismiss(id); dismissed++; }
        return dismissed;
    }

    /// <summary>Возвращает все тосты с приоритетом ≤ указанного.</summary>
    public IReadOnlyList<SgToast> GetByPriority(SgToastPriority minPriority)
    {
        var result = new List<SgToast>();
        foreach (var t in _activeToasts.Values)
            if (t.Priority >= minPriority) result.Add(t);
        return result;
    }

    /// <summary>Дедупликация: если тост с таким же Category+Message уже активен, возвращает его id без показа нового.</summary>
    /// <returns>True, если новый тост показан; false, если найден дубликат.</returns>
    public bool ShowUnique(Action<SgToast> configure, int dedupWindowMs = 1500)
    {
        ArgumentNullException.ThrowIfNull(configure);
        if (Volatile.Read(ref _disposed) == 1) return false;

        var probe = new SgToast { DurationMs = _defaultDurationMs };
        configure(probe);

        // Check for existing duplicate: same Category AND same Message within the dedup window.
        var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        foreach (var existing in _activeToasts.Values)
        {
            if (existing.Category == probe.Category
                && string.Equals(existing.Message, probe.Message, StringComparison.Ordinal))
            {
                // Reset its timer (bump it).
                if (existing.DurationMs > 0)
                {
                    existing.ShowProgress = probe.ShowProgress;
                    Update(existing.Id, t => t.DurationMs = probe.DurationMs);
                }
                return false;
            }
        }
        ShowCore(probe);
        return true;
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
    /// Это scoped DI-сервис — компоненты-хосты НЕ должны вызывать DisposeAsync сами,
    /// диспозит контейнер при shutdown / закрытии circuit.
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
