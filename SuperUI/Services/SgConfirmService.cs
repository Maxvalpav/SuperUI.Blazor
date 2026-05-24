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
}

/// <summary>
/// Сервис показа confirm-диалогов. Singleton.
/// </summary>
/// <remarks>
/// <para>Подписчиком должен быть один <c>SgConfirmHost</c>. Если зарегистрировано несколько,
/// запрос обрабатывает первый успешно ответивший — это страхует от ситуаций prerender→interactive,
/// когда оба экземпляра кратко сосуществуют.</para>
/// <para>Если ни одного подписчика нет, <see cref="ConfirmAsync"/> возвращает <c>false</c>, не блокируясь.</para>
/// </remarks>
public sealed class SgConfirmService : IAsyncDisposable
{
    private readonly ISuperUILocalizer _localizer;
    private readonly string _defaultTitle;
    private readonly string _defaultConfirmText;
    private readonly string _defaultCancelText;
    private int _isDisposed; // 0 / 1

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
        var handler = Requested;
        if (handler is null) return false;

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
                    // Линкуем ожидание с user-токеном
                    var completed = await Task.WhenAny(task, Task.Delay(Timeout.Infinite, request.CancellationToken))
                                              .ConfigureAwait(false);
                    if (completed == task) return await task.ConfigureAwait(false);
                    return false; // отменено
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
