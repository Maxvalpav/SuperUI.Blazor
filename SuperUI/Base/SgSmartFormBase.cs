// SgSmartFormBase.cs — Умная форма с поддержкой .NET 8+ 
// Интеграция с Enhanced Navigation, SupplyParameterFromForm, валидацией 
 
using System.ComponentModel.DataAnnotations; 
using System.Linq.Expressions; 
using System.Reflection; 
using Microsoft.AspNetCore.Components; 
using Microsoft.AspNetCore.Components.Forms; 
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.Logging;
 
namespace SuperUI.Base; 
 
/// <summary> 
/// Умная форма с автоматической валидацией и обработкой. 
/// 
/// .NET 8+ возможности: 
/// - <see cref="SupplyParameterFromFormAttribute"/> для статического SSR 
/// - Enhanced Navigation через <see cref="EnhancedForm"/> 
/// - Автоматическая анти-фальсификация (antiforgery) 
/// - Поддержка как статического SSR, так и интерактивных режимов 
/// </summary> 
/// <typeparam name="TModel">Тип модели формы.</typeparam> 
public abstract class SgSmartFormBase<TModel> : SgComponentBase where TModel : class, new() 
{ 
    // ────────────────────────────────────────────── 
    //  Параметры 
    // ────────────────────────────────────────────── 
 
    /// <summary>Модель формы.</summary> 
    [SupplyParameterFromForm] 
    public TModel Model { get; set; } = new(); 
 
    /// <summary>URL для перенаправления после успешной отправки.</summary> 
    [Parameter] 
    public string? SuccessRedirectUrl { get; set; } 
 
    /// <summary>CSS класс для формы.</summary> 
    [Parameter] 
    public string? FormClass { get; set; } 
 
    /// <summary>Использовать ли Enhanced Navigation.</summary> 
    [Parameter] 
    public bool UseEnhancedNavigation { get; set; } = true; 
 
    /// <summary>Отображать ли summary ошибок валидации.</summary> 
    [Parameter] 
    public bool ShowValidationSummary { get; set; } = true; 
 
    // ────────────────────────────────────────────── 
    //  Состояние 
    // ────────────────────────────────────────────── 
 
    protected EditContext? EditContext { get; set; } 
    protected bool IsSubmitting { get; set; } 
    protected bool IsSubmitted { get; set; } 
    protected string? SubmitError { get; set; } 
    protected List<string> GeneralErrors { get; } = new(); 
 
    // ────────────────────────────────────────────── 
    //  Инжекция 
    // ────────────────────────────────────────────── 
 
    [Inject] 
    protected NavigationManager NavigationManager { get; set; } = default!; 
 
    // ────────────────────────────────────────────── 
    //  Жизненный цикл 
    // ────────────────────────────────────────────── 
 
    protected override Task OnInitializeAsync() 
    { 
        EditContext = new EditContext(Model); 
        EditContext.OnValidationRequested += OnValidationRequested; 
        EditContext.OnFieldChanged += OnFieldChanged; 
 
        return Task.CompletedTask; 
    } 
 
    protected virtual void OnValidationRequested(object? sender, ValidationRequestedEventArgs e) 
    { 
        GeneralErrors.Clear(); 
        var results = new List<ValidationResult>(); 
        var isValid = Validator.TryValidateObject(Model, new ValidationContext(Model), results, validateAllProperties: true); 
 
        foreach (var result in results) 
        { 
            if (result.MemberNames.Any()) 
            { 
                foreach (var member in result.MemberNames) 
                { 
                    EditContext?.NotifyValidationStateChanged(); 
                } 
            } 
            else 
            { 
                GeneralErrors.Add(result.ErrorMessage ?? "Validation error"); 
            } 
        } 
    } 
 
    protected virtual void OnFieldChanged(object? sender, FieldChangedEventArgs e) 
    { 
        // Валидируем только изменённое поле 
        var fieldIdentifier = e.FieldIdentifier; 
        var property = fieldIdentifier.Model.GetType().GetProperty(fieldIdentifier.FieldName); 
        if (property != null) 
        { 
            var value = property.GetValue(fieldIdentifier.Model); 
            var context = new ValidationContext(fieldIdentifier.Model) { MemberName = fieldIdentifier.FieldName }; 
            var results = new List<ValidationResult>(); 
            Validator.TryValidateProperty(value, context, results); 
 
            // Очищаем предыдущие ошибки поля 
            GeneralErrors.RemoveAll(err => err.Contains(fieldIdentifier.FieldName)); 
        } 
    } 
 
    // ────────────────────────────────────────────── 
    //  Отправка формы 
    // ────────────────────────────────────────────── 
 
    /// <summary> 
    /// Обработчик отправки формы. 
    /// </summary> 
    protected async Task OnSubmitAsync() 
    { 
        if (IsSubmitting) return; 
 
        IsSubmitting = true; 
        SubmitError = null; 
        GeneralErrors.Clear(); 
 
        try 
        { 
            // Валидируем 
            var isValid = EditContext?.Validate() ?? true; 
 
            if (!isValid) 
            { 
                IsSubmitting = false; 
                return; 
            } 
 
            // Выполняем бизнес-логику 
            var result = await SubmitAsync(Model, LifecycleToken); 
 
            if (result.IsSuccess) 
            { 
                IsSubmitted = true; 
                await OnSubmitSuccessAsync(result); 
 
                if (!string.IsNullOrEmpty(SuccessRedirectUrl)) 
                { 
                    NavigationManager.NavigateTo(SuccessRedirectUrl, forceLoad: !UseEnhancedNavigation); 
                } 
            } 
            else 
            { 
                SubmitError = result.ErrorMessage ?? "Submission failed."; 
                await OnSubmitErrorAsync(result); 
            } 
        } 
        catch (OperationCanceledException) 
        { 
            // Форма была отменена (например, компонент уничтожен) 
        } 
        catch (Exception ex) 
        { 
            SubmitError = ex.Message; 
            Logger.LogError(ex, "[{ComponentId}] Form submission failed", ComponentId); 
            await OnSubmitExceptionAsync(ex); 
        } 
        finally 
        { 
            IsSubmitting = false; 
        } 
    } 
 
    /// <summary> 
    /// Бизнес-логика отправки формы. 
    /// Переопределите в наследниках. 
    /// </summary> 
    protected abstract Task<FormSubmitResult> SubmitAsync(TModel model, CancellationToken cancellationToken); 
 
    /// <summary> 
    /// Вызывается при успешной отправке. 
    /// </summary> 
    protected virtual Task OnSubmitSuccessAsync(FormSubmitResult result) => Task.CompletedTask; 
 
    /// <summary> 
    /// Вызывается при ошибке валидации на сервере. 
    /// </summary> 
    protected virtual Task OnSubmitErrorAsync(FormSubmitResult result) => Task.CompletedTask; 
 
    /// <summary> 
    /// Вызывается при неожиданном исключении. 
    /// </summary> 
    protected virtual Task OnSubmitExceptionAsync(Exception ex) => Task.CompletedTask; 
 
    // ────────────────────────────────────────────── 
    //  Вспомогательные методы для полей 
    // ────────────────────────────────────────────── 
 
    /// <summary> 
    /// Получить выражение для поля (для For/ValueExpression). 
    /// </summary> 
    protected Expression<Func<T>> FieldExpression<T>(Expression<Func<TModel, T>> expression) 
    { 
        // Компилируем выражение к текущей модели 
        var compiled = expression.Compile(); 
        return () => compiled(Model); 
    } 
 
    /// <summary> 
    /// Получить ошибки для конкретного поля. 
    /// </summary> 
    protected IEnumerable<string> GetFieldErrors(string fieldName) 
    { 
        if (EditContext == null) return Array.Empty<string>(); 
 
        return EditContext.GetValidationMessages( 
            new FieldIdentifier(Model, fieldName)); 
    } 
 
    /// <summary> 
    /// Есть ли ошибки у поля. 
    /// </summary> 
    protected bool HasFieldError(string fieldName) 
    { 
        return GetFieldErrors(fieldName).Any(); 
    } 
 
    /// <summary> 
    /// CSS класс для поля с ошибкой. 
    /// </summary> 
    protected string FieldClass(string fieldName, string baseClass = "") 
    { 
        return HasFieldError(fieldName) 
            ? $"{baseClass} sg-field--error" 
            : baseClass; 
    } 
 
    // ────────────────────────────────────────────── 
    //  Рендеринг 
    // ────────────────────────────────────────────── 
 
    protected override void BuildRenderTree(RenderTreeBuilder builder) 
    { 
        if (IsSubmitted) 
        { 
            RenderSuccess(builder); 
            return; 
        } 
 
        builder.OpenRegion(0); 
        RenderForm(builder); 
        builder.CloseRegion(); 
    } 
 
    /// <summary>Рендерит форму. Переопределите в .razor наследнике.</summary> 
    protected virtual void RenderForm(RenderTreeBuilder builder) { } 
 
    /// <summary>Рендерит сообщение об успехе.</summary> 
    protected virtual void RenderSuccess(RenderTreeBuilder builder) 
    { 
        builder.OpenElement(0, "div"); 
        builder.AddAttribute(1, "class", "sg-form-success"); 
        builder.AddAttribute(2, "role", "status"); 
        builder.AddContent(3, "Form submitted successfully!"); 
        builder.CloseElement(); 
    } 
 
    // ────────────────────────────────────────────── 
    //  Cleanup 
    // ────────────────────────────────────────────── 
 
    protected override void Dispose(bool disposing) 
    { 
        if (disposing && EditContext != null) 
        { 
            EditContext.OnValidationRequested -= OnValidationRequested; 
            EditContext.OnFieldChanged -= OnFieldChanged; 
        } 
 
        base.Dispose(disposing); 
    } 
} 
 
/// <summary> 
/// Результат отправки формы. 
/// </summary> 
public readonly struct FormSubmitResult 
{ 
    public bool IsSuccess { get; init; } 
    public string? ErrorMessage { get; init; } 
    public string? SuccessMessage { get; init; } 
    public object? Data { get; init; } 
 
    public static FormSubmitResult Success(string? message = null, object? data = null) 
        => new() { IsSuccess = true, SuccessMessage = message, Data = data }; 
 
    public static FormSubmitResult Failure(string error) 
        => new() { IsSuccess = false, ErrorMessage = error }; 
} 
