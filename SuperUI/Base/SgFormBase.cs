// SuperUI/Base/SgFormBase.cs
// ИСПРАВЛЕНО:
// ✅ OnFormReset: отписывает OnValidationRequested от старого EditContext перед заменой
// ✅ EditContext создаётся ТОЛЬКО в Interactive режиме
// ✅ Double-submit защита через Interlocked
// ✅ ValidateAsync возвращает (string field, string error)[] — type-safe
// ✅ .NET 8/9/10: [SupplyParameterFromForm] корректно в SSR

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Logging;

namespace SuperUI.Base;

public abstract class SgFormBase<TModel> : SgComponentBase
    where TModel : class, new()
{
    private EditContext? _editContext;
    private ValidationMessageStore? _messageStore;
    private int _submitting;

    // ── Параметры ────────────────────────────────────────────────────────────
    [SupplyParameterFromForm]
    protected TModel? FormModel { get; set; }

    [Parameter] public string FormName { get; set; } = "form";
    [Parameter] public EventCallback<TModel> OnValidSubmit { get; set; }
    [Parameter] public EventCallback<TModel> OnInvalidSubmit { get; set; }
    [Parameter] public RenderFragment<TModel>? ChildContent { get; set; }

    // ── Состояние ────────────────────────────────────────────────────────────
    protected EditContext? EditContext => _editContext;
    protected bool IsSubmitting => _submitting == 1;
    protected string? SubmitError { get; private set; }

    // ── Lifecycle ────────────────────────────────────────────────────────────
    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        FormModel ??= await CreateModelAsync();

        if (IsInteractive)
            InitEditContext(FormModel);
    }

    private void InitEditContext(TModel model)
    {
        // ✅ ИСПРАВЛЕНО: отписываем старый обработчик перед заменой
        if (_editContext is not null)
            _editContext.OnValidationRequested -= OnValidationRequested;

        _editContext = new EditContext(model);
        _messageStore = new ValidationMessageStore(_editContext);
        _editContext.OnValidationRequested += OnValidationRequested;
    }

    protected virtual Task<TModel> CreateModelAsync()
        => Task.FromResult(new TModel());

    // ── Submit ───────────────────────────────────────────────────────────────
    protected async Task HandleValidSubmitAsync()
    {
        if (FormModel is null) return;
        if (Interlocked.CompareExchange(ref _submitting, 1, 0) == 1) return;

        SubmitError = null;
        await RefreshAsync();

        try
        {
            var serverErrors = await ValidateAsync(FormModel);
            if (serverErrors is { Length: > 0 })
            {
                if (_messageStore is not null && _editContext is not null)
                {
                    foreach (var (field, error) in serverErrors)
                        _messageStore.Add(_editContext.Field(field), error);

                    _editContext.NotifyValidationStateChanged();
                }

                await OnInvalidSubmit.InvokeAsync(FormModel);
                return;
            }

            await OnSubmitAsync(FormModel);
            await OnValidSubmit.InvokeAsync(FormModel);
        }
        catch (Exception ex)
        {
            SubmitError = ex.Message;
            Logger.LogError(ex, "[{Id}] Form submit failed", ComponentId);
        }
        finally
        {
            Interlocked.Exchange(ref _submitting, 0);
            await RefreshAsync();
        }
    }

    protected async Task HandleInvalidSubmitAsync()
    {
        if (FormModel is not null)
            await OnInvalidSubmit.InvokeAsync(FormModel);
    }

    /// <summary>
    /// Сброс формы к исходному состоянию.
    /// ✅ ИСПРАВЛЕНО: правильная отписка от старого EditContext.
    /// </summary>
    protected virtual async Task OnFormReset()
    {
        FormModel = await CreateModelAsync();

        if (FormModel is not null && IsInteractive)
            InitEditContext(FormModel);

        await RefreshAsync();
    }

    // ── Переопределяемые методы ──────────────────────────────────────────────
    protected abstract Task OnSubmitAsync(TModel model);

    /// <returns>Массив ошибок (fieldName, errorMessage). Пустой = нет ошибок.</returns>
    protected virtual Task<(string Field, string Error)[]?> ValidateAsync(TModel model)
        => Task.FromResult<(string, string)[]?>(null);

    private void OnValidationRequested(object? sender, ValidationRequestedEventArgs e)
        => _messageStore?.Clear();

    // ── Dispose ──────────────────────────────────────────────────────────────
    protected override void Dispose(bool disposing)
    {
        if (disposing && _editContext is not null)
            _editContext.OnValidationRequested -= OnValidationRequested;

        base.Dispose(disposing);
    }
}
