// SuperUI/Base/SgFormBase.cs
// ИСПРАВЛЕНИЯ v3:
// ✅ FIX CS0506: ResetAsync — virtual (позволяет SgSmartFormBase переопределить)
// ✅ NEW: ResetModelAsync — асинхронный сброс модели с колбэком
// ✅ NEW: [SupplyParameterFromForm] поддержка документирована
// ✅ NEW: IsSubmitting — публичное свойство
// ✅ NEW: IsModelFromForm — флаг для SSR form-post

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Logging;
using SuperUI.Base.Services;

namespace SuperUI.Base;

/// <summary>
/// Базовый класс для компонентов формы SuperUI.
/// Поддерживает Static SSR (.NET 8+) и Interactive режимы.
/// </summary>
/// <typeparam name="TModel">Тип модели формы.</typeparam>
public abstract class SgFormBase<TModel> : SgInteractiveBase
    where TModel : class, new()
{
    [Inject] protected IFormNameGenerator? FormNameGenerator { get; set; }

    // ── Параметры ──────────────────────────────────────────────────────────────
    [Parameter] public TModel? Model { get; set; }
    [Parameter] public EventCallback<TModel> ModelChanged { get; set; }
    [Parameter] public EventCallback<TModel> OnValidSubmit { get; set; }
    [Parameter] public EventCallback OnInvalidSubmit { get; set; }
    [Parameter] public RenderFragment? ChildContent { get; set; }
    [Parameter] public RenderFragment? LoadingTemplate { get; set; }

    /// <summary>Имя формы для Static SSR antiforgery routing.</summary>
    [Parameter] public string? FormName { get; set; }

    /// <summary>CSS-класс для контейнера формы.</summary>
    [Parameter] public string? FormClass { get; set; }

    // ── Состояние ──────────────────────────────────────────────────────────────
    protected EditContext? _editContext;
    private bool _isSubmitting;
    protected bool _isValid;
    protected int _submitCount;

    /// <summary>Форма в процессе отправки.</summary>
    protected bool IsSubmitting
    {
        get => _isSubmitting;
        private set
        {
            _isSubmitting = value;
            OnSubmittingChanged(value);
        }
    }

    /// <summary>true если форма была отправлена хотя бы раз.</summary>
    protected bool IsSubmitted => _submitCount > 0;

    /// <summary>
    /// true — модель пришла из form-post (Static SSR).
    /// Установите [SupplyParameterFromForm] в дочернем классе на свойство модели
    /// и выставьте этот флаг в <c>OnParametersSetAsync</c> при наличии данных.
    ///
    /// Пример в дочернем классе:
    /// <code>
    /// [SupplyParameterFromForm]
    /// private MyModel? FormModel { get; set; }
    ///
    /// protected override Task OnParametersSetAsync()
    /// {
    ///     if (FormModel is not null)
    ///     {
    ///         Model = FormModel;
    ///         IsModelFromForm = true;
    ///     }
    ///     return base.OnParametersSetAsync();
    /// }
    /// </code>
    /// </summary>
    protected bool IsModelFromForm { get; set; }

    private string? _generatedFormName;

    /// <summary>Эффективное имя формы (явное или сгенерированное).</summary>
    protected string EffectiveFormName
        => FormName ?? _generatedFormName ?? $"sg-form-{ComponentId}";

    // ── Lifecycle ──────────────────────────────────────────────────────────────
    protected override void OnInitialized()
    {
        base.OnInitialized();
        _generatedFormName = FormNameGenerator?.GenerateFormName();
        Model ??= new TModel();
        InitEditContext(Model);
    }

    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();
        // Пересоздаём EditContext только при реальной смене объекта Model
        if (Model is not null && _editContext?.Model != Model)
            InitEditContext(Model);
    }

    private void InitEditContext(TModel model)
    {
        if (_editContext is not null)
            _editContext.OnValidationStateChanged -= OnValidationStateChanged;

        _editContext = new EditContext(model);
        _editContext.OnValidationStateChanged += OnValidationStateChanged;
        _isValid = false;
    }

    // ── Submit ─────────────────────────────────────────────────────────────────

    /// <summary>Основной метод отправки формы (вызывается из Razor).</summary>
    protected async Task HandleSubmitAsync()
    {
        if (IsDisposed || _editContext is null || IsEffectivelyDisabled) return;

        IsSubmitting = true;
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
        catch (OperationCanceledException)
        {
            // Нормальная отмена при Dispose
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[{Id}] Form submit error", ComponentId);
        }
        finally
        {
            IsSubmitting = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    /// <summary>
    /// Вызывается после успешной валидации и отправки формы.
    /// Переопределите для своей логики (сохранение в БД, API-вызов и т.д.).
    /// </summary>
    protected virtual Task OnFormValidSubmitAsync() => Task.CompletedTask;

    /// <summary>Вызывается при неудачной валидации.</summary>
    protected virtual Task OnFormInvalidSubmitAsync() => Task.CompletedTask;

    /// <summary>Вызывается при изменении состояния отправки.</summary>
    protected virtual void OnSubmittingChanged(bool isSubmitting) { }

    // ── Validation ─────────────────────────────────────────────────────────────

    // FIX: синхронный обработчик с отложенным StateHasChanged (не async void)
    private void OnValidationStateChanged(object? sender, ValidationStateChangedEventArgs e)
    {
        if (IsDisposed) return;
        _isValid = !_editContext!.GetValidationMessages().Any();
        _ = InvokeAsync(StateHasChanged);
    }

    // ── Reset ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Сбросить форму к исходному состоянию.
    /// ✅ FIX CS0506: virtual — переопределяется в SgSmartFormBase.
    /// </summary>
    public virtual async Task ResetAsync()
    {
        if (IsDisposed) return;
        Model = new TModel();
        InitEditContext(Model);
        IsSubmitting = false;
        _isValid = false;
        _submitCount = 0;
        IsModelFromForm = false;
        await InvokeAsync(StateHasChanged);
    }

    /// <summary>
    /// Асинхронный сброс модели с пользовательским колбэком для
    /// дополнительной логики (очистка полей, уведомления и т.д.).
    /// </summary>
    /// <param name="onReset">
    /// Функция преобразования новой модели перед применением.
    /// Получает свежий <c>new TModel()</c>, возвращает итоговую модель.
    /// </param>
    public virtual async Task ResetModelAsync(Func<TModel, TModel>? onReset = null)
    {
        if (IsDisposed) return;
        var newModel = onReset?.Invoke(new TModel()) ?? new TModel();
        Model = newModel;
        InitEditContext(newModel);
        IsSubmitting = false;
        _isValid = false;
        _submitCount = 0;
        IsModelFromForm = false;
        await ModelChanged.InvokeAsync(newModel);
        await InvokeAsync(StateHasChanged);
    }

    // ── Dispose ────────────────────────────────────────────────────────────────
    protected override async ValueTask DisposeComponentAsync()
    {
        if (_editContext is not null)
            _editContext.OnValidationStateChanged -= OnValidationStateChanged;
        await base.DisposeComponentAsync();
    }
}

/// <summary>Генератор имён форм для Static SSR Antiforgery.</summary>
public interface IFormNameGenerator
{
    string GenerateFormName();
}

internal sealed class DefaultFormNameGenerator : IFormNameGenerator
{
    private long _counter;
    public string GenerateFormName()
        => $"sg-form-{Interlocked.Increment(ref _counter):x}";
}
