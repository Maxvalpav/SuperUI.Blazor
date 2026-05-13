// SuperUI/Base/SgDebounceValidationBase.cs
// ✅ UX-3 NEW: валидация с задержкой — не валидируем при каждом нажатии клавиши
// ✅ Интегрируется с EditContext через SgFormFieldBase
// ✅ Поддерживает разные стратегии: OnChange (debounced), OnBlur, OnSubmit

using Microsoft.AspNetCore.Components;

namespace SuperUI.Base;

/// <summary>
/// Расширение SgFormFieldBase с поддержкой debounced валидации.
/// Валидация запускается через N миллисекунд после прекращения ввода.
/// </summary>
public abstract class SgDebounceValidationField<TValue> : SgFormFieldBase<TValue>
{
    /// <summary>Задержка валидации при вводе (мс). 0 = немедленно.</summary>
    [Parameter] public int ValidationDebounceMs { get; set; } = 400;

    /// <summary>Показывать индикатор валидации во время debounce.</summary>
    [Parameter] public bool ShowValidatingIndicator { get; set; } = true;

    /// <summary>Идёт ли процесс debounced валидации.</summary>
    protected bool IsValidating { get; private set; }

    private CancellationTokenSource? _validationCts;

    protected override async Task SetTextAsync(string? text)
    {
        await base.SetTextAsync(text);

        if (ValidationMode == SgFormValidationMode.OnChange && ValidationDebounceMs > 0)
            await ScheduleValidationAsync();
    }

    private async Task ScheduleValidationAsync()
    {
        // Отменяем предыдущую отложенную валидацию
        _validationCts?.Cancel();
        _validationCts?.Dispose();
        _validationCts = CancellationTokenSource.CreateLinkedTokenSource(ComponentToken);
        var ct = _validationCts.Token;

        if (ShowValidatingIndicator)
        {
            IsValidating = true;
            await InvokeAsync(StateHasChanged);
        }

        try
        {
            await Task.Delay(ValidationDebounceMs, ct);

            if (!ct.IsCancellationRequested && !IsDisposed)
            {
                ValidateNow();
                IsValidating = false;
                await InvokeAsync(StateHasChanged);
            }
        }
        catch (OperationCanceledException)
        {
            IsValidating = false;
        }
    }

    protected override async ValueTask DisposeComponentAsync()
    {
        _validationCts?.Cancel();
        _validationCts?.Dispose();
        await base.DisposeComponentAsync();
    }
}
