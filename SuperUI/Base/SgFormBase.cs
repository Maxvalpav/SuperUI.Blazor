// SuperUI/Base/SgFormBase.cs
// ИСПРАВЛЕНИЯ v2:
// ✅ W8: [SupplyParameterFromForm] требует public в .NET 8 SSR → public
// ✅ L3: InitEditContext вызывается при IsInteractive ИЛИ после hydration
// ✅ Типизированная ValidationError вместо tuple
// ✅ ValidateAsync: nullable return → пустой массив по умолчанию

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Logging;

namespace SuperUI.Base;

/// <summary>Результат серверной валидации поля формы.</summary>
public readonly record struct ValidationError(string Field, string Message);

public abstract class SgFormBase<TModel> : SgComponentBase
    where TModel : class, new()
{
    private EditContext? _editContext;
    private ValidationMessageStore? _messageStore;
    private int _submitting;

    // ── Параметры ──────────────────────────────────────────────────────────────
    // ✅ FIX W8: [SupplyParameterFromForm] требует public доступность в .NET 8 SSR
    // Для библиотечного компонента рекомендуется public с документацией.
    [SupplyParameterFromForm(FormName = nameof(FormName))]
    public TModel? FormModel { get; set; }

    [Parameter] public string FormName { get; set; } = "form";
    [Parameter] public EventCallback<TModel> OnValidSubmit { get; set; }
    [Parameter] public EventCallback<TModel> OnInvalidSubmit { get; set; }
    [Parameter] public RenderFragment<EditContext>? ChildContent { get; set; }

    // ── Состояние ──────────────────────────────────────────────────────────────
    protected EditContext? EditContext => _editContext;
    protected bool IsSubmitting => _submitting == 1;
    protected string? SubmitError { get; private set; }

    // ── Lifecycle ──────────────────────────────────────────────────────────────
    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        FormModel ??= await CreateModelAsync();
        // ✅ FIX L3: создаём EditContext при первом рендере если Interactive
        // При InteractiveAuto prerender: IsInteractive=false, EditContext не создаётся.
        // После hydration OnAfterRenderAsync(firstRender=true) → создаём EditContext.
        if (IsInteractive)
            InitEditContext(FormModel);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
        // ✅ FIX L3: для InteractiveAuto — создаём EditContext после hydration
        if (firstRender && IsInteractive && _editContext is null && FormModel is not null)
        {
            InitEditContext(FormModel);
            await RefreshAsync();
        }
    }

    private void InitEditContext(TModel model)
    {
        // Отписываем от старого EditContext
        if (_editContext is not null)
            _editContext.OnValidationRequested -= OnValidationRequested;

        _editContext = new EditContext(model);
        _messageStore = new ValidationMessageStore(_editContext);
        _editContext.OnValidationRequested += OnValidationRequested;
    }

    protected virtual Task<TModel> CreateModelAsync() => Task.FromResult(new TModel());

    // ── Submit ─────────────────────────────────────────────────────────────────
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
                    foreach (var error in serverErrors)
                        _messageStore.Add(_editContext.Field(error.Field), error.Message);
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

    protected virtual async Task OnFormReset()
    {
        FormModel = await CreateModelAsync();
        if (FormModel is not null && IsInteractive)
            InitEditContext(FormModel);
        await RefreshAsync();
    }

    // ── Переопределяемые методы ────────────────────────────────────────────────
    protected abstract Task OnSubmitAsync(TModel model);

    /// <summary>
    /// ✅ FIX: возвращает ValidationError[] (пустой = нет ошибок), не null.
    /// </summary>
    protected virtual Task<ValidationError[]> ValidateAsync(TModel model)
        => Task.FromResult(Array.Empty<ValidationError>());

    private void OnValidationRequested(object? sender, ValidationRequestedEventArgs e)
        => _messageStore?.Clear();

    // ── Dispose ────────────────────────────────────────────────────────────────
    protected override void Dispose(bool disposing)
    {
        if (disposing && _editContext is not null)
            _editContext.OnValidationRequested -= OnValidationRequested;
        base.Dispose(disposing);
    }
}