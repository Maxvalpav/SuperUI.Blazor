using System.Linq.Expressions;
using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using SuperUI.Base;
using SuperUI.Base.Localization;

namespace SuperUI.Components;

public partial class SgDatePicker : SgFormFieldBase<DateTime?>, IDisposable
{
    private bool _open;
    private DateTime _view = DateTime.Today;
    private bool _showQuickPick;
    private CancellationTokenSource? _blurCts;

    private List<DateTime> _grid = new();

    [Inject] protected ISuperUILocalizer Localizer { get; set; } = default!;

    [Parameter] public string Format { get; set; } = "d";
    [Parameter] public SgLabelPosition LabelPosition { get; set; } = SgLabelPosition.Top;
    [Parameter] public bool AllowClear { get; set; } = true;
    [Parameter] public bool Block { get; set; } = true;
    [Parameter] public DateTime? Min { get; set; }
    [Parameter] public DateTime? Max { get; set; }
    [Parameter] public Func<DateTime, bool>? DisabledDateFunc { get; set; }
    [Parameter] public RenderFragment<DateTime>? DayTemplate { get; set; }
    [Parameter] public bool ShowWeekNumbers { get; set; } = false;
    [Parameter] public DayOfWeek[]? HighlightDays { get; set; }

    protected override string ComponentPrefix => "date";

    private CultureInfo Culture => Localizer.CurrentCulture;

    private string GetFieldClasses() => Css("sgc-field")
        .AddIf("sgc-block", Block)
        .AddEnum(LabelPosition, "sgc-label-")
        .AddIf("sgc-invalid", HasError)
        .Add(Class)
        .ToString();

    private string GetControlClasses() => Css("sgc-date")
        .AddIf("sgc-open", _open)
        .AddIf("sgc-disabled", IsEffectivelyDisabled)
        .AddIf("sgc-invalid", HasError)
        .ToString();

    private string ClearLabel => Localizer["Clear"];

    protected override void OnInitialized()
    {
        base.OnInitialized();
        Localizer.CultureChanged += OnCultureChanged;
        UpdateGrid();
    }

    private void OnCultureChanged(CultureInfo culture)
    {
        UpdateGrid();
        InvokeAsync(StateHasChanged);
    }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (Value.HasValue && !_open) 
        {
            _view = new DateTime(Value.Value.Year, Value.Value.Month, 1);
            UpdateGrid();
        }
    }

    private void UpdateGrid()
    {
        _grid.Clear();
        var culture = Localizer.CurrentCulture;
        var first = new DateTime(_view.Year, _view.Month, 1);
        var fdow = (int)culture.DateTimeFormat.FirstDayOfWeek;
        var startOffset = ((int)first.DayOfWeek - fdow + 7) % 7;
        var start = first.AddDays(-startOffset);
        for (var i = 0; i < 42; i++)
        {
            _grid.Add(start.AddDays(i));
        }
    }

    private async Task HandleFocusOutAsync(FocusEventArgs e)
    {
        _blurCts?.Cancel();
        _blurCts = new CancellationTokenSource();
        var token = _blurCts.Token;
        try
        {
            await Task.Delay(200, token);
            if (_open) 
            { 
                _open = false; 
                _showQuickPick = false;
                await InvokeAsync(StateHasChanged); 
            }
        }
        catch (TaskCanceledException) { }
    }

    private Task ToggleAsync()
    {
        if (IsEffectivelyDisabled) return Task.CompletedTask;
        _blurCts?.Cancel();
        _open = !_open;
        if (_open) 
        {
            _showQuickPick = false;
            if (Value.HasValue) _view = new DateTime(Value.Value.Year, Value.Value.Month, 1);
            else _view = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            UpdateGrid();
        }
        return InvokeAsync(StateHasChanged);
    }

    private void Shift(int months) 
    {
        _view = _view.AddMonths(months);
        UpdateGrid();
    }

    private void ShiftYear(int years) 
    {
        _view = _view.AddYears(years);
        UpdateGrid();
    }

    private async Task ToggleQuickPick()
    {
        _showQuickPick = !_showQuickPick;
        if (_showQuickPick)
        {
            var targetDate = Value ?? DateTime.Today;
            _view = new DateTime(targetDate.Year, targetDate.Month, 1);
            UpdateGrid();
            await InvokeAsync(StateHasChanged);
            // Пытаемся прокрутить к выбранному году через JS
            await JS.InvokeVoidAsync("sgui.scrollToSelected", "sg-datepicker-years");
        }
    }

    private void SelectMonth(int month)
    {
        _view = new DateTime(_view.Year, month, 1);
        UpdateGrid();
    }

    private void SelectYear(int year)
    {
        _view = new DateTime(year, _view.Month, 1);
        _showQuickPick = false;
        UpdateGrid();
    }

    private bool IsDateDisabled(DateTime d) =>
        (Min.HasValue && d.Date < Min.Value.Date) ||
        (Max.HasValue && d.Date > Max.Value.Date) ||
        (DisabledDateFunc?.Invoke(d) ?? false);

    private bool IsHighlightDay(DateTime d) => 
        HighlightDays?.Contains(d.DayOfWeek) ?? false;

    private int GetWeekNumber(DateTime d)
    {
        return Culture.Calendar.GetWeekOfYear(d, 
            Culture.DateTimeFormat.CalendarWeekRule, 
            Culture.DateTimeFormat.FirstDayOfWeek);
    }

    private async Task PickAsync(DateTime d)
    {
        if (IsDateDisabled(d)) return;
        _open = false;
        await SetValueAsync(d.Date);
    }

    private Task TodayAsync() => PickAsync(DateTime.Today);

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
            if (Localizer != null)
            {
                Localizer.CultureChanged -= OnCultureChanged;
            }
        }
        base.Dispose(disposing);
    }
}
