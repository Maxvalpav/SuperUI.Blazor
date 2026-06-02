using Microsoft.Extensions.Options;
using SuperUI;
using SuperUI.Localization;
using SuperUI.Enums;

namespace SuperUI.Components;

/// <summary>
/// Запрос на показ confirm-диалога.
/// </summary>
public sealed class SgConfirmRequest
{
    /// <summary>Заголовок диалога.</summary>
    public string? Title { get; init; }

    /// <summary>Сообщение.</summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>Вариант оформления. По умолчанию <see cref="SgAlertVariant.Danger"/>.</summary>
    public SgAlertVariant Variant { get; init; } = SgAlertVariant.Danger;

    /// <summary>Текст кнопки подтверждения.</summary>
    public string? ConfirmText { get; init; }

    /// <summary>Текст кнопки отмены.</summary>
    public string? CancelText { get; init; }

    /// <summary>Токен отмены — позволяет программно отменить ожидающий диалог.</summary>
    public CancellationToken CancellationToken { get; init; }

    /// <summary>Внутреннее состояние (используется для очереди). Не устанавливайте вручную.</summary>
    internal object? UserState { get; set; }
}

/// <summary>
/// Сервис показа confirm-диалогов. Singleton.
/// </summary>
/// <remarks>
/// <para>Подписчиком должен быть один <c>SgConfirmHost</c>. Если зарегистрировано несколько,
/// запрос обрабатывает первый успешно ответивший — это страхует от ситуаций prerender→interactive,
/// когда оба экземпляра кратко сосуществуют.</para>
/// <para>Если ни одного подписчика нет, <see cref="ConfirmAsync"/> возвращает <c>false</c>, не блокируясь.</para>
/// <para><b>Queue mode (default):</b> если confirm уже показывается и приходит новый запрос,
/// он ставится в очередь и показывается последовательно. Это предотвращает
/// "стек модалок" с перекрывающимися backdrop'ами. Очередь можно отключить
/// через <see cref="EnableQueue"/> = false.</para>
/// </remarks>
public sealed class SgConfirmService : IAsyncDisposable
{
    private readonly ISuperUILocalizer _localizer;
    private readonly string _defaultTitle;
    private readonly string _defaultConfirmText;
    private readonly string _defaultCancelText;
    private readonly object _queueGate = new();
    private readonly Queue<SgConfirmRequest> _queue = new();
    private int _isDisposed; // 0 / 1
    private int _activeCount; // 0 or 1
    private int _maxQueueSize = 16;

    /// <summary>Initializes a new instance.</summary>
    public SgConfirmService(ISuperUILocalizer localizer) : this(localizer, null) { }

    /// <summary>Initializes a new instance with options.</summary>
    public SgConfirmService(ISuperUILocalizer localizer, IOptions<SuperUiOptions>? options)
    {
        ArgumentNullException.ThrowIfNull(localizer);
        _localizer = localizer;
        var o = options?.Value;
        _defaultTitle       = _localizer["Common_Confirmation"];
        _defaultConfirmText = _localizer["Common_Confirm"];
        _defaultCancelText  = _localizer["Common_Cancel"];
    }

    /// <summary>Возникает, когда запрашивается confirm. Подписывается <c>SgConfirmHost</c>.</summary>
    public event Func<SgConfirmRequest, Task<bool>>? Requested;

    /// <summary>Подписан ли хост.</summary>
    public bool HasHost => Requested is not null;

    /// <summary>Включает очередь (default true). Если false — каждый новый запрос прерывает предыдущий.</summary>
    public bool EnableQueue { get; set; } = true;

    /// <summary>Максимальный размер очереди. Превышение — новые запросы сразу возвращают false.</summary>
    public int MaxQueueSize
    {
        get { lock (_queueGate) return _maxQueueSize; }
        set { lock (_queueGate) _maxQueueSize = Math.Max(0, value); }
    }

    /// <summary>Текущая длина очереди (без учёта активного диалога).</summary>
    public int QueueLength { get { lock (_queueGate) return _queue.Count; } }

    /// <summary>Показывается ли confirm прямо сейчас.</summary>
    public bool IsActive => Volatile.Read(ref _activeCount) > 0;

    /// <summary>Очищает очередь ожидающих confirm-запросов.</summary>
    public void ClearQueue() { lock (_queueGate) _queue.Clear(); }

    /// <summary>
    /// Показывает confirm-диалог и ждёт ответ пользователя.
    /// </summary>
    /// <param name="message">Сообщение.</param>
    /// <param name="title">Заголовок (опционально).</param>
    /// <param name="variant">Вариант оформления. По умолчанию <see cref="SgAlertVariant.Danger"/>.</param>
    /// <param name="confirmText">Текст кнопки «OK».</param>
    /// <param name="cancelText">Текст кнопки «Отмена».</param>
    /// <param name="cancellationToken">Программная отмена ожидания.</param>
    /// <returns><c>true</c>, если пользователь подтвердил.</returns>
    public Task<bool> ConfirmAsync(
        string message,
        string? title = null,
        SgAlertVariant variant = SgAlertVariant.Danger,
        string? confirmText = null,
        string? cancelText = null,
        CancellationToken cancellationToken = default)
    {
        if (Volatile.Read(ref _isDisposed) == 1) return Task.FromResult(false);
        if (cancellationToken.IsCancellationRequested) return Task.FromResult(false);

        var request = new SgConfirmRequest
        {
            Title       = string.IsNullOrWhiteSpace(title)       ? _defaultTitle       : title,
            Message     = message ?? string.Empty,
            Variant     = variant,
            ConfirmText = string.IsNullOrWhiteSpace(confirmText) ? _defaultConfirmText : confirmText,
            CancelText  = string.IsNullOrWhiteSpace(cancelText)  ? _defaultCancelText  : cancelText,
            CancellationToken = cancellationToken
        };

        return ConfirmCoreAsync(request);
    }

    /// <summary>Builder-овариант: <c>ConfirmAsync(r =&gt; { ... })</c>.</summary>
    public Task<bool> ConfirmAsync(Action<SgConfirmRequestBuilder> configure, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(configure);
        if (Volatile.Read(ref _isDisposed) == 1) return Task.FromResult(false);
        if (cancellationToken.IsCancellationRequested) return Task.FromResult(false);

        var builder = new SgConfirmRequestBuilder(_defaultTitle, _defaultConfirmText, _defaultCancelText);
        configure(builder);
        var request = builder.Build(cancellationToken);
        return ConfirmCoreAsync(request);
    }

    private async Task<bool> ConfirmCoreAsync(SgConfirmRequest request)
    {
        // Queue mode: если уже есть активный диалог, ставим в очередь и ждём очереди.
        if (EnableQueue && Volatile.Read(ref _activeCount) > 0)
        {
            TaskCompletionSource<bool>? tcs = null;
            lock (_queueGate)
            {
                if (_queue.Count >= _maxQueueSize) return false; // overflow
                _queue.Enqueue(request);
                tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                request.UserState = tcs;
            }

            // Wait for the queue to drain to our request, then answer.
            try
            {
                using var registration = request.CancellationToken.Register(() => tcs.TrySetCanceled());
                var result = await tcs.Task.ConfigureAwait(false);
                return result;
            }
            catch (OperationCanceledException) { return false; }
        }

        return await ExecuteNowAsync(request).ConfigureAwait(false);
    }

    private async Task<bool> ExecuteNowAsync(SgConfirmRequest request)
    {
        var handler = Requested;
        if (handler is null) return false;

        Interlocked.Increment(ref _activeCount);
        try
        {
            // Идём по подписчикам по порядку: первый, у которого получилось — побеждает.
            foreach (var d in handler.GetInvocationList())
            {
                var func = (Func<SgConfirmRequest, Task<bool>>)d;
                try
                {
                    var task = func(request);
                    if (task is null) continue;

                    if (request.CancellationToken.CanBeCanceled)
                    {
                        var completed = await Task.WhenAny(task, Task.Delay(Timeout.Infinite, request.CancellationToken))
                                                  .ConfigureAwait(false);
                        if (completed == task) return await task.ConfigureAwait(false);
                        return false;
                    }

                    return await task.ConfigureAwait(false);
                }
                catch (OperationCanceledException) { return false; }
                catch
                {
                    // Этот хост уже не живой — пробуем следующего.
                }
            }
            return false;
        }
        finally
        {
            Interlocked.Decrement(ref _activeCount);
            // If we are in queue mode, dequeue the next request and dispatch it.
            if (EnableQueue) DispatchNextQueued();
        }
    }

    private void DispatchNextQueued()
    {
        SgConfirmRequest? next = null;
        TaskCompletionSource<bool>? tcs = null;
        lock (_queueGate)
        {
            if (_queue.Count == 0) return;
            next = _queue.Dequeue();
            tcs = next.UserState as TaskCompletionSource<bool>;
        }
        if (next is null) return;

        _ = Task.Run(async () =>
        {
            try
            {
                var result = await ExecuteNowAsync(next).ConfigureAwait(false);
                tcs?.TrySetResult(result);
            }
            catch (Exception ex)
            {
                tcs?.TrySetException(ex);
            }
        });
    }

    /// <summary>Освобождает сервис.</summary>
    public ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _isDisposed, 1) == 1) return ValueTask.CompletedTask;
        Requested = null;
        return ValueTask.CompletedTask;
    }
}

/// <summary>
/// Билдер для <see cref="SgConfirmRequest"/>.
/// </summary>
public sealed class SgConfirmRequestBuilder
{
    private string? _title;
    private string  _message = string.Empty;
    private SgAlertVariant _variant = SgAlertVariant.Danger;
    private string? _confirmText;
    private string? _cancelText;

    internal SgConfirmRequestBuilder(string defaultTitle, string defaultConfirm, string defaultCancel)
    {
        _title       = defaultTitle;
        _confirmText = defaultConfirm;
        _cancelText  = defaultCancel;
    }

    /// <summary>Заголовок.</summary>
    public SgConfirmRequestBuilder Title(string? value)       { _title = value;       return this; }
    /// <summary>Сообщение.</summary>
    public SgConfirmRequestBuilder Message(string value)      { _message = value ?? ""; return this; }
    /// <summary>Вариант оформления.</summary>
    public SgConfirmRequestBuilder Variant(SgAlertVariant v)  { _variant = v;         return this; }
    /// <summary>Текст «OK».</summary>
    public SgConfirmRequestBuilder ConfirmText(string? value) { _confirmText = value; return this; }
    /// <summary>Текст «Отмена».</summary>
    public SgConfirmRequestBuilder CancelText(string? value)  { _cancelText = value;  return this; }

    internal SgConfirmRequest Build(CancellationToken ct) => new()
    {
        Title             = _title,
        Message           = _message,
        Variant           = _variant,
        ConfirmText       = _confirmText,
        CancelText        = _cancelText,
        CancellationToken = ct
    };
}
