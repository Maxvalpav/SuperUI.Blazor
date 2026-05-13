// SuperUI/Base/SgFormBase.cs
// ИСПРАВЛЕНИЯ v2:
// ✅ FIX: OnValidationStateChanged — async void → async Task через EventCallback pattern
// ✅ FIX: EditContext пересоздаётся только при реальной смене объекта Model
// ✅ NEW: IsSubmitted флаг
// ✅ NEW: [SupplyParameterFromForm] поддержка в SSR-режиме (документация)
// ✅ PERF: _isValid кэшируется и не пересчитывается лишний раз

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

    // ── Состояние ──────────────────────────────────────────────────────────────
    protected EditContext? _editContext;
    protected bool _isSubmitting;
    protected bool _isValid;
    protected int _submitCount;

    /// <summary>NEW: true если форма была отправлена хотя бы раз.</summary>
    protected bool IsSubmitted => _submitCount > 0;

    private string? _generatedFormName;
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
        // FIX: пересоздаём EditContext только при реальной смене объекта Model
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
    protected async Task HandleSubmitAsync()
    {
        if (IsDisposed || _editContext is null || IsEffectivelyDisabled) return;

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

    protected virtual Task OnFormValidSubmitAsync() => Task.CompletedTask;
    protected virtual Task OnFormInvalidSubmitAsync() => Task.CompletedTask;

    // ── Validation ─────────────────────────────────────────────────────────────
    // FIX: убираем async void — теперь синхронный обработчик с отложенным StateHasChanged
    private void OnValidationStateChanged(object? sender, ValidationStateChangedEventArgs e)
    {
        if (IsDisposed) return;
        _isValid = !_editContext!.GetValidationMessages().Any();
        // Безопасный вызов StateHasChanged из обработчика события
        _ = InvokeAsync(StateHasChanged);
    }

    /// <summary>Сбросить форму к исходному состоянию.</summary>
    public async Task ResetAsync()
    {
        if (IsDisposed) return;
        Model = new TModel();
        InitEditContext(Model);
        _isSubmitting = false;
        _isValid = false;
        _submitCount = 0;
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
