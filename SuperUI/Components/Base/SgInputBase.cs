// Файл: Components/Base/SgInputBase.cs
// Зависимости: SgFormBase<T> (уровень 3A)

using Microsoft.AspNetCore.Components;
using SuperUI.Converters;

namespace SuperUI.Components.Base;

/// <summary>
/// УРОВЕНЬ 4: Базовый класс для текстовых инпутов.
/// Добавляет: text sync, adornments, clearable, character counter.
/// </summary>
public abstract class SgInputBase<TValue> : SgFormBase<TValue>
{
    // ── Параметры ─────────────────────────────────────────────────────────────

    [Parameter] public bool Clearable { get; set; }
    [Parameter] public int MaxLength { get; set; } = 0;
    [Parameter] public RenderFragment? StartAdornment { get; set; }
    [Parameter] public RenderFragment? EndAdornment { get; set; }
    [Parameter] public bool ShowCharacterCount { get; set; }
    [Parameter] public InputType InputType { get; set; } = InputType.Text;

    // ── Состояние ─────────────────────────────────────────────────────────────

    protected string? InternalText { get; private set; }
    protected bool ShowClearButton => Clearable && !string.IsNullOrEmpty(InternalText) && !Disabled && !Readonly;
    protected int CharacterCount => InternalText?.Length ?? 0;

    // ── Input handling ────────────────────────────────────────────────────────

    protected async Task HandleInputAsync(ChangeEventArgs e)
    {
        var text = e.Value?.ToString();
        InternalText = text;

        await HandleWithDebounceAsync(async ct =>
        {
            ValueAsString = text;
            await RequestStateUpdateAsync();
        });
    }

    protected async Task HandleClearAsync()
    {
        InternalText = string.Empty;
        await SetValueAsync(default);
        await RequestStateUpdateAsync();
    }

    protected override void OnComponentParametersSet()
    {
        base.OnComponentParametersSet();
        // Синхронизируем InternalText с Value при изменении параметра снаружи
        InternalText = EffectiveConverter.Convert(_valueState.Value);
    }

    protected override string GetComponentPrefix() => "input";
}

public enum InputType { Text, Password, Email, Tel, Url, Number, Search }
