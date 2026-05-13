// SuperUI/Base/SgFormBase.cs
// Улучшенная версия с поддержкой Static SSR Forms (.NET 8+)
//
// ИСПОЛНЯЮЩИЙ ФАЙЛ:
//   SgFormBase<TModel> — контейнер-обёртка для формы.
//   SgFormFieldBase<TValue> — отдельное поле формы (см. SgFormFieldBase.cs).
//
// Static SSR:
//   - EffectiveFormName для HTML аттрибута name на <form>
//   - IFormNameGenerator — генерация уникальных имён для antiforgery
//   - IsStaticSSR определяется через RenderMode (из SgComponentBase)
//
// Interactive:
//   - OnValidSubmit / OnInvalidSubmit
//   - EditContext валидация
//   - LoadingTemplate

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Logging;
using SuperUI.Base.Services;

namespace SuperUI.Base;

/// <summary>
/// Базовый класс для компонентов формы SuperUI.
/// Поддерживает как интерактивные компоненты, так и Static SSR (.NET 8+).
/// </summary>
/// <typeparam name="TModel">Тип модели формы (должен иметь конструктор без параметров).</typeparam>
public abstract class SgFormBase<TModel> : SgInteractiveBase where TModel : class, new()
{
    [Inject] protected IFormNameGenerator? FormNameGenerator { get; set; }

    // ── Параметры ─────────────────────────────────────────────────────────

    [Parameter] public TModel? Model { get; set; }
    [Parameter] public EventCallback<TModel> ModelChanged { get; set; }
    [Parameter] public EventCallback<TModel> OnValidSubmit { get; set; }
    [Parameter] public EventCallback OnInvalidSubmit { get; set; }
    [Parameter] public RenderFragment? ChildContent { get; set; }
    [Parameter] public RenderFragment? LoadingTemplate { get; set; }

    /// <summary>
    /// Имя формы для Static SSR (используется для antiforgery и routing).
    /// </summary>
    [Parameter] public string? FormName { get; set; }

    // ── Состояние ──────────────────────────────────────────────────────────

    protected EditContext? _editContext;
    protected bool _isSubmitting;
    protected bool _isValid;
    protected int _submitCount;
    private string? _generatedFormName;

    protected string EffectiveFormName =>
        FormName ?? _generatedFormName ?? $"sg-form-{ComponentId}";

    // ── Lifecycle ──────────────────────────────────────────────────────────

    protected override void OnInitialized()
    {
        base.OnInitialized();
        _generatedFormName = FormNameGenerator?.GenerateFormName();

        Model ??= new TModel();
        _editContext = new EditContext(Model);
        _editContext.OnValidationStateChanged += OnValidationStateChanged;
    }

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();

        if (Model is not null && _editContext?.Model != Model)
        {
            // Model изменился извне — пересоздаём EditContext
            if (_editContext is not null)
                _editContext.OnValidationStateChanged -= OnValidationStateChanged;

            _editContext = new EditContext(Model);
            _editContext.OnValidationStateChanged += OnValidationStateChanged;
        }
    }

    // ── Submit ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Обработчик отправки формы.
    /// В Static SSR: вызывается через form submit.
    /// В Interactive: вызывается через OnValidSubmit callback.
    /// </summary>
    protected async Task HandleSubmitAsync()
    {
        if (IsDisposed || _editContext is null || IsEffectivelyDisabled)
            return;

        _isSubmitting = true;
        _submitCount++;

        try
        {
            var isValid = _editContext.Validate();
            _isValid = isValid;

            if (isValid)
            {
                await OnValidSubmit.InvokeAsync(Model!);
                await OnFormValidSubmitAsync();
            }
            else
            {
                await OnInvalidSubmit.InvokeAsync();
                await OnFormInvalidSubmitAsync();
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[{Id}] Form submit error", ComponentId);
        }
        finally
        {
            _isSubmitting = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    /// <summary>
    /// Extension point для кастомной логики при успешной отправке.
    /// </summary>
    protected virtual Task OnFormValidSubmitAsync() => Task.CompletedTask;

    /// <summary>
    /// Extension point для кастомной логики при ошибке валидации.
    /// </summary>
    protected virtual Task OnFormInvalidSubmitAsync() => Task.CompletedTask;

    // ── Validation ─────────────────────────────────────────────────────────

    private async void OnValidationStateChanged(object? sender, ValidationStateChangedEventArgs e)
    {
        if (IsDisposed) return;

        _isValid = !_editContext!.GetValidationMessages().Any();
        await InvokeAsync(StateHasChanged);
    }

    /// <summary>
    /// Сбросить форму к исходному состоянию.
    /// </summary>
    public async Task ResetAsync()
    {
        if (IsDisposed) return;

        Model = new TModel();
        _editContext = new EditContext(Model);
        _editContext.OnValidationStateChanged += OnValidationStateChanged;
        _isSubmitting = false;
        _isValid = false;
        _submitCount = 0;

        await InvokeAsync(StateHasChanged);
    }

    // ── IDisposable ────────────────────────────────────────────────────────

    protected override async ValueTask DisposeComponentAsync()
    {
        if (_editContext is not null)
            _editContext.OnValidationStateChanged -= OnValidationStateChanged;

        await base.DisposeComponentAsync();
    }
}

/// <summary>
/// Генератор имён форм для Static SSR Antiforgery.
/// Регистрируется как Scoped-сервис.
/// </summary>
public interface IFormNameGenerator
{
    string GenerateFormName();
}

internal sealed class DefaultFormNameGenerator : IFormNameGenerator
{
    private long _counter;

    public string GenerateFormName() =>
        $"sg-form-{Interlocked.Increment(ref _counter):x}";
}