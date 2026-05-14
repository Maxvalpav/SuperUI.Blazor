using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using SuperUI.Base;

namespace SuperUI.Components;

public partial class SgTimePicker : SgFormFieldBase<TimeSpan?>
{
    private bool _open;
    private CancellationTokenSource? _blurCts;

    [Inject] protected ISuperUILocalizer Localizer { get; set; } = default!;

    [Parameter] public bool ShowSeconds { get; set; }
    [Parameter] public SgLabelPosition LabelPosition { get; set; } = SgLabelPosition.Top;
    [Parameter] public bool AllowClear { get; set; } = true;
    [Parameter] public bool Block { get; set; } = true;

    protected override string ComponentPrefix => "time";

    private string GetFieldClasses() => Css("sgc-field")
        .AddIf("sgc-block", Block)
        .AddEnum(LabelPosition, "sgc-label-")
        .AddIf("sgc-invalid", HasError)
        .Add(Class)
        .ToString();

    private string GetControlClasses() => Css("sgc-combo sgc-combo-time")
        .AddIf("sgc-block", Block)
        .AddIf("sgc-open", _open)
        .AddIf("sgc-disabled", IsEffectivelyDisabled)
        .AddIf("sgc-invalid", HasError)
        .ToString();

    private string ClearLabel => Localizer["Clear"];

    private async Task ToggleAsync()
    {
        if (IsEffectivelyDisabled || ReadOnly) return;
        _blurCts?.Cancel();
        _open = !_open;
        await InvokeAsync(StateHasChanged);
    }

    private async Task HandleFocusOutAsync(FocusEventArgs e)
    {
        _blurCts?.Cancel();
        _blurCts = new CancellationTokenSource();
        var token = _blurCts.Token;
        try
        {
            await Task.Delay(200, token);
            _open = false;
            await InvokeAsync(StateHasChanged);
        }
        catch (TaskCanceledException) { }
    }

    private async Task SetHour(int h)
    {
        var current = Value ?? TimeSpan.Zero;
        var next = new TimeSpan(h, current.Minutes, current.Seconds);
        await SetValueAsync(next);
    }

    private async Task SetMinute(int m)
    {
        var current = Value ?? TimeSpan.Zero;
        var next = new TimeSpan(current.Hours, m, current.Seconds);
        await SetValueAsync(next);
        if (!ShowSeconds) _open = false;
    }

    private async Task SetSecond(int s)
    {
        var current = Value ?? TimeSpan.Zero;
        var next = new TimeSpan(current.Hours, current.Minutes, s);
        await SetValueAsync(next);
        _open = false;
    }

    private async Task ClearAsync()
    {
        _open = false;
        await SetValueAsync(null);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _blurCts?.Cancel();
            _blurCts?.Dispose();
        }
        base.Dispose(disposing);
    }
}
