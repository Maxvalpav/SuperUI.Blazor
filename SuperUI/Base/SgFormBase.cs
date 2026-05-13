// SuperUI/Base/SgFormBase.cs 
// Улучшения: 
// - [SupplyParameterFromForm] для SSR POST-форм (.NET 8) 
// - AntiforgeryToken автоматически 
// - Dual-mode: SSR POST + Interactive EditForm 
// - Валидация в обоих режимах 
 
using System; 
using System.Threading.Tasks; 
using Microsoft.AspNetCore.Components; 
using Microsoft.AspNetCore.Components.Forms; 
using Microsoft.Extensions.Logging;
 
namespace SuperUI.Base; 
 
/// <summary> 
/// Базовый класс для форм, работающих в обоих режимах: 
/// - Static SSR: обработка через POST (SupplyParameterFromForm) 
/// - Interactive: стандартный EditForm с валидацией 
/// </summary> 
public abstract class SgFormBase<TModel> : SgComponentBase where TModel : class, new() 
{ 
    private EditContext? _editContext; 
    private ValidationMessageStore? _messageStore; 
 
    // ────────────────────────────────────────────────────────────────────── 
    // Параметры 
    // ────────────────────────────────────────────────────────────────────── 
 
    /// <summary> 
    /// Модель формы. В SSR режиме заполняется из POST данных через [SupplyParameterFromForm]. 
    /// </summary> 
    [SupplyParameterFromForm] 
    protected TModel? FormModel { get; set; } 
 
    [Parameter] public string FormName { get; set; } = "form"; 
    [Parameter] public EventCallback<TModel> OnValidSubmit { get; set; } 
    [Parameter] public EventCallback<TModel> OnInvalidSubmit { get; set; } 
    [Parameter] public RenderFragment<EditContext>? ChildContent { get; set; } 
 
    // ────────────────────────────────────────────────────────────────────── 
    // Состояние 
    // ────────────────────────────────────────────────────────────────────── 
 
    protected EditContext? EditContext => _editContext; 
    protected bool IsSubmitting { get; private set; } 
    protected string? SubmitError { get; private set; } 
 
    // ────────────────────────────────────────────────────────────────────── 
    // Жизненный цикл 
    // ────────────────────────────────────────────────────────────────────── 
 
    protected override async Task OnInitializedAsync() 
    { 
        await base.OnInitializedAsync(); 
 
        // Создаём модель если не пришла из SSR POST 
        FormModel ??= await CreateModelAsync(); 
 
        // Инициализируем EditContext для интерактивного режима 
        _editContext = new EditContext(FormModel); 
        _messageStore = new ValidationMessageStore(_editContext); 
        _editContext.OnValidationRequested += OnValidationRequested; 
    } 
 
    /// <summary> 
    /// Создаёт начальную модель формы (переопределите для инициализации из БД и т.д.). 
    /// </summary> 
    protected virtual Task<TModel> CreateModelAsync() => Task.FromResult(new TModel()); 
 
    // ────────────────────────────────────────────────────────────────────── 
    // Обработка отправки 
    // ────────────────────────────────────────────────────────────────────── 
 
    protected async Task HandleValidSubmitAsync() 
    { 
        if (FormModel == null) return; 
 
        IsSubmitting = true; 
        SubmitError = null; 
        StateHasChanged(); 
 
        try 
        { 
            // Серверная валидация 
            var serverErrors = await ValidateAsync(FormModel); 
            if (serverErrors != null && serverErrors.Length > 0) 
            { 
                foreach (var (field, error) in serverErrors) 
                    _messageStore?.Add(_editContext!.Field(field), error); 
                _editContext?.NotifyValidationStateChanged(); 
                await OnInvalidSubmit.InvokeAsync(FormModel); 
                return; 
            } 
 
            await OnSubmitAsync(FormModel); 
            await OnValidSubmit.InvokeAsync(FormModel); 
        } 
        catch (Exception ex) 
        { 
            SubmitError = ex.Message; 
            Logger.LogError(ex, "Form submit failed"); 
        } 
        finally 
        { 
            IsSubmitting = false; 
            StateHasChanged(); 
        } 
    } 
 
    protected async Task HandleInvalidSubmitAsync() 
    { 
        if (FormModel != null) 
            await OnInvalidSubmit.InvokeAsync(FormModel); 
    } 
 
    // ────────────────────────────────────────────────────────────────────── 
    // Переопределяемые методы 
    // ────────────────────────────────────────────────────────────────────── 
 
    /// <summary> 
    /// Основная логика обработки формы. Переопределите. 
    /// </summary> 
    protected abstract Task OnSubmitAsync(TModel model); 
 
    /// <summary> 
    /// Серверная валидация. Возвращает массив (поле, ошибка) или null. 
    /// </summary> 
    protected virtual Task<(string Field, string Error)[]?> ValidateAsync(TModel model) 
        => Task.FromResult<(string, string)[]?>(null); 
 
    private void OnValidationRequested(object? sender, ValidationRequestedEventArgs e) 
    { 
        _messageStore?.Clear(); 
    } 
 
    protected override void Dispose(bool disposing) 
    { 
        if (disposing && _editContext != null) 
            _editContext.OnValidationRequested -= OnValidationRequested; 
        base.Dispose(disposing); 
    }

    /// <summary>
    /// Log a form error with exception details.
    /// </summary>
    protected void LogFormError(Exception ex, string message)
    {
        Logger.LogError(ex, message);
    }
} 
